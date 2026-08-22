using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView.Editor
{
    public static class ShoulderViewDemoBuilder
    {
        private const string DemoRoot = "Assets/01.hansol/ShoulderView/Demo";
        private const string ScenePath = DemoRoot + "/ShoulderView_UI_Demo.unity";
        private const string SettingsPath = DemoRoot + "/ShoulderViewDemoSettings.asset";

        private static readonly Color Navy = new(0.035f, 0.055f, 0.09f, 0.96f);
        private static readonly Color Panel = new(0.055f, 0.085f, 0.13f, 0.95f);
        private static readonly Color Cyan = new(0.18f, 0.87f, 0.94f, 1f);
        private static readonly Color Lime = new(0.55f, 0.95f, 0.42f, 1f);
        private static readonly Color Muted = new(0.58f, 0.68f, 0.76f, 1f);

        [MenuItem("Railgame/Hansol/Build Shoulder View UI Demo")]
        public static void Build()
        {
            EnsureFolders();
            ShoulderViewSettings settings = LoadOrCreateSettings();
            Material groundMaterial = LoadOrCreateMaterial("M_DemoGround", new Color(0.08f, 0.17f, 0.2f));
            Material accentMaterial = LoadOrCreateMaterial("M_DemoAccent", new Color(0.1f, 0.73f, 0.78f));
            Material obstacleMaterial = LoadOrCreateMaterial("M_DemoObstacle", new Color(0.93f, 0.4f, 0.2f));
            Material playerMaterial = LoadOrCreateMaterial("M_DemoPlayer", new Color(0.65f, 0.95f, 0.42f));
            Material terminalMaterial = LoadOrCreateMaterial("M_ShopTerminal", new Color(0.12f, 0.86f, 0.9f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting();
            BuildEnvironment(groundMaterial, accentMaterial, obstacleMaterial);

            GameObject player = BuildPlayer(settings, playerMaterial);
            ShoulderCameraRig cameraRig = BuildCamera(player.transform, settings);
            ShoulderLocomotionController locomotion = player.GetComponent<ShoulderLocomotionController>();
            locomotion.SetOrientationSource(cameraRig.transform);
            InterfaceResult interfaceResult = BuildInterface(cameraRig, locomotion);
            ShoulderInteractor interactor = BuildShopTerminal(player, cameraRig, interfaceResult, terminalMaterial);

            ShoulderViewEvidenceCapture evidence =
                new GameObject("EvidenceCapture").AddComponent<ShoulderViewEvidenceCapture>();
            evidence.Initialize(interactor, interfaceResult.shopPanel,
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs")), true, true);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SHOULDER_VIEW_DEMO_READY scene={ScenePath}");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void BuildEvidencePlayerFromCommandLine()
        {
            Build();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ShoulderEvidenceBuild",
                "ShoulderShopEvidence.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildPlayerOptions options = new()
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new UnityEditor.Build.BuildFailedException($"Evidence player build failed: {report.summary.result}");
            Debug.Log($"SHOULDER_VIEW_EVIDENCE_PLAYER_READY path={output}");
        }

        private static void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.28f, 0.35f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.07f, 0.09f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 55f;

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.82f, 0.92f, 1f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void BuildEnvironment(Material ground, Material accent, Material obstacle)
        {
            CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.5f, 4f),
                new Vector3(32f, 1f, 36f), ground);

            for (int index = 0; index < 8; index++)
            {
                float z = index * 3.5f - 5f;
                CreatePrimitive($"Guide_{index:00}", PrimitiveType.Cube, new Vector3(0f, 0.03f, z),
                    new Vector3(1.2f, 0.06f, 2.2f), accent);
            }

            CreatePrimitive("CameraCollisionWall", PrimitiveType.Cube, new Vector3(-3.4f, 1.6f, 2.5f),
                new Vector3(0.7f, 3.2f, 5.5f), obstacle);
            CreatePrimitive("CoverBlock_A", PrimitiveType.Cube, new Vector3(4.5f, 1f, 7f),
                new Vector3(2.5f, 2f, 2.5f), obstacle);
            CreatePrimitive("CoverBlock_B", PrimitiveType.Cube, new Vector3(6.5f, 1.75f, 13f),
                new Vector3(3f, 3.5f, 2f), accent);

            for (int index = 0; index < 12; index++)
            {
                float angle = index * Mathf.PI * 2f / 12f;
                Vector3 position = new(Mathf.Cos(angle) * 14f, 1.5f, 8f + Mathf.Sin(angle) * 15f);
                CreatePrimitive($"BoundaryPillar_{index:00}", PrimitiveType.Cylinder, position,
                    new Vector3(0.45f, 1.5f, 0.45f), index % 2 == 0 ? accent : obstacle);
            }
        }

        private static GameObject BuildPlayer(ShoulderViewSettings settings, Material material)
        {
            GameObject player = CreatePrimitive("ShoulderPlayer", PrimitiveType.Capsule, new Vector3(0f, 1f, -2f),
                Vector3.one, material);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.35f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.25f;
            player.transform.rotation = Quaternion.Euler(0f, 22f, 0f);

            ShoulderLocomotionController locomotion = player.AddComponent<ShoulderLocomotionController>();
            locomotion.SetSettings(settings);

            GameObject visor = CreatePrimitive("Visor", PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.55f, 0.22f, 0.12f), material);
            visor.transform.SetParent(player.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.4f, 0.47f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            return player;
        }

        private static ShoulderCameraRig BuildCamera(Transform target, ShoulderViewSettings settings)
        {
            GameObject cameraObject = new("Shoulder Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.05f, 0.075f);
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            ShoulderCameraRig rig = cameraObject.AddComponent<ShoulderCameraRig>();
            rig.SetSettings(settings);
            rig.SetTarget(target);
            rig.SetMouseInputEnabled(false);
            rig.SnapToTarget();
            return rig;
        }

        private static InterfaceResult BuildInterface(ShoulderCameraRig cameraRig,
            ShoulderLocomotionController locomotion)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new("Shoulder View HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateImage("TopBar", canvasObject.transform, Navy, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -74f), Vector2.zero);
            Text brand = CreateText("Brand", canvasObject.transform, "RAILGAME  /  SHOULDER PROTOTYPE", font, 25,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            SetRect(brand.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -37f),
                new Vector2(650f, 74f));
            Text live = CreateText("LiveStatus", canvasObject.transform, "●  LIVE GAME VIEW", font, 20,
                FontStyle.Bold, Lime, TextAnchor.MiddleRight);
            SetRect(live.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -37f),
                new Vector2(300f, 74f));

            Image panelImage = CreateImage("OptionsPanel", canvasObject.transform, Panel,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(390f, 680f));
            Transform panel = panelImage.transform;

            Text title = CreateText("Title", panel, "SHOULDER VIEW", font, 30, FontStyle.Bold, Color.white,
                TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -52f),
                new Vector2(-48f, 54f));
            Text subtitle = CreateText("Subtitle", panel, "REAL-TIME CAMERA OPTIONS", font, 15, FontStyle.Bold, Cyan,
                TextAnchor.MiddleLeft);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -91f),
                new Vector2(-48f, 28f));

            CreateDivider(panel, -120f);
            Text sensitivityValue;
            Slider sensitivity = CreateSliderRow(panel, font, "LOOK SENSITIVITY", 0.25f, 3f, 1f, -170f,
                out sensitivityValue);
            Text fieldOfViewValue;
            Slider fieldOfView = CreateSliderRow(panel, font, "FIELD OF VIEW", 40f, 90f, 62f, -280f,
                out fieldOfViewValue);
            Toggle invert = CreateToggleRow(panel, font, "INVERT VERTICAL LOOK", -380f);

            Text shoulderValue;
            Button shoulder = CreateButtonRow(panel, font, "ACTIVE SHOULDER", "SWAP", -470f, out shoulderValue);
            Text unused;
            Button reset = CreateButtonRow(panel, font, "RESTORE CAMERA", "RESET", -560f, out unused);
            unused.text = "DEFAULTS";

            Text footer = CreateText("Footer", panel, "WASD MOVE   •   SHIFT SPRINT   •   SPACE JUMP", font, 13,
                FontStyle.Normal, Muted, TextAnchor.MiddleCenter);
            SetRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 27f),
                new Vector2(-32f, 34f));

            ShoulderViewOptionsPanel options = panelImage.gameObject.AddComponent<ShoulderViewOptionsPanel>();
            options.Initialize(cameraRig, sensitivity, fieldOfView, invert, shoulder, reset, sensitivityValue,
                fieldOfViewValue, shoulderValue);

            Text crosshair = CreateText("Crosshair", canvasObject.transform, "+", font, 28, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter);
            SetRect(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(42f, 42f));

            Text proof = CreateText("Proof", canvasObject.transform,
                "PERSPECTIVE  •  WORLD TARGETING  •  STATION SHOP", font, 16, FontStyle.Bold, Cyan,
                TextAnchor.MiddleLeft);
            SetRect(proof.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 38f),
                new Vector2(740f, 46f));

            Image promptBack = CreateImage("InteractionPrompt", canvasObject.transform, Navy,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(420f, 58f));
            Text prompt = CreateText("PromptText", promptBack.transform, string.Empty, font, 19, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            SetRect(prompt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ShoulderShopEconomy economy = canvasObject.AddComponent<ShoulderShopEconomy>();
            economy.Initialize(7);
            ShoulderShopPanel shopPanel = BuildShopOverlay(canvasObject.transform, font, economy, cameraRig, locomotion);

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            return new InterfaceResult { shopPanel = shopPanel, prompt = prompt };
        }

        private static ShoulderShopPanel BuildShopOverlay(Transform canvas, Font font, ShoulderShopEconomy economy,
            ShoulderCameraRig cameraRig, ShoulderLocomotionController locomotion)
        {
            Image overlay = CreateImage("StationShopOverlay", canvas, new Color(0.015f, 0.025f, 0.045f, 0.985f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image header = CreateImage("ShopHeader", overlay.transform, Navy, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -98f), new Vector2(0f, 196f));
            Text station = CreateText("Station", header.transform, "STATION 04  /  UPGRADE DEPOT", font, 18,
                FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            SetRect(station.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -44f),
                new Vector2(560f, 40f));
            Text title = CreateText("ShopTitle", header.transform, "PREPARE THE NEXT LEG", font, 38,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -105f),
                new Vector2(740f, 70f));
            Text bolts = CreateText("Bolts", header.transform, "BOLTS   07", font, 28, FontStyle.Bold, Lime,
                TextAnchor.MiddleRight);
            SetRect(bolts.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f),
                new Vector2(350f, 76f));

            Image closeImage = CreateImage("Close", header.transform, new Color(0.16f, 0.22f, 0.3f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -78f), new Vector2(84f, 60f));
            Button close = closeImage.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            Text closeText = CreateText("Text", closeImage.transform, "X", font, 24, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter);
            SetRect(closeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ShoulderShopOffer[] offers =
            {
                new("CRAFT DRIVE", "Prototype assembly cycle becomes faster.", "CRAFT SPEED", 1f, 0.25f, 2),
                new("CARGO RACK", "Prototype station capacity expands for longer legs.", "CAPACITY", 6f, 2f, 2),
                new("COOLANT LOOP", "Prototype engine heat recovery improves.", "COOLING", 10f, 5f, 3)
            };

            ShoulderShopPanel.OfferView[] views = new ShoulderShopPanel.OfferView[3];
            float[] anchors = { 0.18f, 0.5f, 0.82f };
            for (int index = 0; index < views.Length; index++)
                views[index] = CreateOfferCard(overlay.transform, font, anchors[index], index + 1);

            Text feedback = CreateText("ShopFeedback", overlay.transform, "SELECT AN UPGRADE FOR THE NEXT LEG", font,
                18, FontStyle.Bold, Muted, TextAnchor.MiddleCenter);
            SetRect(feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f),
                new Vector2(920f, 46f));
            Text source = CreateText("DesignNote", overlay.transform,
                "STATION CONTEXT  •  3 CLEAR OFFERS  •  COST + STAT DELTA  •  IMMEDIATE PROTOTYPE EFFECT", font,
                13, FontStyle.Normal, Cyan, TextAnchor.MiddleCenter);
            SetRect(source.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f),
                new Vector2(1000f, 30f));

            ShoulderShopPanel panel = overlay.gameObject.AddComponent<ShoulderShopPanel>();
            panel.Initialize(overlay.gameObject, bolts, feedback, close, views, economy, offers, cameraRig, locomotion);
            return panel;
        }

        private static ShoulderShopPanel.OfferView CreateOfferCard(Transform parent, Font font, float anchorX,
            int sequence)
        {
            Image card = CreateImage($"Offer_{sequence:00}", parent, Panel, new Vector2(anchorX, 0.5f),
                new Vector2(anchorX, 0.5f), new Vector2(0f, -20f), new Vector2(510f, 620f));
            Image number = CreateImage("Number", card.transform, Cyan, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -34f), new Vector2(52f, 52f));
            Text numberText = CreateText("Text", number.transform, sequence.ToString("00"), font, 17,
                FontStyle.Bold, Navy, TextAnchor.MiddleCenter);
            SetRect(numberText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Text tier = CreateText("Tier", card.transform, string.Empty, font, 15, FontStyle.Bold, Muted,
                TextAnchor.MiddleRight);
            SetRect(tier.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -34f),
                new Vector2(190f, 40f));
            Text title = CreateText("Title", card.transform, string.Empty, font, 29, FontStyle.Bold, Color.white,
                TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -112f),
                new Vector2(-56f, 54f));
            Text description = CreateText("Description", card.transform, string.Empty, font, 17, FontStyle.Normal,
                Muted, TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(description.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -182f),
                new Vector2(-56f, 90f));

            Image statBack = CreateImage("StatBack", card.transform, new Color(0.035f, 0.13f, 0.17f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(450f, 160f));
            Text stat = CreateText("Stat", statBack.transform, string.Empty, font, 22, FontStyle.Bold, Cyan,
                TextAnchor.MiddleCenter);
            SetRect(stat.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image buyImage = CreateImage("Buy", card.transform, new Color(0.13f, 0.62f, 0.5f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(450f, 76f));
            Button buy = buyImage.gameObject.AddComponent<Button>();
            buy.targetGraphic = buyImage;
            Text cost = CreateText("Cost", buyImage.transform, string.Empty, font, 19, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter);
            SetRect(cost.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return new ShoulderShopPanel.OfferView
            {
                title = title, description = description, tier = tier, stat = stat, cost = cost, buyButton = buy
            };
        }

        private static ShoulderInteractor BuildShopTerminal(GameObject player, ShoulderCameraRig cameraRig,
            InterfaceResult interfaceResult, Material material)
        {
            Camera camera = cameraRig.GetComponent<Camera>();
            Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 focus = player.transform.position + Vector3.up * 1.6f;
            float distance = Vector3.Distance(camera.transform.position, focus) + 3f;
            Vector3 position = centerRay.GetPoint(distance);
            GameObject terminal = CreatePrimitive("StationShopTerminal", PrimitiveType.Cube,
                new Vector3(position.x, 1.25f, position.z), new Vector3(2.8f, 2.5f, 0.45f), material);
            Vector3 faceCamera = camera.transform.position - terminal.transform.position;
            faceCamera.y = 0f;
            terminal.transform.rotation = Quaternion.LookRotation(faceCamera, Vector3.up);
            ShoulderShopTerminal shopTerminal = terminal.AddComponent<ShoulderShopTerminal>();
            shopTerminal.Initialize(interfaceResult.shopPanel);

            GameObject sign = CreatePrimitive("ShopSign", PrimitiveType.Cube, Vector3.zero,
                new Vector3(1.4f, 0.32f, 0.08f), material);
            sign.transform.SetParent(terminal.transform, false);
            sign.transform.localPosition = new Vector3(0f, 0.55f, -0.56f);
            Object.DestroyImmediate(sign.GetComponent<Collider>());

            ShoulderInteractor interactor = player.AddComponent<ShoulderInteractor>();
            interactor.Initialize(camera, interfaceResult.prompt, 9f);
            return interactor;
        }

        private sealed class InterfaceResult
        {
            public ShoulderShopPanel shopPanel;
            public Text prompt;
        }

        private static Slider CreateSliderRow(Transform parent, Font font, string label, float minimum, float maximum,
            float value, float y, out Text valueText)
        {
            Text labelText = CreateText(label + " Label", parent, label, font, 17, FontStyle.Bold, Color.white,
                TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y),
                new Vector2(-140f, 32f));
            valueText = CreateText(label + " Value", parent, string.Empty, font, 17, FontStyle.Bold, Cyan,
                TextAnchor.MiddleRight);
            SetRect(valueText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, y),
                new Vector2(100f, 32f));

            GameObject sliderObject = new(label + " Slider", typeof(RectTransform));
            sliderObject.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetRect(sliderRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y - 42f),
                new Vector2(-48f, 24f));
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = value;

            Image background = CreateImage("Background", sliderObject.transform, new Color(0.12f, 0.18f, 0.24f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.rectTransform.offsetMin = new Vector2(0f, 8f);
            background.rectTransform.offsetMax = new Vector2(0f, -8f);
            Image fill = CreateImage("Fill", sliderObject.transform, Cyan, Vector2.zero, new Vector2(0.7f, 1f),
                Vector2.zero, Vector2.zero);
            fill.rectTransform.offsetMin = new Vector2(0f, 8f);
            fill.rectTransform.offsetMax = new Vector2(0f, -8f);
            Image handle = CreateImage("Handle", sliderObject.transform, Color.white, new Vector2(0.7f, 0.5f),
                new Vector2(0.7f, 0.5f), Vector2.zero, new Vector2(18f, 32f));
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        private static Toggle CreateToggleRow(Transform parent, Font font, string label, float y)
        {
            Text labelText = CreateText(label + " Label", parent, label, font, 17, FontStyle.Bold, Color.white,
                TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y),
                new Vector2(-90f, 42f));
            Image background = CreateImage(label + " Toggle", parent, new Color(0.12f, 0.18f, 0.24f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, y), new Vector2(48f, 30f));
            Toggle toggle = background.gameObject.AddComponent<Toggle>();
            Image check = CreateImage("Checkmark", background.transform, Lime, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 18f));
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }

        private static Button CreateButtonRow(Transform parent, Font font, string label, string buttonText, float y,
            out Text valueText)
        {
            Text labelText = CreateText(label + " Label", parent, label, font, 17, FontStyle.Bold, Color.white,
                TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y),
                new Vector2(-190f, 44f));
            valueText = CreateText(label + " Value", parent, string.Empty, font, 15, FontStyle.Bold, Cyan,
                TextAnchor.MiddleRight);
            SetRect(valueText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-128f, y),
                new Vector2(90f, 44f));
            Image buttonImage = CreateImage(label + " Button", parent, new Color(0.1f, 0.55f, 0.62f, 1f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, y), new Vector2(88f, 40f));
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            Text text = CreateText("Text", buttonImage.transform, buttonText, font, 14, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void CreateDivider(Transform parent, float y)
        {
            Image divider = CreateImage("Divider", parent, new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y), new Vector2(-48f, 2f));
            divider.raycastTarget = false;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Material material)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.position = position;
            value.transform.localScale = scale;
            if (material != null)
                value.GetComponent<Renderer>().sharedMaterial = material;
            return value;
        }

        private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            value.transform.SetParent(parent, false);
            Image image = value.GetComponent<Image>();
            image.color = color;
            SetRect(image.rectTransform, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            return image;
        }

        private static Text CreateText(string name, Transform parent, string content, Font font, int size,
            FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            value.transform.SetParent(parent, false);
            Text text = value.GetComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static ShoulderViewSettings LoadOrCreateSettings()
        {
            ShoulderViewSettings settings = AssetDatabase.LoadAssetAtPath<ShoulderViewSettings>(SettingsPath);
            if (settings != null)
                return settings;
            settings = ScriptableObject.CreateInstance<ShoulderViewSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static Material LoadOrCreateMaterial(string name, Color color)
        {
            string path = $"{DemoRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DemoRoot))
                AssetDatabase.CreateFolder("Assets/01.hansol/ShoulderView", "Demo");
            string editorFolder = Path.GetDirectoryName(ScenePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(editorFolder) || !AssetDatabase.IsValidFolder(editorFolder))
                throw new DirectoryNotFoundException(DemoRoot);
        }
    }
}
