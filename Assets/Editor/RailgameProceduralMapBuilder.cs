using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Railgame.Map;
using Railgame.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Railgame.Editor
{
    public static class RailgameProceduralMapBuilder
    {
        private const string MapFolder = "Assets/00.main/Map";
        private const string PrefabFolder = MapFolder + "/Prefabs";
        private const string ProfileFolder = MapFolder + "/Profiles";
        private const string SceneFolder = MapFolder + "/Scenes";
        private const string TextureFolder = MapFolder + "/Textures";
        private const string PlayerFolder = "Assets/00.main/Player";
        private const string PlayerPrefabFolder = PlayerFolder + "/Prefabs";
        private const string PlayerPrefabPath = PlayerPrefabFolder + "/PF_RailgamePlayer.prefab";
        private const string CharacterVisualPath = "Assets/00.main/Character/Prefabs/PF_RailgameCharacterVisual.prefab";
        private const string SpringProfilePath = ProfileFolder + "/MapGenerationProfile_Spring.asset";
        private const string SummerProfilePath = ProfileFolder + "/MapGenerationProfile_Summer.asset";
        private const string SpringScenePath = SceneFolder + "/Map_Procedural_Spring.unity";
        private const string SummerScenePath = SceneFolder + "/Map_Procedural_Summer.unity";

        [MenuItem("Railgame/Build Procedural Map")]
        public static void Build()
        {
            EnsureFolder(ProfileFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(PlayerPrefabFolder);
            CreateVoxelTextures();
            ConfigureCharacterTextures();
            GameObject groundCell = CreateGroundCellPrefab();
            GameObject boundary = CreateBoundaryPrefab();
            ConfigureWaterPrefab();
            CreatePlayerPrefab();
            Material springGrass = CreateSeasonMaterial("M_Spring_Grass", "M_Grass.mat", "T_Spring_Grass.asset", new Color32(0x93, 0xC9, 0x5A, 0xFF));
            Material springDirt = CreateSeasonMaterial("M_Spring_Dirt", "M_Dirt.mat", "T_Spring_Dirt.asset", new Color32(0xA6, 0x68, 0x3F, 0xFF));
            Material springLeaves = CreateSeasonMaterial("M_Spring_Leaves", "M_Leaves.mat", "T_Spring_Leaves.asset", new Color32(0x7A, 0xC3, 0x4A, 0xFF));
            Material springWater = CreateSeasonMaterial("M_Spring_Water", "M_Water.mat", "T_Spring_Water.asset", new Color32(0x45, 0xBC, 0xE1, 0x9E));
            Material summerGrass = CreateSeasonMaterial("M_Summer_Grass", "M_Grass.mat", "T_Summer_Grass.asset", new Color32(0x6D, 0xAA, 0x3E, 0xFF));
            Material summerDirt = CreateSeasonMaterial("M_Summer_Dirt", "M_Dirt.mat", "T_Summer_Dirt.asset", new Color32(0x87, 0x51, 0x2E, 0xFF));
            Material summerLeaves = CreateSeasonMaterial("M_Summer_Leaves", "M_Leaves.mat", "T_Summer_Leaves.asset", new Color32(0x43, 0x8A, 0x38, 0xFF));
            Material summerWater = CreateSeasonMaterial("M_Summer_Water", "M_Water.mat", "T_Summer_Water.asset", new Color32(0x24, 0x8E, 0xC8, 0x9E));

            CreateProfile(SpringProfilePath, groundCell, boundary,
                springGrass, springDirt, springWater, springLeaves, 0.25f, 2, 3, 5, 5, 8, 2, 0.25f);
            CreateProfile(SummerProfilePath, groundCell, boundary,
                summerGrass, summerDirt, summerWater, summerLeaves, 0.40f, 3, 2, 4, 3, 12, 4, 0.65f);

            AssetDatabase.SaveAssets();
            string springHash = BuildSeasonScene(SpringScenePath, SpringProfilePath, 20260818, "Spring");
            string summerHash = BuildSeasonScene(SummerScenePath, SummerProfilePath, 20260819, "Summer");
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RAILGAME_SEASON_MAPS_BUILD_OK spring={springHash} summer={summerHash}");
        }

        private static string BuildSeasonScene(string scenePath, string profilePath, int seed, string season)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MapGenerationProfile profile = AssetDatabase.LoadAssetAtPath<MapGenerationProfile>(profilePath);
            Require(profile != null, $"{season} profile missing");
            GameObject root = new($"ProceduralMapRoot_24x128_{season}");
            root.transform.position = new Vector3(-12f, 0f, 0f);

            NavMeshSurface surface = root.AddComponent<NavMeshSurface>();
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(12f, 1.5f, 64f);
            surface.size = new Vector3(20f, 5f, 128f);
            surface.layerMask = (1 << 0) | (1 << 4);
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            surface.overrideTileSize = true;
            surface.tileSize = 128;
            surface.minRegionArea = 0.1f;

            RuntimeNavigationController navigation = root.AddComponent<RuntimeNavigationController>();
            SerializedObject navigationData = new(navigation);
            navigationData.FindProperty("surface").objectReferenceValue = surface;

            GameObject enemies = new("Enemies_TeamPrefabRuntimeRoot");
            enemies.transform.SetParent(root.transform, false);
            navigationData.FindProperty("enemyRoot").objectReferenceValue = enemies.transform;
            navigationData.ApplyModifiedPropertiesWithoutUndo();

            ProceduralMapGenerator generator = root.AddComponent<ProceduralMapGenerator>();
            SerializedObject generatorData = new(generator);
            generatorData.FindProperty("profile").objectReferenceValue = profile;
            generatorData.FindProperty("navigation").objectReferenceValue = navigation;
            generatorData.FindProperty("worldSeed").intValue = seed;
            generatorData.FindProperty("generateOnStart").boolValue = true;
            generatorData.FindProperty("buildNavMeshAfterGenerate").boolValue = false;
            generatorData.ApplyModifiedPropertiesWithoutUndo();

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Require(playerPrefab != null, "Player prefab missing");
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, root.transform);
            player.name = "Player";
            player.transform.localPosition = new Vector3(11.5f, 1.05f, 2.5f);
            player.transform.localRotation = Quaternion.identity;

            BuildMarkers(root.transform);
            BuildLightingAndCamera(player.transform);
            generator.GenerateNow();
            generatorData.Update();
            generatorData.FindProperty("buildNavMeshAfterGenerate").boolValue = true;
            generatorData.ApplyModifiedPropertiesWithoutUndo();
            surface.RemoveData();
            surface.navMeshData = null;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException($"Failed to save {season} procedural map scene.");
            Debug.Log($"RAILGAME_PROCEDURAL_BUILD_OK season={season} scene={scenePath} seed={generator.WorldSeed} hash={generator.LastLayoutHash}");
            return generator.LastLayoutHash;
        }

        [MenuItem("Railgame/Validate Procedural Map")]
        public static void Validate()
        {
            MapGenerationProfile spring = AssetDatabase.LoadAssetAtPath<MapGenerationProfile>(SpringProfilePath);
            MapGenerationProfile summer = AssetDatabase.LoadAssetAtPath<MapGenerationProfile>(SummerProfilePath);
            Require(spring != null && spring.RiverWidth == 2 && spring.FordWidth == 5 && spring.DirtBaseCount == 8,
                "Spring profile settings mismatch");
            Require(summer != null && summer.RiverWidth == 3 && summer.FordWidth == 3 && summer.DirtBaseCount == 12,
                "Summer profile settings mismatch");
            Require(spring.GroundMaterial != summer.GroundMaterial && spring.WaterMaterial != summer.WaterMaterial,
                "Season material profiles are not distinct");
            RequireVoxelMaterial(spring.GroundMaterial, "Spring grass");
            RequireVoxelMaterial(spring.DirtMaterial, "Spring dirt");
            RequireVoxelMaterial(spring.WaterMaterial, "Spring water");
            RequireVoxelMaterial(spring.LeavesMaterial, "Spring leaves");
            RequireVoxelMaterial(summer.GroundMaterial, "Summer grass");
            RequireVoxelMaterial(summer.DirtMaterial, "Summer dirt");
            RequireVoxelMaterial(summer.WaterMaterial, "Summer water");
            RequireVoxelMaterial(summer.LeavesMaterial, "Summer leaves");
            ValidateScene(SpringScenePath, "Spring");
            ValidateScene(SummerScenePath, "Summer");
            Debug.Log("RAILGAME_SEASON_MAPS_VALIDATE_OK seasons=2");
        }

        private static void ValidateScene(string scenePath, string season)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new FileNotFoundException($"{season} procedural map scene missing.", scenePath);

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ProceduralMapGenerator generator = Object.FindFirstObjectByType<ProceduralMapGenerator>();
            RuntimeNavigationController navigation = Object.FindFirstObjectByType<RuntimeNavigationController>();
            Require(generator != null, "ProceduralMapGenerator missing");
            Require(navigation != null && navigation.Surface != null, "Runtime navigation wiring missing");
            Require(generator.Profile != null, "Map profile missing");
            RailgamePlayerController player = Object.FindFirstObjectByType<RailgamePlayerController>();
            Require(player != null && player.GetComponent<CharacterController>() != null, "Player movement wiring missing");
            Require(player.JumpHeight > 1f && player.WaterSpeedMultiplier < 1f, "Player jump or water slowdown settings mismatch");

            generator.GenerateNow();
            string firstHash = generator.LastLayoutHash;
            generator.GenerateNow();
            Require(firstHash == generator.LastLayoutHash, "Same seed produced different layout hash");

            int legCount = ProceduralMapGenerator.MapLength / ProceduralMapGenerator.LegLength;
            Require(generator.GeneratedWaterCount >= generator.Profile.WaterCellCount * legCount, "Water guarantee failed");
            Require(generator.GeneratedTreeCount >= generator.Profile.TreeCount * legCount, "Tree slot guarantee failed");
            Require(generator.GeneratedIronCount >= generator.Profile.IronCount * legCount, "Iron slot guarantee failed");
            int expectedDirt = generator.Profile.DirtBaseCount * legCount +
                               generator.Profile.DirtIncreasePerLeg * legCount * (legCount - 1) / 2;
            Require(generator.GeneratedDirtCount == expectedDirt, "Dirt progression count mismatch");
            Require(generator.Profile.GroundMaterial != null && generator.Profile.DirtMaterial != null &&
                    generator.Profile.WaterMaterial != null && generator.Profile.LeavesMaterial != null,
                "Season materials are not fully wired");
            Require(generator.GeneratedHillResourceCount > 0, "No resource generated on a 1m hill");
            Require(generator.GeneratedRiverCount == 2, "Transverse river count mismatch");
            Require(generator.GeneratedResourceClusterCount == legCount * 6, "Resource cluster count mismatch");
            Require(generator.GeneratedMountainCount == 64, "Background mountain count mismatch");
            Require(generator.GeneratedJumpLinkCount > 0, "No one-block traversal links generated");
            Require(generator.HasCompleteMovementPath(), "Player/enemy movement graph is incomplete");
            Require(generator.HasRailPathAfterMining(), "Flat rail route after mining is incomplete");
            Require(navigation.Surface.navMeshData != null, "NavMesh data was not built");

            Transform safetyFloor = generator.transform.Find("GeneratedMap/SafetyFloor_NoSpawns");
            Require(safetyFloor != null && safetyFloor.childCount == 48, "Continuous lower safety floor is incomplete");
            Require(safetyFloor.GetComponentsInChildren<ResourceSpawnSlot>(true).Length == 0 &&
                    safetyFloor.GetComponentsInChildren<DirtBlock>(true).Length == 0,
                "Gameplay content spawned on lower safety floor");
            int disabledWaterBasins = generator.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "BasinFloor" && !item.gameObject.activeSelf);
            Require(disabledWaterBasins == generator.GeneratedWaterCount, "Water prefab basin floors overlap the continuous safety floor");
            Require(generator.GetComponentsInChildren<WaterSlowVolume>(true).Length == generator.GeneratedWaterCount,
                "Water slowdown volumes are incomplete");

            Vector3 start = generator.transform.TransformPoint(new Vector3(11.5f, 1f, 2.5f));
            Vector3 goal = generator.transform.TransformPoint(new Vector3(11.5f, 1f, 125.5f));
            Require(NavMesh.SamplePosition(start, out NavMeshHit startHit, 2f, NavMesh.AllAreas), "NavMesh start missing");
            Require(NavMesh.SamplePosition(goal, out NavMeshHit goalHit, 2f, NavMesh.AllAreas), "NavMesh goal missing");
            NavMeshPath path = new();
            Require(NavMesh.CalculatePath(startHit.position, goalHit.position, NavMesh.AllAreas, path), "NavMesh path calculation failed");
            if (path.status != NavMeshPathStatus.PathComplete)
                LogNavMeshReach(generator, startHit.position);
            Require(path.status == NavMeshPathStatus.PathComplete, "NavMesh start-goal path is not complete");

            int boundaryRenderers = GameObject.Find("TransparentBoundaries")?.GetComponentsInChildren<Renderer>(true).Length ?? -1;
            Require(boundaryRenderers == 0, "Transparent boundaries contain Renderer");
            Require(LayerMask.NameToLayer("WorldBoundary") == 6, "WorldBoundary layer missing");
            Require(LayerMask.NameToLayer("ResourceObstacle") == 7, "ResourceObstacle layer missing");
            Require(LayerMask.NameToLayer("BackgroundTerrain") == 8, "BackgroundTerrain layer missing");

            Debug.Log($"RAILGAME_PROCEDURAL_MAP_OK season={season} seed={generator.WorldSeed} hash={generator.LastLayoutHash} water={generator.GeneratedWaterCount} dirt={generator.GeneratedDirtCount} tree={generator.GeneratedTreeCount} iron={generator.GeneratedIronCount} resourceClusters={generator.GeneratedResourceClusterCount} hillResources={generator.GeneratedHillResourceCount} mountains={generator.GeneratedMountainCount} links={generator.GeneratedJumpLinkCount}");
        }

        private static void LogNavMeshReach(ProceduralMapGenerator generator, Vector3 start)
        {
            int lastReachableZ = -1;
            Dictionary<int, int> samplesByRow = new();
            Dictionary<int, int> reachableByRow = new();
            for (int z = 0; z < ProceduralMapGenerator.MapLength; z++)
            for (int x = ProceduralMapGenerator.PlayableMinX; x <= ProceduralMapGenerator.PlayableMaxX; x++)
            {
                Vector3 point = generator.transform.TransformPoint(new Vector3(x + 0.5f, 1f, z + 0.5f));
                if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 1.2f, NavMesh.AllAreas))
                    continue;
                samplesByRow[z] = samplesByRow.GetValueOrDefault(z) + 1;
                NavMeshPath probe = new();
                if (NavMesh.CalculatePath(start, hit.position, NavMesh.AllAreas, probe) && probe.status == NavMeshPathStatus.PathComplete)
                {
                    lastReachableZ = Mathf.Max(lastReachableZ, z);
                    reachableByRow[z] = reachableByRow.GetValueOrDefault(z) + 1;
                }
            }
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            int activeLinks = Object.FindObjectsByType<NavMeshLink>(FindObjectsSortMode.None).Count(link => link.activated);
            string rows = string.Join(",", Enumerable.Range(20, 16).Select(z => $"{z}:{samplesByRow.GetValueOrDefault(z)}/{reachableByRow.GetValueOrDefault(z)}"));
            Debug.LogError($"NAVMESH_REACH_DIAGNOSTIC lastReachableZ={lastReachableZ} vertices={triangulation.vertices.Length} activeLinks={activeLinks} rows={rows}");
        }

        [MenuItem("Railgame/Validate 2000 Seasonal Seeds")]
        public static void ValidateSeedBatch()
        {
            int springUnique = ValidateSeedBatchForScene(SpringScenePath, "Spring");
            int summerUnique = ValidateSeedBatchForScene(SummerScenePath, "Summer");
            Debug.Log($"RAILGAME_SEASON_2000_SEEDS_OK springUnique={springUnique} summerUnique={summerUnique}");
        }

        private static int ValidateSeedBatchForScene(string scenePath, string season)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ProceduralMapGenerator generator = Object.FindFirstObjectByType<ProceduralMapGenerator>();
            Require(generator != null, $"{season} ProceduralMapGenerator missing");

            HashSet<string> hashes = new();
            const int firstSeed = 20260000;
            const int seedCount = 1000;
            for (int seed = firstSeed; seed < firstSeed + seedCount; seed++)
                hashes.Add(generator.GenerateLogicalLayoutForValidation(seed));

            string first = generator.GenerateLogicalLayoutForValidation(firstSeed);
            string repeated = generator.GenerateLogicalLayoutForValidation(firstSeed);
            Require(first == repeated, "Logical generation is not deterministic");
            Require(hashes.Count >= 990, $"Layout diversity too low: {hashes.Count}/{seedCount}");
            generator.GenerateNow();
            Debug.Log($"RAILGAME_PROCEDURAL_1000_SEEDS_OK season={season} seeds={seedCount} unique={hashes.Count}");
            return hashes.Count;
        }

        public static void CaptureOverview()
        {
            string springPath = Environment.GetEnvironmentVariable("RAILGAME_SPRING_CAPTURE");
            if (string.IsNullOrWhiteSpace(springPath))
                springPath = Path.GetFullPath("Temp/railgame-spring-overview.png");
            string summerPath = Environment.GetEnvironmentVariable("RAILGAME_SUMMER_CAPTURE");
            if (string.IsNullOrWhiteSpace(summerPath))
                summerPath = Path.GetFullPath("Temp/railgame-summer-overview.png");
            CaptureScene(SpringScenePath, springPath, "Spring");
            CaptureScene(SummerScenePath, summerPath, "Summer");
            Debug.Log($"RAILGAME_SEASON_CAPTURES_OK spring={springPath} summer={summerPath}");
        }

        private static void CaptureScene(string scenePath, string outputPath, string season)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            Require(camera != null, $"{season} procedural overview camera missing");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Invalid capture path"));

            RenderTexture target = new(1024, 2048, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D image = new(1024, 2048, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1024, 2048), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(target);
            }

            Require(File.Exists(outputPath) && new FileInfo(outputPath).Length > 0, "Procedural capture missing");
            Debug.Log($"RAILGAME_PROCEDURAL_CAPTURE_OK season={season} path={outputPath}");
        }

        private static MapGenerationProfile CreateProfile(string path, GameObject groundCell, GameObject boundary,
            Material groundMaterial, Material dirtMaterial, Material waterMaterial, Material leavesMaterial,
            float hillResourceChance, int riverWidth, int riverBendMin, int riverBendMax, int fordWidth,
            int dirtBaseCount, int dirtIncreasePerLeg, float resourceSideBias)
        {
            MapGenerationProfile profile = AssetDatabase.LoadAssetAtPath<MapGenerationProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MapGenerationProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            SerializedObject data = new(profile);
            SetObject(data, "groundChunkPrefab", PrefabFolder + "/PF_Ground_8x8.prefab");
            data.FindProperty("groundCellPrefab").objectReferenceValue = groundCell;
            SetObject(data, "dirtPrefab", PrefabFolder + "/PF_DirtBlock_1x1.prefab");
            SetObject(data, "waterPrefab", PrefabFolder + "/PF_WaterCell_1x1.prefab");
            data.FindProperty("boundaryPrefab").objectReferenceValue = boundary;
            SetObject(data, "backgroundMountainPrefab", PrefabFolder + "/PF_DirtBlock_1x1.prefab");
            SetObject(data, "treePrefab", PrefabFolder + "/PF_Tree_2x2.prefab");
            SetObject(data, "ironPrefab", PrefabFolder + "/PF_Rock_1x1.prefab");
            data.FindProperty("railPrefab").objectReferenceValue = null;
            data.FindProperty("enemyPrefab").objectReferenceValue = null;
            data.FindProperty("treeCount").intValue = 12;
            data.FindProperty("ironCount").intValue = 12;
            data.FindProperty("waterCellCount").intValue = 12;
            data.FindProperty("hillResourceChance").floatValue = hillResourceChance;
            data.FindProperty("groundMaterial").objectReferenceValue = groundMaterial;
            data.FindProperty("dirtMaterial").objectReferenceValue = dirtMaterial;
            data.FindProperty("waterMaterial").objectReferenceValue = waterMaterial;
            data.FindProperty("leavesMaterial").objectReferenceValue = leavesMaterial;
            data.FindProperty("riverWidth").intValue = riverWidth;
            data.FindProperty("riverBendMin").intValue = riverBendMin;
            data.FindProperty("riverBendMax").intValue = riverBendMax;
            data.FindProperty("fordWidth").intValue = fordWidth;
            data.FindProperty("dirtBaseCount").intValue = dirtBaseCount;
            data.FindProperty("dirtIncreasePerLeg").intValue = dirtIncreasePerLeg;
            data.FindProperty("resourceSideBias").floatValue = resourceSideBias;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Material CreateSeasonMaterial(string name, string templateFile, string textureFile, Color color)
        {
            string path = $"{MapFolder}/Materials/{name}.mat";
            Material template = AssetDatabase.LoadAssetAtPath<Material>($"{MapFolder}/Materials/{templateFile}");
            if (template == null)
                throw new FileNotFoundException("Season material template missing.", templateFile);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(template);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.CopySerialized(template, material);
            }
            material.name = name;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureFile}");
            material.mainTexture = texture;
            material.color = texture != null ? Color.white : color;
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateVoxelTextures()
        {
            CreatePixelTexture("T_Spring_Grass", new Color32(0x93, 0xC9, 0x5A, 0xFF), new Color32(0xA8, 0xD8, 0x72, 0xFF), new Color32(0x76, 0xAD, 0x43, 0xFF), 11, PixelPattern.Scatter);
            CreatePixelTexture("T_Spring_Dirt", new Color32(0xA6, 0x68, 0x3F, 0xFF), new Color32(0xBE, 0x7D, 0x50, 0xFF), new Color32(0x7E, 0x48, 0x2D, 0xFF), 17, PixelPattern.Clusters);
            CreatePixelTexture("T_Spring_Leaves", new Color32(0x7A, 0xC3, 0x4A, 0xFF), new Color32(0x94, 0xD6, 0x61, 0xFF), new Color32(0x55, 0x9B, 0x35, 0xFF), 23, PixelPattern.Clusters);
            CreatePixelTexture("T_Spring_Water", new Color32(0x45, 0xBC, 0xE1, 0xA8), new Color32(0x76, 0xD4, 0xEB, 0xB0), new Color32(0x2A, 0x98, 0xC8, 0xA8), 29, PixelPattern.Waves);
            CreatePixelTexture("T_Summer_Grass", new Color32(0x6D, 0xAA, 0x3E, 0xFF), new Color32(0x82, 0xBC, 0x50, 0xFF), new Color32(0x4F, 0x85, 0x2D, 0xFF), 31, PixelPattern.Scatter);
            CreatePixelTexture("T_Summer_Dirt", new Color32(0x87, 0x51, 0x2E, 0xFF), new Color32(0xA1, 0x67, 0x3D, 0xFF), new Color32(0x63, 0x37, 0x22, 0xFF), 37, PixelPattern.Clusters);
            CreatePixelTexture("T_Summer_Leaves", new Color32(0x43, 0x8A, 0x38, 0xFF), new Color32(0x58, 0xA2, 0x48, 0xFF), new Color32(0x2E, 0x69, 0x2A, 0xFF), 41, PixelPattern.Clusters);
            CreatePixelTexture("T_Summer_Water", new Color32(0x24, 0x8E, 0xC8, 0xA8), new Color32(0x4A, 0xAD, 0xD8, 0xB0), new Color32(0x1C, 0x6E, 0xA6, 0xA8), 43, PixelPattern.Waves);
            Texture2D stone = CreatePixelTexture("T_Stone", new Color32(0x5D, 0x66, 0x70, 0xFF), new Color32(0x77, 0x81, 0x8A, 0xFF), new Color32(0x43, 0x4B, 0x55, 0xFF), 47, PixelPattern.Clusters);
            Texture2D trunk = CreatePixelTexture("T_Trunk", new Color32(0x75, 0x46, 0x2B, 0xFF), new Color32(0x91, 0x5C, 0x37, 0xFF), new Color32(0x54, 0x31, 0x20, 0xFF), 53, PixelPattern.Stripes);
            ApplyVoxelTextureToMaterial("M_Stone.mat", stone);
            ApplyVoxelTextureToMaterial("M_Trunk.mat", trunk);
        }

        private static Texture2D CreatePixelTexture(string name, Color32 baseColor, Color32 light, Color32 dark, int seed, PixelPattern pattern)
        {
            string path = $"{TextureFolder}/{name}.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = name };
                AssetDatabase.CreateAsset(texture, path);
            }
            else
            {
                texture.Reinitialize(16, 16, TextureFormat.RGBA32, false);
            }

            Color32[] pixels = new Color32[256];
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int noise = Mathf.Abs((x * 73 + y * 151 + seed * 199) ^ ((x + seed) * (y + 17))) % 100;
                pixels[y * 16 + x] = pattern switch
                {
                    PixelPattern.Waves => (y % 5 == 1 && noise < 65) ? light : (y % 5 == 3 && noise < 45) ? dark : baseColor,
                    PixelPattern.Stripes => (x + seed) % 5 == 0 ? dark : (x + seed) % 5 == 2 ? light : baseColor,
                    PixelPattern.Clusters => noise < 18 ? dark : noise > 82 ? light : baseColor,
                    _ => noise < 10 ? dark : noise > 89 ? light : baseColor
                };
            }

            texture.SetPixels32(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.anisoLevel = 0;
            texture.Apply(false, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static void ApplyVoxelTextureToMaterial(string materialFile, Texture2D texture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MapFolder}/Materials/{materialFile}");
            if (material == null)
                return;
            material.mainTexture = texture;
            material.color = Color.white;
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureCharacterTextures()
        {
            string[] paths =
            {
                "Assets/00.main/Character/Source/Textures/Cute_Ghost_skin.jpg",
                "Assets/00.main/Character/Source/Textures/Cute_Ghost_face.png"
            };
            foreach (string path in paths)
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.maxTextureSize = 128;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

            foreach (string path in new[]
                     {
                         "Assets/00.main/Character/Materials/M_CharacterGhost_Body.mat",
                         "Assets/00.main/Character/Materials/M_CharacterGhost_Face.mat"
                     })
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    continue;
                material.SetFloat("_Smoothness", 0f);
                material.SetFloat("_SpecularHighlights", 0f);
                EditorUtility.SetDirty(material);
            }
        }

        private enum PixelPattern
        {
            Scatter,
            Clusters,
            Waves,
            Stripes
        }

        private static GameObject CreateGroundCellPrefab()
        {
            string path = PrefabFolder + "/PF_Ground_1x1.prefab";
            GameObject root = new("PF_Ground_1x1");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.one * 0.5f;
            visual.GetComponent<BoxCollider>().size = new Vector3(1.1f, 1f, 1.1f);
            Material grass = AssetDatabase.LoadAssetAtPath<Material>(MapFolder + "/Materials/M_Grass.mat");
            visual.GetComponent<Renderer>().sharedMaterial = grass;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBoundaryPrefab()
        {
            string path = PrefabFolder + "/PF_WorldBoundary_1x8.prefab";
            GameObject root = new("PF_WorldBoundary_1x8");
            root.layer = 6;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 2.5f, 4f);
            collider.size = new Vector3(0.1f, 5f, 8f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureWaterPrefab()
        {
            string path = PrefabFolder + "/PF_WaterCell_1x1.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform slowVolume = root.transform.Find("SlowVolume");
                Require(slowVolume != null && slowVolume.TryGetComponent(out BoxCollider trigger) && trigger.isTrigger,
                    "Water SlowVolume trigger missing");
                if (slowVolume.GetComponent<WaterSlowVolume>() == null)
                    slowVolume.gameObject.AddComponent<WaterSlowVolume>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreatePlayerPrefab()
        {
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterVisualPath);
            if (visualPrefab == null)
                throw new FileNotFoundException("Character visual prefab missing.", CharacterVisualPath);

            GameObject root = new("PF_RailgamePlayer");
            try
            {
                CharacterController controller = root.AddComponent<CharacterController>();
                controller.radius = 0.34f;
                controller.height = 1.8f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.stepOffset = 0.25f;
                controller.skinWidth = 0.04f;
                root.AddComponent<RailgamePlayerController>();

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                visual.name = "Visual";
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildMarkers(Transform parent)
        {
            GameObject markers = new("Markers");
            markers.transform.SetParent(parent, false);
            CreateMarker(markers.transform, "StartMarker", new Vector3(11.5f, 1.01f, 2.5f), new Color(0.2f, 0.8f, 0.3f));
            CreateMarker(markers.transform, "GoalMarker", new Vector3(11.5f, 1.01f, 125.5f), new Color(0.9f, 0.65f, 0.15f));
        }

        private static void CreateMarker(Transform parent, string name, Vector3 localPosition, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = new Vector3(1f, 0.02f, 1f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            marker.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildLightingAndCamera(Transform player)
        {
            GameObject cameraObject = new("Procedural Overview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 68f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250f;
            cameraObject.transform.position = new Vector3(0f, 150f, 64f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            RailgameCameraFollow follow = cameraObject.AddComponent<RailgameCameraFollow>();
            follow.SetTarget(player);

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            lightObject.AddComponent<UniversalAdditionalLightData>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            VolumeProfile volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (volumeProfile != null)
            {
                GameObject volumeObject = new("Global Volume");
                Volume volume = volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = volumeProfile;
                volume.enabled = false;
            }
        }

        private static void AddScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(item => !string.Equals(item.path, SpringScenePath, StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(item.path, SummerScenePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(SummerScenePath, true));
            scenes.Insert(0, new EditorBuildSettingsScene(SpringScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObject(SerializedObject data, string propertyName, string assetPath)
        {
            GameObject value = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (value == null)
                throw new FileNotFoundException("Required prefab missing.", assetPath);
            data.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void RequireVoxelMaterial(Material material, string label)
        {
            Require(material != null && material.mainTexture is Texture2D texture && texture.width == 16 &&
                    texture.height == 16 && texture.filterMode == FilterMode.Point,
                $"{label} voxel texture wiring mismatch");
        }
    }
}
