using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NewGen.Game
{
    /// <summary>Orthographic camera that follows the player. Zoom with the mouse wheel.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public float MinSize = 4f, MaxSize = 40f, Size = 10f;
        public Transform Target;
        public float Follow = 12f;

        public static CameraRig Create()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Cam = cam;
            rig.Cam.orthographicSize = rig.Size;
            // URP needs a camera data component; AddComponent is harmless if it already exists via GetUniversalAdditionalCameraData.
            cam.GetUniversalAdditionalCameraData();
            return rig;
        }

        public void Zoom(float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;
            Size = Mathf.Clamp(Size * (delta > 0 ? 0.9f : 1.1f), MinSize, MaxSize);
        }

        void LateUpdate()
        {
            Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, Size, Time.unscaledDeltaTime * 10f);
            if (Target == null) return;
            var p = transform.position;
            var t = Target.position;
            float k = 1f - Mathf.Exp(-Follow * Time.unscaledDeltaTime);
            transform.position = new Vector3(Mathf.Lerp(p.x, t.x, k), Mathf.Lerp(p.y, t.y, k), p.z);
        }

        public Vector3 MouseWorld(Vector2 screen)
        {
            var w = Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            w.z = 0f; return w;
        }

        /// <summary>World-space rectangle currently visible (with margin in tiles).</summary>
        public Rect VisibleRect(float margin)
        {
            float h = Cam.orthographicSize * 2f, w = h * Cam.aspect;
            var c = transform.position;
            return new Rect(c.x - w / 2f - margin, c.y - h / 2f - margin, w + margin * 2f, h + margin * 2f);
        }
    }
}
