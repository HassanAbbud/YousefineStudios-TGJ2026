using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// A pickup placed in the world. Each fragment shows one digit (0-9) when collected.
    /// Implements IInteractable so the existing PlayerInteraction.cs raycast picks it up
    /// when the first-person player presses E.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CodeFragment : NetworkBehaviour, IInteractable
    {
        [Header("Fragment Identity")]
        [Tooltip("Which digit position is this? 0 = hundreds, 1 = tens, 2 = ones")]
        [Range(0, 2)]
        public int fragmentIndex = 0;

        [Header("Visual")]
        [Tooltip("Optional TMP text on the fragment that shows the digit (e.g. on a sticky note mesh)")]
        public TMP_Text digitDisplay;

        [Tooltip("GameObject(s) hidden when this fragment is collected (the visible mesh)")]
        public GameObject[] visualsToHide;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip pickupClip;

        public override void OnNetworkSpawn()
        {
            // Make sure the digit display reflects current state for late-joiners
            UpdateVisuals();

            if (CodeFragmentManager.Instance != null)
            {
                CodeFragmentManager.Instance.Collected0.OnValueChanged += OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Collected1.OnValueChanged += OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Collected2.OnValueChanged += OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Digit0.OnValueChanged += OnAnyDigitChanged;
                CodeFragmentManager.Instance.Digit1.OnValueChanged += OnAnyDigitChanged;
                CodeFragmentManager.Instance.Digit2.OnValueChanged += OnAnyDigitChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (CodeFragmentManager.Instance != null)
            {
                CodeFragmentManager.Instance.Collected0.OnValueChanged -= OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Collected1.OnValueChanged -= OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Collected2.OnValueChanged -= OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Digit0.OnValueChanged -= OnAnyDigitChanged;
                CodeFragmentManager.Instance.Digit1.OnValueChanged -= OnAnyDigitChanged;
                CodeFragmentManager.Instance.Digit2.OnValueChanged -= OnAnyDigitChanged;
            }
        }

        void OnAnyCollectedChanged(bool prev, bool now) => UpdateVisuals();
        void OnAnyDigitChanged(int prev, int now) => UpdateVisuals();

        void UpdateVisuals()
        {
            if (CodeFragmentManager.Instance == null) return;

            bool collected = CodeFragmentManager.Instance.IsCollected(fragmentIndex);

            // Hide the world mesh after collection
            if (visualsToHide != null)
                foreach (var v in visualsToHide)
                    if (v != null) v.SetActive(!collected);

            // Display the digit on the fragment itself (so the player sees it as they pick it up)
            if (digitDisplay != null)
                digitDisplay.text = CodeFragmentManager.Instance.GetDigit(fragmentIndex).ToString();
        }

        // ===== IInteractable (matches your existing PlayerInteraction.cs system) =====
        public string GetPromptText() => "[E] Pick up code fragment";

        public bool CanInteract(PlayerInteraction player)
        {
            if (CodeFragmentManager.Instance == null) return false;
            return !CodeFragmentManager.Instance.IsCollected(fragmentIndex);
        }

        public void Interact(PlayerInteraction player)
        {
            CollectServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void CollectServerRpc(ServerRpcParams rpcParams = default)
        {
            if (CodeFragmentManager.Instance == null) return;
            if (CodeFragmentManager.Instance.IsCollected(fragmentIndex)) return;

            CodeFragmentManager.Instance.MarkCollected(fragmentIndex);
            PlayPickupSoundClientRpc();
        }

        [ClientRpc]
        void PlayPickupSoundClientRpc()
        {
            if (audioSource != null && pickupClip != null)
                audioSource.PlayOneShot(pickupClip);
        }
    }
}