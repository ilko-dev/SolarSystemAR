using System;

namespace SolarSystemAR.Core
{
    public interface IAppController
    {
        AppState State { get; }

        event Action<AppState> StateChanged;
        event Action<PlanetId> PlanetSelected;

        void Execute(AppCommand command);
    }
}
