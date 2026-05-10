using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// Generates and stores the labyrinth door's 2-digit code, split into 2 fragments.
    /// </summary>
    public class CodeFragmentManager : NetworkBehaviour
    {
        public static CodeFragmentManager Instance { get; private set; }

        // Index 0 = tens, 1 = ones
        public NetworkVariable<int> Digit0 = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Digit1 = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> Collected0 = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Collected1 = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                int fullCode = Random.Range(0, 100); // 00-99
                Digit0.Value = (fullCode / 10) % 10;
                Digit1.Value = fullCode % 10;
                Debug.Log($"[CodeFragmentManager] Labyrinth code: {Digit0.Value}{Digit1.Value}");
            }
        }

        /// <summary>Returns the full 2-digit code as an int (0-99).</summary>
        public int GetFullCode() => Digit0.Value * 10 + Digit1.Value;

        public int GetDigit(int fragmentIndex) => fragmentIndex switch
        {
            0 => Digit0.Value,
            1 => Digit1.Value,
            _ => 0
        };

        public bool IsCollected(int fragmentIndex) => fragmentIndex switch
        {
            0 => Collected0.Value,
            1 => Collected1.Value,
            _ => false
        };

        public void MarkCollected(int fragmentIndex)
        {
            if (!IsServer) return;
            switch (fragmentIndex)
            {
                case 0: Collected0.Value = true; break;
                case 1: Collected1.Value = true; break;
            }
        }

        public bool AllCollected() => Collected0.Value && Collected1.Value;
    }
}