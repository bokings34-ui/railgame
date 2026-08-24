using UnityEngine;
using UnityEngine.InputSystem;

namespace Railgame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class RailgamePlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float waterSpeedMultiplier = 0.45f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -24f;

        private readonly Collider[] waterOverlapBuffer = new Collider[16];
        private CharacterController characterController;
        private float verticalVelocity;
        private bool isInWater;

        public bool IsInWater => isInWater;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public float EffectiveMoveSpeed => moveSpeed * (IsInWater ? waterSpeedMultiplier : 1f);
        public float MoveSpeed => moveSpeed;
        public float WaterSpeedMultiplier => waterSpeedMultiplier;
        public float JumpHeight => jumpHeight;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                SimulateInput(Vector2.zero, false, Time.deltaTime);
                return;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            SimulateInput(input, keyboard.spaceKey.wasPressedThisFrame, Time.deltaTime);
        }

        public void SimulateInput(Vector2 input, bool jumpPressed, float deltaTime)
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (deltaTime <= 0f)
                return;

            RefreshWaterState();

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (jumpPressed && characterController.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            Vector3 planar = new(input.x, 0f, input.y);
            if (planar.sqrMagnitude > 1f)
                planar.Normalize();
            if (planar.sqrMagnitude > 0.001f)
                transform.forward = planar;

            verticalVelocity += gravity * deltaTime;
            characterController.Move((planar * EffectiveMoveSpeed + Vector3.up * verticalVelocity) * deltaTime);
        }

        private void RefreshWaterState()
        {
            Vector3 center = transform.TransformPoint(characterController.center);
            float halfSegment = Mathf.Max(0f, characterController.height * 0.5f - characterController.radius);
            int count = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * halfSegment,
                center - Vector3.up * halfSegment, characterController.radius, waterOverlapBuffer,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            isInWater = false;
            for (int index = 0; index < count; index++)
                if (waterOverlapBuffer[index] != null && waterOverlapBuffer[index].GetComponent<WaterSlowVolume>() != null)
                {
                    isInWater = true;
                    break;
                }
        }
    }
}
