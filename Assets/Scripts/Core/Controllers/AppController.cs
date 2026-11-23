
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

        // Read-only access for the voice system (prepared questions for speech matching).
        public System.Collections.Generic.IReadOnlyList<PlanetData> PlanetCatalog =>
            planetCatalog ?? new PlanetData[0];

        public event Action<AppState> StateChanged;
        public event Action<PlanetId> PlanetSelected;

        // Separate retained presentation state; never forwards arbitrary narration.
        public PlanetInfoContent CurrentPlanetInfo { get; private set; }
        public event Action<PlanetInfoContent> PlanetInfoChanged;

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

                        if (state.SelectedPlanet == command.Planet)
                            SpeakPlanetIntro(command.Planet.Value);
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

                case CommandType.ShowHelp:
                    Speak(HelpText);
                    break;

                case CommandType.ListQuestions:
                    ListQuestions(command.Planet);
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

            PublishPlanetInfo(id, PlanetInfoSection.Introduction);

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

            CurrentPlanetInfo = null;
            PlanetInfoChanged?.Invoke(null);

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
                Speak($"Про планету {PlanetNames.Ukrainian(id.Value)} ще немає даних.");

            return data;
        }

        private void SpeakPlanetInfo(PlanetId? planet, bool history)
        {
            PlanetId? infoPlanet = planet ?? state.SelectedPlanet;
            if (infoPlanet.HasValue)
                PublishPlanetInfo(infoPlanet.Value, history
                    ? PlanetInfoSection.History : PlanetInfoSection.Fact);

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

        private const string HelpText =
            "Я вмію показувати планети: скажіть, наприклад, «покажи Юпітер». " +
            "Можу розповісти цікавий факт або історію відкриття, відповісти на запитання про планету — " +
            "скажіть «які питання», щоб дізнатися, що можна запитати. " +
            "Скажіть «почни екскурсію» для подорожі планетами, «далі» або «назад» — щоб перемикати їх, " +
            "«пауза» — щоб зупинити рух планет, «огляд» — щоб побачити всю систему, " +
            "і «стоп» — щоб я замовк.";

        // Short spoken card when a planet is chosen by the user (button or voice).
        private void SpeakPlanetIntro(PlanetId id)
        {
            PlanetData data = GetPlanetData(id);
            if (data == null)
                return;

            Speak(PlanetIntro.Build(data.displayName, data.planetType, data.diameterKm,
                data.distanceFromSunMillionKm, data.dayDuration, data.yearDuration, data.moonCount));
        }

        private void ListQuestions(PlanetId? planet)
        {
            PlanetData data = ResolvePlanetData(planet);

            if (data == null)
                return;

            var questions = new System.Collections.Generic.List<string>();
            if (data.questions != null)
            {
                foreach (var question in data.questions)
                {
                    if (question != null && !string.IsNullOrWhiteSpace(question.question))
                        questions.Add(question.question.Trim());
                }
            }

            Speak(questions.Count == 0
                ? "Для цієї планети запитань ще немає."
                : "Можете запитати: " + string.Join(" ", questions));
        }

        private void Speak(string text)
        {
            lastNarrationText = text ?? string.Empty;

            Debug.Log($"Narration: {lastNarrationText}");
            NarrationRequested?.Invoke(lastNarrationText);
        }

        private void PublishPlanetInfo(PlanetId id, PlanetInfoSection section)
        {
            CurrentPlanetInfo = PlanetInfoContent.Create(id, GetPlanetData(id), section);
            PlanetInfoChanged?.Invoke(CurrentPlanetInfo);
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
