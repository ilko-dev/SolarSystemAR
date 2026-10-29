
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SolarSystemAR.AR
{
    public class ARScaleController : MonoBehaviour
    {
        [SerializeField] private Transform solarSystemRoot;

        [Header("Scale Settings")]
        [SerializeField] private float minScale = 0.025f;
        [SerializeField] private float maxScale = 0.1f;
        [SerializeField] private float keyboardStep = 1.1f;

        private float previousDistance;
        private bool isPinching;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (solarSystemRoot == null ||
                !solarSystemRoot.gameObject.activeInHierarchy)
            {
                isPinching = false;
                return;
            }

            HandleKeyboard();
            HandlePinch();
        }

        private void HandleKeyboard()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                ApplyScale(keyboardStep);

            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                ApplyScale(1f / keyboardStep);
        }

        private void HandlePinch()
        {
            Vector2 first = default;
            Vector2 second = default;
            int count = 0;

            foreach (var touch in Touch.activeTouches)
            {
                if (!touch.isInProgress)
                    continue;

                if (count == 0)
                    first = touch.screenPosition;
                else if (count == 1)
                    second = touch.screenPosition;

                count++;
            }

            if (count != 2)
            {
                isPinching = false;
                return;
            }

            float distance = Vector2.Distance(first, second);

            if (isPinching && previousDistance > 0f)
                ApplyScale(distance / previousDistance);

            previousDistance = distance;
            isPinching = true;
        }

        private void ApplyScale(float factor)
        {
            float newScale = Mathf.Clamp(
                solarSystemRoot.localScale.x * factor,
                minScale,
                maxScale
            );

            solarSystemRoot.localScale = Vector3.one * newScale;
        }
    }
}
