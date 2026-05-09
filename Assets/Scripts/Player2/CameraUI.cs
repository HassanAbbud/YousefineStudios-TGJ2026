using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Shared;

namespace Player2
{
    public class CameraUI : MonoBehaviour, IPointerClickHandler
    {
        public static CameraUI Instance { get; private set; }

        [Header("Feed Display")]
        [Tooltip("RawImage that shows the CCTV feed RenderTexture")]
        public RawImage feedImage;
        public RectTransform feedRect;

        [Header("Camera Label")]
        public TMP_Text cameraNameLabel;

        [Header("Code Input Modal")]
        public GameObject codeModal;
        public TMP_InputField codeInput;
        public TMP_Text codeFeedbackLabel;
        public Button codeSubmitButton;
        public Button codeCancelButton;

        [Header("Interaction")]
        [Tooltip("Layers that Player 1 can click through the camera view (doors, lights)")]
        public LayerMask interactableLayers = ~0;
        [Tooltip("Max distance the click-raycast travels from the CCTV camera")]
        public float maxInteractionDistance = 50f;

        // Generic pending interactable that needs a code (decoupled from Networked namespace).
        ICodeInteractable pendingCodeTarget;

        void Awake()
        {
            Instance = this;

            if (feedImage != null) feedImage.raycastTarget = true;
            if (codeModal != null) codeModal.SetActive(false);
            if (codeSubmitButton != null) codeSubmitButton.onClick.AddListener(SubmitCode);
            if (codeCancelButton != null) codeCancelButton.onClick.AddListener(CancelCode);
        }

        void Start()
        {
            CameraManager.Instance.OnCameraChanged += HandleCameraChanged;
            HandleCameraChanged(CameraManager.Instance.ActiveIndex);

            if (feedImage != null && CameraManager.Instance.feedTexture != null)
                feedImage.texture = CameraManager.Instance.feedTexture;
        }

        void OnDestroy()
        {
            if (CameraManager.Instance != null)
                CameraManager.Instance.OnCameraChanged -= HandleCameraChanged;
            if (Instance == this) Instance = null;
        }

        void HandleCameraChanged(int newIndex)
        {
            var cam = CameraManager.Instance.GetCamera(newIndex);
            if (cam != null && cameraNameLabel != null)
                cameraNameLabel.text = cam.cameraName;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (codeModal != null && codeModal.activeSelf) return;

            var activeCam = CameraManager.Instance.ActiveCamera;
            if (activeCam == null) return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                feedRect, eventData.position, eventData.pressEventCamera, out localPoint))
                return;

            Rect r = feedRect.rect;
            float u = (localPoint.x - r.x) / r.width;
            float v = (localPoint.y - r.y) / r.height;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return;

            Ray ray = activeCam.Cam.ViewportPointToRay(new Vector3(u, v, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance, interactableLayers))
            {
                var interactable = hit.collider.GetComponentInParent<IPlayer2Interactable>();
                if (interactable != null) interactable.OnPlayer2Click();
            }
        }

        // Generic code-modal interface so CameraUI doesn't depend on the Networked namespace.
        public void OpenCodeModal(ICodeInteractable target)
        {
            pendingCodeTarget = target;
            if (codeInput != null)
            {
                codeInput.text = "";
                codeInput.Select();
                codeInput.ActivateInputField();
            }
            if (codeFeedbackLabel != null) codeFeedbackLabel.text = "";
            if (codeModal != null) codeModal.SetActive(true);
        }

        void SubmitCode()
        {
            if (pendingCodeTarget == null) { CancelCode(); return; }
            string entered = codeInput != null ? codeInput.text.Trim() : "";
            if (entered.Length != 3 || !int.TryParse(entered, out _))
            {
                if (codeFeedbackLabel != null) codeFeedbackLabel.text = "Enter 3 digits.";
                return;
            }
            pendingCodeTarget.TryUnlock(entered, success =>
            {
                if (success)
                {
                    if (codeFeedbackLabel != null) codeFeedbackLabel.text = "Unlocked.";
                    Invoke(nameof(CloseModalDelayed), 0.4f);
                }
                else
                {
                    if (codeFeedbackLabel != null) codeFeedbackLabel.text = "Wrong code.";
                }
            });
        }

        void CloseModalDelayed() => CancelCode();

        void CancelCode()
        {
            if (codeModal != null) codeModal.SetActive(false);
            pendingCodeTarget = null;
        }
    }

    /// <summary>
    /// Implemented by anything that can show the code modal (doors, etc.).
    /// Lets CameraUI stay decoupled from the Networked namespace.
    /// </summary>
    public interface ICodeInteractable
    {
        void TryUnlock(string enteredCode, System.Action<bool> callback);
    }
}