
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

        private Transform targetPlanet;
        private Vector3 homePosition;

        private GuideState? currentState;
        private float talkingUntil;
        private bool isListening;

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

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );

            bool isMoving =
                Vector3.Distance(transform.position, destination)
                > arrivalDistance;

            if (isMoving)
            {
                SetState(GuideState.Flying);
            }
            else if (isListening)
            {
                SetState(GuideState.Listening);
            }
            else if (Time.time < talkingUntil)
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

            // Temporary duration until audio integration
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
