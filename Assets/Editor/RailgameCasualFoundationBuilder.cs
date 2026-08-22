using System;
using System.IO;
using System.Linq;
using Railgame.Enemy;
using Railgame.Map;
using Railgame.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Railgame.Editor
{
    public static class RailgameCasualFoundationBuilder
    {
        public const string LobbyScenePath = "Assets/00.main/UI/Scenes/Railgame_Lobby.unity";
        public const string GameplayUiPrefabPath = "Assets/00.main/UI/Prefabs/PF_CasualGameplayUI.prefab";
        public const string DummyEnemyPrefabPath = "Assets/00.main/Enemy/Prefabs/PF_DummyEnemy.prefab";

        private const string SettingsPrefabPath = "Assets/00.main/UI/Prefabs/PF_SettingsPanel.prefab";
        private const string ShopPrefabPath = "Assets/00.main/UI/Prefabs/PF_ShopScreen.prefab";
        private static readonly Color Ink = new(0.12f, 0.14f, 0.18f, 1f);
        private static readonly Color Cream = new(1f, 0.96f, 0.82f, 1f);
        private static readonly Color Green = new(0.45f, 0.72f, 0.31f, 1f);
        private static readonly Color Yellow = new(0.96f, 0.72f, 0.24f, 1f);
        private static readonly Color Coral = new(0.94f, 0.39f, 0.34f, 1f);

        [MenuItem("Railgame/Build Casual Solo Foundation")]
        public static void Build()
        {
            RailgameProceduralMapBuilder.Build();
        }

        public static void BuildSharedAssets()
        {
            EnsureFolder("Assets/00.main/UI/Scenes");
            EnsureFolder("Assets/00.main/UI/Prefabs");
            EnsureFolder("Assets/00.main/Enemy/Prefabs");
            CreateSettingsPrefab();
            CreateShopPrefab();
            CreateGameplayUiPrefab();
            CreateDummyEnemyPrefab();
        }

        public static void AddGameplayFoundation(Transform sceneRoot, Transform player, Transform enemyRoot,
            RuntimeNavigationController navigation)
        {
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiPrefabPath);
            GameObject dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyEnemyPrefabPath);
            Require(uiPrefab != null && dummyPrefab != null, "Casual gameplay prefabs missing");

            GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, sceneRoot);
            ui.name = "CasualSoloUI";
            Camera camera = Camera.main;
            Canvas canvas = ui.GetComponent<Canvas>();
            if (canvas != null && camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            CreateEventSystem(sceneRoot);
            DummyEnemySpawner spawner = enemyRoot.gameObject.AddComponent<DummyEnemySpawner>();
            spawner.Configure(navigation, player, dummyPrefab.GetComponent<DummyEnemyChaser>());
        }

        public static void BuildLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.40f, 0.72f, 0.82f, 1f);
            camera.orthographic = true;

            GameObject canvasObject = CreateCanvas("CasualLobbyCanvas");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CreateEventSystem(null);

            Image backdrop = CreateImage("Backdrop", canvasObject.transform, new Color(0.40f, 0.72f, 0.82f, 1f));
            Stretch(backdrop.rectTransform);
            CreateBlockStrip(backdrop.transform);

            Image card = CreateImage("LobbyCard", backdrop.transform, Cream);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 760f));
            Text title = CreateText("Title", card.transform, "RAIL TRAIL", 64, Ink, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(540f, 90f));
            Text subtitle = CreateText("Subtitle", card.transform, "CASUAL VOXEL JOURNEY", 23, new Color(0.25f, 0.40f, 0.31f), FontStyle.Bold);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -155f), new Vector2(540f, 45f));

            Button spring = CreateButton("SpringButton", card.transform, "SPRING  ·  EASY START", Green, new Vector2(0f, 90f));
            Button summer = CreateButton("SummerButton", card.transform, "SUMMER  ·  SUNNY RUN", Yellow, new Vector2(0f, -5f));
            Button settings = CreateButton("SettingsButton", card.transform, "OPTIONS", new Color(0.39f, 0.62f, 0.72f), new Vector2(0f, -100f));
            Button quit = CreateButton("QuitButton", card.transform, "QUIT", Coral, new Vector2(0f, -195f));

            GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
            GameObject settingsInstance = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, canvasObject.transform);
            settingsInstance.name = "SettingsPanel";
            settingsInstance.SetActive(false);

            RailgameLobbyController controller = canvasObject.AddComponent<RailgameLobbyController>();
            SerializedObject data = new(controller);
            data.FindProperty("springButton").objectReferenceValue = spring;
            data.FindProperty("summerButton").objectReferenceValue = summer;
            data.FindProperty("settingsButton").objectReferenceValue = settings;
            data.FindProperty("quitButton").objectReferenceValue = quit;
            data.FindProperty("settingsPanel").objectReferenceValue = settingsInstance.GetComponent<RailgameSettingsPanel>();
            data.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, LobbyScenePath), "Failed to save lobby scene");
        }

        [MenuItem("Railgame/Validate Casual Solo Foundation")]
        public static void Validate()
        {
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiPrefabPath) != null, "Gameplay UI prefab missing");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(DummyEnemyPrefabPath)?.GetComponent<DummyEnemyChaser>() != null,
                "Dummy enemy prefab missing chaser");

            Scene lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            Require(Object.FindAnyObjectByType<RailgameLobbyController>() != null, "Lobby controller missing");
            Require(Object.FindAnyObjectByType<RailgameSettingsPanel>(FindObjectsInactive.Include) != null, "Lobby settings missing");
            Require(Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Length >= 7,
                "Lobby buttons incomplete");

            ValidateGameplayScene("Assets/00.main/Map/Scenes/Map_Procedural_Spring.unity", "Spring");
            ValidateGameplayScene("Assets/00.main/Map/Scenes/Map_Procedural_Summer.unity", "Summer");
            Debug.Log("RAILGAME_CASUAL_FOUNDATION_OK lobby=1 settings=1 pause=2 shop=2 spawnMarkers=16 dummyChaser=2");
        }

        public static void Capture()
        {
            CaptureScene(LobbyScenePath, Path.GetFullPath("Temp/casual-lobby.png"), false);
            CaptureScene("Assets/00.main/Map/Scenes/Map_Procedural_Spring.unity", Path.GetFullPath("Temp/casual-shop.png"), true);
            Debug.Log("RAILGAME_CASUAL_CAPTURES_OK");
        }

        private static void CaptureScene(string scenePath, string outputPath, bool showShop)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (showShop)
                Object.FindAnyObjectByType<RailgameShopScreen>(FindObjectsInactive.Include).gameObject.SetActive(true);
            Camera camera = Camera.main;
            Require(camera != null, $"Capture camera missing: {scenePath}");
            RenderTexture target = new(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            RenderTexture.active = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        private static void ValidateGameplayScene(string path, string season)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ProceduralMapGenerator generator = Object.FindAnyObjectByType<ProceduralMapGenerator>();
            NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
            Require(generator != null && surface != null && Mathf.Approximately(surface.size.x, 24f),
                $"{season} outer NavMesh strip missing");
            EnemySpawnMarker[] markers = Object.FindObjectsByType<EnemySpawnMarker>(FindObjectsInactive.Include);
            Require(markers.Length == 8,
                $"{season} enemy spawn marker count mismatch");
            Require(Object.FindAnyObjectByType<DummyEnemySpawner>() != null, $"{season} dummy spawner missing");
            Require(Object.FindAnyObjectByType<RailgameGameMenuController>(FindObjectsInactive.Include) != null,
                $"{season} game menu missing");
            Require(Object.FindAnyObjectByType<RailgameShopScreen>(FindObjectsInactive.Include) != null,
                $"{season} shop screen missing");

            generator.GenerateNow();
            markers = Object.FindObjectsByType<EnemySpawnMarker>(FindObjectsInactive.Include);
            EnemySpawnMarker marker = markers.OrderBy(item => item.LegIndex).ThenByDescending(item => item.LeftSide).First();
            Transform player = GameObject.Find("Player").transform;
            Require(HasCompletePath(marker.SpawnPoint.position, marker.EntryPoint.position),
                $"{season} outer spawn-entry path incomplete");
            Require(HasCompletePath(marker.EntryPoint.position, player.position),
                $"{season} entry-player chase path incomplete");
        }

        private static bool HasCompletePath(Vector3 from, Vector3 to)
        {
            if (!NavMesh.SamplePosition(from, out NavMeshHit fromHit, 2f, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(to, out NavMeshHit toHit, 2f, NavMesh.AllAreas))
                return false;
            NavMeshPath path = new();
            return NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path) &&
                   path.status == NavMeshPathStatus.PathComplete;
        }

        private static void CreateSettingsPrefab()
        {
            GameObject root = CreatePanelRoot("PF_SettingsPanel", new Color(0.05f, 0.08f, 0.10f, 0.86f));
            Image card = CreateImage("Card", root.transform, Cream);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 680f));
            Text title = CreateText("Title", card.transform, "OPTIONS", 50, Ink, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(600f, 70f));

            Text volumeLabel = CreateText("VolumeLabel", card.transform, "MASTER VOLUME", 25, Ink, FontStyle.Bold);
            SetRect(volumeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(-115f, -165f), new Vector2(330f, 45f));
            GameObject volumeObject = DefaultControls.CreateSlider(UiResources());
            volumeObject.name = "VolumeSlider";
            volumeObject.transform.SetParent(card.transform, false);
            SetRect((RectTransform)volumeObject.transform, new Vector2(0.5f, 1f), new Vector2(-35f, -220f), new Vector2(420f, 38f));
            Slider volume = volumeObject.GetComponent<Slider>();
            volume.minValue = 0f;
            volume.maxValue = 1f;
            Text volumeValue = CreateText("VolumeValue", card.transform, "100%", 24, Ink, FontStyle.Bold);
            SetRect(volumeValue.rectTransform, new Vector2(0.5f, 1f), new Vector2(230f, -220f), new Vector2(100f, 40f));

            Toggle fullscreen = CreateToggle("FullscreenToggle", card.transform, "FULLSCREEN", new Vector2(0f, -300f));
            Toggle vsync = CreateToggle("VSyncToggle", card.transform, "VSYNC", new Vector2(0f, -360f));
            Text qualityLabel = CreateText("QualityLabel", card.transform, "QUALITY", 24, Ink, FontStyle.Bold);
            SetRect(qualityLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(-190f, -430f), new Vector2(180f, 45f));
            GameObject qualityObject = DefaultControls.CreateDropdown(UiResources());
            qualityObject.name = "QualityDropdown";
            qualityObject.transform.SetParent(card.transform, false);
            SetRect((RectTransform)qualityObject.transform, new Vector2(0.5f, 1f), new Vector2(95f, -430f), new Vector2(300f, 48f));
            Dropdown quality = qualityObject.GetComponent<Dropdown>();

            Button apply = CreateButton("ApplyButton", card.transform, "APPLY", Green, new Vector2(-190f, -560f), new Vector2(190f, 66f));
            Button defaults = CreateButton("DefaultsButton", card.transform, "DEFAULT", Yellow, new Vector2(0f, -560f), new Vector2(190f, 66f));
            Button cancel = CreateButton("CancelButton", card.transform, "BACK", Coral, new Vector2(190f, -560f), new Vector2(190f, 66f));

            RailgameSettingsPanel controller = root.AddComponent<RailgameSettingsPanel>();
            SerializedObject data = new(controller);
            data.FindProperty("volumeSlider").objectReferenceValue = volume;
            data.FindProperty("volumeValueText").objectReferenceValue = volumeValue;
            data.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
            data.FindProperty("vSyncToggle").objectReferenceValue = vsync;
            data.FindProperty("qualityDropdown").objectReferenceValue = quality;
            data.FindProperty("applyButton").objectReferenceValue = apply;
            data.FindProperty("cancelButton").objectReferenceValue = cancel;
            data.FindProperty("defaultsButton").objectReferenceValue = defaults;
            data.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(root, SettingsPrefabPath);
        }

        private static void CreateShopPrefab()
        {
            GameObject root = CreatePanelRoot("PF_ShopScreen", new Color(0.05f, 0.08f, 0.10f, 0.86f));
            Image card = CreateImage("Card", root.transform, Cream);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1050f, 650f));
            Text title = CreateText("Title", card.transform, "TRACKSIDE SHOP", 52, Ink, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(-145f, -72f), new Vector2(650f, 70f));
            Text wallet = CreateText("Wallet", card.transform, "BOLTS  6", 30, Coral, FontStyle.Bold);
            SetRect(wallet.rectTransform, new Vector2(1f, 1f), new Vector2(-180f, -75f), new Vector2(260f, 60f));

            Button[] offers = new Button[3];
            Text[] labels = new Text[3];
            Color[] colors = { Green, Yellow, new Color(0.42f, 0.68f, 0.82f) };
            for (int index = 0; index < 3; index++)
            {
                offers[index] = CreateButton($"Offer{index + 1}", card.transform, $"TEST ITEM {index + 1}\n{index + 2} BOLTS",
                    colors[index], new Vector2(-330f + index * 330f, -20f), new Vector2(280f, 280f));
                labels[index] = offers[index].GetComponentInChildren<Text>();
            }
            Text note = CreateText("Note", card.transform, "Dummy stock · team products connect later", 20, new Color(0.35f, 0.35f, 0.35f), FontStyle.Italic);
            SetRect(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(800f, 42f));
            Button close = CreateButton("CloseButton", card.transform, "BACK TO MAP", Coral, new Vector2(0f, -260f), new Vector2(310f, 68f));

            RailgameShopScreen controller = root.AddComponent<RailgameShopScreen>();
            SerializedObject data = new(controller);
            data.FindProperty("walletText").objectReferenceValue = wallet;
            AssignArray(data.FindProperty("offerButtons"), offers);
            AssignArray(data.FindProperty("offerTexts"), labels);
            data.FindProperty("closeButton").objectReferenceValue = close;
            data.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(root, ShopPrefabPath);
        }

        private static void CreateGameplayUiPrefab()
        {
            GameObject root = CreateCanvas("PF_CasualGameplayUI");
            root.GetComponent<Canvas>().sortingOrder = 100;
            Button shopButton = CreateButton("OpenShopButton", root.transform, "SHOP TEST", Yellow, new Vector2(-135f, -70f), new Vector2(220f, 64f));
            RectTransform shopRect = (RectTransform)shopButton.transform;
            shopRect.anchorMin = shopRect.anchorMax = new Vector2(1f, 1f);

            GameObject pause = CreatePanelRoot("PausePanel", new Color(0.05f, 0.08f, 0.10f, 0.82f));
            pause.transform.SetParent(root.transform, false);
            Stretch((RectTransform)pause.transform);
            Image card = CreateImage("Card", pause.transform, Cream);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 680f));
            Text title = CreateText("Title", card.transform, "PAUSED", 54, Ink, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(430f, 70f));
            Button resume = CreateButton("ResumeButton", card.transform, "RESUME", Green, new Vector2(0f, 130f));
            Button settings = CreateButton("SettingsButton", card.transform, "OPTIONS", new Color(0.39f, 0.62f, 0.72f), new Vector2(0f, 35f));
            Button restart = CreateButton("RestartButton", card.transform, "RESTART", Yellow, new Vector2(0f, -60f));
            Button lobby = CreateButton("LobbyButton", card.transform, "LOBBY", new Color(0.60f, 0.52f, 0.75f), new Vector2(0f, -155f));
            Button quit = CreateButton("QuitButton", card.transform, "QUIT", Coral, new Vector2(0f, -250f));

            GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
            GameObject settingsPanel = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, root.transform);
            settingsPanel.name = "SettingsPanel";
            GameObject shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);
            GameObject shopPanel = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, root.transform);
            shopPanel.name = "ShopScreen";

            RailgameGameMenuController controller = root.AddComponent<RailgameGameMenuController>();
            SerializedObject data = new(controller);
            data.FindProperty("pausePanel").objectReferenceValue = pause;
            data.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.GetComponent<RailgameSettingsPanel>();
            data.FindProperty("shopScreen").objectReferenceValue = shopPanel.GetComponent<RailgameShopScreen>();
            data.FindProperty("resumeButton").objectReferenceValue = resume;
            data.FindProperty("settingsButton").objectReferenceValue = settings;
            data.FindProperty("restartButton").objectReferenceValue = restart;
            data.FindProperty("lobbyButton").objectReferenceValue = lobby;
            data.FindProperty("quitButton").objectReferenceValue = quit;
            data.FindProperty("openShopButton").objectReferenceValue = shopButton;
            data.ApplyModifiedPropertiesWithoutUndo();
            pause.SetActive(false);
            settingsPanel.SetActive(false);
            shopPanel.SetActive(false);
            SavePrefab(root, GameplayUiPrefabPath);
        }

        private static void CreateDummyEnemyPrefab()
        {
            Material material = CreateMaterial("Assets/00.main/Enemy/Prefabs/M_DummyEnemy.mat", Coral);
            GameObject root = new("PF_DummyEnemy");
            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.5f;
            agent.speed = 3.6f;
            agent.angularSpeed = 720f;
            agent.acceleration = 18f;
            agent.stoppingDistance = 0.75f;
            root.AddComponent<DummyEnemyChaser>();
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "VoxelVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 1.5f, 0.7f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = material;
            SavePrefab(root, DummyEnemyPrefabPath);
        }

        private static GameObject CreateCanvas(string name)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static GameObject CreatePanelRoot(string name, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.GetComponent<Image>().color = color;
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, Color color, FontStyle style)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color, Vector2 position,
            Vector2? size = null)
        {
            GameObject buttonObject = DefaultControls.CreateButton(UiResources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            SetRect((RectTransform)buttonObject.transform, new Vector2(0.5f, 0.5f), position, size ?? new Vector2(440f, 72f));
            Button button = buttonObject.GetComponent<Button>();
            buttonObject.GetComponent<Image>().color = color;
            Text text = button.GetComponentInChildren<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.fontStyle = FontStyle.Bold;
            text.color = Ink;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            button.colors = colors;
            return button;
        }

        private static Toggle CreateToggle(string name, Transform parent, string label, Vector2 position)
        {
            GameObject toggleObject = DefaultControls.CreateToggle(UiResources());
            toggleObject.name = name;
            toggleObject.transform.SetParent(parent, false);
            SetRect((RectTransform)toggleObject.transform, new Vector2(0.5f, 1f), position, new Vector2(420f, 48f));
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            Text text = toggle.GetComponentInChildren<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.color = Ink;
            return toggle;
        }

        private static DefaultControls.Resources UiResources() => new()
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };

        private static void CreateEventSystem(Transform parent)
        {
            GameObject item = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (parent != null) item.transform.SetParent(parent, false);
        }

        private static void CreateBlockStrip(Transform parent)
        {
            Color[] colors = { Green, Yellow, Coral, Cream };
            for (int index = 0; index < 16; index++)
            {
                Image block = CreateImage($"VoxelBlock_{index}", parent, colors[index % colors.Length]);
                RectTransform rect = block.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(index / 15f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, index % 3 * 22f);
                rect.sizeDelta = new Vector2(130f, 120f + index % 4 * 35f);
            }
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AssignArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
