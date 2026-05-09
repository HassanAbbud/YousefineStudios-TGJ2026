using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Lobby
{
    /// <summary>
    /// Wraps Unity's Lobby service. Handles create, join, role updates, and polling.
    /// The lobby holds shared state (player list, roles, eventually the Relay code)
    /// before Netcode is even started.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        public const int MAX_PLAYERS = 2;
        public const string KEY_RELAY_CODE = "RelayCode";
        public const string KEY_PLAYER_ROLE = "Role";
        public const string KEY_PLAYER_NAME = "Name";

        public Unity.Services.Lobbies.Models.Lobby CurrentLobby { get; private set; }

        public event Action<Unity.Services.Lobbies.Models.Lobby> OnLobbyUpdated;
        public event Action OnLobbyLeft;
        public event Action OnGameStarting;

        float heartbeatTimer;
        float pollTimer;
        const float HEARTBEAT_INTERVAL = 15f; // host pings to keep lobby alive
        const float POLL_INTERVAL = 1.5f;     // clients poll for updates

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (CurrentLobby == null) return;

            if (IsHost())
            {
                heartbeatTimer -= Time.deltaTime;
                if (heartbeatTimer <= 0f)
                {
                    heartbeatTimer = HEARTBEAT_INTERVAL;
                    _ = HeartbeatAsync();
                }
            }

            pollTimer -= Time.deltaTime;
            if (pollTimer <= 0f)
            {
                pollTimer = POLL_INTERVAL;
                _ = PollAsync();
            }
        }

        bool IsHost() => CurrentLobby != null
            && CurrentLobby.HostId == AuthenticationService.Instance.PlayerId;

        async Task HeartbeatAsync()
        {
            try { await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id); }
            catch (LobbyServiceException e) { Debug.LogWarning($"Heartbeat failed: {e.Message}"); }
        }

        async Task PollAsync()
        {
            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
                CurrentLobby = lobby;
                OnLobbyUpdated?.Invoke(lobby);

                // Clients: when host writes the relay code into lobby data, join the game
                if (!IsHost() && lobby.Data != null
                    && lobby.Data.TryGetValue(KEY_RELAY_CODE, out var entry)
                    && !string.IsNullOrEmpty(entry.Value)
                    && entry.Value != "0")
                {
                    OnGameStarting?.Invoke();
                    await GameNetworkManager.Instance.StartClientWithRelay(entry.Value);
                    CurrentLobby = null; // stop polling after handoff
                }
            }
            catch (LobbyServiceException e) { Debug.LogWarning($"Poll failed: {e.Message}"); }
        }

        public async Task<Unity.Services.Lobbies.Models.Lobby> CreateLobbyAsync(
            string lobbyName, string playerName)
        {
            await GameNetworkManager.Instance.EnsureSignedIn();

            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = BuildPlayer(playerName, PlayerRole.Unassigned),
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYERS, options);
            heartbeatTimer = HEARTBEAT_INTERVAL;
            pollTimer = POLL_INTERVAL;
            OnLobbyUpdated?.Invoke(CurrentLobby);
            return CurrentLobby;
        }

        public async Task<Unity.Services.Lobbies.Models.Lobby> JoinByCodeAsync(
            string lobbyCode, string playerName)
        {
            await GameNetworkManager.Instance.EnsureSignedIn();

            var options = new JoinLobbyByCodeOptions
            {
                Player = BuildPlayer(playerName, PlayerRole.Unassigned)
            };
            CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            pollTimer = POLL_INTERVAL;
            OnLobbyUpdated?.Invoke(CurrentLobby);
            return CurrentLobby;
        }

        Unity.Services.Lobbies.Models.Player BuildPlayer(string name, PlayerRole role)
        {
            return new Unity.Services.Lobbies.Models.Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject>
                {
                    { KEY_PLAYER_NAME,
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) },
                    { KEY_PLAYER_ROLE,
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ((int)role).ToString()) }
                });
        }

        public async Task UpdateMyRoleAsync(PlayerRole role)
        {
            if (CurrentLobby == null) return;
            try
            {
                var options = new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { KEY_PLAYER_ROLE,
                            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ((int)role).ToString()) }
                    }
                };
                CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(
                    CurrentLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    options);
                PlayerSession.Instance.SetRole(role);
                OnLobbyUpdated?.Invoke(CurrentLobby);
            }
            catch (LobbyServiceException e) { Debug.LogError($"Role update failed: {e.Message}"); }
        }

        public async Task HostStartGameAsync()
        {
            if (!IsHost()) { Debug.LogWarning("Only host can start"); return; }
            if (!AllRolesAssigned()) { Debug.LogWarning("Not all roles assigned"); return; }

            string relayCode = await GameNetworkManager.Instance.StartHostWithRelay(MAX_PLAYERS);

            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            };
            CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);
            OnGameStarting?.Invoke();
        }

        public bool AllRolesAssigned()
        {
            if (CurrentLobby == null || CurrentLobby.Players.Count < MAX_PLAYERS) return false;
            var roles = new HashSet<PlayerRole>();
            foreach (var p in CurrentLobby.Players)
            {
                if (p.Data == null || !p.Data.TryGetValue(KEY_PLAYER_ROLE, out var rd)) return false;
                if (!int.TryParse(rd.Value, out int ri)) return false;
                var role = (PlayerRole)ri;
                if (role == PlayerRole.Unassigned) return false;
                if (!roles.Add(role)) return false; // duplicate role
            }
            return roles.Count == MAX_PLAYERS;
        }

        public PlayerRole GetRoleOf(string playerId)
        {
            if (CurrentLobby == null) return PlayerRole.Unassigned;
            foreach (var p in CurrentLobby.Players)
            {
                if (p.Id == playerId && p.Data != null
                    && p.Data.TryGetValue(KEY_PLAYER_ROLE, out var rd))
                {
                    if (int.TryParse(rd.Value, out int ri)) return (PlayerRole)ri;
                }
            }
            return PlayerRole.Unassigned;
        }

        public bool IsRoleTakenByOther(PlayerRole role)
        {
            if (CurrentLobby == null) return false;
            string myId = AuthenticationService.Instance.PlayerId;
            foreach (var p in CurrentLobby.Players)
            {
                if (p.Id == myId) continue;
                if (GetRoleOf(p.Id) == role) return true;
            }
            return false;
        }

        public async Task LeaveLobbyAsync()
        {
            if (CurrentLobby == null) return;
            try
            {
                if (IsHost())
                    await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
                else
                    await LobbyService.Instance.RemovePlayerAsync(
                        CurrentLobby.Id,
                        AuthenticationService.Instance.PlayerId);
            }
            catch (LobbyServiceException e) { Debug.LogWarning($"Leave failed: {e.Message}"); }
            CurrentLobby = null;
            OnLobbyLeft?.Invoke();
        }

        void OnApplicationQuit()
        {
            _ = LeaveLobbyAsync();
        }
    }
}