
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SolarSystemAR.Core;

namespace SolarSystemAR.AR
{
    public class ARTrackingController : MonoBehaviour
    {
        [SerializeField] private AppController appController;
        [SerializeField] private Transform solarSystemRoot;

        [Header("Debug")]
        [SerializeField] private bool simulateTrackingLoss;

        private bool wasTracking;
        private bool showWarning;

        private void Update()
        {
            bool sessionTracking =
                ARSession.state == ARSessionState.SessionTracking;

            // Check the anchor after placing the Solar System
            ARAnchor anchor = solarSystemRoot != null
                ? solarSystemRoot.GetComponentInParent<ARAnchor>()
                : null;

            bool anchorTracking = anchor == null ||
                anchor.trackingState == TrackingState.Tracking;

            bool isTracking = sessionTracking &&
                anchorTracking &&
                !simulateTrackingLoss;

            if (isTracking)
                wasTracking = true;

            showWarning = wasTracking && !isTracking;

            if (appController != null)
                appController.SetTrackingAvailable(isTracking);
        }

        private void OnGUI()
        {
            if (!showWarning)
                return;

            GUI.Box(
                new Rect(20, 20, 360, 70),
                "AR tracking lost!\n" +
                "Move your device slowly to restore tracking."
            );
        }
    }
}
