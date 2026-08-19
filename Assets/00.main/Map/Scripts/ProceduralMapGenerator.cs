using System;
using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Railgame.Map
{
    public sealed class ProceduralMapGenerator : MonoBehaviour
    {
        public const int TotalWidth = 24;
        public const int PlayableMinX = 2;
        public const int PlayableMaxX = 21;
        public const int MapLength = 128;
        public const int LegLength = 32;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
        };

        [SerializeField] private MapGenerationProfile profile;
        [SerializeField] private RuntimeNavigationController navigation;
        [SerializeField] private int worldSeed = 20260818;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool buildNavMeshAfterGenerate = true;

        private readonly Cell[,] cells = new Cell[TotalWidth, MapLength];
        private readonly Dictionary<Vector2Int, DirtBlock> dirtBlocks = new();
        private Transform generatedRoot;

        public string LastLayoutHash { get; private set; } = string.Empty;
        public int GeneratedWaterCount { get; private set; }
        public int GeneratedDirtCount { get; private set; }
        public int GeneratedTreeCount { get; private set; }
        public int GeneratedIronCount { get; private set; }
        public int GeneratedHillResourceCount { get; private set; }
        public int GeneratedResourceClusterCount { get; private set; }
        public int GeneratedRiverCount { get; private set; }
        public int GeneratedMountainCount { get; private set; }
        public int GeneratedJumpLinkCount { get; private set; }
        public int WorldSeed => worldSeed;
        public MapGenerationProfile Profile => profile;

        private void Start()
        {
            if (generateOnStart)
                GenerateNow();
        }

        [ContextMenu("Generate Map")]
        public void GenerateNow()
        {
            RequireProfile();
            ClearGenerated();
            GenerateLogicalLayoutForValidation(worldSeed);
            BuildHierarchy(new Random(worldSeed ^ 0x51F15EED));

            if (buildNavMeshAfterGenerate && navigation != null)
                navigation.BuildInitialNavMesh();
        }

        public string GenerateLogicalLayoutForValidation(int seed)
        {
            RequireProfile();
            ResetState();
            Random random = new(seed);
            GenerateRoute(random);
            GenerateWater(random);
            GenerateDirt(random);
            GenerateResources(random);
            ValidateLayout();
            LastLayoutHash = CalculateLayoutHash();
            return LastLayoutHash;
        }

        public bool TryMineDirt(Vector2Int cell)
        {
            if (!IsPlayable(cell) || !cells[cell.x, cell.y].Dirt || cells[cell.x, cell.y].Resource)
                return false;
            if (!dirtBlocks.TryGetValue(cell, out DirtBlock block) || block == null)
                return false;

            block.DisableForMining();
            cells[cell.x, cell.y].Dirt = false;
            dirtBlocks.Remove(cell);
            navigation?.RequestUpdate(transform.TransformPoint(new Vector3(cell.x + 0.5f, 1f, cell.y + 0.5f)));
            DestroyMapObject(block.gameObject);
            return true;
        }

        public void NotifyResourceRemoved(Vector2Int anchor, Vector2Int footprint)
        {
            for (int x = 0; x < footprint.x; x++)
            for (int z = 0; z < footprint.y; z++)
            {
                Vector2Int cell = anchor + new Vector2Int(x, z);
                if (IsPlayable(cell))
                    cells[cell.x, cell.y].Resource = false;
            }
        }

        public bool HasCompleteMovementPath()
        {
            return HasPath(new Vector2Int(11, 2), new Vector2Int(11, 125), false);
        }

        public bool HasRailPathAfterMining()
        {
            return HasPath(new Vector2Int(11, 2), new Vector2Int(11, 125), true);
        }

        private void RequireProfile()
        {
            if (profile == null)
                throw new MissingReferenceException("ProceduralMapGenerator requires MapGenerationProfile.");
            if (profile.GroundChunkPrefab == null || profile.GroundCellPrefab == null || profile.DirtPrefab == null ||
                profile.WaterPrefab == null || profile.BoundaryPrefab == null)
                throw new MissingReferenceException("MapGenerationProfile core map prefabs are incomplete.");
            if (profile.RiverBendMax < profile.RiverBendMin)
                throw new InvalidOperationException("River bend range is invalid.");
        }

        private void ResetState()
        {
            Array.Clear(cells, 0, cells.Length);
            dirtBlocks.Clear();
            GeneratedWaterCount = 0;
            GeneratedDirtCount = 0;
            GeneratedTreeCount = 0;
            GeneratedIronCount = 0;
            GeneratedHillResourceCount = 0;
            GeneratedResourceClusterCount = 0;
            GeneratedRiverCount = 0;
            GeneratedMountainCount = 0;
            GeneratedJumpLinkCount = 0;
        }

        private void GenerateRoute(Random random)
        {
            int center = 11;
            for (int z = 0; z < MapLength; z++)
            {
                if (z > 8 && z < MapLength - 16 && z % 8 == 0)
                    center = Mathf.Clamp(center + random.Next(-1, 2), 5, 18);
                if (z >= MapLength - 16)
                    center += Math.Sign(11 - center);
                for (int x = center - 1; x <= center + 1; x++)
                    cells[x, z].Route = true;
            }
        }

        private void GenerateWater(Random random)
        {
            GenerateRiver(random, random.Next(20, 37), 1);
            GenerateRiver(random, random.Next(76, 101), 2);

            for (int leg = 0; leg < MapLength / LegLength; leg++)
            {
                int missing = profile.WaterCellCount - CountWaterInLeg(leg);
                int attempts = 0;
                while (missing > 0 && attempts++ < 200)
                {
                    int startX = random.Next(0, 2) == 0 ? random.Next(3, 7) : random.Next(16, 20);
                    int startZ = random.Next(leg * LegLength + 6, (leg + 1) * LegLength - 8);
                    Vector2Int anchor = new(startX, startZ);
                    if (!CanPlaceWaterPatch(anchor, leg))
                        continue;

                    for (int x = startX; x < startX + 2; x++)
                    for (int z = startZ; z < startZ + 3; z++)
                        MarkWater(x, z, 0);
                    missing -= 6;
                }

                if (missing > 0)
                    throw new InvalidOperationException($"Could not guarantee water in leg {leg}.");
            }
        }

        private void GenerateRiver(Random random, int centerZ, byte riverId)
        {
            int z = centerZ;
            int nextBend = random.Next(profile.RiverBendMin, profile.RiverBendMax + 1);
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            {
                if (--nextBend == 0)
                {
                    z = Mathf.Clamp(z + random.Next(-1, 2), 8, MapLength - 10);
                    nextBend = random.Next(profile.RiverBendMin, profile.RiverBendMax + 1);
                }
                for (int width = 0; width < profile.RiverWidth; width++)
                    MarkRiverCell(x, z + width, riverId);
            }
            GeneratedRiverCount++;
        }

        private void MarkRiverCell(int x, int z, byte riverId)
        {
            int extraFordWidth = (profile.FordWidth - 3) / 2;
            bool crossingCell = false;
            for (int offset = -extraFordWidth; offset <= extraFordWidth && !crossingCell; offset++)
                crossingCell = x + offset >= PlayableMinX && x + offset <= PlayableMaxX && cells[x + offset, z].Route;
            if (crossingCell)
            {
                Cell crossing = cells[x, z];
                crossing.Crossing = true;
                crossing.RiverId = riverId;
                cells[x, z] = crossing;
                return;
            }
            MarkWater(x, z, riverId);
        }

        private void MarkWater(int x, int z, byte riverId)
        {
            Cell state = cells[x, z];
            if (!state.Water)
                GeneratedWaterCount++;
            state.Water = true;
            state.RiverId = riverId;
            cells[x, z] = state;
        }

        private int CountWaterInLeg(int leg)
        {
            int count = 0;
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            for (int z = leg * LegLength; z < (leg + 1) * LegLength; z++)
                if (cells[x, z].Water)
                    count++;
            return count;
        }

        private bool CanPlaceWaterPatch(Vector2Int anchor, int leg)
        {
            int legStart = leg * LegLength;
            int legEnd = (leg + 1) * LegLength;
            if (anchor.x < PlayableMinX || anchor.x + 1 > PlayableMaxX || anchor.y < legStart || anchor.y + 2 >= legEnd)
                return false;

            for (int x = anchor.x - 1; x <= anchor.x + 2; x++)
            for (int z = anchor.y - 1; z <= anchor.y + 3; z++)
                if (x >= PlayableMinX && x <= PlayableMaxX && z >= legStart && z < legEnd && cells[x, z].Water)
                    return false;
            return true;
        }

        private void GenerateDirt(Random random)
        {
            for (int leg = 0; leg < MapLength / LegLength; leg++)
            {
                int target = profile.DirtBaseCount + leg * profile.DirtIncreasePerLeg;
                int placed = 0;
                for (int plateau = 0; plateau < 2; plateau++)
                {
                    if (TryPlaceDirtPlateau(random, leg, out int added))
                        placed += added;
                }

                int attempts = 0;
                while (placed < target && attempts++ < 500)
                {
                    List<Vector2Int> frontier = new();
                    for (int x = PlayableMinX; x <= PlayableMaxX; x++)
                    for (int z = leg * LegLength + 3; z < (leg + 1) * LegLength - 3; z++)
                    {
                        Vector2Int candidate = new(x, z);
                        if (!CanPlaceDirt(candidate) || !HasAdjacentDirt(candidate))
                            continue;
                        frontier.Add(candidate);
                    }
                    if (frontier.Count == 0)
                        continue;
                    Vector2Int cell = frontier[random.Next(frontier.Count)];
                    cells[cell.x, cell.y].Dirt = true;
                    placed++;
                }

                if (placed < target)
                    throw new InvalidOperationException($"Could not place dirt in leg {leg}.");
                GeneratedDirtCount += placed;
            }
        }

        private bool HasAdjacentDirt(Vector2Int cell)
        {
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbor = cell + direction;
                if (IsPlayable(neighbor) && cells[neighbor.x, neighbor.y].Dirt)
                    return true;
            }
            return false;
        }

        private bool TryPlaceDirtPlateau(Random random, int leg, out int added)
        {
            added = 0;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                Vector2Int anchor = new(random.Next(3, 20), random.Next(leg * LegLength + 5, (leg + 1) * LegLength - 6));
                bool valid = true;
                for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                    valid &= CanPlaceDirt(anchor + new Vector2Int(x, z));
                if (!valid)
                    continue;

                for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                {
                    cells[anchor.x + x, anchor.y + z].Dirt = true;
                    added++;
                }
                return true;
            }
            return false;
        }

        private void GenerateResources(Random random)
        {
            for (int leg = 0; leg < MapLength / LegLength; leg++)
            {
                PlaceResources(random, leg, profile.TreeCount, true);
                PlaceResources(random, leg, profile.IronCount, false);
            }
        }

        private void PlaceResources(Random random, int leg, int target, bool tree)
        {
            Vector2Int footprint = tree ? new Vector2Int(2, 2) : Vector2Int.one;
            int placed = 0;
            int clusterCount = 3;
            for (int cluster = 0; cluster < clusterCount; cluster++)
            {
                int clusterTarget = target / clusterCount + (cluster < target % clusterCount ? 1 : 0);
                Vector2Int center = new(ResourceClusterX(random), leg * LegLength + 7 + cluster * 9);
                int clusterPlaced = 0;
                while (clusterPlaced < clusterTarget)
                {
                    bool hillOnly = clusterPlaced == 0 && random.NextDouble() < profile.HillResourceChance;
                    if (!TryFindNearestResource(random, leg, center, footprint, hillOnly, out Vector2Int anchor, out int height) &&
                        !TryFindNearestResource(random, leg, center, footprint, false, out anchor, out height))
                        throw new InvalidOperationException($"Could not place {(tree ? "trees" : "iron")} cluster {cluster} in leg {leg}.");

                    for (int x = 0; x < footprint.x; x++)
                    for (int z = 0; z < footprint.y; z++)
                        cells[anchor.x + x, anchor.y + z].Resource = true;

                    Cell state = cells[anchor.x, anchor.y];
                    state.ResourceAnchor = true;
                    state.Tree = tree;
                    state.ResourceHeight = height;
                    state.ResourceFootprint = footprint;
                    cells[anchor.x, anchor.y] = state;

                    if (height == 1)
                        GeneratedHillResourceCount++;
                    if (tree)
                        GeneratedTreeCount++;
                    else
                        GeneratedIronCount++;
                    placed++;
                    clusterPlaced++;
                }
                GeneratedResourceClusterCount++;
            }
            if (placed != target)
                throw new InvalidOperationException($"Wrong {(tree ? "tree" : "iron")} count in leg {leg}.");
        }

        private int ResourceClusterX(Random random)
        {
            if (random.NextDouble() >= profile.ResourceSideBias)
                return random.Next(PlayableMinX + 1, PlayableMaxX);
            return random.Next(0, 2) == 0 ? random.Next(PlayableMinX + 1, 8) : random.Next(16, PlayableMaxX);
        }

        private bool TryFindNearestResource(Random random, int leg, Vector2Int center, Vector2Int footprint, bool hillOnly,
            out Vector2Int anchor, out int height)
        {
            List<Vector2Int> best = new();
            int bestDistance = int.MaxValue;
            int startZ = leg * LegLength + 4;
            int endZ = (leg + 1) * LegLength - 4 - footprint.y;
            for (int x = PlayableMinX; x <= PlayableMaxX - footprint.x + 1; x++)
            for (int z = startZ; z <= endZ; z++)
            {
                Vector2Int candidate = new(x, z);
                if (!CanPlaceResource(candidate, footprint, hillOnly, out _))
                    continue;
                int distance = Mathf.Abs(x - center.x) + Mathf.Abs(z - center.y);
                if (distance > bestDistance)
                    continue;
                if (distance < bestDistance)
                {
                    best.Clear();
                    bestDistance = distance;
                }
                best.Add(candidate);
            }
            if (best.Count == 0)
            {
                anchor = default;
                height = 0;
                return false;
            }
            anchor = best[random.Next(best.Count)];
            return CanPlaceResource(anchor, footprint, hillOnly, out height);
        }

        private bool CanPlaceResource(Vector2Int anchor, Vector2Int footprint, bool hillOnly, out int height)
        {
            height = -1;
            if (anchor.x < PlayableMinX || anchor.x + footprint.x - 1 > PlayableMaxX || anchor.y < 4 || anchor.y + footprint.y >= MapLength - 4)
                return false;
            for (int x = 0; x < footprint.x; x++)
            for (int z = 0; z < footprint.y; z++)
            {
                Cell state = cells[anchor.x + x, anchor.y + z];
                int cellHeight = state.Dirt ? 1 : 0;
                if (state.Water || state.Resource || state.Route || (height >= 0 && cellHeight != height))
                    return false;
                height = cellHeight;
            }
            return !hillOnly || height == 1;
        }

        private bool CanPlaceDirt(Vector2Int cell)
        {
            if (!IsPlayable(cell) || cell.y < 4 || cell.y >= MapLength - 4 ||
                cells[cell.x, cell.y].Water || cells[cell.x, cell.y].Dirt || cells[cell.x, cell.y].Route)
                return false;
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbor = cell + direction;
                if (IsPlayable(neighbor) && cells[neighbor.x, neighbor.y].Water)
                    return false;
            }
            return true;
        }

        private void ValidateLayout()
        {
            int legs = MapLength / LegLength;
            if (GeneratedWaterCount < profile.WaterCellCount * legs || GeneratedTreeCount < profile.TreeCount * legs ||
                GeneratedIronCount < profile.IronCount * legs)
                throw new InvalidOperationException("Generated map resource guarantee failed.");
            if (GeneratedRiverCount != 2 || !RiverCrossesMap(1) || !RiverCrossesMap(2))
                throw new InvalidOperationException("Two complete transverse rivers were not generated.");
            if (GeneratedResourceClusterCount != legs * 6)
                throw new InvalidOperationException("Resource cluster guarantee failed.");
            for (int leg = 0; leg < legs; leg++)
                if (CountWaterInLeg(leg) < profile.WaterCellCount)
                    throw new InvalidOperationException($"Water guarantee failed in leg {leg}.");
            if (!HasCompleteMovementPath())
                throw new InvalidOperationException("Generated map has no complete player/enemy movement path.");
            if (!HasRailPathAfterMining())
                throw new InvalidOperationException("Generated map has no flat rail path after mining.");
        }

        private bool RiverCrossesMap(byte riverId)
        {
            bool west = false;
            bool east = false;
            for (int z = 0; z < MapLength; z++)
            {
                west |= cells[PlayableMinX, z].RiverId == riverId;
                east |= cells[PlayableMaxX, z].RiverId == riverId;
            }
            return west && east;
        }

        private bool HasPath(Vector2Int start, Vector2Int goal, bool railAfterMining)
        {
            bool[,] visited = new bool[TotalWidth, MapLength];
            Queue<Vector2Int> open = new();
            open.Enqueue(start);
            visited[start.x, start.y] = true;

            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();
                if (current == goal)
                    return true;

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (!IsPlayable(next) || visited[next.x, next.y] || cells[next.x, next.y].Resource)
                        continue;
                    if (!railAfterMining && Mathf.Abs(GetHeight(current) - GetHeight(next)) > 1)
                        continue;

                    visited[next.x, next.y] = true;
                    open.Enqueue(next);
                }
            }
            return false;
        }

        private void BuildHierarchy(Random random)
        {
            generatedRoot = new GameObject("GeneratedMap").transform;
            generatedRoot.SetParent(transform, false);

            Transform safetyFloorRoot = CreateGroup("SafetyFloor_NoSpawns");
            Transform groundRoot = CreateGroup("Ground");
            Transform waterRoot = CreateGroup("Water");
            Transform dirtRoot = CreateGroup("DirtHills_1m");
            Transform resourceRoot = CreateGroup("TeamResourceSlots");
            Transform linkRoot = CreateGroup("TraversalLinks");
            Transform boundaryRoot = CreateGroup("TransparentBoundaries");
            Transform mountainRoot = CreateGroup("BackgroundMountains_2to6m");

            BuildSafetyFloor(safetyFloorRoot);
            BuildGround(groundRoot);
            BuildWater(waterRoot, linkRoot);
            BuildDirt(dirtRoot, linkRoot);
            BuildResources(resourceRoot);
            BuildBoundaries(boundaryRoot);
            BuildMountains(random, mountainRoot);
        }

        private void BuildSafetyFloor(Transform parent)
        {
            Material bottomMaterial = profile.DirtMaterial != null
                ? profile.DirtMaterial
                : profile.DirtPrefab.GetComponentInChildren<Renderer>(true)?.sharedMaterial;
            for (int chunkX = 0; chunkX < TotalWidth / 8; chunkX++)
            for (int chunkZ = 0; chunkZ < MapLength / 8; chunkZ++)
            {
                GameObject chunk = PlacePrefab(profile.GroundChunkPrefab, parent,
                    new Vector3(chunkX * 8, -1f, chunkZ * 8), $"SafetyFloor_{chunkX}_{chunkZ}");
                if (bottomMaterial == null)
                    continue;
                foreach (Renderer renderer in chunk.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = bottomMaterial;
            }
        }

        private void BuildGround(Transform parent)
        {
            HashSet<Vector2Int> waterChunks = new();
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            for (int z = 0; z < MapLength; z++)
                if (cells[x, z].Water)
                    waterChunks.Add(new Vector2Int(x / 8, z / 8));

            for (int chunkX = 0; chunkX < TotalWidth / 8; chunkX++)
            for (int chunkZ = 0; chunkZ < MapLength / 8; chunkZ++)
            {
                Vector2Int chunk = new(chunkX, chunkZ);
                if (!waterChunks.Contains(chunk))
                {
                    GameObject ground = PlacePrefab(profile.GroundChunkPrefab, parent, new Vector3(chunkX * 8, 0f, chunkZ * 8), $"GroundChunk_{chunkX}_{chunkZ}");
                    ApplyMaterial(ground, profile.GroundMaterial);
                    continue;
                }

                for (int localX = 0; localX < 8; localX++)
                for (int localZ = 0; localZ < 8; localZ++)
                {
                    int x = chunkX * 8 + localX;
                    int z = chunkZ * 8 + localZ;
                    if (!cells[x, z].Water)
                    {
                        GameObject ground = PlacePrefab(profile.GroundCellPrefab, parent, new Vector3(x, 0f, z), $"GroundCell_{x}_{z}");
                        ApplyMaterial(ground, profile.GroundMaterial);
                    }
                }
            }
        }

        private void BuildWater(Transform parent, Transform links)
        {
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            for (int z = 0; z < MapLength; z++)
            {
                if (!cells[x, z].Water)
                    continue;
                Vector2Int cell = new(x, z);
                GameObject water = PlacePrefab(profile.WaterPrefab, parent, new Vector3(x, 0f, z), $"Water_{x}_{z}");
                Transform basinFloor = water.transform.Find("BasinFloor");
                if (basinFloor != null)
                    basinFloor.gameObject.SetActive(false);
                Transform visual = water.transform.Find("WaterVisual");
                if (visual != null)
                {
                    SetLayerRecursively(visual.gameObject, 8);
                    ApplyMaterial(visual.gameObject, profile.WaterMaterial);
                }

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighbor = cell + direction;
                    if (IsPlayable(neighbor) && !cells[neighbor.x, neighbor.y].Water)
                        CreateTraversalLink(links, cell, direction, 0f, $"WaterBank_{x}_{z}_{DirectionName(direction)}", null);
                }
            }
        }

        private void BuildDirt(Transform parent, Transform links)
        {
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            for (int z = 0; z < MapLength; z++)
            {
                if (!cells[x, z].Dirt)
                    continue;

                Vector2Int cell = new(x, z);
                GameObject instance = PlacePrefab(profile.DirtPrefab, parent, new Vector3(x, 1f, z), $"Dirt_{x}_{z}_H1");
                ApplyMaterial(instance, profile.DirtMaterial);
                DirtBlock block = instance.GetComponent<DirtBlock>() ?? instance.AddComponent<DirtBlock>();
                block.Initialize(this, cell);
                dirtBlocks[cell] = block;

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighbor = cell + direction;
                    if (!IsPlayable(neighbor) || cells[neighbor.x, neighbor.y].Dirt || cells[neighbor.x, neighbor.y].Resource)
                        continue;
                    CreateTraversalLink(links, cell, direction, 2f, $"DirtJump_{x}_{z}_{DirectionName(direction)}", block);
                }
            }
        }

        private void BuildResources(Transform parent)
        {
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            for (int z = 0; z < MapLength; z++)
            {
                Cell state = cells[x, z];
                if (!state.ResourceAnchor)
                    continue;

                string kind = state.Tree ? "Tree" : "Iron";
                GameObject slot = new($"{kind}Slot_{x}_{z}_H{state.ResourceHeight}");
                slot.transform.SetParent(parent, false);
                slot.transform.localPosition = new Vector3(x, 1f + state.ResourceHeight, z);
                SetLayerRecursively(slot, 7);

                GameObject visualPrefab = state.Tree ? profile.TreePrefab : profile.IronPrefab;
                if (visualPrefab != null)
                {
                    GameObject visual = Object.Instantiate(visualPrefab, slot.transform);
                    visual.name = "TeamPrefab";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    SetLayerRecursively(visual, 7);
                    if (state.Tree && profile.LeavesMaterial != null)
                        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                            if (renderer.name.Contains("Leaves", StringComparison.OrdinalIgnoreCase))
                                renderer.sharedMaterial = profile.LeavesMaterial;
                }

                NavMeshObstacle obstacle = slot.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = new Vector3(state.ResourceFootprint.x * 0.5f, state.Tree ? 1.5f : 0.5f, state.ResourceFootprint.y * 0.5f);
                obstacle.size = new Vector3(state.ResourceFootprint.x * 0.9f, state.Tree ? 3f : 1f, state.ResourceFootprint.y * 0.9f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;

                ResourceSpawnSlot component = slot.AddComponent<ResourceSpawnSlot>();
                component.Initialize(this, new Vector2Int(x, z), state.ResourceFootprint, state.ResourceHeight);
            }
        }

        private void BuildBoundaries(Transform parent)
        {
            for (int z = 0; z < MapLength; z += 8)
            {
                GameObject left = PlacePrefab(profile.BoundaryPrefab, parent, new Vector3(2f, 0f, z), $"Boundary_Left_{z}");
                GameObject right = PlacePrefab(profile.BoundaryPrefab, parent, new Vector3(22f, 0f, z), $"Boundary_Right_{z}");
                SetLayerRecursively(left, 6);
                SetLayerRecursively(right, 6);
            }
        }

        private void BuildMountains(Random random, Transform parent)
        {
            if (profile.BackgroundMountainPrefab == null)
                return;

            for (int z = 0; z < MapLength; z += 4)
            {
                int leftHeight = random.Next(2, 7);
                int rightHeight = random.Next(2, 7);
                BuildMountainColumn(parent, -2f, z, leftHeight, $"Mountain_Left_{z}_H{leftHeight}");
                BuildMountainColumn(parent, 22f, z, rightHeight, $"Mountain_Right_{z}_H{rightHeight}");
            }
        }

        private void BuildMountainColumn(Transform parent, float x, int z, int height, string name)
        {
            GameObject item = PlacePrefab(profile.BackgroundMountainPrefab, parent, new Vector3(x, 1f, z), name);
            ApplyMaterial(item, profile.DirtMaterial);
            item.transform.localScale = new Vector3(4f, height, 4f);
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetLayerRecursively(item, 8);
            GeneratedMountainCount++;
        }

        private void CreateTraversalLink(Transform parent, Vector2Int cell, Vector2Int direction, float cellSurfaceY, string name, DirtBlock dirtOwner)
        {
            GameObject item = new(name);
            item.transform.SetParent(parent, false);
            Vector3 cellPoint = new(cell.x + 0.5f, cellSurfaceY, cell.y + 0.5f);
            Vector3 groundPoint = new(cell.x + 0.5f + direction.x, 1f, cell.y + 0.5f + direction.y);

            NavMeshLink link = item.AddComponent<NavMeshLink>();
            link.enabled = false;
            link.agentTypeID = 0;
            link.startPoint = groundPoint;
            link.endPoint = cellPoint;
            link.width = 0.8f;
            link.bidirectional = true;
            link.area = NavMesh.GetAreaFromName("Jump");
            link.enabled = true;
            dirtOwner?.AddJumpLink(link);
            GeneratedJumpLinkCount++;
        }

        private string CalculateLayoutHash()
        {
            StringBuilder data = new(TotalWidth * MapLength * 2);
            for (int z = 0; z < MapLength; z++)
            for (int x = PlayableMinX; x <= PlayableMaxX; x++)
            {
                Cell cell = cells[x, z];
                data.Append(cell.Water ? (char)('0' + cell.RiverId) : cell.Tree ? 'T' : cell.ResourceAnchor ? 'I' : cell.Dirt ? 'D' : '.');
                data.Append(cell.Crossing ? 'C' : cell.Route ? 'R' : '_');
                if (cell.ResourceAnchor)
                    data.Append((char)('0' + cell.ResourceHeight)).Append(cell.ResourceFootprint.x).Append(cell.ResourceFootprint.y);
            }

            unchecked
            {
                uint hash = 2166136261;
                foreach (char value in data.ToString())
                {
                    hash ^= value;
                    hash *= 16777619;
                }
                return hash.ToString("X8");
            }
        }

        private Vector2Int RandomCell(Random random, int leg, int margin)
        {
            return new Vector2Int(random.Next(PlayableMinX, PlayableMaxX + 1), random.Next(leg * LegLength + margin, (leg + 1) * LegLength - margin));
        }

        private int GetHeight(Vector2Int cell)
        {
            return cells[cell.x, cell.y].Dirt ? 1 : 0;
        }

        private static bool IsPlayable(Vector2Int cell)
        {
            return cell.x >= PlayableMinX && cell.x <= PlayableMaxX && cell.y >= 0 && cell.y < MapLength;
        }

        private Transform CreateGroup(string name)
        {
            GameObject group = new(name);
            group.transform.SetParent(generatedRoot, false);
            return group.transform;
        }

        private static GameObject PlacePrefab(GameObject prefab, Transform parent, Vector3 localPosition, string name)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            if (material == null)
                return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }

        private void ClearGenerated()
        {
            Transform existing = transform.Find("GeneratedMap");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                DestroyMapObject(existing.gameObject);
            }
            generatedRoot = null;
        }

        private static void DestroyMapObject(GameObject item)
        {
            if (Application.isPlaying)
                Object.Destroy(item);
            else
                Object.DestroyImmediate(item);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static string DirectionName(Vector2Int direction)
        {
            if (direction == Vector2Int.left) return "W";
            if (direction == Vector2Int.right) return "E";
            if (direction == Vector2Int.down) return "S";
            return "N";
        }

        [Serializable]
        private struct Cell
        {
            public bool Water;
            public byte RiverId;
            public bool Dirt;
            public bool Route;
            public bool Crossing;
            public bool Resource;
            public bool ResourceAnchor;
            public bool Tree;
            public int ResourceHeight;
            public Vector2Int ResourceFootprint;
        }
    }
}
