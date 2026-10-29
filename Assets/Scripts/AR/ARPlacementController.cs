
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SolarSystemAR.AR
{
    public class ARPlacementController : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private Transform solarSystemRoot;

        private readonly List<ARRaycastHit> hits = new();

        private bool isPlaced;
        private bool isPlacing;

        private void Update()
        {
            if (isPlaced || isPlacing)
                return;

            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 position =
                    Touchscreen.current.primaryTouch.position.ReadValue();

                TryPlace(position);
                return;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlace(Mouse.current.position.ReadValue());
            }

            if (Keyboard.current != null &&
                Keyboard.current.pKey.wasPressedThisFrame)
            {
                TryPlace(new Vector2(
                    Screen.width / 2f,
                    Screen.height / 2f
                ));
            }
        }

        private async void TryPlace(Vector2 screenPosition)
        {
            if (raycastManager == null ||
                anchorManager == null ||
                solarSystemRoot == null)
            {
                Debug.LogError("AR references are missing!");
                return;
            }

            if (!raycastManager.Raycast(
                    screenPosition,
                    hits,
                    TrackableType.PlaneWithinPolygon))
            {
                Debug.Log("No AR plane found.");
                return;
            }

            isPlacing = true;

            try
            {
                Pose pose = hits[0].pose;

                var result =
                    await anchorManager.TryAddAnchorAsync(pose);

                if (!result.status.IsSuccess() ||
                    result.value == null)
                {
                    Debug.LogWarning("Failed to create AR Anchor.");
                    return;
                }

                ARAnchor anchor = result.value;

                // Attach the Solar System to the anchor
                solarSystemRoot.SetParent(anchor.transform, false);

                solarSystemRoot.localPosition =
                    Vector3.up * 0.03f;

                solarSystemRoot.localRotation =
                    Quaternion.identity;

                solarSystemRoot.gameObject.SetActive(true);

                isPlaced = true;

                Debug.Log("Solar System anchored successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                isPlacing = false;
            }
        }
    }
}
