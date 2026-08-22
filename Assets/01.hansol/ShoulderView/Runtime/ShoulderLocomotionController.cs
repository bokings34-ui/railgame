using UnityEngine;
using UnityEngine.InputSystem;

namespace Railgame.Hansol.ShoulderView
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ShoulderLocomotionController : MonoBehaviour
    {
        [SerializeField] private Transform orientationSource;
        [SerializeField] private ShoulderViewSettings settings;
        [SerializeField] private bool readKeyboardInput = true;

        private CharacterController characterController;
        private float verticalVelocity;
        private bool ownsSettings;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public float VerticalVelocity => verticalVelocity;
        public Transform OrientationSource => orientationSource;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            EnsureSettings();
        }

        private void Update()
        {
            if (!readKeyboardInput)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                SimulateInput(Vector2.zero, false, false, Time.deltaTime);
                return;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            bool sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            SimulateInput(input, keyboard.spaceKey.wasPressedThisFrame, sprint, Time.deltaTime);
        }

        public void SetOrientationSource(Transform value)
        {
            orientationSource = value;
        }

        public void SetSettings(ShoulderViewSettings value)
        {
            if (settings != value)
            {
                ReleaseOwnedSettings();
                settings = value;
            }
            EnsureSettings();
        }

        private void OnDestroy()
        {
            ReleaseOwnedSettings();
        }

        public void SetKeyboardInputEnabled(bool value)
        {
            readKeyboardInput = value;
        }

        public void ResetVerticalVelocity()
        {
            verticalVelocity = 0f;
        }

        public void SimulateInput(Vector2 input, bool jumpPressed, bool sprint, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            EnsureSettings();

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (jumpPressed && characterController.isGrounded)
                verticalVelocity = Mathf.Sqrt(settings.JumpHeight * -2f * settings.Gravity);

            Vector3 planar = CameraRelativeDirection(input);
            if (planar.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(planar, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    settings.RotationSpeed * deltaTime);
            }

            float speed = settings.MoveSpeed * (sprint ? settings.SprintMultiplier : 1f);
            verticalVelocity += settings.Gravity * deltaTime;
            characterController.Move((planar * speed + Vector3.up * verticalVelocity) * deltaTime);
        }

        public Vector3 CameraRelativeDirection(Vector2 input)
        {
            Vector3 forward = orientationSource != null ? orientationSource.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = right * input.x + forward * input.y;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void EnsureSettings()
        {
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ShoulderViewSettings>();
                settings.hideFlags = HideFlags.HideAndDontSave;
                ownsSettings = true;
            }
        }

        private void ReleaseOwnedSettings()
        {
            if (!ownsSettings || settings == null)
                return;

            ShoulderViewSettings owned = settings;
            settings = null;
            ownsSettings = false;
            if (Application.isPlaying)
                Destroy(owned);
            else
                DestroyImmediate(owned);
        }
    }
}
