using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lobby
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Header")]
        public TMP_Text lobbyNameLabel;
        public TMP_Text lobbyCodeLabel;
        public Button copyCodeButton;

        [Header("Role Selection")]
        public LobbySlotUI player1Slot; // First-person
        public LobbySlotUI player2Slot; // Camera operator

        [Header("Controls")]
        public Button startGameButton;   // host only
        public Button leaveButton;
        public TMP_Text statusText;

        [Header("Scenes")]
        public string mainMenuScene = "01_MainMenu";
        public string gameScene = "03_Game";

        void Awake()
        {
            // Lobby is a UI scene — make sure the cursor is free.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            player1Slot.Setup(PlayerRole.Player1_FirstPerson, "PLAYER 1\nFIRST PERSON");
            player2Slot.Setup(PlayerRole.Player2_Camera, "PLAYER 2\nCAMERA OPERATOR");

            startGameButton.onClick.AddListener(OnStartGame);
            leaveButton.onClick.AddListener(OnLeave);
            if (copyCodeButton != null) copyCodeButton.onClick.AddListener(CopyCodeToClipboard);
        }

        void OnEnable()
        {
            LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdated;
            LobbyManager.Instance.OnGameStarting += HandleGameStarting;
        }

        void OnDisable()
        {
            if (LobbyManager.Instance == null) return;
            LobbyManager.Instance.OnLobbyUpdated -= HandleLobbyUpdated;
            LobbyManager.Instance.OnGameStarting -= HandleGameStarting;
        }

        void Start()
        {
            HandleLobbyUpdated(LobbyManager.Instance.CurrentLobby);
        }

        void HandleLobbyUpdated(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            if (lobby == null) return;

            lobbyNameLabel.text = lobby.Name;
            lobbyCodeLabel.text = $"CODE: {lobby.LobbyCode}";

            string myId = AuthenticationService.Instance.PlayerId;
            bool isHost = lobby.HostId == myId;

            player1Slot.Refresh(lobby, myId);
            player2Slot.Refresh(lobby, myId);

            startGameButton.gameObject.SetActive(isHost);
            startGameButton.interactable = isHost && LobbyManager.Instance.AllRolesAssigned();

            if (statusText != null)
            {
                if (lobby.Players.Count < 2)
                    statusText.text = "Waiting for second player...";
                else if (!LobbyManager.Instance.AllRolesAssigned())
                    statusText.text = "Both players must pick a different role.";
                else
                    statusText.text = isHost ? "Ready to start!" : "Waiting for host to start...";
            }
        }

        void HandleGameStarting()
        {
            statusText.text = "Starting game...";
            startGameButton.interactable = false;
        }

        async void OnStartGame()
        {
            startGameButton.interactable = false;
            await LobbyManager.Instance.HostStartGameAsync();

            // Wait one frame so NetworkManager.SceneManager is ready
            await System.Threading.Tasks.Task.Yield();

            // Load via Netcode's scene manager so all clients follow
            if (Unity.Netcode.NetworkManager.Singleton != null
                && Unity.Netcode.NetworkManager.Singleton.SceneManager != null)
            {
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(
                    gameScene,
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("[LobbyUI] NetworkManager.SceneManager not ready!");
            }
        }

        async void OnLeave()
        {
            // Make sure the cursor is free before bouncing back to the main menu.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            await LobbyManager.Instance.LeaveLobbyAsync();
            GameNetworkManager.Instance.Shutdown();
            SceneManager.LoadScene(mainMenuScene);
        }

        void CopyCodeToClipboard()
        {
            if (LobbyManager.Instance.CurrentLobby == null) return;
            GUIUtility.systemCopyBuffer = LobbyManager.Instance.CurrentLobby.LobbyCode;
            if (statusText != null) statusText.text = "Code copied!";
        }
    }
}