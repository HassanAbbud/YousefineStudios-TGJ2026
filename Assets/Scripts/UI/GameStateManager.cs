using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public enum GameEndReason
    {
        None = 0,
        AllObjectivesComplete = 1,    // Win
        Caught = 2,                    // Lose: NPC caught Player 1
        Disconnected = 3               // Handled by DisconnectHandler, included for completeness
    }

    /// <summary>
    /// Networked singleton that tracks game-end state.
    /// Server triggers Win/Lose; broadcasts to all clients via NetworkVariable.
    /// Place ONE in 03_Game with a NetworkObject component.
    /// </summary>
    public class GameStateManager : NetworkBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public NetworkVariable<int> EndReason = new NetworkVariable<int>(
            (int)GameEndReason.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool GameEnded => EndReason.Value != (int)GameEndReason.None;

        // Local UI references — wired in inspector. Each client has its own copy.
        [Header("UI Screens (assigned in scene)")]
        [SerializeField] WinScreenUI winScreen;
        [SerializeField] LoseScreenUI loseScreen;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            EndReason.OnValueChanged += HandleEndReasonChanged;
            // If we late-joined and the value already changed, apply now
            if (EndReason.Value != (int)GameEndReason.None)
                HandleEndReasonChanged(0, EndReason.Value);
        }

        public override void OnNetworkDespawn()
        {
            EndReason.OnValueChanged -= HandleEndReasonChanged;
        }

        void HandleEndReasonChanged(int prev, int now)
        {
            var reason = (GameEndReason)now;
            switch (reason)
            {
                case GameEndReason.AllObjectivesComplete:
                    if (winScreen != null) winScreen.Show();
                    break;
                case GameEndReason.Caught:
                    if (loseScreen != null) loseScreen.Show();
                    break;
            }
        }

        // ===== Server-only triggers =====

        /// <summary>Call from ObjectiveManager when all 3 objectives are complete.</summary>
        public void TriggerWin()
        {
            if (!IsServer) { TriggerWinServerRpc(); return; }
            if (GameEnded) return;
            EndReason.Value = (int)GameEndReason.AllObjectivesComplete;
        }

        /// <summary>Call from NPCController.OnAlarmed when NPC catches Player 1.</summary>
        public void TriggerLose()
        {
            if (!IsServer) { TriggerLoseServerRpc(); return; }
            if (GameEnded) return;
            EndReason.Value = (int)GameEndReason.Caught;
        }

        [ServerRpc(RequireOwnership = false)]
        void TriggerWinServerRpc(ServerRpcParams rpcParams = default)
        {
            if (GameEnded) return;
            EndReason.Value = (int)GameEndReason.AllObjectivesComplete;
        }

        [ServerRpc(RequireOwnership = false)]
        void TriggerLoseServerRpc(ServerRpcParams rpcParams = default)
        {
            if (GameEnded) return;
            EndReason.Value = (int)GameEndReason.Caught;
        }
    }
}