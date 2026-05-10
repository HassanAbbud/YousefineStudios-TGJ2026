using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Networked
{
    /// <summary>
    /// Displays a door's 3-digit code on a world-space sticky note.
    /// Reads the code from a linked InteractableDoor's NetworkVariable, so it
    /// auto-syncs to both players and updates if the code changes.
    /// </summary>
    public class CodeDisplayNote : MonoBehaviour
    {
        [Header("Linked Door")]
        [Tooltip("The door whose code this note displays.")]
        public InteractableDoor door;

        [Header("Display")]
        [Tooltip("TMP text component (3D or UI) that shows the digits.")]
        public TMP_Text codeLabel;

        [Tooltip("Optional: hide the note once the door is unlocked (sticky note 'used up').")]
        public bool hideWhenUnlocked = false;

        [Tooltip("GameObject(s) hidden when the door is unlocked. Defaults to this GameObject if empty.")]
        public GameObject[] visualsToHide;

        void Start()
        {
            if (door == null)
            {
                Debug.LogWarning($"[CodeDisplayNote] No door assigned on '{name}'");
                if (codeLabel != null) codeLabel.text = "???";
                return;
            }

            // Subscribe to changes so the note updates if the code regenerates
            door.Code.OnValueChanged += HandleCodeChanged;
            door.IsUnlocked.OnValueChanged += HandleUnlockedChanged;

            UpdateLabel();
            UpdateVisibility();
        }

        void OnDestroy()
        {
            if (door == null) return;
            door.Code.OnValueChanged -= HandleCodeChanged;
            door.IsUnlocked.OnValueChanged -= HandleUnlockedChanged;
        }

        void HandleCodeChanged(int prev, int now) => UpdateLabel();
        void HandleUnlockedChanged(bool prev, bool now) => UpdateVisibility();

        void UpdateLabel()
        {
            if (codeLabel == null || door == null) return;
            codeLabel.text = door.CodeString; // already formatted as "042", "153", etc.
        }

        void UpdateVisibility()
        {
            if (!hideWhenUnlocked) return;

            bool show = !door.IsUnlocked.Value;

            if (visualsToHide != null && visualsToHide.Length > 0)
            {
                foreach (var go in visualsToHide)
                    if (go != null) go.SetActive(show);
            }
            else
            {
                gameObject.SetActive(show);
            }
        }
    }
}