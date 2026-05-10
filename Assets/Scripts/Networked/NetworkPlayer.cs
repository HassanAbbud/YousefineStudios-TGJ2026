using Lobby;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// One prefab spawned per client. Contains both rigs as children.
    /// On spawn, asks the server what role this player picked in the lobby,
    /// then activates only the matching rig locally for the owner.
    ///
    /// IMPORTANT: This prefab uses ClientNetworkTransform (owner-authoritative).
    /// That means the owning client controls its own position. The server cannot
    /// teleport the client by setting transform.position; instead the OWNER
    /// teleports itself locally once it knows its role.
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
            // Server records the role. Position is then set by the owner itself
            // (since ClientNetworkTransform is owner-authoritative).
            RoleValue.Value = role;
        }

        void TeleportToSpawnPointAsOwner(PlayerRole role)
        {
            // Owner-authoritative: only the owner moves itself. The position is then
            // automatically synced to other clients via ClientNetworkTransform.
            if (!IsOwner) return;

            string spawnName = role == PlayerRole.Player1_FirstPerson
                ? player1SpawnPointName
                : player2SpawnPointName;

            GameObject spawn = GameObject.Find(spawnName);
            if (spawn == null)
            {
                Debug.LogWarning($"[NetworkPlayer] Spawn point '{spawnName}' not found in scene. " +
                                 "Player will spawn at world origin.");
                return;
            }

            // CharacterController fights direct transform.position writes.
            // Disable it for one frame, teleport, then re-enable.
            var cc = GetComponentInChildren<CharacterController>(true);
            if (cc != null) cc.enabled = false;

            // Use NetworkTransform.Teleport so interpolation doesn't lerp us across the map.
            var nt = GetComponent<NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(spawn.transform.position, spawn.transform.rotation, transform.localScale);
            }
            else
            {
                transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            }

            if (cc != null) cc.enabled = true;
        }

        void HandleRoleChanged(int prev, int now)
        {
            // Each client only activates THEIR OWN rig — not the other player's.
            if (!IsOwner) return;

            var role = (PlayerRole)now;
            bool isFP = role == PlayerRole.Player1_FirstPerson;
            bool isCam = role == PlayerRole.Player2_Camera;

            if (firstPersonRig != null) firstPersonRig.SetActive(isFP);
            if (cameraOperatorRig != null) cameraOperatorRig.SetActive(isCam);

            // Cursor: locked for FP, free for camera operator.
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

            // Now that we know our role, teleport ourselves to the right spawn point.
            // Owner teleports because ClientNetworkTransform is owner-authoritative.
            TeleportToSpawnPointAsOwner(role);
        }
    }
}