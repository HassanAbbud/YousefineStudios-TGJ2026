using UnityEngine;

namespace Lobby
{
    public enum PlayerRole
    {
        Unassigned = 0,
        Player1_FirstPerson = 1,   // The sneaker (uses PlayerMovement.cs)
        Player2_Camera = 2          // The camera operator (uses CameraUI.cs)
    }

    /// <summary>
    /// Persists across scene loads. Stores the local player's chosen role and name
    /// so other systems (like the game scene's spawning logic) can read them.
    /// </summary>
    public class PlayerSession : MonoBehaviour
    {
        public static PlayerSession Instance { get; private set; }

        public PlayerRole SelectedRole { get; private set; } = PlayerRole.Unassigned;
        public string PlayerName { get; private set; } = "Player";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetRole(PlayerRole r) => SelectedRole = r;
        public void SetName(string n) => PlayerName = string.IsNullOrWhiteSpace(n) ? "Player" : n.Trim();
    }
}