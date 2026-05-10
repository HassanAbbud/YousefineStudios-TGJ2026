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

        [Tooltip("Transform that actually rotates. If empty, this transform is used.")]
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

        // Pending code-callback machinery
        Action<bool> _pendingCallback;

        public Transform Transform => transform;
        public string CodeString => Code.Value.ToString(codeDigitCount == 2 ? "D2" : "D3");

        // ICodeInteractable interface — tells CameraUI how many digits to expect
        public int CodeDigitCount => codeDigitCount;

        public override void OnNetworkSpawn()
        {
            if (pivot == null) pivot = transform;

            if (IsServer)
            {
                if (startsUnlocked)
                {
                    IsUnlocked.Value = true;
                    Code.Value = 0;
                }
                else if (isLabyrinthDoor && CodeFragmentManager.Instance != null)
                {
                    // Labyrinth door pulls its 2-digit code from CodeFragmentManager
                    Code.Value = CodeFragmentManager.Instance.GetFullCode();
                    codeDigitCount = 2;
                }
                else
                {
                    Code.Value = UnityEngine.Random.Range(0, 1000);
                    codeDigitCount = 3;
                }
                IsOpen.Value = false;
            }

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
        }

        void Update()
        {
            if (!Mathf.Approximately(currentAngle, targetAngle))
            {
                float speed = Mathf.Abs(openAngle - closedAngle) / Mathf.Max(swingDuration, 0.01f);
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed * Time.deltaTime);
                ApplyAngle(currentAngle);
            }
        }

        void ApplyAngle(float yaw)
        {
            var e = pivot.localEulerAngles;
            pivot.localEulerAngles = new Vector3(e.x, yaw, e.z);
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