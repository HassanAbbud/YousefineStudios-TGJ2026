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
    /// Shown when an NPC catches Player 1 (or sees too much evidence).
    /// Both players see it simultaneously.
    /// </summary>
    public class LoseScreenUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;
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
            if (titleLabel != null) titleLabel.text = "YOU'VE BEEN CAUGHT";
            if (subtitleLabel != null) subtitleLabel.text = "Should've hidden the body better.";

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        void OnMainMenu()
        {
            Time.timeScale = 1f;

            if (Lobby.VoiceChatManager.Instance != null)
                _ = Lobby.VoiceChatManager.Instance.LeaveChannelAsync();

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