using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Railgame.UI
{
    public sealed class RailgameLobbyController : MonoBehaviour
    {
        [SerializeField] private Button springButton;
        [SerializeField] private Button summerButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private RailgameSettingsPanel settingsPanel;
        [SerializeField] private string springSceneName = "Map_Procedural_Spring";
        [SerializeField] private string summerSceneName = "Map_Procedural_Summer";

        private void Awake()
        {
            RailgameSettingsPanel.ApplySavedSettings();
            springButton?.onClick.AddListener(StartSpring);
            summerButton?.onClick.AddListener(StartSummer);
            settingsButton?.onClick.AddListener(OpenSettings);
            quitButton?.onClick.AddListener(Quit);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            springButton?.onClick.RemoveListener(StartSpring);
            summerButton?.onClick.RemoveListener(StartSummer);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            quitButton?.onClick.RemoveListener(Quit);
        }

        private void StartSpring() => SceneManager.LoadScene(springSceneName);
        private void StartSummer() => SceneManager.LoadScene(summerSceneName);
        private void OpenSettings() => settingsPanel?.Open();

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
