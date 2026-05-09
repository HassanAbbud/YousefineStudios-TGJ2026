using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// NPC brain — state machine + NavMesh patrol.
/// States:  Patrolling → Suspicious → Alarmed
/// Attach to: NPC root GameObject (with NavMeshAgent + NPCDetectionCone)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCDetectionCone))]
public class NPCController : MonoBehaviour
{
    // ── State ────────────────────────────────────────────────────────────────
    public enum NPCState { Patrolling, Reacting, Suspicious, Alarmed }
    public NPCState CurrentState { get; private set; } = NPCState.Patrolling;

    // ── Patrol ───────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointWaitTime = 1.5f;    // seconds to idle at each point
    [SerializeField] private float waypointReachThreshold = 0.4f;

    // ── Suspicion ────────────────────────────────────────────────────────────
    [Header("Suspicion Meter")]
    [Tooltip("Seconds to fill the meter to full (→ Alarmed)")]
    [SerializeField] private float suspicionFillTime = 5f;
    [Tooltip("Seconds to fully drain the meter once player is lost")]
    [SerializeField] private float suspicionDrainTime = 8f;
    [Tooltip("How long the NPC investigates last known pos before giving up")]
    [SerializeField] private float investigateDuration = 6f;

    [Header("Reaction")]
    [Tooltip("Seconds the NPC freezes when it FIRST spots the player — gives them a chance to hide")]
    [SerializeField] private float reactionDelay = 1f;

    // ── Movement speeds ──────────────────────────────────────────────────────
    [Header("Movement Speeds")]
    [SerializeField] private float patrolSpeed = 1.8f;
    [SerializeField] private float suspiciousSpeed = 2.8f;
    [SerializeField] private float alarmedSpeed = 4f;

    // ── Events (hook up game-over screen, music stinger, etc.) ───────────────
    [Header("Events")]
    public UnityEvent OnAlarmed;            // suspicion meter filled
    public UnityEvent OnSuspicionRaised;    // first moment NPC spots player
    public UnityEvent OnSuspicionLost;      // NPC loses the player again

    // ── Internal ─────────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private NPCDetectionCone _detection;

    private int _waypointIndex;
    private float _waitTimer;
    private bool _waiting;

    private float _suspicionLevel;    // 0 → 1  (1 = alarmed)
    private float _investigateTimer;
    private float _reactionTimer;     // counts down on first detection — NPC freezes
    private Vector3 _lastKnownPlayerPos;

    // Public read — SuspicionMeterUI reads this
    public float SuspicionNormalized => _suspicionLevel;

    // ── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _detection = GetComponent<NPCDetectionCone>();
    }

    private void Update()
    {
        bool playerVisible = _detection.CanSeePlayer(out Vector3 playerPos);
        bool playerHeard = _detection.CanHearPlayer();
        bool suspiciousObj = _detection.CanSeeSuspiciousObject(out float objWeight);

        // Suspicious objects fill the meter slower (no direct alarm, just raises concern)
        bool detected = playerVisible || playerHeard;
        bool anythingWrong = detected || suspiciousObj;

        UpdateSuspicion(detected, suspiciousObj, objWeight, playerPos);
        UpdateStateMachine(detected, playerPos);
    }

    // ── Suspicion ────────────────────────────────────────────────────────────
    private void UpdateSuspicion(bool detected, bool suspiciousObj, float objWeight, Vector3 playerPos)
    {
        if (CurrentState == NPCState.Alarmed) return;

        if (detected)
        {
            _lastKnownPlayerPos = playerPos;
            _suspicionLevel += Time.deltaTime / suspicionFillTime;
            _suspicionLevel = Mathf.Clamp01(_suspicionLevel);

            if (_suspicionLevel >= 1f)
            {
                SetState(NPCState.Alarmed);
                OnAlarmed?.Invoke();
            }
        }
        else if (suspiciousObj)
        {
            // Fill rate scales by weight: body(1.0) fills at half player-speed,
            // blood stain(0.5) fills at quarter speed, etc.
            // So: fillRate = weight / (suspicionFillTime * 2)
            _suspicionLevel += (Time.deltaTime * objWeight) / (suspicionFillTime * 2f);
            _suspicionLevel = Mathf.Clamp01(_suspicionLevel);

            if (CurrentState == NPCState.Patrolling)
            {
                // ⚠ Set destination BEFORE SetState — SetState(Suspicious) calls
                // _agent.SetDestination(_lastKnownPlayerPos), so it must be the
                // object position by then, not the stale player position.
                _lastKnownPlayerPos = _detection.LastSeenSuspiciousPosition;
                SetState(NPCState.Suspicious);
                OnSuspicionRaised?.Invoke();
            }
        }
        else
        {
            if (CurrentState == NPCState.Patrolling)
            {
                _suspicionLevel -= Time.deltaTime / suspicionDrainTime;
                _suspicionLevel = Mathf.Clamp01(_suspicionLevel);
            }
        }
    }

    // ── State Machine ────────────────────────────────────────────────────────
    private void UpdateStateMachine(bool detected, Vector3 playerPos)
    {
        switch (CurrentState)
        {
            case NPCState.Patrolling:
                DoPatrol();
                if (detected)
                {
                    _lastKnownPlayerPos = playerPos;
                    SetState(NPCState.Reacting);
                    OnSuspicionRaised?.Invoke();
                }
                break;

            case NPCState.Reacting:
                // NPC freezes in place — player's window to hide
                _reactionTimer -= Time.deltaTime;
                if (!detected) _reactionTimer = 0f;   // lost sight — skip straight back
                if (_reactionTimer <= 0f)
                    SetState(detected ? NPCState.Suspicious : NPCState.Patrolling);
                break;

            case NPCState.Suspicious:
                DoInvestigate();
                if (!detected)
                {
                    _investigateTimer += Time.deltaTime;
                    if (_investigateTimer >= investigateDuration)
                    {
                        SetState(NPCState.Patrolling);
                        OnSuspicionLost?.Invoke();
                    }
                }
                else
                {
                    _investigateTimer = 0f;     // reset if player spotted again
                    _agent.SetDestination(playerPos);
                }
                break;

            case NPCState.Alarmed:
                // Move directly to last known position; win/lose handled externally via OnAlarmed
                _agent.SetDestination(_lastKnownPlayerPos);
                break;
        }
    }

    // ── Patrol helpers ───────────────────────────────────────────────────────
    private void DoPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                AdvanceWaypoint();
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= waypointReachThreshold)
        {
            _waiting = true;
            _waitTimer = waypointWaitTime;
        }
    }

    private void AdvanceWaypoint()
    {
        _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
        _agent.SetDestination(waypoints[_waypointIndex].position);
    }

    // ── Investigate (suspicious state) ───────────────────────────────────────
    private void DoInvestigate()
    {
        // Already moving toward last known pos — just check arrival
        if (!_agent.pathPending && _agent.remainingDistance <= waypointReachThreshold)
            _agent.ResetPath();     // arrived, stand and look around
    }

    // ── State transitions ────────────────────────────────────────────────────
    private void SetState(NPCState newState)
    {
        CurrentState = newState;
        _investigateTimer = 0f;

        switch (newState)
        {
            case NPCState.Patrolling:
                _agent.speed = patrolSpeed;
                if (waypoints != null && waypoints.Length > 0)
                    _agent.SetDestination(waypoints[_waypointIndex].position);
                break;

            case NPCState.Reacting:
                _agent.ResetPath();             // stop dead in tracks
                _agent.velocity = Vector3.zero;
                _reactionTimer = reactionDelay;
                break;

            case NPCState.Suspicious:
                _agent.speed = suspiciousSpeed;
                _agent.SetDestination(_lastKnownPlayerPos);
                break;

            case NPCState.Alarmed:
                _agent.speed = alarmedSpeed;
                break;
        }

        Debug.Log($"[NPC:{name}] → {newState}");
    }

    // ── Gizmos (scene view helpers) ──────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var wp in waypoints)
            if (wp != null) Gizmos.DrawSphere(wp.position, 0.2f);
    }
}