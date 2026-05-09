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

        [Header("Status")]
        public TMP_Text statusText;
        public GameObject loadingOverlay;

        [Header("Scenes")]
        public string lobbySceneName = "02_Lobby";

        void Awake()
        {
            createButton.onClick.AddListener(() => _ = OnCreate());
            joinButton.onClick.AddListener(() => _ = OnJoin());
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
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
    }
}