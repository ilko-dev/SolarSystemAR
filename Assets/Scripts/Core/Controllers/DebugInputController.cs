
using UnityEngine;
using UnityEngine.InputSystem;

namespace SolarSystemAR.Core
{
    public class DebugInputController : MonoBehaviour
    {
        [SerializeField] private AppController appController;

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.SelectPlanet,
                    Planet = PlanetId.Mars
                });
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.PauseOrbits
                });
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.ResumeOrbits
                });
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.ShowOverview
                });
            }

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.StartTour
                });
            }

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.NextPlanet
                });
            }

            if (Keyboard.current.digit7Key.wasPressedThisFrame)
            {
                appController.Execute(new AppCommand
                {
                    Type = CommandType.PreviousPlanet
                });
            }
        }
    }
}
