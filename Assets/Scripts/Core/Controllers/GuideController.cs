
using UnityEngine;

namespace SolarSystemAR.Core
{
    public enum GuideState
    {
        Idle,
        Listening,
        Talking,
        Pointing
    }

    public class GuideController : MonoBehaviour
    {
        [SerializeField] private AppController appController;
        [SerializeField] private SolarSystemController solarSystem;
        [SerializeField] private Renderer guideRenderer;

        [SerializeField] private float moveSpeed = 0.3f;
        [SerializeField] private Vector3 planetOffset =
            new Vector3(0.7f, 0f, 0.7f);

        private Transform targetPlanet;
        private Vector3 homePosition;
        private GuideState currentState;
        private float talkingUntil;

        private void Awake()
        {
            homePosition = transform.localPosition;
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

            if (talkingUntil > 0f && Time.time >= talkingUntil)
            {
                talkingUntil = 0f;
                SetState(targetPlanet != null
                    ? GuideState.Pointing
                    : GuideState.Idle);
            }
        }

        private void OnPlanetSelected(PlanetId id)
        {
            PlanetOrbit planet = solarSystem.GetPlanet(id);

            if (planet == null)
                return;

            targetPlanet = planet.transform;
            talkingUntil = 0f;
            SetState(GuideState.Pointing);
        }

        private void OnStateChanged(AppState state)
        {
            if (state.Mode != AppMode.Overview)
                return;

            targetPlanet = null;
            talkingUntil = 0f;
            SetState(GuideState.Idle);
        }

        private void OnNarrationRequested(string text)
        {
            SetState(GuideState.Talking);

            // Temporary duration until real audio is connected
            talkingUntil = Time.time +
                Mathf.Clamp(text.Length * 0.05f, 2f, 8f);
        }

        private void OnNarrationStopped()
        {
            talkingUntil = 0f;
            SetState(targetPlanet != null
                ? GuideState.Pointing
                : GuideState.Idle);
        }

        public void SetListening()
        {
            talkingUntil = 0f;
            SetState(GuideState.Listening);
        }

        private void SetState(GuideState state)
        {
            currentState = state;

            if (guideRenderer == null)
                return;

            guideRenderer.material.color = state switch
            {
                GuideState.Listening => Color.cyan,
                GuideState.Talking => Color.green,
                GuideState.Pointing => Color.yellow,
                _ => Color.white
            };
        }
    }
}
