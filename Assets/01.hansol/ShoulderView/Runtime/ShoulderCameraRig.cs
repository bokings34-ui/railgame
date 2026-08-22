using UnityEngine;
using UnityEngine.InputSystem;

namespace Railgame.Hansol.ShoulderView
{
    [RequireComponent(typeof(Camera))]
    public sealed class ShoulderCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private ShoulderViewSettings settings;
        [SerializeField] private bool readMouseInput = true;

        private Camera view;
        private float yaw;
        private float pitch;
        private float shoulderSign = 1f;
        private bool ownsSettings;

        public Transform Target => target;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public bool IsRightShoulder => shoulderSign > 0f;

        private void Awake()
        {
            view = GetComponent<Camera>();
            EnsureSettings();
            pitch = settings.InitialPitch;
            ApplyProjection();
        }

        private void Update()
        {
            if (!readMouseInput || Mouse.current == null)
                return;

            AddLookInput(Mouse.current.delta.ReadValue());
        }

        private void LateUpdate()
        {
            UpdateRig(Time.deltaTime, false);
        }

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null)
                yaw = target.eulerAngles.y;
        }

        public void SetSettings(ShoulderViewSettings value)
        {
            if (settings != value)
            {
                ReleaseOwnedSettings();
                settings = value;
            }
            EnsureSettings();
            pitch = Mathf.Clamp(pitch, settings.MinimumPitch, settings.MaximumPitch);
            ApplyProjection();
        }

        private void OnDestroy()
        {
            ReleaseOwnedSettings();
        }

        public void SetMouseInputEnabled(bool value)
        {
            readMouseInput = value;
        }

        public void AddLookInput(Vector2 mouseDelta)
        {
            EnsureSettings();
            AddLookDegrees(mouseDelta * settings.LookSensitivity);
        }

        public void AddLookDegrees(Vector2 degrees)
        {
            EnsureSettings();
            yaw += degrees.x;
            pitch = Mathf.Clamp(pitch - degrees.y, settings.MinimumPitch, settings.MaximumPitch);
        }

        public void SwapShoulder()
        {
            shoulderSign *= -1f;
        }

        public void SnapToTarget()
        {
            UpdateRig(0f, true);
        }

        public void UpdateRig(float deltaTime, bool immediate)
        {
            if (target == null)
                return;

            EnsureSettings();
            ApplyProjection();

            Vector3 focus = target.position + Vector3.up * settings.PivotHeight;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 forward = orbit * Vector3.forward;
            Vector3 right = orbit * Vector3.right;
            Vector3 desired = focus - forward * settings.CameraDistance +
                              right * (settings.ShoulderOffset * shoulderSign);
            desired = ResolveCollision(focus, desired);

            Quaternion desiredRotation = Quaternion.LookRotation(focus - desired, Vector3.up);
            if (immediate || deltaTime <= 0f)
            {
                transform.SetPositionAndRotation(desired, desiredRotation);
                return;
            }

            float positionBlend = 1f - Mathf.Exp(-settings.PositionSharpness * deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-settings.RotationSharpness * deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }

        private Vector3 ResolveCollision(Vector3 focus, Vector3 desired)
        {
            Vector3 offset = desired - focus;
            float distance = offset.magnitude;
            if (distance <= Mathf.Epsilon)
                return desired;

            Vector3 direction = offset / distance;
            RaycastHit[] hits = Physics.SphereCastAll(focus, settings.CollisionRadius, direction, distance,
                settings.CollisionMask, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == target || hitTransform.IsChildOf(target))
                    continue;
                nearest = Mathf.Min(nearest, hit.distance);
            }

            if (nearest >= distance)
                return desired;

            float correctedDistance = Mathf.Max(settings.MinimumCameraDistance,
                nearest - settings.CollisionPadding);
            return focus + direction * correctedDistance;
        }

        private void ApplyProjection()
        {
            if (view == null)
                view = GetComponent<Camera>();
            view.orthographic = false;
            view.fieldOfView = settings.FieldOfView;
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
