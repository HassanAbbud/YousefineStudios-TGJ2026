using Lobby;
using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// One prefab spawned per client. Contains both rigs as children.
    /// On spawn, asks the server what role this player picked in the lobby,
    /// then activates only the matching rig locally for the owner.
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Rigs (assigned in prefab)")]
        [Tooltip("First-person rig: PlayerMovement, PlayerInteraction, FP camera, hands, FP HUD")]
        public GameObject firstPersonRig;

        [Tooltip("Camera operator rig: Player2Canvas with CameraUI")]
        public GameObject cameraOperatorRig;

        [Header("Spawn Points (looked up by name in 03_Game)")]
        [Tooltip("Name of the GameObject in 03_Game where Player 1 spawns")]
        public string player1SpawnPointName = "SpawnPoint_Player1";

        [Tooltip("Name of the GameObject in 03_Game where Player 2 (camera op) spawns")]
        public string player2SpawnPointName = "SpawnPoint_Player2";

        // Networked role. Server is authoritative.
        public NetworkVariable<int> RoleValue = new NetworkVariable<int>(
            (int)PlayerRole.Unassigned,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public PlayerRole Role => (PlayerRole)RoleValue.Value;

        public override void OnNetworkSpawn()
        {
            // Both rigs start off; we enable the right one once the role syncs.
            if (firstPersonRig != null) firstPersonRig.SetActive(false);
            if (cameraOperatorRig != null) cameraOperatorRig.SetActive(false);

            if (IsOwner)
            {
                // Send the server my role from PlayerSession (set in lobby scene)
                var role = PlayerSession.Instance != null
                    ? PlayerSession.Instance.SelectedRole
                    : PlayerRole.Player1_FirstPerson;
                SubmitRoleServerRpc((int)role);
            }

            RoleValue.OnValueChanged += HandleRoleChanged;

            // If role already arrived (late join), apply immediately
            if (RoleValue.Value != (int)PlayerRole.Unassigned)
                HandleRoleChanged(0, RoleValue.Value);
        }

        public override void OnNetworkDespawn()
        {
            RoleValue.OnValueChanged -= HandleRoleChanged;
        }

        [ServerRpc]
        void SubmitRoleServerRpc(int role, ServerRpcParams rpcParams = default)
        {
            RoleValue.Value = role;

            // Server teleports the player to the correct spawn point.
            // (Position sync via NetworkTransform will broadcast it to the client.)
            TeleportToSpawnPoint((PlayerRole)role);
        }

        void TeleportToSpawnPoint(PlayerRole role)
        {
            string spawnName = role == PlayerRole.Player1_FirstPerson
                ? player1SpawnPointName
                : player2SpawnPointName;

            GameObject spawn = GameObject.Find(spawnName);
            if (spawn != null)
            {
                transform.position = spawn.transform.position;
                transform.rotation = spawn.transform.rotation;
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayer] Spawn point '{spawnName}' not found in scene. " +
                                 "Player will spawn at world origin.");
            }
        }

        void HandleRoleChanged(int prev, int now)
        {
            // Each client only activates THEIR OWN rig — not the other player's.
            // The remote player's rig stays off so we don't see two cameras / two FP characters.
            if (!IsOwner) return;

            var role = (PlayerRole)now;
            bool isFP = role == PlayerRole.Player1_FirstPerson;
            bool isCam = role == PlayerRole.Player2_Camera;

            if (firstPersonRig != null) firstPersonRig.SetActive(isFP);
            if (cameraOperatorRig != null) cameraOperatorRig.SetActive(isCam);

            if (isFP)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (isCam)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}