
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SolarSystemAR.AR
{
    public class ARDiagnostics : MonoBehaviour
    {
        [SerializeField]
        private ARCameraManager cameraManager;

        private int framesReceived;
        private float lastFrameTime = -1f;

        private void OnEnable()
        {
            if (cameraManager != null)
                cameraManager.frameReceived += OnFrameReceived;
        }

        private void OnDisable()
        {
            if (cameraManager != null)
                cameraManager.frameReceived -= OnFrameReceived;
        }

        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            framesReceived++;
            lastFrameTime = Time.realtimeSinceStartup;
        }

        private void OnGUI()
        {
            bool permission = cameraManager != null &&
                              cameraManager.permissionGranted;

            bool cameraRunning = cameraManager != null &&
                                 cameraManager.subsystem != null &&
                                 cameraManager.subsystem.running;

            bool recentFrame = lastFrameTime >= 0 &&
                Time.realtimeSinceStartup - lastFrameTime < 2f;

            string info =
                "AR DIAGNOSTICS\n" +
                $"Session: {ARSession.state}\n" +
                $"Tracking reason: {ARSession.notTrackingReason}\n" +
                $"Camera permission: {permission}\n" +
                $"Camera running: {cameraRunning}\n" +
                $"Camera frames: {framesReceived}\n" +
                $"Recent frame: {recentFrame}";

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = Mathf.Max(20, Screen.width / 35);
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = true;
            style.normal.textColor = Color.white;

            GUI.Box(
                new Rect(15, Screen.height * 0.12f,
                         Screen.width - 30, Screen.height * 0.38f),
                info,
                style
            );
        }
    }
}
