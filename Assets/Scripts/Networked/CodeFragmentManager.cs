using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// Singleton that generates and stores the labyrinth door's 3-digit code,
    /// split into 3 individual digits that fragments can read.
    /// Place ONE of these in the game scene.
    /// </summary>
    public class CodeFragmentManager : NetworkBehaviour
    {
        public static CodeFragmentManager Instance { get; private set; }

        // Each digit (0-9) is stored separately. Index 0 = hundreds, 1 = tens, 2 = ones.
        public NetworkVariable<int> Digit0 = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Digit1 = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> Digit2 = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Tracks which fragments have been collected by Player 1 (visible to all so UI can update)
        public NetworkVariable<bool> Collected0 = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Collected1 = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> Collected2 = new NetworkVariable<bool>(
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
                // Generate one random 3-digit code (000-999), then split it
                int fullCode = Random.Range(0, 1000);
                Digit0.Value = (fullCode / 100) % 10;
                Digit1.Value = (fullCode / 10) % 10;
                Digit2.Value = fullCode % 10;

                Debug.Log($"[CodeFragmentManager] Labyrinth code generated: " +
                          $"{Digit0.Value}{Digit1.Value}{Digit2.Value}");
            }
        }

        /// <summary>
        /// Returns the full 3-digit code as an int (0-999).
        /// Used by the labyrinth door to validate code submissions.
        /// </summary>
        public int GetFullCode() => Digit0.Value * 100 + Digit1.Value * 10 + Digit2.Value;

        /// <summary>Get the digit (0-9) for a given fragment index (0-2).</summary>
        public int GetDigit(int fragmentIndex) => fragmentIndex switch
        {
            0 => Digit0.Value,
            1 => Digit1.Value,
            2 => Digit2.Value,
            _ => 0
        };

        /// <summary>Has fragment N been collected?</summary>
        public bool IsCollected(int fragmentIndex) => fragmentIndex switch
        {
            0 => Collected0.Value,
            1 => Collected1.Value,
            2 => Collected2.Value,
            _ => false
        };

        /// <summary>Mark fragment N as collected (server only).</summary>
        public void MarkCollected(int fragmentIndex)
        {
            if (!IsServer) return;
            switch (fragmentIndex)
            {
                case 0: Collected0.Value = true; break;
                case 1: Collected1.Value = true; break;
                case 2: Collected2.Value = true; break;
            }
        }

        public bool AllCollected() => Collected0.Value && Collected1.Value && Collected2.Value;
    }
}