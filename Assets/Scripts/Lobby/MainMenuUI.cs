using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lobby
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Inputs")]
        public TMP_InputField playerNameInput;
        public TMP_InputField createLobbyNameInput;
        public TMP_InputField joinCodeInput;

        [Header("Buttons")]
        public Button createButton;
        public Button joinButton;
        public Button quitButton;
        public Button creditButton;
        public Button backButton;
        public Button backButton2;
        public Button helpButton;



        [Header("Panels")]
        public GameObject creditPanel;
        public GameObject helpPanel;

        [Header("Status")]
        public TMP_Text statusText;
        public GameObject loadingOverlay;

        [Header("Scenes")]
        public string lobbySceneName = "02_Lobby";

        void Awake()
        {
            // CRITICAL: when we land on the main menu (fresh start, after death,
            // after disconnect, after leaving the lobby), the cursor must be
            // visible and unlocked so the user can click the buttons.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            createButton.onClick.AddListener(() => _ = OnCreate());
            joinButton.onClick.AddListener(() => _ = OnJoin());
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
            quitButton.onClick.AddListener(OnQuit);
            creditButton.onClick.AddListener(Open);
            backButton.onClick.AddListener(Close);
            helpButton.onClick.AddListener(OpenHelp);
            backButton2.onClick.AddListener(CloseHelp);
        }

        void OnEnable()
        {
            // Re-assert in case something else (a stale netcode callback during
            // teardown, etc.) flips the cursor between Awake and Start.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Start()
        {
            // Pre-warm UGS sign-in so the user doesn't wait when they click Create
            _ = GameNetworkManager.Instance.EnsureSignedIn();
        }

        async Task OnCreate()
        {
            SetBusy(true, "Creating lobby...");
            try
            {
                string playerName = playerNameInput != null ? playerNameInput.text : "Host";
                PlayerSession.Instance.SetName(playerName);

                string lobbyName = string.IsNullOrWhiteSpace(createLobbyNameInput?.text)
                    ? $"{playerName}'s Lobby" : createLobbyNameInput.text.Trim();

                await LobbyManager.Instance.CreateLobbyAsync(lobbyName, playerName);
                SceneManager.LoadScene(lobbySceneName);
            }
            catch (System.Exception e)
            {
                SetStatus($"Failed: {e.Message}");
                SetBusy(false);
            }
        }

        async Task OnJoin()
        {
            string code = joinCodeInput != null ? joinCodeInput.text.Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Enter a lobby code.");
                return;
            }

            SetBusy(true, "Joining lobby...");
            try
            {
                string playerName = playerNameInput != null ? playerNameInput.text : "Guest";
                PlayerSession.Instance.SetName(playerName);

                await LobbyManager.Instance.JoinByCodeAsync(code, playerName);
                SceneManager.LoadScene(lobbySceneName);
            }
            catch (System.Exception e)
            {
                SetStatus($"Failed: {e.Message}");
                SetBusy(false);
            }
        }

        void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
            Debug.Log(msg);
        }

        void SetBusy(bool busy, string msg = null)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(busy);
            createButton.interactable = !busy;
            joinButton.interactable = !busy;
            if (msg != null) SetStatus(msg);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Open()
        {
            creditPanel.SetActive(true);
        }

        private void Close()
        {
            creditPanel.SetActive(false);
        }

        private void OpenHelp()
        {
            helpPanel.SetActive(true);
        }

        private void CloseHelp()
        {
            helpPanel.SetActive(false);
        }
    }
}