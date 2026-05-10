using UnityEngine;

/// <summary>
/// Plays a looping ambient sound (office white noise, hum, rain, etc.) at a fixed
/// volume across the whole scene. Place on a single GameObject in 03_Game.
///
/// Survives both rigs (FP and Camera Operator) since it's a 2D sound at scene level —
/// no spatial blend, no distance falloff.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbientAudio : MonoBehaviour
{
    [Header("Clip")]
    [Tooltip("The looping ambient clip (white noise, office hum, etc.)")]
    public AudioClip clip;

    [Header("Playback")]
    [Range(0f, 1f)]
    [Tooltip("Final volume of the loop. Keep it low (0.1–0.3) so it sits under sfx.")]
    public float volume = 0.2f;

    [Tooltip("Seconds to fade in from silence when the scene starts.")]
    public float fadeInDuration = 2f;

    AudioSource _src;
    float _fadeTimer;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.clip = clip;
        _src.loop = true;
        _src.playOnAwake = false;
        _src.spatialBlend = 0f;         // 2D — same volume everywhere
        _src.bypassEffects = true;
        _src.bypassListenerEffects = true;
        _src.bypassReverbZones = true;
        _src.volume = 0f;
    }

    void Start()
    {
        if (clip == null)
        {
            Debug.LogWarning("[AmbientAudio] No clip assigned — skipping.");
            return;
        }
        _src.Play();
    }

    void Update()
    {
        if (clip == null) return;
        if (_fadeTimer < fadeInDuration)
        {
            _fadeTimer += Time.deltaTime;
            float t = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(_fadeTimer / fadeInDuration);
            _src.volume = Mathf.Lerp(0f, volume, t);
        }
        else if (!Mathf.Approximately(_src.volume, volume))
        {
            _src.volume = volume;
        }
    }
}