using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating world-space suspicion bar above the NPC's head.
///
/// Setup:
///   1. Create a World Space Canvas as a child of the NPC.
///   2. Add a Slider (or raw Image with fillAmount) to that Canvas.
///   3. Assign references in Inspector.
///
/// The bar:
///   • Hidden while suspicion = 0
///   • Yellow  while filling  (Suspicious state)
///   • Red     when full      (Alarmed)
///   • Drains visually when player escapes
///   • Always faces the camera (billboard)
/// </summary>
public class SuspicionMeterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NPCController npcController;
    [SerializeField] private Canvas        worldCanvas;     // World Space Canvas child
    [SerializeField] private Image         fillImage;       // Fill image of the bar
    [SerializeField] private GameObject    meterRoot;       // Root to show/hide

    [Header("Colors")]
    [SerializeField] private Color suspiciousColor = new Color(1f, 0.85f, 0f);   // yellow
    [SerializeField] private Color alarmedColor    = Color.red;
    [SerializeField] private Color hiddenColor     = Color.clear;

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;

    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
        if (meterRoot != null) meterRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (npcController == null) return;

        float level = npcController.SuspicionNormalized;

        // Show / hide
        bool shouldShow = level > 0.01f;
        if (meterRoot != null && meterRoot.activeSelf != shouldShow)
            meterRoot.SetActive(shouldShow);

        if (!shouldShow) return;

        // Fill amount
        if (fillImage != null)
            fillImage.fillAmount = level;

        // Color
        if (fillImage != null)
        {
            fillImage.color = npcController.CurrentState == NPCController.NPCState.Alarmed
                ? alarmedColor
                : suspiciousColor;
        }

        // Billboard — rotate canvas to face camera
        if (faceCamera && _mainCam != null && worldCanvas != null)
        {
            worldCanvas.transform.LookAt(
                worldCanvas.transform.position + _mainCam.transform.forward
            );
        }
    }
}
