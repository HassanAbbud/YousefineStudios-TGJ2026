using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    [RequireComponent(typeof(Collider))]
    public class CodeFragment : NetworkBehaviour, IInteractable
    {
        [Header("Fragment Identity")]
        [Tooltip("Which digit position is this? 0 = tens, 1 = ones")]
        [Range(0, 1)]
        public int fragmentIndex = 0;

        [Header("Visual")]
        public TMP_Text digitDisplay;
        public GameObject[] visualsToHide;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip pickupClip;

        public override void OnNetworkSpawn()
        {
            UpdateVisuals();

            if (CodeFragmentManager.Instance != null)
            {
                CodeFragmentManager.Instance.Collected0.OnValueChanged += OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Collected1.OnValueChanged += OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Digit0.OnValueChanged += OnAnyDigitChanged;
                CodeFragmentManager.Instance.Digit1.OnValueChanged += OnAnyDigitChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (CodeFragmentManager.Instance != null)
            {
                CodeFragmentManager.Instance.Collected0.OnValueChanged -= OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Collected1.OnValueChanged -= OnAnyCollectedChanged;
                CodeFragmentManager.Instance.Digit0.OnValueChanged -= OnAnyDigitChanged;
                CodeFragmentManager.Instance.Digit1.OnValueChanged -= OnAnyDigitChanged;
            }
        }

        void OnAnyCollectedChanged(bool prev, bool now) => UpdateVisuals();
        void OnAnyDigitChanged(int prev, int now) => UpdateVisuals();

        void UpdateVisuals()
        {
            if (CodeFragmentManager.Instance == null) return;

            bool collected = CodeFragmentManager.Instance.IsCollected(fragmentIndex);

            if (visualsToHide != null)
                foreach (var v in visualsToHide)
                    if (v != null) v.SetActive(!collected);

            if (digitDisplay != null)
                digitDisplay.text = CodeFragmentManager.Instance.GetDigit(fragmentIndex).ToString();
        }

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