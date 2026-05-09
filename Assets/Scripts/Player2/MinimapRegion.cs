using UnityEngine;
using UnityEngine.UI;

namespace Player2
{
    [RequireComponent(typeof(Button))]
    public class MinimapRegion : MonoBehaviour
    {
        [Tooltip("Index into CameraManager.cameras (0 = first camera, 1 = second, etc.)")]
        public int cameraIndex;

        [Tooltip("Highlight overlay shown when this camera is active")]
        public GameObject activeHighlight;

        Button btn;

        void Awake()
        {
            btn = GetComponent<Button>();
            btn.onClick.AddListener(OnClicked);
        }

        void Start()
        {
            CameraManager.Instance.OnCameraChanged += HandleCameraChanged;
            HandleCameraChanged(CameraManager.Instance.ActiveIndex);
        }

        void OnDestroy()
        {
            if (CameraManager.Instance != null)
                CameraManager.Instance.OnCameraChanged -= HandleCameraChanged;
        }

        void OnClicked()
        {
            CameraManager.Instance.SwitchTo(cameraIndex);
        }

        void HandleCameraChanged(int newIndex)
        {
            if (activeHighlight != null)
                activeHighlight.SetActive(newIndex == cameraIndex);
        }
    }
}