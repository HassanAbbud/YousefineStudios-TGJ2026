using Lobby;
using Networked;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Local pause menu. Each player has their own. Pausing freezes time
    /// LOCALLY only — does not affect the other player.
    /// Toggled with ESC. Includes volume slider, mute, quit.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;
        public Slider volumeSlider;
        public Toggle muteToggle;
        public TMP_Text volumeLabel;
        public Button resumeButton;
        public Button mainMenuButton;
        public Button quitButton;

        [Header("Audio")]
        [Tooltip("Optional: drag your master AudioMixer here to control mixer volume. " +
                 "If null, falls back to AudioListener.volume.")]
        public AudioMixer audioMixer;
        [Tooltip("Exposed parameter name on the mixer (e.g. 'MasterVolume'). Required if AudioMixer assigned.")]
        public string mixerVolumeParam = "MasterVolume";

        [Header("Scene")]
        public string mainMenuScene = "01_MainMenu";

        const string VOLUME_PREF_KEY = "MasterVolume";
        const string MUTE_PREF_KEY = "MasterMute";

        bool isPaused;
        bool wasCursorVisible;
        CursorLockMode previousCursorLock;

        void Awake()
        {
            if (root != null) root.SetActive(false);

            // Load saved settings
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 0.8f);
            bool savedMute = PlayerPrefs.GetInt(MUTE_PREF_KEY, 0) == 1;

            if (volumeSlider != null)
            {
                volumeSlider.value = savedVolume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            if (muteToggle != null)
            {
                muteToggle.isOn = savedMute;
                muteToggle.onValueChanged.AddListener(OnMuteChanged);
            }
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            // Apply on startup
            ApplyVolume(savedVolume, savedMute);
            UpdateVolumeLabel(savedVolume);
        }

        void Update()
        {
            // Don't open pause menu if a game-end screen is up
            if (GameStateManager.Instance != null && GameStateManager.Instance.GameEnded)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused) Resume();
                else Pause();
            }
        }

        void Pause()
        {
            isPaused = true;
            if (root != null) root.SetActive(true);

            // Remember cursor state and unlock
            previousCursorLock = Cursor.lockState;
            wasCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;
        }

        public void Resume()
        {
            isPaused = false;
            if (root != null) root.SetActive(false);

            // Restore cursor state
            Cursor.lockState = previousCursorLock;
            Cursor.visible = wasCursorVisible;

            Time.timeScale = 1f;
        }

        void OnVolumeChanged(float v)
        {
            PlayerPrefs.SetFloat(VOLUME_PREF_KEY, v);
            ApplyVolume(v, muteToggle != null && muteToggle.isOn);
            UpdateVolumeLabel(v);
        }

        void OnMuteChanged(bool muted)
        {
            PlayerPrefs.SetInt(MUTE_PREF_KEY, muted ? 1 : 0);
            float v = volumeSlider != null ? volumeSlider.value : 0.8f;
            ApplyVolume(v, muted);
        }

        void ApplyVolume(float linear, bool muted)
        {
            float effective = muted ? 0f : linear;

            if (audioMixer != null)
            {
                // AudioMixer expects dB values: -80 = silent, 0 = full
                float db = effective <= 0.0001f ? -80f : Mathf.Log10(effective) * 20f;
                audioMixer.SetFloat(mixerVolumeParam, db);
            }
            else
            {
                AudioListener.volume = effective;
            }
        }

        void UpdateVolumeLabel(float v)
        {
            if (volumeLabel != null)
                volumeLabel.text = $"{Mathf.RoundToInt(v * 100)}%";
        }

        void OnMainMenu()
        {
            Time.timeScale = 1f;

            if (LobbyManager.Instance != null)
                _ = LobbyManager.Instance.LeaveLobbyAsync();

            if (GameNetworkManager.Instance != null)
                GameNetworkManager.Instance.Shutdown();

            SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
        }

        void OnQuit()
        {
            Time.timeScale = 1f;

            if (GameNetworkManager.Instance != null)
                GameNetworkManager.Instance.Shutdown();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}