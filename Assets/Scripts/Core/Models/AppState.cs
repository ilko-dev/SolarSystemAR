namespace SolarSystemAR.Core
{
    public enum AppMode
    {
        Overview,
        PlanetSelected,
        DetailView,
        GuidedTour
    }

    public class AppState
    {
        public AppMode Mode { get; set; } = AppMode.Overview;

        public PlanetId? SelectedPlanet { get; set; }

        public bool IsOrbitPaused { get; set; }

        public bool IsTrackingAvailable { get; set; } = true;

        public int TourIndex { get; set; }
    }
}
