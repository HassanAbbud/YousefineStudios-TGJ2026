using Lobby;
using Networked;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Shown when all objectives complete. Both players see it simultaneously.
    /// </summary>
    public class WinScreenUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;                  // The container (disabled by default)
        public TMP_Text titleLabel;
        public TMP_Text subtitleLabel;
        public Button mainMenuButton;
        public Button quitButton;

        [Header("Scene")]
        public string mainMenuScene = "01_MainMenu";

        void Awake()
        {
            if (root != null) root.SetActive(false);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        public void Show()
        {
            if (root != null) root.SetActive(true);
            if (titleLabel != null) titleLabel.text = "YOU GOT AWAY WITH IT";
            if (subtitleLabel != null) subtitleLabel.text = "All evidence hidden. The perfect crime.";

            // Free the cursor so players can click the buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable in-game input by pausing the simulation? No — let it run, but
            // the buttons are above all other UI, so clicks land here.
            // Mark time-scale: pause makes the world freeze, which is appropriate for an end screen.
            Time.timeScale = 0f;
        }

        void OnMainMenu()
        {
            // Always reset time scale before changing scenes
            Time.timeScale = 1f;

            if (LobbyManager.Instance != null)
                _ = LobbyManager.Instance.LeaveLobbyAsync();

            if (GameNetworkManager.Instance != null)
                GameNetworkManager.Instance.Shutdown();

            SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
        }

        void OnQuit()
        {
            Time.timeScale = 1f;

            if (GameNetworkManager.Instance != null)
                GameNetworkManager.Instance.Shutdown();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}