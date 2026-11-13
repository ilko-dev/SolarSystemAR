
using System;
using UnityEngine;

namespace SolarSystemAR.Core
{
    public class AppController : MonoBehaviour, IAppController
    {
        [SerializeField] private SolarSystemController solarSystem;
        [SerializeField] private TourController tourController;
        [SerializeField] private PlanetData[] planetCatalog;

        private readonly AppState state = new AppState();
        private string lastNarrationText;

        public AppState State => state;
        public string LastNarrationText => lastNarrationText;

        public event Action<AppState> StateChanged;
        public event Action<PlanetId> PlanetSelected;

        // Temporary integration points for Artem's audio system
        public event Action<string> NarrationRequested;
        public event Action NarrationStopped;

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

        public void Execute(AppCommand command)
        {
            if (command == null)
                return;

            switch (command.Type)
            {
                case CommandType.SelectPlanet:
                    if (command.Planet.HasValue)
                    {
                        tourController?.StopTour();
                        SelectPlanet(command.Planet.Value);
                    }
                    break;

                case CommandType.StartTour:
                    tourController?.StartTour();
                    break;

                case CommandType.NextPlanet:
                    tourController?.NextPlanet();
                    break;

                case CommandType.PreviousPlanet:
                    tourController?.PreviousPlanet();
                    break;

                case CommandType.PauseOrbits:
                    solarSystem?.Pause();
                    state.IsOrbitPaused = true;
                    NotifyStateChanged();
                    break;

                case CommandType.ResumeOrbits:
                    solarSystem?.Resume();
                    state.IsOrbitPaused = false;
                    NotifyStateChanged();
                    break;

                case CommandType.ShowOverview:
                    ShowOverview();
                    break;

                case CommandType.ShowFact:
                    SpeakPlanetInfo(command.Planet, false);
                    break;

                case CommandType.ShowHistory:
                    SpeakPlanetInfo(command.Planet, true);
                    break;

                case CommandType.AskQuestion:
                    AnswerQuestion(command.Planet, command.QuestionId);
                    break;

                case CommandType.StopNarration:
                    NarrationStopped?.Invoke();
                    Debug.Log("Narration stopped.");
                    break;

                case CommandType.RepeatNarration:
                    if (!string.IsNullOrEmpty(lastNarrationText))
                        NarrationRequested?.Invoke(lastNarrationText);
                    break;

                case CommandType.Cancel:
                    ShowOverview();
                    break;

                case CommandType.EnterDetailView:
                    if (state.SelectedPlanet.HasValue)
                    {
                        state.Mode = AppMode.DetailView;
                        NotifyStateChanged();
                    }
                    break;

                case CommandType.ExitDetailView:
                    if (state.SelectedPlanet.HasValue)
                    {
                        state.Mode = AppMode.PlanetSelected;
                        NotifyStateChanged();
                    }
                    else
                    {
                        ShowOverview();
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown command type: {command.Type}");
                    break;
            }
        }

        private void SelectPlanet(
            PlanetId id,
            AppMode mode = AppMode.PlanetSelected)
        {
            if (solarSystem == null ||
                solarSystem.GetPlanet(id) == null)
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

        private void OnTourPlanetChanged(PlanetId id)
        {
            state.TourIndex = tourController.CurrentIndex;
            SelectPlanet(id, AppMode.GuidedTour);
            SpeakPlanetInfo(id, false);
        }

        private void ShowOverview()
        {
            tourController?.StopTour();

            state.SelectedPlanet = null;
            state.Mode = AppMode.Overview;
            state.TourIndex = 0;

            NarrationStopped?.Invoke();
            NotifyStateChanged();
        }

        public PlanetData GetSelectedPlanetData()
        {
            if (!state.SelectedPlanet.HasValue)
                return null;

            return GetPlanetData(state.SelectedPlanet.Value);
        }

        private PlanetData GetPlanetData(PlanetId id)
        {
            if (planetCatalog == null)
                return null;

            foreach (var data in planetCatalog)
            {
                if (data != null && data.id == id)
                    return data;
            }

            return null;
        }

        private PlanetData ResolvePlanetData(PlanetId? requested)
        {
            PlanetId? id = requested ?? state.SelectedPlanet;

            if (!id.HasValue)
            {
                Speak("Уточніть, про яку планету йдеться.");
                return null;
            }

            PlanetData data = GetPlanetData(id.Value);

            if (data == null)
                Speak($"Немає довідкових даних для {id.Value}.");

            return data;
        }

        private void SpeakPlanetInfo(PlanetId? planet, bool history)
        {
            PlanetData data = ResolvePlanetData(planet);

            if (data == null)
                return;

            string text = history
                ? data.discoveryHistory
                : data.interestingFact;

            Speak(string.IsNullOrWhiteSpace(text)
                ? "Ця інформація ще не підготовлена."
                : text);
        }

        private void AnswerQuestion(PlanetId? planet, string questionId)
        {
            PlanetData data = ResolvePlanetData(planet);

            if (data == null)
                return;

            if (data.questions != null)
            {
                foreach (var question in data.questions)
                {
                    if (question != null && question.id == questionId)
                    {
                        Speak(question.answer);
                        return;
                    }
                }
            }

            Speak("Оберіть доступне запитання з меню.");
        }

        private void Speak(string text)
        {
            lastNarrationText = text ?? string.Empty;

            Debug.Log($"Narration: {lastNarrationText}");
            NarrationRequested?.Invoke(lastNarrationText);
        }

        public void SetTrackingAvailable(bool available)
        {
            if (state.IsTrackingAvailable == available)
                return;

            state.IsTrackingAvailable = available;
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(state);
        }
    }
}
