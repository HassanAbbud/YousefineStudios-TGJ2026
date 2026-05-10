using Networked;
using TMPro;
using UnityEngine;

namespace Player1
{
    public class FragmentTrackerUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;
        public TMP_Text digitsLabel;

        [Header("Optional Status")]
        public TMP_Text statusLabel;

        void Update()
        {
            var mgr = CodeFragmentManager.Instance;
            if (mgr == null)
            {
                if (root != null) root.SetActive(false);
                return;
            }

            bool anyCollected = mgr.IsCollected(0) || mgr.IsCollected(1);
            if (root != null) root.SetActive(anyCollected);
            if (!anyCollected) return;

            string d0 = mgr.IsCollected(0) ? mgr.GetDigit(0).ToString() : "_";
            string d1 = mgr.IsCollected(1) ? mgr.GetDigit(1).ToString() : "_";

            if (digitsLabel != null) digitsLabel.text = $"{d0}  {d1}";

            if (statusLabel != null)
            {
                statusLabel.text = mgr.AllCollected()
                    ? "Tell your partner the code!"
                    : "Find more digits...";
            }
        }
    }
}