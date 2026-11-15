namespace SolarSystemAR.Core
{
    public enum CommandType
    {
        SelectPlanet,
        StartTour,
        NextPlanet,
        PreviousPlanet,
        PauseOrbits,
        ResumeOrbits,
        ShowOverview,
        ShowFact,
        ShowHistory,
        AskQuestion,
        StopNarration,
        RepeatNarration,
        Cancel,
        EnterDetailView,
        ExitDetailView,
        ShowHelp,
        ListQuestions
    }

    public class AppCommand
    {
        public CommandType Type { get; set; }

        public PlanetId? Planet { get; set; }

        public string QuestionId { get; set; }
    }
}
