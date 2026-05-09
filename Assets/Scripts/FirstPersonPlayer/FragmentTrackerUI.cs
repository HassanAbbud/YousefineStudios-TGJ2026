using Networked;
using TMPro;
using UnityEngine;

namespace Player1
{
    /// <summary>
    /// HUD overlay shown to the first-person player. Displays the labyrinth code
    /// digits as they're collected: "_ _ _" → "5 _ _" → "5 2 _" → "5 2 8".
    /// </summary>
    public class FragmentTrackerUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;          // The container (hidden if no fragments collected yet)
        public TMP_Text digitsLabel;     // Displays "5 2 8" with underscores for missing

        [Header("Optional Status")]
        public TMP_Text statusLabel;     // "Find more digits..." / "Code complete!"

        void Update()
        {
            var mgr = CodeFragmentManager.Instance;
            if (mgr == null)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            bool anyCollected = mgr.IsCollected(0) || mgr.IsCollected(1) || mgr.IsCollected(2);
            if (root != null) root.SetActive(anyCollected);
            if (!anyCollected) return;

            string d0 = mgr.IsCollected(0) ? mgr.GetDigit(0).ToString() : "_";
            string d1 = mgr.IsCollected(1) ? mgr.GetDigit(1).ToString() : "_";
            string d2 = mgr.IsCollected(2) ? mgr.GetDigit(2).ToString() : "_";

            if (digitsLabel != null) digitsLabel.text = $"{d0}  {d1}  {d2}";

            if (statusLabel != null)
            {
                statusLabel.text = mgr.AllCollected()
                    ? "Tell your partner the code!"
                    : "Find more digits...";
            }
        }
    }
}