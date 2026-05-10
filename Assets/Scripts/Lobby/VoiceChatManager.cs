using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

namespace Lobby
{
    /// <summary>
    /// Wraps Vivox voice chat. Auto-joins a channel named after the lobby ID
    /// when the game starts; leaves on shutdown. Persistent across scenes.
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance { get; private set; }

        [Header("Channel Settings")]
        [Tooltip("Push-to-talk key. Set to None to enable open mic.")]
        public KeyCode pushToTalkKey = KeyCode.V;

        [Tooltip("If true, mic is open all the time. If false, hold pushToTalkKey to transmit.")]
        public bool openMic = false;

        // ===== State =====
        public bool IsLoggedIn { get; private set; }
        public bool IsInChannel { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsTransmitting { get; private set; }

        // ===== Events for UI =====
        public event Action OnLoggedIn;
        public event Action OnJoinedChannel;
        public event Action OnLeftChannel;
        public event Action<bool> OnMuteChanged;          // true = muted
        public event Action<bool> OnTransmittingChanged;  // true = transmitting

        string currentChannelName;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (!IsInChannel || openMic) return;

            // Push-to-talk: hold the key to transmit
            bool keyDown = Input.GetKey(pushToTalkKey);
            if (keyDown != IsTransmitting)
            {
                SetTransmitting(keyDown);
            }
        }

        // ===== Init / Login =====

        /// <summary>
        /// Initializes Vivox and logs the local user in.
        /// Safe to call multiple times.
        /// </summary>
        public async Task EnsureLoggedIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Initialize the Vivox service
            await VivoxService.Instance.InitializeAsync();

            if (!VivoxService.Instance.IsLoggedIn)
            {
                var loginOptions = new LoginOptions
                {
                    DisplayName = PlayerSession.Instance != null
                        ? PlayerSession.Instance.PlayerName
                        : "Player",
                    EnableTTS = false
                };
                await VivoxService.Instance.LoginAsync(loginOptions);
            }

            IsLoggedIn = true;
            OnLoggedIn?.Invoke();
            Debug.Log("[VoiceChat] Logged into Vivox");
        }

        // ===== Join / Leave Channel =====

        /// <summary>
        /// Joins a voice channel named after the lobby ID.
        /// Both players in the same lobby end up in the same channel automatically.
        /// </summary>
        public async Task JoinLobbyChannelAsync(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId))
            {
                Debug.LogWarning("[VoiceChat] No lobby ID, can't join channel");
                return;
            }

            await EnsureLoggedIn();

            // Vivox channel names: alphanumeric + underscores only, max 200 chars
            string channelName = "lobby_" + lobbyId.Replace("-", "_");

            try
            {
                var channelOptions = new ChannelOptions { MakeActiveChannelUponJoining = true };

                // ChatCapability.AudioOnly = voice only, no text chat
                await VivoxService.Instance.JoinGroupChannelAsync(
                    channelName,
                    ChatCapability.AudioOnly,
                    channelOptions);

                currentChannelName = channelName;
                IsInChannel = true;

                // Start with mic muted if push-to-talk; open mic starts unmuted
                if (!openMic)
                {
                    SetTransmitting(false); // PTT default: not transmitting
                }
                else
                {
                    SetTransmitting(true);
                }

                OnJoinedChannel?.Invoke();
                Debug.Log($"[VoiceChat] Joined channel: {channelName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceChat] Failed to join channel: {e.Message}");
            }
        }

        public async Task LeaveChannelAsync()
        {
            if (!IsInChannel || string.IsNullOrEmpty(currentChannelName)) return;

            try
            {
                await VivoxService.Instance.LeaveChannelAsync(currentChannelName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VoiceChat] Leave channel error: {e.Message}");
            }

            IsInChannel = false;
            IsTransmitting = false;
            currentChannelName = null;
            OnLeftChannel?.Invoke();
        }

        // ===== Mute / Transmit Control =====

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            // Vivox mutes input (your mic) for everyone in the channel
            if (VivoxService.Instance != null)
                VivoxService.Instance.MuteInputDevice(muted);

            OnMuteChanged?.Invoke(muted);
        }

        public void ToggleMute() => SetMuted(!IsMuted);

        void SetTransmitting(bool transmitting)
        {
            IsTransmitting = transmitting;

            if (VivoxService.Instance == null) return;

            // When NOT transmitting, mute input. When transmitting, unmute (unless globally muted).
            bool effectiveMute = !transmitting || IsMuted;
            VivoxService.Instance.MuteInputDevice(effectiveMute);

            OnTransmittingChanged?.Invoke(transmitting);
        }

        // ===== Cleanup =====

        async void OnApplicationQuit()
        {
            if (IsInChannel) await LeaveChannelAsync();
            if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
                await VivoxService.Instance.LogoutAsync();
        }

        public async Task ShutdownAsync()
        {
            if (IsInChannel) await LeaveChannelAsync();
            if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
                await VivoxService.Instance.LogoutAsync();
            IsLoggedIn = false;
        }
    }
}