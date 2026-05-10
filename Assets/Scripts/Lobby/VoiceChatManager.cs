using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace Lobby
{
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance { get; private set; }

        [Header("Channel Settings")]
        public KeyCode pushToTalkKey = KeyCode.V;
        public bool openMic = false;

        public bool IsLoggedIn { get; private set; }
        public bool IsInChannel { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsTransmitting { get; private set; }

        public event Action OnLoggedIn;
        public event Action OnJoinedChannel;
        public event Action OnLeftChannel;
        public event Action<bool> OnMuteChanged;
        public event Action<bool> OnTransmittingChanged;

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
            bool keyDown = Input.GetKey(pushToTalkKey);
            if (keyDown != IsTransmitting) SetTransmitting(keyDown);
        }

        public async Task EnsureLoggedIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

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

        public async Task JoinLobbyChannelAsync(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId))
            {
                Debug.LogWarning("[VoiceChat] No lobby ID");
                return;
            }

            await EnsureLoggedIn();

            string channelName = "lobby_" + lobbyId.Replace("-", "_");

            try
            {
                await VivoxService.Instance.JoinGroupChannelAsync(
                    channelName, ChatCapability.AudioOnly);

                currentChannelName = channelName;
                IsInChannel = true;
                SetTransmitting(openMic);
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

            try { await VivoxService.Instance.LeaveChannelAsync(currentChannelName); }
            catch (Exception e) { Debug.LogWarning($"[VoiceChat] Leave error: {e.Message}"); }

            IsInChannel = false;
            IsTransmitting = false;
            currentChannelName = null;
            OnLeftChannel?.Invoke();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            if (VivoxService.Instance != null)
            {
                if (muted) VivoxService.Instance.MuteInputDevice();
                else VivoxService.Instance.UnmuteInputDevice();
            }
            OnMuteChanged?.Invoke(muted);
        }

        public void ToggleMute() => SetMuted(!IsMuted);

        void SetTransmitting(bool transmitting)
        {
            IsTransmitting = transmitting;
            if (VivoxService.Instance == null) return;

            bool effectiveMute = !transmitting || IsMuted;
            if (effectiveMute) VivoxService.Instance.MuteInputDevice();
            else VivoxService.Instance.UnmuteInputDevice();

            OnTransmittingChanged?.Invoke(transmitting);
        }

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