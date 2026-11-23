
using UnityEngine;

namespace SolarSystemAR.Core
{
    public enum GuideState
    {
        Idle,
        Flying,
        Listening,
        Talking,
        Pointing
    }

    public class GuideController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AppController appController;
        [SerializeField] private SolarSystemController solarSystem;
        [SerializeField] private Animator animator;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 0.3f;
        [SerializeField] private float arrivalDistance = 0.005f;
        [SerializeField] private Vector3 planetOffset =
            new Vector3(0.7f, 0f, 0.7f);

        [Header("Floating (when still)")]
        [Tooltip("How far up and down he floats, in the same units as Planet Offset.")]
        [SerializeField] private float floatHeight = 0.04f;
        [SerializeField] private float floatPeriod = 3f;
        [Tooltip("Seconds to fade the floating in after arriving (and out when taking off).")]
        [SerializeField] private float floatFade = 0.5f;

        [Header("Turning")]
        [Tooltip("How quickly he turns. Higher = snappier.")]
        [SerializeField] private float turnSharpness = 6f;
        [Tooltip("Extra turn (degrees) so the model's face points forward. 180 because the astronaut model " +
                 "inside this object is turned around (its face looks along -Z).")]
        [SerializeField] private float facingOffset = 180f;

        private Transform targetPlanet;
        private Vector3 homePosition;

        private float floatWeight;      // 0 = no floating (flying), 1 = full floating (still)
        private Vector3 floatOffset;    // added to the position last frame

        private GuideState? currentState;
        private float talkingUntil;
        private bool isListening;

        // Set once the voice system reports real speech; then the talking timer is not used.
        private bool speechDriven;
        private bool isTalking;

        private void Awake()
        {
            homePosition = transform.localPosition;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (appController == null)
                return;

            appController.PlanetSelected += OnPlanetSelected;
            appController.StateChanged += OnStateChanged;
            appController.NarrationRequested += OnNarrationRequested;
            appController.NarrationStopped += OnNarrationStopped;
        }

        private void OnDisable()
        {
            if (appController == null)
                return;

            appController.PlanetSelected -= OnPlanetSelected;
            appController.StateChanged -= OnStateChanged;
            appController.NarrationRequested -= OnNarrationRequested;
            appController.NarrationStopped -= OnNarrationStopped;
        }

        private void Start()
        {
            SetState(GuideState.Idle);
        }

        private void Update()
        {
            if (transform.parent == null)
                return;

            Vector3 destination = targetPlanet != null
                ? targetPlanet.position +
                  transform.parent.TransformVector(planetOffset)
                : transform.parent.TransformPoint(homePosition);

            // Take last frame's float offset off, so moving and arriving use the real position.
            transform.position -= floatOffset;
            Vector3 previousPosition = transform.position;

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );

            bool isMoving =
                Vector3.Distance(transform.position, destination)
                > arrivalDistance;

            if (isMoving)
                TurnTowards(transform.position - previousPosition);
            else if (Camera.main != null)
                TurnTowards(Camera.main.transform.position - transform.position); // look at the viewer

            UpdateFloating(isMoving);

            if (isMoving)
            {
                SetState(GuideState.Flying);
            }
            else if (isListening)
            {
                SetState(GuideState.Listening);
            }
            else if (speechDriven ? isTalking : Time.time < talkingUntil)
            {
                SetState(GuideState.Talking);
            }
            else
            {
                SetState(targetPlanet != null
                    ? GuideState.Pointing
                    : GuideState.Idle);
            }
        }

        // Smoothly turns him left/right (around the solar system's up axis) to face a direction.
        // He never tips up or down, so the Blender animations still look upright.
        private void TurnTowards(Vector3 direction)
        {
            Vector3 up = transform.parent.up;
            Vector3 flat = Vector3.ProjectOnPlane(direction, up);
            if (flat.sqrMagnitude < 1e-8f)
                return;

            Quaternion target = Quaternion.LookRotation(flat, up) * Quaternion.Euler(0f, facingOffset, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                target,
                1f - Mathf.Exp(-turnSharpness * Time.deltaTime)
            );
        }

        // Gentle up-and-down bob while he hangs next to a planet (or at home).
        // Moves the whole astronaut, on top of the Blender animation.
        private void UpdateFloating(bool isMoving)
        {
            floatWeight = Mathf.MoveTowards(
                floatWeight,
                isMoving ? 0f : 1f,
                Time.deltaTime / Mathf.Max(floatFade, 0.01f)
            );

            float wave = Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(floatPeriod, 0.1f));
            floatOffset = transform.parent.TransformVector(Vector3.up * (wave * floatHeight * floatWeight));
            transform.position += floatOffset;
        }

        private void OnPlanetSelected(PlanetId id)
        {
            if (solarSystem == null)
                return;

            PlanetOrbit planet = solarSystem.GetPlanet(id);

            if (planet == null)
                return;

            targetPlanet = planet.transform;
            isListening = false;
            talkingUntil = 0f;
        }

        private void OnStateChanged(AppState state)
        {
            if (state.Mode != AppMode.Overview)
                return;

            targetPlanet = null;
            isListening = false;
            talkingUntil = 0f;
        }

        private void OnNarrationRequested(string text)
        {
            isListening = false;

            // Guessed duration, used only when no voice system reports real speech (SetTalking).
            talkingUntil = Time.time +
                Mathf.Clamp(
                    (text?.Length ?? 0) * 0.05f,
                    2f,
                    8f
                );
        }

        private void OnNarrationStopped()
        {
            talkingUntil = 0f;
            isListening = false;
        }

        public void SetListening()
        {
            isListening = true;
            talkingUntil = 0f;
        }

        public void StopListening()
        {
            isListening = false;
        }

        /// <summary>Called by the voice system when the voice starts and stops speaking.</summary>
        public void SetTalking(bool talking)
        {
            speechDriven = true;
            isTalking = talking;

            if (talking)
                isListening = false;
        }

        private void SetState(GuideState state)
        {
            if (currentState == state)
                return;

            currentState = state;

            if (animator == null)
                return;

            string animationName = state switch
            {
                GuideState.Flying => "Model|Fly",
                GuideState.Listening => "Model|Thinking",
                GuideState.Talking => "Model|Talking",
                GuideState.Pointing => "Model|Pointing",
                _ => "Model|Idle_Breathing"
            };

            animator.CrossFadeInFixedTime(
                $"Base Layer.{animationName}",
                0.15f
            );

            Debug.Log($"Guide state: {state}");
        }
    }
}
