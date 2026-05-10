
using Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Small HUD widget showing voice chat state. Each player has their own.
    /// Shows: PTT key hint, transmitting indicator, mute button.
    /// </summary>
    public class VoiceChatHUD : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;
        public Image transmitIndicator;     // Glows when transmitting
        public TMP_Text statusLabel;         // "Hold V to talk" / "Talking..." / "Muted"
        public Button muteButton;
        public TMP_Text muteButtonLabel;

        [Header("Colors")]
        public Color idleColor = new Color(1, 1, 1, 0.3f);
        public Color transmittingColor = new Color(0.2f, 1f, 0.2f, 1f);
        public Color mutedColor = new Color(1f, 0.2f, 0.2f, 1f);

        void Awake()
        {
            if (muteButton != null) muteButton.onClick.AddListener(OnMuteClicked);
            UpdateDisplay();
        }

        void OnEnable()
        {
            if (VoiceChatManager.Instance == null) return;
            VoiceChatManager.Instance.OnTransmittingChanged += HandleTransmittingChanged;
            VoiceChatManager.Instance.OnMuteChanged += HandleMuteChanged;
            VoiceChatManager.Instance.OnJoinedChannel += UpdateDisplay;
            VoiceChatManager.Instance.OnLeftChannel += UpdateDisplay;
        }

        void OnDisable()
        {
            if (VoiceChatManager.Instance == null) return;
            VoiceChatManager.Instance.OnTransmittingChanged -= HandleTransmittingChanged;
            VoiceChatManager.Instance.OnMuteChanged -= HandleMuteChanged;
            VoiceChatManager.Instance.OnJoinedChannel -= UpdateDisplay;
            VoiceChatManager.Instance.OnLeftChannel -= UpdateDisplay;
        }

        void HandleTransmittingChanged(bool _) => UpdateDisplay();
        void HandleMuteChanged(bool _) => UpdateDisplay();

        void UpdateDisplay()
        {
            var vc = VoiceChatManager.Instance;
            if (vc == null) return;

            // Transmit indicator color
            if (transmitIndicator != null)
            {
                if (vc.IsMuted) transmitIndicator.color = mutedColor;
                else if (vc.IsTransmitting) transmitIndicator.color = transmittingColor;
                else transmitIndicator.color = idleColor;
            }

            // Status text
            if (statusLabel != null)
            {
                if (!vc.IsInChannel) statusLabel.text = "Voice off";
                else if (vc.IsMuted) statusLabel.text = "Muted";
                else if (vc.IsTransmitting) statusLabel.text = "Talking...";
                else statusLabel.text = $"Hold {vc.pushToTalkKey} to talk";
            }

            // Mute button label
            if (muteButtonLabel != null)
                muteButtonLabel.text = vc.IsMuted ? "UNMUTE" : "MUTE";
        }

        void OnMuteClicked()
        {
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.ToggleMute();
        }
    }
}