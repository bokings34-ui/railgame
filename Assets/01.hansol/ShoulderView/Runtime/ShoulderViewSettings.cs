using UnityEngine;

namespace Railgame.Hansol.ShoulderView
{
    [CreateAssetMenu(menuName = "Railgame/Hansol/Shoulder View Settings", fileName = "ShoulderViewSettings")]
    public sealed class ShoulderViewSettings : ScriptableObject
    {
        [Header("Locomotion")]
        [Min(0.1f)] [SerializeField] private float moveSpeed = 5f;
        [Min(1f)] [SerializeField] private float sprintMultiplier = 1.45f;
        [Min(0.1f)] [SerializeField] private float jumpHeight = 1.15f;
        [Min(0.1f)] [SerializeField] private float gravityMagnitude = 24f;
        [Min(1f)] [SerializeField] private float rotationSpeed = 720f;

        [Header("Shoulder camera")]
        [Min(0f)] [SerializeField] private float pivotHeight = 1.55f;
        [Min(0.5f)] [SerializeField] private float cameraDistance = 4f;
        [SerializeField] private float shoulderOffset = 0.65f;
        [Range(30f, 100f)] [SerializeField] private float fieldOfView = 62f;
        [Range(-80f, 0f)] [SerializeField] private float minimumPitch = -35f;
        [Range(0f, 80f)] [SerializeField] private float maximumPitch = 65f;
        [SerializeField] private float initialPitch = 12f;
        [Min(0.001f)] [SerializeField] private float lookSensitivity = 0.08f;
        [Min(0f)] [SerializeField] private float positionSharpness = 18f;
        [Min(0f)] [SerializeField] private float rotationSharpness = 24f;

        [Header("Camera collision")]
        [Min(0.01f)] [SerializeField] private float collisionRadius = 0.2f;
        [Min(0f)] [SerializeField] private float collisionPadding = 0.08f;
        [Min(0.05f)] [SerializeField] private float minimumCameraDistance = 0.35f;
        [SerializeField] private LayerMask collisionMask = ~0;

        public float MoveSpeed => moveSpeed;
        public float SprintMultiplier => sprintMultiplier;
        public float JumpHeight => jumpHeight;
        public float Gravity => -gravityMagnitude;
        public float RotationSpeed => rotationSpeed;
        public float PivotHeight => pivotHeight;
        public float CameraDistance => cameraDistance;
        public float ShoulderOffset => shoulderOffset;
        public float FieldOfView => fieldOfView;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public float InitialPitch => Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
        public float LookSensitivity => lookSensitivity;
        public float PositionSharpness => positionSharpness;
        public float RotationSharpness => rotationSharpness;
        public float CollisionRadius => collisionRadius;
        public float CollisionPadding => collisionPadding;
        public float MinimumCameraDistance => Mathf.Min(minimumCameraDistance, cameraDistance);
        public int CollisionMask => collisionMask.value;

        private void OnValidate()
        {
            gravityMagnitude = Mathf.Max(0.1f, gravityMagnitude);
            maximumPitch = Mathf.Max(0f, maximumPitch);
            minimumPitch = Mathf.Min(0f, minimumPitch);
            initialPitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
            minimumCameraDistance = Mathf.Min(minimumCameraDistance, cameraDistance);
        }
    }
}
