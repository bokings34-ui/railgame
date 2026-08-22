using UnityEngine;
using UnityEngine.UI;

namespace Railgame.Hansol.ShoulderView
{
    public sealed class ShoulderViewOptionsPanel : MonoBehaviour
    {
        private const string SensitivityKey = "Railgame.ShoulderView.Sensitivity";
        private const string FieldOfViewKey = "Railgame.ShoulderView.FieldOfView";
        private const string InvertYKey = "Railgame.ShoulderView.InvertY";
        private const string RightShoulderKey = "Railgame.ShoulderView.RightShoulder";

        [SerializeField] private ShoulderCameraRig cameraRig;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider fieldOfViewSlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Button shoulderButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Text sensitivityValue;
        [SerializeField] private Text fieldOfViewValue;
        [SerializeField] private Text shoulderValue;

        private bool bound;

        public void Initialize(ShoulderCameraRig rig, Slider sensitivity, Slider fieldOfView, Toggle invertY,
            Button shoulder, Button reset, Text sensitivityLabel, Text fieldOfViewLabel, Text shoulderLabel)
        {
            Unbind();
            cameraRig = rig;
            sensitivitySlider = sensitivity;
            fieldOfViewSlider = fieldOfView;
            invertYToggle = invertY;
            shoulderButton = shoulder;
            resetButton = reset;
            sensitivityValue = sensitivityLabel;
            fieldOfViewValue = fieldOfViewLabel;
            shoulderValue = shoulderLabel;
            Bind();
            LoadPreferences();
        }

        private void Start()
        {
            Bind();
            LoadPreferences();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void SetSensitivity(float value)
        {
            if (cameraRig == null)
                return;
            cameraRig.SetLookSensitivityMultiplier(value);
            if (sensitivityValue != null)
                sensitivityValue.text = $"{cameraRig.LookSensitivityMultiplier:F2}x";
            PlayerPrefs.SetFloat(SensitivityKey, cameraRig.LookSensitivityMultiplier);
        }

        public void SetFieldOfView(float value)
        {
            if (cameraRig == null)
                return;
            cameraRig.SetFieldOfView(value);
            if (fieldOfViewValue != null)
                fieldOfViewValue.text = $"{cameraRig.EffectiveFieldOfView:F0}°";
            PlayerPrefs.SetFloat(FieldOfViewKey, cameraRig.EffectiveFieldOfView);
        }

        public void SetInvertY(bool value)
        {
            if (cameraRig == null)
                return;
            cameraRig.SetInvertVerticalLook(value);
            PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
        }

        public void ToggleShoulder()
        {
            if (cameraRig == null)
                return;
            SetRightShoulder(!cameraRig.IsRightShoulder);
        }

        public void ResetToDefaults()
        {
            ApplyPreferences(1f, 62f, false, true, true);
        }

        public void SavePreferences()
        {
            PlayerPrefs.Save();
        }

        private void LoadPreferences()
        {
            if (cameraRig == null)
                return;
            ApplyPreferences(PlayerPrefs.GetFloat(SensitivityKey, 1f),
                PlayerPrefs.GetFloat(FieldOfViewKey, 62f), PlayerPrefs.GetInt(InvertYKey, 0) != 0,
                PlayerPrefs.GetInt(RightShoulderKey, 1) != 0, false);
        }

        private void ApplyPreferences(float sensitivity, float fieldOfView, bool invertY, bool rightShoulder,
            bool save)
        {
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(sensitivity);
            if (fieldOfViewSlider != null)
                fieldOfViewSlider.SetValueWithoutNotify(fieldOfView);
            if (invertYToggle != null)
                invertYToggle.SetIsOnWithoutNotify(invertY);

            SetSensitivity(sensitivity);
            SetFieldOfView(fieldOfView);
            SetInvertY(invertY);
            SetRightShoulder(rightShoulder);
            if (save)
                SavePreferences();
        }

        private void SetRightShoulder(bool value)
        {
            if (cameraRig == null)
                return;
            cameraRig.SetRightShoulder(value);
            if (shoulderValue != null)
                shoulderValue.text = value ? "RIGHT" : "LEFT";
            PlayerPrefs.SetInt(RightShoulderKey, value ? 1 : 0);
            cameraRig.SnapToTarget();
        }

        private void Bind()
        {
            if (bound)
                return;
            sensitivitySlider?.onValueChanged.AddListener(SetSensitivity);
            fieldOfViewSlider?.onValueChanged.AddListener(SetFieldOfView);
            invertYToggle?.onValueChanged.AddListener(SetInvertY);
            shoulderButton?.onClick.AddListener(ToggleShoulder);
            resetButton?.onClick.AddListener(ResetToDefaults);
            bound = true;
        }

        private void Unbind()
        {
            if (!bound)
                return;
            sensitivitySlider?.onValueChanged.RemoveListener(SetSensitivity);
            fieldOfViewSlider?.onValueChanged.RemoveListener(SetFieldOfView);
            invertYToggle?.onValueChanged.RemoveListener(SetInvertY);
            shoulderButton?.onClick.RemoveListener(ToggleShoulder);
            resetButton?.onClick.RemoveListener(ResetToDefaults);
            bound = false;
        }
    }
}
