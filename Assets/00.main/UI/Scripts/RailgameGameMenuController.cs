using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Railgame.UI
{
    public sealed class RailgameGameMenuController : MonoBehaviour
    {
        private enum MenuState
        {
            Playing,
            Paused,
            Settings,
            Shopping
        }

        [SerializeField] private GameObject pausePanel;
        [SerializeField] private RailgameSettingsPanel settingsPanel;
        [SerializeField] private RailgameShopScreen shopScreen;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button openShopButton;
        [SerializeField] private string lobbySceneName = "Railgame_Lobby";

        private MenuState state;

        private void Awake()
        {
            RailgameSettingsPanel.ApplySavedSettings();
            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(OpenSettings);
            restartButton?.onClick.AddListener(Restart);
            lobbyButton?.onClick.AddListener(ReturnToLobby);
            quitButton?.onClick.AddListener(Quit);
            openShopButton?.onClick.AddListener(OpenShop);
            if (settingsPanel != null) settingsPanel.Closed += ReturnFromSettings;
            if (shopScreen != null) shopScreen.Closed += Resume;
            SetState(MenuState.Playing);
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            switch (state)
            {
                case MenuState.Playing: SetState(MenuState.Paused); break;
                case MenuState.Paused: Resume(); break;
                case MenuState.Settings: settingsPanel?.CancelAndClose(); break;
                case MenuState.Shopping: shopScreen?.Close(); break;
            }
        }

        private void OnDestroy()
        {
            if (settingsPanel != null) settingsPanel.Closed -= ReturnFromSettings;
            if (shopScreen != null) shopScreen.Closed -= Resume;
            Time.timeScale = 1f;
        }

        public void OpenShop() => SetState(MenuState.Shopping);
        public void Resume() => SetState(MenuState.Playing);

        private void OpenSettings()
        {
            state = MenuState.Settings;
            if (pausePanel != null) pausePanel.SetActive(false);
            settingsPanel?.Open();
        }

        private void ReturnFromSettings() => SetState(MenuState.Paused);

        private void SetState(MenuState next)
        {
            state = next;
            if (pausePanel != null) pausePanel.SetActive(next == MenuState.Paused);
            if (settingsPanel != null && next != MenuState.Settings) settingsPanel.gameObject.SetActive(false);
            if (shopScreen != null && next != MenuState.Shopping) shopScreen.gameObject.SetActive(false);
            if (next == MenuState.Shopping) shopScreen?.Open();

            bool frozen = next != MenuState.Playing;
            Time.timeScale = frozen ? 0f : 1f;
            Cursor.visible = frozen;
            Cursor.lockState = frozen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private static void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ReturnToLobby()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(lobbySceneName);
        }

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
