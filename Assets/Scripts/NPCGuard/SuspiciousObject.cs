using UnityEngine;

/// <summary>
/// Attach this to any world object that should make NPCs suspicious if seen:
///   - The body (before it's hidden in the locker)
///   - The weapon (before it's hidden in the drawer)
///   - Blood stains (before they're cleaned)
///
/// NPCDetectionCone scans for these every frame.
/// Call SetVisible(false) once the object is hidden/cleaned so NPCs ignore it.
///
/// This is automatically managed by the Interactable scripts:
///   - BodyInteractable     → hides when picked up, unhides if dropped
///   - BloodStainInteractable → hides when cleaned
///   - WeaponInteractable   → hides when picked up, unhides if dropped
/// </summary>
public class SuspiciousObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How suspicious is this object? 1 = maximum (body), 0.5 = moderate (blood stain)")]
    [Range(0f, 1f)]
    [SerializeField] private float suspicionWeight = 1f;

    [Tooltip("Start visible (i.e. the NPC can see it from the start)")]
    [SerializeField] private bool startVisible = true;

    /// <summary>
    /// Whether this object is currently detectable by NPCs.
    /// Set to false when the player picks it up or hides/cleans it.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>How much this object contributes to the suspicion meter (0–1).</summary>
    public float SuspicionWeight => suspicionWeight;

    private void Awake()
    {
        IsVisible = startVisible;
    }

    /// <summary>
    /// Call this to show or hide from NPC detection.
    /// e.g. SetVisible(false) when body is picked up.
    ///      SetVisible(true)  when body is dropped back on the floor.
    /// </summary>
    public void SetVisible(bool visible) => IsVisible = visible;
}
