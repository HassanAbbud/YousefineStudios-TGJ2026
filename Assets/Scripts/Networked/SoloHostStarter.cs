using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// TEST-ONLY: auto-starts a host on Play so solo testing works without the lobby flow.
    /// Put this in personal test scenes only — do NOT put in 03_Game.
    /// </summary>
    public class SoloHostStarter : MonoBehaviour
    {
        [Tooltip("Role to use when spawning solo. Set to Player1_FirstPerson for FP testing, Player2_Camera for camera testing.")]
        public Lobby.PlayerRole soloRole = Lobby.PlayerRole.Player1_FirstPerson;

        void Start()
        {
            // Make sure PlayerSession exists with the chosen role
            if (Lobby.PlayerSession.Instance == null)
            {
                var go = new GameObject("_PlayerSession");
                go.AddComponent<Lobby.PlayerSession>();
            }
            Lobby.PlayerSession.Instance.SetRole(soloRole);
            Lobby.PlayerSession.Instance.SetName("SoloDev");

            // Start host (no Relay — direct UTP, fine for local solo testing)
            NetworkManager.Singleton.StartHost();
        }
    }
}