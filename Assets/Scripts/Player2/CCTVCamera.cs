using UnityEngine;

namespace Player2
{
    public enum CCTVType { Static, Rotating }

    [RequireComponent(typeof(Camera))]
    public class CCTVCamera : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Display name shown on the minimap, e.g. 'Kitchen', 'Hallway A'")]
        public string cameraName = "Camera";

        [Header("Type")]
        public CCTVType type = CCTVType.Static;

        [Header("Rotating Settings (only used if type is Rotating)")]
        [Tooltip("Local Y rotation (degrees) at point A")]
        public float rotationA = -45f;
        [Tooltip("Local Y rotation (degrees) at point B")]
        public float rotationB = 45f;
        [Tooltip("Seconds to travel from A to B")]
        public float rotationDuration = 4f;
        [Tooltip("Pause time at each end before swinging back")]
        public float pauseAtEnds = 0.5f;

        Camera cam;
        float rotationTimer;
        bool goingToB = true;
        float pauseTimer;

        void Awake()
        {
            cam = GetComponent<Camera>();
            // Cameras start disabled; CameraManager enables one at a time
            cam.enabled = false;
        }

        public Camera Cam => cam;

        public void SetActive(bool active)
        {
            cam.enabled = active;
        }

        void Update()
        {
            if (type != CCTVType.Rotating) return;

            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                return;
            }

            rotationTimer += Time.deltaTime / rotationDuration;
            if (rotationTimer >= 1f)
            {
                rotationTimer = 0f;
                goingToB = !goingToB;
                pauseTimer = pauseAtEnds;
            }

            float t = goingToB ? rotationTimer : 1f - rotationTimer;
            // Smooth ease-in-out so it doesn't look robotic
            t = Mathf.SmoothStep(0f, 1f, t);
            float yaw = Mathf.Lerp(rotationA, rotationB, t);

            var localEuler = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(localEuler.x, yaw, localEuler.z);
        }

#if UNITY_EDITOR
        // Visualize the rotation arc in the editor for level designers
        void OnDrawGizmosSelected()
        {
            if (type != CCTVType.Rotating) return;
            Gizmos.color = Color.yellow;
            var parent = transform.parent;
            Quaternion baseRot = parent ? parent.rotation : Quaternion.identity;
            Vector3 origin = transform.position;
            Vector3 dirA = baseRot * Quaternion.Euler(0, rotationA, 0) * Vector3.forward;
            Vector3 dirB = baseRot * Quaternion.Euler(0, rotationB, 0) * Vector3.forward;
            Gizmos.DrawLine(origin, origin + dirA * 3f);
            Gizmos.DrawLine(origin, origin + dirB * 3f);
        }
#endif
    }
}