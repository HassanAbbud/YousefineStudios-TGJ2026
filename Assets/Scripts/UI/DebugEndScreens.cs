using UnityEngine;

namespace UI
{
    /// <summary>
    /// TEMP: press F1 to trigger Win, F2 to trigger Lose. For testing only — remove before final build.
    /// </summary>
    public class DebugEndScreens : MonoBehaviour
    {
        void Update()
        {
            if (GameStateManager.Instance == null) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("[Debug] Triggering Win");
                GameStateManager.Instance.TriggerWin();
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("[Debug] Triggering Lose");
                GameStateManager.Instance.TriggerLose();
            }
        }
    }
}