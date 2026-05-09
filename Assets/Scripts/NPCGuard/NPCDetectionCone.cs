using UnityEngine;

/// <summary>
/// NPC Detection — two senses:
///   1. VISION  — forward cone (angle + range). Blocked by obstacles (raycast).
///                Crouching player → shorter range, narrower angle.
///   2. HEARING — radius check. Only triggers if player is MOVING (not crouching).
///
/// Requires: Player2 GameObject must be on the "Player" layer.
/// Attach to: same NPC root as NPCController.
/// </summary>
public class NPCDetectionCone : MonoBehaviour
{
    // ── Vision ───────────────────────────────────────────────────────────────
    [Header("Vision — Normal")]
    [SerializeField] private float viewRange = 10f;
    [SerializeField] private float viewAngle = 90f;     // full cone FOV in degrees

    [Header("Vision — Crouching player penalty")]
    [SerializeField] private float crouchViewRange = 5f;
    [SerializeField] private float crouchViewAngle = 50f;

    [Header("Obstacle Mask")]
    [Tooltip("Layers that block line-of-sight (walls, furniture, etc.)")]
    [SerializeField] private LayerMask obstacleMask;

    // ── Hearing ──────────────────────────────────────────────────────────────
    [Header("Hearing")]
    [Tooltip("Radius the NPC can hear a walking player")]
    [SerializeField] private float hearingRange = 4f;
    [Tooltip("Extra radius if player is running (IsMoving + not crouching)")]
    [SerializeField] private float runHearingBonus = 2f;

    // ── Layer ────────────────────────────────────────────────────────────────
    [Header("Player Layer")]
    [SerializeField] private LayerMask playerMask;    // Set to "Player" layer

    // ── Eye transform ─────────────────────────────────────────────────────────
    [Tooltip("Empty child at NPC eye-level. Rays cast from here.")]
    [SerializeField] private Transform eyeTransform;

    // ── Cached player references (found at runtime) ───────────────────────────
    private Transform _playerTransform;
    private PlayerMovement _playerMovement;

    private void Start()
    {
        // Find Player 2 in the scene. Works for local + networked (finds local owner).
        GameObject playerObj = GameObject.FindWithTag("Player2");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogWarning("[NPCDetectionCone] No GameObject tagged 'Player2' found!");
        }

        if (eyeTransform == null)
            eyeTransform = transform;   // fallback to root if not set
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the player is within the vision cone AND has line-of-sight.
    /// Outputs the player's current world position.
    /// </summary>
    public bool CanSeePlayer(out Vector3 playerPosition)
    {
        playerPosition = Vector3.zero;
        if (_playerTransform == null) return false;

        bool playerCrouching = _playerMovement != null && _playerMovement.IsCrouching;
        float range = playerCrouching ? crouchViewRange : viewRange;
        float angle = playerCrouching ? crouchViewAngle : viewAngle;

        Vector3 dirToPlayer = _playerTransform.position - eyeTransform.position;
        float dist = dirToPlayer.magnitude;

        if (dist > range) return false;

        float angleToPlayer = Vector3.Angle(eyeTransform.forward, dirToPlayer);
        if (angleToPlayer > angle / 2f) return false;

        // Line-of-sight raycast — does a wall block us?
        if (Physics.Raycast(eyeTransform.position, dirToPlayer.normalized, dist, obstacleMask))
            return false;   // obstacle in the way

        playerPosition = _playerTransform.position;
        return true;
    }

    /// <summary>
    /// Returns true if the player is close enough to be heard.
    /// Player must be moving; crouching reduces footstep radius.
    /// </summary>
    public bool CanHearPlayer()
    {
        if (_playerTransform == null || _playerMovement == null) return false;
        if (!_playerMovement.IsMoving) return false;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        float radius = hearingRange;
        if (!_playerMovement.IsCrouching)
            radius += runHearingBonus;  // walking upright is louder

        return dist <= radius;
    }

    // ── Suspicious objects ────────────────────────────────────────────────────
    [Header("Suspicious Objects")]
    [Tooltip("How far the NPC can notice a suspicious object (body, weapon on floor)")]
    [SerializeField] private float suspiciousObjectRange = 6f;

    /// <summary>World position of the last suspicious object the NPC noticed.</summary>
    public Vector3 LastSeenSuspiciousPosition { get; private set; }

    /// <summary>
    /// Returns true if a SuspiciousObject is within range AND has line-of-sight.
    /// Also outputs the object's suspicionWeight (0–1) so the fill rate can scale by object type:
    ///   body = 1.0 → fills fast   |   blood stain = 0.5 → fills slowly
    /// </summary>
    public bool CanSeeSuspiciousObject(out float weight)
    {
        weight = 0f;

        // Overlap sphere to find all nearby suspicious objects
        Collider[] hits = Physics.OverlapSphere(transform.position, suspiciousObjectRange);

        // Pick the MOST suspicious visible object in range (highest weight wins)
        SuspiciousObject best = null;
        float bestWeight = 0f;
        Vector3 bestPos = Vector3.zero;

        foreach (Collider hit in hits)
        {
            SuspiciousObject obj = hit.GetComponent<SuspiciousObject>();
            if (obj == null || !obj.IsVisible) continue;

            Vector3 dir = hit.transform.position - eyeTransform.position;
            float dist = dir.magnitude;

            // Line-of-sight check — wall between NPC and the object?
            if (Physics.Raycast(eyeTransform.position, dir.normalized, dist, obstacleMask))
                continue;

            if (obj.SuspicionWeight > bestWeight)
            {
                bestWeight = obj.SuspicionWeight;
                bestPos = hit.transform.position;
                best = obj;
            }
        }

        if (best == null) return false;

        weight = bestWeight;
        LastSeenSuspiciousPosition = bestPos;
        return true;
    }

    // ── Gizmos — visualise cone in Scene view ────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Transform eye = eyeTransform != null ? eyeTransform : transform;

        // Vision cone
        Gizmos.color = Color.yellow;
        DrawConeGizmo(eye, viewRange, viewAngle);

        // Crouch cone (dimmer)
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        DrawConeGizmo(eye, crouchViewRange, crouchViewAngle);

        // Hearing radius
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, hearingRange + runHearingBonus);
    }

    private void DrawConeGizmo(Transform origin, float range, float angle)
    {
        Vector3 forward = origin.forward;
        Vector3 leftDir = Quaternion.Euler(0, -angle / 2f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, angle / 2f, 0) * forward;

        Gizmos.DrawRay(origin.position, leftDir * range);
        Gizmos.DrawRay(origin.position, rightDir * range);
        Gizmos.DrawRay(origin.position, forward * range);

        // Arc approximation (8 segments)
        int segments = 8;
        Vector3 prev = origin.position + leftDir * range;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 dir = Quaternion.Euler(0, Mathf.Lerp(-angle / 2f, angle / 2f, t), 0) * forward;
            Vector3 next = origin.position + dir * range;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}