
using UnityEngine;

namespace Lobby
{
    /// <summary>
    /// Drop one of these in 03_Game. On Start, joins the Vivox channel
    /// matching the current lobby ID. Both players auto-end-up in the same channel.
    /// </summary>
    public class VoiceChatAutoJoin : MonoBehaviour
    {
        async void Start()
        {
            if (LobbyManager.Instance == null || LobbyManager.Instance.CurrentLobby == null)
            {
                Debug.LogWarning("[VoiceChatAutoJoin] No lobby found — skipping voice join");
                return;
            }

            if (VoiceChatManager.Instance == null)
            {
                Debug.LogWarning("[VoiceChatAutoJoin] No VoiceChatManager found");
                return;
            }

            string lobbyId = LobbyManager.Instance.CurrentLobby.Id;
            await VoiceChatManager.Instance.JoinLobbyChannelAsync(lobbyId);
        }
    }
}