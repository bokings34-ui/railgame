using System;
using UnityEngine;
using UnityEngine.UI;

namespace Railgame.UI
{
    public sealed class RailgameSettingsPanel : MonoBehaviour
    {
        private const string VolumeKey = "Railgame.MasterVolume";
        private const string FullscreenKey = "Railgame.Fullscreen";
        private const string VSyncKey = "Railgame.VSync";
        private const string QualityKey = "Railgame.Quality";

        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueText;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button defaultsButton;

        private Snapshot snapshot;

        public event Action Closed;

        private void Awake()
        {
            applyButton?.onClick.AddListener(ApplyAndClose);
            cancelButton?.onClick.AddListener(CancelAndClose);
            defaultsButton?.onClick.AddListener(LoadDefaults);
            volumeSlider?.onValueChanged.AddListener(RefreshVolumeLabel);
        }

        private void OnDestroy()
        {
            applyButton?.onClick.RemoveListener(ApplyAndClose);
            cancelButton?.onClick.RemoveListener(CancelAndClose);
            defaultsButton?.onClick.RemoveListener(LoadDefaults);
            volumeSlider?.onValueChanged.RemoveListener(RefreshVolumeLabel);
        }

        public static void ApplySavedSettings()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey, 1) != 0 ? 1 : 0;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
                    0, QualitySettings.names.Length - 1));
        }

        public void Open()
        {
            snapshot = CaptureCurrent();
            LoadSavedValues();
            gameObject.SetActive(true);
        }

        public void CancelAndClose()
        {
            Restore(snapshot);
            Close();
        }

        private void ApplyAndClose()
        {
            float volume = volumeSlider == null ? AudioListener.volume : volumeSlider.value;
            bool fullscreen = fullscreenToggle != null && fullscreenToggle.isOn;
            bool vSync = vSyncToggle != null && vSyncToggle.isOn;
            int quality = qualityDropdown == null ? QualitySettings.GetQualityLevel() : qualityDropdown.value;

            AudioListener.volume = volume;
            Screen.fullScreen = fullscreen;
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1));

            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSyncKey, vSync ? 1 : 0);
            PlayerPrefs.SetInt(QualityKey, quality);
            PlayerPrefs.Save();
            Close();
        }

        private void LoadDefaults()
        {
            if (volumeSlider != null) volumeSlider.value = 1f;
            if (fullscreenToggle != null) fullscreenToggle.isOn = true;
            if (vSyncToggle != null) vSyncToggle.isOn = true;
            if (qualityDropdown != null) qualityDropdown.value = Mathf.Max(0, QualitySettings.names.Length - 1);
        }

        private void LoadSavedValues()
        {
            if (volumeSlider != null) volumeSlider.value = PlayerPrefs.GetFloat(VolumeKey, AudioListener.volume);
            if (fullscreenToggle != null) fullscreenToggle.isOn = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
            if (vSyncToggle != null) vSyncToggle.isOn = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) != 0;
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.value = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
                    0, Mathf.Max(0, QualitySettings.names.Length - 1));
            }
            RefreshVolumeLabel(volumeSlider == null ? AudioListener.volume : volumeSlider.value);
        }

        private void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void RefreshVolumeLabel(float value)
        {
            if (volumeValueText != null)
                volumeValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private static Snapshot CaptureCurrent() => new()
        {
            Volume = AudioListener.volume,
            Fullscreen = Screen.fullScreen,
            VSync = QualitySettings.vSyncCount > 0,
            Quality = QualitySettings.GetQualityLevel()
        };

        private static void Restore(Snapshot value)
        {
            AudioListener.volume = value.Volume;
            Screen.fullScreen = value.Fullscreen;
            QualitySettings.vSyncCount = value.VSync ? 1 : 0;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(value.Quality, 0, QualitySettings.names.Length - 1));
        }

        private struct Snapshot
        {
            public float Volume;
            public bool Fullscreen;
            public bool VSync;
            public int Quality;
        }
    }
}
