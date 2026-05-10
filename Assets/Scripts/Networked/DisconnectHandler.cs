using Lobby;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networked
{
    /// <summary>
    /// Listens for any client disconnect. If the host disconnects OR the other client disconnects,
    /// shut down Netcode and load the main menu. Place ONE of these in 03_Game.
    /// </summary>
    public class DisconnectHandler : MonoBehaviour
    {
        [SerializeField] string mainMenuScene = "01_MainMenu";

        bool gameEnding;

        void Start()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }

        void OnClientDisconnect(ulong clientId)
        {
            if (gameEnding) return;
            // If we're a client and we got disconnected from the host, OR if any peer leaves, end the game.
            Debug.Log($"[DisconnectHandler] Client {clientId} disconnected. Ending game.");
            EndGame();
        }

        void OnServerStopped(bool _)
        {
            if (gameEnding) return;
            EndGame();
        }

        void EndGame()
        {
            gameEnding = true;

            // Leave voice channel before tearing down lobby
            if (Lobby.VoiceChatManager.Instance != null)
                _ = Lobby.VoiceChatManager.Instance.LeaveChannelAsync();

            if (LobbyManager.Instance != null)
                _ = LobbyManager.Instance.LeaveLobbyAsync();

            if (GameNetworkManager.Instance != null)
                GameNetworkManager.Instance.Shutdown();

            SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
        }
    }
}