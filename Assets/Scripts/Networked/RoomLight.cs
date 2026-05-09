using Shared;
using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// Networked light. Camera operator can click it via the feed; first-person player can flip it in person.
    /// Server-authoritative state. Used by NPC AI for detection multiplier.
    /// </summary>
    public class RoomLight : NetworkBehaviour, IPlayer2Interactable
    {
        [Header("Identity")]
        public string lightName = "Light";

        [Header("Lights to Toggle")]
        [Tooltip("All Light components affected by this switch")]
        public Light[] lights;

        [Tooltip("Optional emissive bulb meshes")]
        public Renderer[] emissiveRenderers;

        [Header("Emissive Settings")]
        public Color emissiveOnColor = Color.white;
        public float emissiveOnIntensity = 5f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip switchOnClip;
        public AudioClip switchOffClip;

        [Header("Initial State")]
        public bool startsOn = true;

        public NetworkVariable<bool> IsOn = new NetworkVariable<bool>(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public Transform Transform => transform;

        public override void OnNetworkSpawn()
        {
            if (IsServer) IsOn.Value = startsOn;

            ApplyState(IsOn.Value, playSound: false);
            IsOn.OnValueChanged += HandleStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            IsOn.OnValueChanged -= HandleStateChanged;
        }

        void HandleStateChanged(bool prev, bool now)
        {
            ApplyState(now, playSound: true);
        }

        void ApplyState(bool on, bool playSound)
        {
            if (lights != null)
            {
                foreach (var l in lights)
                    if (l != null) l.enabled = on;
            }

            if (emissiveRenderers != null)
            {
                foreach (var r in emissiveRenderers)
                {
                    if (r == null) continue;
                    foreach (var mat in r.materials)
                    {
                        if (mat.HasProperty("_EmissiveColor"))
                        {
                            mat.SetColor("_EmissiveColor",
                                on ? emissiveOnColor * emissiveOnIntensity : Color.black);
                        }
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor",
                                on ? emissiveOnColor * emissiveOnIntensity : Color.black);
                            if (on) mat.EnableKeyword("_EMISSION");
                            else mat.DisableKeyword("_EMISSION");
                        }
                    }
                }
            }

            if (playSound && audioSource != null)
            {
                var clip = on ? switchOnClip : switchOffClip;
                if (clip != null) audioSource.PlayOneShot(clip);
            }
        }

        public void OnPlayer2Click()
        {
            RequestToggleServerRpc();
        }

        public void OnFirstPersonInteract()
        {
            RequestToggleServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void RequestToggleServerRpc(ServerRpcParams rpcParams = default)
        {
            IsOn.Value = !IsOn.Value;
        }

        /// <summary>
        /// Used by NPC AI: detection range scales by this. Dark room = harder to spot.
        /// </summary>
        public float DetectionMultiplier => IsOn.Value ? 1.0f : 0.4f;
    }
}