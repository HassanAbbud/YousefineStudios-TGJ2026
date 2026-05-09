using System.Collections.Generic;
using UnityEngine;

namespace Player2
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("Render Target")]
        [Tooltip("Shared RenderTexture all CCTV cameras will render to. Assign one from the project.")]
        public RenderTexture feedTexture;

        [Header("Cameras")]
        [Tooltip("Auto-populated from CCTVCamera components in the scene. You can also drag them in manually.")]
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
            // Auto-find any CCTVCameras that didn't register via Register()
            // This catches cameras placed before CameraManager existed.
            var found = FindObjectsByType<CCTVCamera>(FindObjectsSortMode.None);
            foreach (var c in found)
            {
                if (!cameras.Contains(c)) cameras.Add(c);
                // Make sure every CCTV camera renders to our shared feed texture
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

            if (ActiveCamera != null) ActiveCamera.SetActive(false);

            ActiveIndex = index;
            ActiveCamera = cameras[index];
            ActiveCamera.Cam.targetTexture = feedTexture; // safety reassign
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