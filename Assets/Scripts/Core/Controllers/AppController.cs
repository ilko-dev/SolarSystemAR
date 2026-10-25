
using System;
using UnityEngine;

namespace SolarSystemAR.Core
{
    public class AppController : MonoBehaviour, IAppController
    {
        [SerializeField]
        private SolarSystemController solarSystem;
        [SerializeField]
        private TourController tourController;

        private readonly AppState state = new();

        public AppState State => state;

        public event Action<AppState> StateChanged;
        public event Action<PlanetId> PlanetSelected;

        public void Execute(AppCommand command)
        {
            if (command == null)
                return;

            switch (command.Type)
            {
                case CommandType.SelectPlanet:
                    if (command.Planet.HasValue) {
                        tourController.StopTour();
                        SelectPlanet(command.Planet.Value);
                    }
                        
                    break;

                case CommandType.PauseOrbits:
                    solarSystem.Pause();
                    state.IsOrbitPaused = true;
                    NotifyStateChanged();
                    break;

                case CommandType.ResumeOrbits:
                    solarSystem.Resume();
                    state.IsOrbitPaused = false;
                    NotifyStateChanged();
                    break;

                case CommandType.ShowOverview:
                    ShowOverview();
                    break;

                case CommandType.StartTour:
                    tourController.StartTour();
                    break;

                case CommandType.NextPlanet:
                    tourController.NextPlanet();
                    break;

                case CommandType.PreviousPlanet:
                    tourController.PreviousPlanet();
                    break;

                default:
                    Debug.Log(
                        $"Command not implemented yet: {command.Type}");
                    break;
            }
        }

        private void SelectPlanet(
            PlanetId id,
            AppMode mode = AppMode.PlanetSelected)
        {
            PlanetOrbit planet = solarSystem.GetPlanet(id);

            if (planet == null)
            {
                Debug.LogWarning($"Planet not found: {id}");
                return;
            }

            state.SelectedPlanet = id;
            state.Mode = mode;

            PlanetSelected?.Invoke(id);
            NotifyStateChanged();

            Debug.Log($"Selected planet: {id}");
        }

        private void ShowOverview()
        {
            tourController.StopTour();

            state.SelectedPlanet = null;
            state.Mode = AppMode.Overview;
            state.TourIndex = 0;

            NotifyStateChanged();

            Debug.Log("Showing Solar System overview");
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(state);
        }

        private void OnEnable()
        {
            if (tourController != null)
                tourController.TourPlanetChanged += OnTourPlanetChanged;
        }

        private void OnDisable()
        {
            if (tourController != null)
                tourController.TourPlanetChanged -= OnTourPlanetChanged;
        }

        private void OnTourPlanetChanged(PlanetId planet)
        {
            state.TourIndex = tourController.CurrentIndex;
            SelectPlanet(planet, AppMode.GuidedTour);
        }
    }
}
