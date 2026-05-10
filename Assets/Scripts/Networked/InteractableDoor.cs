using System;
using Player2;
using Shared;
using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// A door the camera operator can click via the CCTV feed.
    /// Locked doors open a code modal; unlocked doors toggle open/closed.
    /// Server-authoritative.
    ///
    /// PIVOT: To open from the side (like a real door), add an empty child GameObject
    /// named "Pivot" at the position of the hinge (one edge of the door), and drag it
    /// into the 'pivot' field. The door root will rotate AROUND that point.
    /// If pivot is empty, the door rotates around its own center.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableDoor : NetworkBehaviour, IPlayer2Interactable, ICodeInteractable
    {
        [Header("Door Identity")]
        public string doorName = "Door";

        [Header("Code Settings")]
        [Tooltip("Labyrinth doors get their code from separate fragment pickups (handled by CodeFragmentManager). " +
                 "Standard doors get a random code at game start, displayed on a sticky note nearby.")]
        public bool isLabyrinthDoor = false;

        [Tooltip("If true, this door starts unlocked (no code needed).")]
        public bool startsUnlocked = false;

        [Tooltip("How many digits the code modal should expect. Labyrinth doors use 2, normal doors use 3. " +
                 "This is auto-set at spawn for labyrinth doors.")]
        public int codeDigitCount = 3;

        [Header("Animation")]
        public float closedAngle = 0f;
        public float openAngle = 90f;
        public float swingDuration = 0.5f;

        [Tooltip("Optional. Empty child GameObject placed at the hinge edge of the door. " +
                 "If assigned, the door rotates AROUND this point (side-hinged door behavior). " +
                 "If left empty, the door rotates around its own center.")]
        public Transform pivot;

        [Header("Audio (optional)")]
        public AudioSource audioSource;
        public AudioClip openClip;
        public AudioClip closeClip;
        public AudioClip lockedRattleClip;
        public AudioClip unlockedChimeClip;

        // Networked state
        public NetworkVariable<int> Code = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsUnlocked = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> IsOpen = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Local animation state
        float currentAngle;
        float targetAngle;

        // Cached starting rotation of the door root, so we can rotate "around the pivot" cleanly
        Quaternion baseRotation;
        Vector3 pivotWorldOffset; // pivot world position relative to door at spawn

        // Pending code-callback machinery
        Action<bool> _pendingCallback;

        public Transform Transform => transform;
        public string CodeString => Code.Value.ToString(codeDigitCount == 2 ? "D2" : "D3");

        // ICodeInteractable interface — tells CameraUI how many digits to expect
        public int CodeDigitCount => codeDigitCount;

        public override void OnNetworkSpawn()
        {
            // Cache the door's starting rotation; all swings are relative to this.
            baseRotation = transform.rotation;

            if (IsServer)
            {
                if (startsUnlocked)
                {
                    IsUnlocked.Value = true;
                    Code.Value = 0;
                }
                else if (isLabyrinthDoor)
                {
                    // Labyrinth doors use a 2-digit code from the fragment manager.
                    // The manager may not have spawned yet — try now, and subscribe so we
                    // catch the value when the digits get set.
                    codeDigitCount = 2;
                    TryPullLabyrinthCode();
                }
                else
                {
                    Code.Value = UnityEngine.Random.Range(0, 1000);
                    codeDigitCount = 3;
                }
                IsOpen.Value = false;
            }

            // Clients also need codeDigitCount synced for labyrinth doors so the
            // modal shows the right number of digits. (NetworkVariable would be cleaner,
            // but inspector-set + isLabyrinthDoor is enough since both sides know.)
            if (isLabyrinthDoor) codeDigitCount = 2;

            currentAngle = IsOpen.Value ? openAngle : closedAngle;
            targetAngle = currentAngle;
            ApplyAngle(currentAngle);

            IsOpen.OnValueChanged += HandleOpenChanged;
            IsUnlocked.OnValueChanged += HandleUnlockedChanged;
        }

        public override void OnNetworkDespawn()
        {
            IsOpen.OnValueChanged -= HandleOpenChanged;
            IsUnlocked.OnValueChanged -= HandleUnlockedChanged;

            // Clean up labyrinth subscriptions if any
            if (CodeFragmentManager.Instance != null)
            {
                CodeFragmentManager.Instance.Digit0.OnValueChanged -= OnLabyrinthDigitChanged;
                CodeFragmentManager.Instance.Digit1.OnValueChanged -= OnLabyrinthDigitChanged;
            }
        }

        // ===== Labyrinth code handling (server only) =====
        void TryPullLabyrinthCode()
        {
            if (!IsServer) return;
            var mgr = CodeFragmentManager.Instance;
            if (mgr == null) return;

            int full = mgr.GetFullCode();
            if (full > 0)
            {
                Code.Value = full;
            }
            else
            {
                // Manager hasn't generated yet — wait for digits to land.
                mgr.Digit0.OnValueChanged += OnLabyrinthDigitChanged;
                mgr.Digit1.OnValueChanged += OnLabyrinthDigitChanged;
            }
        }

        void OnLabyrinthDigitChanged(int prev, int now)
        {
            if (!IsServer) return;
            var mgr = CodeFragmentManager.Instance;
            if (mgr == null) return;

            int full = mgr.GetFullCode();
            if (full > 0)
            {
                Code.Value = full;
                // Once we have it, unsubscribe — the code is set.
                mgr.Digit0.OnValueChanged -= OnLabyrinthDigitChanged;
                mgr.Digit1.OnValueChanged -= OnLabyrinthDigitChanged;
            }
        }

        // ===== Animation =====
        void Update()
        {
            if (!Mathf.Approximately(currentAngle, targetAngle))
            {
                float speed = Mathf.Abs(openAngle - closedAngle) / Mathf.Max(swingDuration, 0.01f);
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed * Time.deltaTime);
                ApplyAngle(currentAngle);
            }
        }

        /// <summary>
        /// Rotates the door so it sits at 'yaw' degrees relative to its starting rotation.
        /// If a pivot transform is assigned, the door rotates around the pivot's world position
        /// (so the door swings open from one edge, like a real door).
        /// If no pivot is assigned, the door rotates around its own origin.
        /// </summary>
        void ApplyAngle(float yaw)
        {
            if (pivot != null && pivot != transform)
            {
                // Reset to base rotation, then rotate the whole door around the pivot's
                // world position by 'yaw' degrees on the world Y axis.
                transform.rotation = baseRotation;

                // After resetting rotation, we need to recompute the pivot world position
                // (it moves when we reset rotation since the pivot is a child).
                Vector3 pivotPos = pivot.position;
                transform.RotateAround(pivotPos, Vector3.up, yaw);
            }
            else
            {
                // No pivot — rotate door around its own center.
                Vector3 e = baseRotation.eulerAngles;
                transform.rotation = Quaternion.Euler(e.x, e.y + yaw, e.z);
            }
        }

        void HandleOpenChanged(bool prev, bool now)
        {
            targetAngle = now ? openAngle : closedAngle;
            PlayClip(now ? openClip : closeClip);
        }

        void HandleUnlockedChanged(bool prev, bool now)
        {
            if (_pendingCallback != null)
            {
                var cb = _pendingCallback;
                _pendingCallback = null;
                CancelInvoke(nameof(TimeoutCallback));
                cb.Invoke(now);
            }

            if (now && !prev) PlayClip(unlockedChimeClip);
        }

        void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        // ===== Camera operator click =====
        public void OnPlayer2Click()
        {
            if (IsUnlocked.Value)
            {
                RequestToggleOpenServerRpc();
            }
            else
            {
                if (CameraUI.Instance != null)
                    CameraUI.Instance.OpenCodeModal(this);
                else
                    PlayClip(lockedRattleClip);
            }
        }

        // ===== ICodeInteractable =====
        public void TryUnlock(string enteredCode, Action<bool> callback)
        {
            if (!int.TryParse(enteredCode, out int parsed))
            {
                callback?.Invoke(false);
                return;
            }

            if (IsUnlocked.Value)
            {
                callback?.Invoke(true);
                return;
            }

            SubmitCodeServerRpc(parsed);

            _pendingCallback = callback;
            Invoke(nameof(TimeoutCallback), 1.0f);
        }

        void TimeoutCallback()
        {
            if (_pendingCallback != null)
            {
                var cb = _pendingCallback;
                _pendingCallback = null;
                cb.Invoke(IsUnlocked.Value);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void SubmitCodeServerRpc(int submitted, ServerRpcParams rpcParams = default)
        {
            if (IsUnlocked.Value) return;
            if (submitted == Code.Value)
            {
                IsUnlocked.Value = true;
                IsOpen.Value = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void RequestToggleOpenServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsUnlocked.Value) return;
            IsOpen.Value = !IsOpen.Value;
        }

        // ===== First-person player interaction =====
        public void OnFirstPersonInteract()
        {
            if (!IsUnlocked.Value)
            {
                PlayClip(lockedRattleClip);
                return;
            }
            RequestToggleOpenServerRpc();
        }
    }
}