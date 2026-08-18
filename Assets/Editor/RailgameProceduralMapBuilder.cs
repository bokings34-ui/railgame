using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Railgame.Map;
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
        private const string SpringProfilePath = ProfileFolder + "/MapGenerationProfile_Spring.asset";
        private const string SummerProfilePath = ProfileFolder + "/MapGenerationProfile_Summer.asset";
        private const string ScenePath = SceneFolder + "/Map_Procedural_Spring.unity";

        [MenuItem("Railgame/Build Procedural Map")]
        public static void Build()
        {
            EnsureFolder(ProfileFolder);
            GameObject groundCell = CreateGroundCellPrefab();
            GameObject boundary = CreateBoundaryPrefab();
            MapGenerationProfile spring = CreateProfile(SpringProfilePath, groundCell, boundary, 0.35f);
            CreateProfile(SummerProfilePath, groundCell, boundary, 0.25f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("ProceduralMapRoot_24x128");
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
            generatorData.FindProperty("profile").objectReferenceValue = spring;
            generatorData.FindProperty("navigation").objectReferenceValue = navigation;
            generatorData.FindProperty("worldSeed").intValue = 20260818;
            generatorData.FindProperty("generateOnStart").boolValue = true;
            generatorData.FindProperty("buildNavMeshAfterGenerate").boolValue = false;
            generatorData.ApplyModifiedPropertiesWithoutUndo();

            BuildMarkers(root.transform);
            BuildLightingAndCamera();
            generator.GenerateNow();
            generatorData.Update();
            generatorData.FindProperty("buildNavMeshAfterGenerate").boolValue = true;
            generatorData.ApplyModifiedPropertiesWithoutUndo();
            surface.RemoveData();
            surface.navMeshData = null;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save procedural map scene.");

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RAILGAME_PROCEDURAL_BUILD_OK scene={ScenePath} seed={generator.WorldSeed} hash={generator.LastLayoutHash}");
        }

        [MenuItem("Railgame/Validate Procedural Map")]
        public static void Validate()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new FileNotFoundException("Procedural map scene missing.", ScenePath);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ProceduralMapGenerator generator = Object.FindFirstObjectByType<ProceduralMapGenerator>();
            RuntimeNavigationController navigation = Object.FindFirstObjectByType<RuntimeNavigationController>();
            Require(generator != null, "ProceduralMapGenerator missing");
            Require(navigation != null && navigation.Surface != null, "Runtime navigation wiring missing");
            Require(generator.Profile != null, "Map profile missing");

            generator.GenerateNow();
            string firstHash = generator.LastLayoutHash;
            generator.GenerateNow();
            Require(firstHash == generator.LastLayoutHash, "Same seed produced different layout hash");

            int legCount = ProceduralMapGenerator.MapLength / ProceduralMapGenerator.LegLength;
            Require(generator.GeneratedWaterCount >= generator.Profile.WaterCellCount * legCount, "Water guarantee failed");
            Require(generator.GeneratedTreeCount >= generator.Profile.TreeCount * legCount, "Tree slot guarantee failed");
            Require(generator.GeneratedIronCount >= generator.Profile.IronCount * legCount, "Iron slot guarantee failed");
            Require(generator.GeneratedDirtCount >= 32, "Dirt hill count too low");
            Require(generator.GeneratedHillResourceCount > 0, "No resource generated on a 1m hill");
            Require(generator.GeneratedRiverCount == 2, "Transverse river count mismatch");
            Require(generator.GeneratedResourceClusterCount == legCount * 6, "Resource cluster count mismatch");
            Require(generator.GeneratedMountainCount == 64, "Background mountain count mismatch");
            Require(generator.GeneratedJumpLinkCount > 0, "No one-block traversal links generated");
            Require(generator.HasCompleteMovementPath(), "Player/enemy movement graph is incomplete");
            Require(generator.HasRailPathAfterMining(), "Flat rail route after mining is incomplete");
            Require(navigation.Surface.navMeshData != null, "NavMesh data was not built");

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

            Debug.Log($"RAILGAME_PROCEDURAL_MAP_OK seed={generator.WorldSeed} hash={generator.LastLayoutHash} water={generator.GeneratedWaterCount} dirt={generator.GeneratedDirtCount} tree={generator.GeneratedTreeCount} iron={generator.GeneratedIronCount} resourceClusters={generator.GeneratedResourceClusterCount} hillResources={generator.GeneratedHillResourceCount} mountains={generator.GeneratedMountainCount} links={generator.GeneratedJumpLinkCount}");
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

        [MenuItem("Railgame/Validate 1000 Procedural Seeds")]
        public static void ValidateSeedBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ProceduralMapGenerator generator = Object.FindFirstObjectByType<ProceduralMapGenerator>();
            Require(generator != null, "ProceduralMapGenerator missing");

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
            Debug.Log($"RAILGAME_PROCEDURAL_1000_SEEDS_OK seeds={seedCount} unique={hashes.Count}");
        }

        public static void CaptureOverview()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Camera.main;
            Require(camera != null, "Procedural overview camera missing");

            string outputPath = Environment.GetEnvironmentVariable("RAILGAME_PROCEDURAL_CAPTURE");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = Path.GetFullPath("Temp/railgame-procedural-overview.png");
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
            Debug.Log("RAILGAME_PROCEDURAL_CAPTURE_OK path=" + outputPath);
        }

        private static MapGenerationProfile CreateProfile(string path, GameObject groundCell, GameObject boundary, float hillResourceChance)
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
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
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

        private static void BuildLightingAndCamera()
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

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(item => !string.Equals(item.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
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
    }
}
