using System.Collections.Generic;
using UnityEngine;

namespace Player2
{
    /// <summary>
    /// Manages all CCTV cameras and which one is active.
    ///
    /// CAMERA ORDER:
    /// To control button-to-camera mapping, drag cameras into the 'cameras' list
    /// in the Inspector in the order you want them (index 0 = button 1, etc.).
    /// If left empty, the manager auto-fills from the scene (alphabetical order).
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("Render Target")]
        [Tooltip("Shared RenderTexture all CCTV cameras will render to. Drag CCTV_Feed here.")]
        public RenderTexture feedTexture;

        [Header("Cameras (drop in order — index 0 = first button, etc.)")]
        [Tooltip("Drag CCTV_Cam_01, CCTV_Cam_02, ... into this list in the order you want " +
                 "them mapped to the minimap buttons. If left empty, cameras will be auto-found " +
                 "in alphabetical order.")]
        public List<CCTVCamera> cameras = new();

        public CCTVCamera ActiveCamera { get; private set; }
        public int ActiveIndex { get; private set; } = -1;

        public System.Action<int> OnCameraChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            // If the inspector list is empty, auto-fill from the scene (alphabetical order
            // by name, so CCTV_Cam_01 < CCTV_Cam_02 < ...).
            if (cameras == null || cameras.Count == 0)
            {
                cameras = new List<CCTVCamera>();
                var found = FindObjectsByType<CCTVCamera>(FindObjectsSortMode.None);
                System.Array.Sort(found, (a, b) =>
                    string.Compare(a.name, b.name, System.StringComparison.Ordinal));
                cameras.AddRange(found);
            }

            // Bind every camera to the shared feed texture.
            foreach (var c in cameras)
            {
                if (c == null) continue;
                c.Cam.targetTexture = feedTexture;
            }

            if (cameras.Count > 0) SwitchTo(0);
        }

        public void Register(CCTVCamera cam)
        {
            if (!cameras.Contains(cam)) cameras.Add(cam);
        }

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= cameras.Count) return;
            if (index == ActiveIndex) return;
            if (cameras[index] == null) return;

            if (ActiveCamera != null) ActiveCamera.SetActive(false);

            ActiveIndex = index;
            ActiveCamera = cameras[index];
            ActiveCamera.Cam.targetTexture = feedTexture;
            ActiveCamera.SetActive(true);

            OnCameraChanged?.Invoke(index);
        }

        public CCTVCamera GetCamera(int index)
        {
            if (index < 0 || index >= cameras.Count) return null;
            return cameras[index];
        }
    }
}