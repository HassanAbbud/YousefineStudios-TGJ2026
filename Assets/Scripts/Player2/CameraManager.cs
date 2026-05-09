using System.Collections.Generic;
using UnityEngine;

namespace Player2
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("Render Target")]
        [Tooltip("Shared RenderTexture all CCTV cameras will render to. Drag CCTV_Feed here.")]
        public RenderTexture feedTexture;

        [Header("Cameras")]
        [Tooltip("Auto-populated from CCTVCamera components in the scene.")]
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
            var found = FindObjectsByType<CCTVCamera>(FindObjectsSortMode.None);
            foreach (var c in found)
            {
                if (!cameras.Contains(c)) cameras.Add(c);
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