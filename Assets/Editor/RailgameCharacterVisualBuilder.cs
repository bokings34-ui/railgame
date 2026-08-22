using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RailgameCharacterVisualBuilder
{
    private const string Root = "Assets/00.main/Character";
    private const string ModelPath = Root + "/Source/AnimatedGhost.fbx";
    private const string SkinPath = Root + "/Source/Textures/Cute_Ghost_skin.jpg";
    private const string FacePath = Root + "/Source/Textures/Cute_Ghost_face.png";
    private const string BodyMaterialPath = Root + "/Materials/M_CharacterGhost_Body.mat";
    private const string FaceMaterialPath = Root + "/Materials/M_CharacterGhost_Face.mat";
    private const string PrefabPath = Root + "/Prefabs/PF_RailgameCharacterVisual.prefab";
    private const string CapturePath = @"C:\Users\hanso\.codex\visualizations\2026\08\18\01a01487-47c5-7210-a510-33a82ca81c97\railgame-character-visual.png";

    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigurePixelTexture(SkinPath);
        ConfigurePixelTexture(FacePath);

        Require<Texture2D>(SkinPath);
        Require<Texture2D>(FacePath);
        Material bodyMaterial = Require<Material>(BodyMaterialPath);
        Material faceMaterial = Require<Material>(FaceMaterialPath);

        GameObject model = Require<GameObject>(ModelPath);
        var root = new GameObject("PF_RailgameCharacterVisual");
        try
        {
            GameObject visual = UnityEngine.Object.Instantiate(model, root.transform);
            visual.name = "GhostVisual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);

            SkinnedMeshRenderer[] renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
                throw new InvalidOperationException($"Expected one Ghost renderer, found {renderers.Length}.");

            renderers[0].sharedMaterials = new[] { bodyMaterial, faceMaterial };
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        Validate();
        Capture();
        Debug.Log($"RAILGAME_CHARACTER_VISUAL_BUILD_OK prefab={PrefabPath}");
    }

    public static void Validate()
    {
        GameObject prefab = Require<GameObject>(PrefabPath);
        Component[] components = prefab.GetComponentsInChildren<Component>(true);
        int skinnedCount = 0;

        foreach (Component component in components)
        {
            if (component is Transform)
                continue;
            if (component is SkinnedMeshRenderer)
            {
                skinnedCount++;
                continue;
            }
            throw new InvalidOperationException($"Unexpected component in visual prefab: {component.GetType().Name} at {component.name}");
        }

        if (skinnedCount != 1)
            throw new InvalidOperationException($"Expected one SkinnedMeshRenderer, found {skinnedCount}.");

        SkinnedMeshRenderer renderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer.sharedMesh == null || renderer.sharedMaterials.Length != 2)
            throw new InvalidOperationException("Ghost mesh or its two visual materials are missing.");

        Debug.Log($"RAILGAME_CHARACTER_VISUAL_OK renderers={skinnedCount} mesh={renderer.sharedMesh.name} materials={renderer.sharedMaterials[0].name},{renderer.sharedMaterials[1].name}");
    }

    private static void Capture()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject instance = UnityEngine.Object.Instantiate(Require<GameObject>(PrefabPath));
        SkinnedMeshRenderer renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();

        var lightObject = new GameObject("Preview Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

        var cameraObject = new GameObject("Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.14f, 0.12f);
        camera.fieldOfView = 32f;

        Bounds bounds = renderer.bounds;
        Vector3 target = bounds.center;
        cameraObject.transform.position = target + new Vector3(3.1f, 0.35f, 4.6f);
        cameraObject.transform.LookAt(target + Vector3.up * 0.05f);

        var texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = texture;
        camera.Render();
        RenderTexture.active = texture;
        var image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        image.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(CapturePath));
        File.WriteAllBytes(CapturePath, image.EncodeToPNG());

        RenderTexture.active = null;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(instance);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(lightObject);
        Debug.Log($"RAILGAME_CHARACTER_VISUAL_CAPTURE_OK path={CapturePath} bounds={bounds.size}");
    }

    private static void ConfigurePixelTexture(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            return;
        importer.maxTextureSize = 128;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static T Require<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new FileNotFoundException($"Required asset missing: {path}");
        return asset;
    }
}
