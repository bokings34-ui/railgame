using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView.Tests
{
    public sealed class ShoulderViewPlayModeTests
    {
        private readonly List<Object> cleanup = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (Object item in cleanup)
                if (item != null)
                    Object.Destroy(item);
            cleanup.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraUsesPerspectiveShoulderOffsetAndAvoidsWalls()
        {
            ShoulderViewSettings settings = Track(ScriptableObject.CreateInstance<ShoulderViewSettings>());
            GameObject target = Track(new GameObject("ShoulderTarget"));
            GameObject cameraObject = Track(new GameObject("ShoulderCamera"));
            Camera view = cameraObject.AddComponent<Camera>();
            ShoulderCameraRig rig = cameraObject.AddComponent<ShoulderCameraRig>();
            rig.SetSettings(settings);
            rig.SetTarget(target.transform);
            rig.SetMouseInputEnabled(false);

            rig.SnapToTarget();
            yield return null;

            Vector3 focus = target.transform.position + Vector3.up * settings.PivotHeight;
            Vector3 unobstructedPosition = cameraObject.transform.position;
            Assert.That(view.orthographic, Is.False);
            Assert.That(view.fieldOfView, Is.EqualTo(settings.FieldOfView).Within(0.01f));
            Assert.That(unobstructedPosition.x, Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(focus, unobstructedPosition), Is.GreaterThan(3.8f));

            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.name = "CameraCollisionWall";
            wall.transform.position = Vector3.Lerp(focus, unobstructedPosition, 0.5f);
            wall.transform.localScale = new Vector3(2f, 3f, 0.35f);
            Physics.SyncTransforms();

            rig.SnapToTarget();
            yield return null;
            Assert.That(Vector3.Distance(focus, cameraObject.transform.position),
                Is.LessThan(Vector3.Distance(focus, unobstructedPosition) - 0.5f));

            wall.SetActive(false);
            Physics.SyncTransforms();
            rig.SwapShoulder();
            rig.SnapToTarget();
            Assert.That(cameraObject.transform.position.x, Is.LessThan(0f));
        }

        [UnityTest]
        public IEnumerator LocomotionMovesAndJumpsRelativeToCameraYaw()
        {
            ShoulderViewSettings settings = Track(ScriptableObject.CreateInstance<ShoulderViewSettings>());
            GameObject ground = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);

            GameObject orientation = Track(new GameObject("CameraOrientation"));
            orientation.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject player = Track(new GameObject("ShoulderPlayer"));
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = Vector3.up;
            ShoulderLocomotionController locomotion = player.AddComponent<ShoulderLocomotionController>();
            locomotion.SetSettings(settings);
            locomotion.SetOrientationSource(orientation.transform);
            locomotion.SetKeyboardInputEnabled(false);
            Physics.SyncTransforms();

            for (int frame = 0; frame < 8; frame++)
            {
                locomotion.SimulateInput(Vector2.zero, false, false, 0.02f);
                yield return null;
            }
            Assert.That(locomotion.IsGrounded, Is.True);

            for (int frame = 0; frame < 50; frame++)
            {
                locomotion.SimulateInput(Vector2.up, false, false, 0.02f);
                yield return null;
            }

            Assert.That(player.transform.position.x, Is.GreaterThan(4.5f));
            Assert.That(Mathf.Abs(player.transform.position.z), Is.LessThan(0.2f));
            Assert.That(Vector3.Dot(player.transform.forward, Vector3.right), Is.GreaterThan(0.95f));

            float jumpStartY = player.transform.position.y;
            float maximumY = jumpStartY;
            locomotion.SimulateInput(Vector2.zero, true, false, 0.02f);
            for (int frame = 0; frame < 35; frame++)
            {
                locomotion.SimulateInput(Vector2.zero, false, false, 0.02f);
                maximumY = Mathf.Max(maximumY, player.transform.position.y);
                yield return null;
            }
            Assert.That(maximumY, Is.GreaterThan(jumpStartY + 0.8f));
        }

        [UnityTest]
        public IEnumerator InteractorTargetsCameraCenterAndInvokesWorldTerminal()
        {
            GameObject cameraObject = Track(new GameObject("InteractionCamera"));
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 1f, -4f);
            cameraObject.transform.rotation = Quaternion.identity;

            GameObject player = Track(new GameObject("InteractorOwner"));
            ShoulderInteractor interactor = player.AddComponent<ShoulderInteractor>();
            interactor.Initialize(camera, null, 8f);

            GameObject target = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            target.name = "WorldShopTerminal";
            target.transform.position = new Vector3(0f, 1f, 2f);
            TestInteractable interactable = target.AddComponent<TestInteractable>();
            Physics.SyncTransforms();
            yield return null;

            Assert.That(interactor.ScanForTarget(), Is.SameAs(interactable));
            Assert.That(interactor.TryInteract(), Is.True);
            Assert.That(interactable.InteractionCount, Is.EqualTo(1));
        }

        [Test]
        public void ShopUpgradeSpendsBoltsAndIncreasesTierAndStat()
        {
            GameObject economyObject = Track(new GameObject("ShopEconomy"));
            ShoulderShopEconomy economy = economyObject.AddComponent<ShoulderShopEconomy>();
            economy.Initialize(7);
            ShoulderShopOffer offer = new("CARGO RACK", "Capacity prototype", "CAPACITY", 6f, 2f, 2);

            Assert.That(offer.TryUpgrade(economy), Is.True);
            Assert.That(economy.Bolts, Is.EqualTo(5));
            Assert.That(offer.Tier, Is.EqualTo(1));
            Assert.That(offer.CurrentValue, Is.EqualTo(8f));
            Assert.That(offer.CurrentCost, Is.EqualTo(3));
        }

        [Test]
        public void CameraOptionsApplyRuntimeValues()
        {
            GameObject cameraObject = Track(new GameObject("OptionsCamera"));
            Camera camera = cameraObject.AddComponent<Camera>();
            ShoulderCameraRig rig = cameraObject.AddComponent<ShoulderCameraRig>();

            rig.SetLookSensitivityMultiplier(2f);
            rig.SetFieldOfView(78f);
            rig.SetInvertVerticalLook(true);
            rig.SetRightShoulder(false);

            Assert.That(rig.LookSensitivityMultiplier, Is.EqualTo(2f));
            Assert.That(camera.fieldOfView, Is.EqualTo(78f).Within(0.01f));
            Assert.That(rig.InvertVerticalLook, Is.True);
            Assert.That(rig.IsRightShoulder, Is.False);
        }

        [Test]
        public void UiThemeAppliesSemanticFallbackWithoutSprites()
        {
            ShoulderUiTheme theme = Track(ScriptableObject.CreateInstance<ShoulderUiTheme>());
            GameObject card = Track(new GameObject("ThemeCard", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image)));
            Image image = card.GetComponent<Image>();
            ShoulderUiSkinElement skin = card.AddComponent<ShoulderUiSkinElement>();
            skin.Initialize(ShoulderUiRole.Card);

            skin.Apply(theme);

            Assert.That(image.sprite, Is.Null);
            Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(image.color, Is.EqualTo(theme.GetColor(ShoulderUiRole.Card)));
        }

        [Test]
        public void UiFocusFeedbackProvidesFocusAndPressedScaleStates()
        {
            GameObject button = Track(new GameObject("FocusButton", typeof(RectTransform)));
            ShoulderUiFocusFeedback feedback = button.AddComponent<ShoulderUiFocusFeedback>();

            feedback.OnSelect(null);
            Assert.That(button.transform.localScale.x, Is.GreaterThan(1f));

            feedback.OnPointerDown(null);
            Assert.That(button.transform.localScale.x, Is.LessThan(1f));

            feedback.OnPointerUp(null);
            feedback.OnDeselect(null);
            Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
        }

        private T Track<T>(T item) where T : Object
        {
            cleanup.Add(item);
            return item;
        }
    }

    public sealed class TestInteractable : MonoBehaviour, IShoulderInteractable
    {
        public string InteractionPrompt => "OPEN TEST SHOP";
        public bool CanInteract => true;
        public int InteractionCount { get; private set; }

        public void Interact(ShoulderInteractor interactor)
        {
            InteractionCount++;
        }
    }
}
