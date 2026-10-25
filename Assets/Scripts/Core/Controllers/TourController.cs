
using System;
using UnityEngine;

namespace SolarSystemAR.Core
{
    public class TourController : MonoBehaviour
    {
        private readonly PlanetId[] tourOrder =
        {
            PlanetId.Mercury,
            PlanetId.Venus,
            PlanetId.Earth,
            PlanetId.Mars,
            PlanetId.Jupiter,
            PlanetId.Saturn,
            PlanetId.Uranus,
            PlanetId.Neptune
        };

        private int currentIndex;
        private bool isActive;

        public bool IsActive => isActive;
        public int CurrentIndex => currentIndex;

        public event Action<PlanetId> TourPlanetChanged;

        public void StartTour()
        {
            currentIndex = 0;
            isActive = true;
            NotifyPlanetChanged();
        }

        public void NextPlanet()
        {
            if (!isActive)
                return;

            if (currentIndex >= tourOrder.Length - 1)
                return;

            currentIndex++;
            NotifyPlanetChanged();
        }

        public void PreviousPlanet()
        {
            if (!isActive)
                return;

            if (currentIndex <= 0)
                return;

            currentIndex--;
            NotifyPlanetChanged();
        }

        public void StopTour()
        {
            isActive = false;
        }

        private void NotifyPlanetChanged()
        {
            PlanetId planet = tourOrder[currentIndex];
            TourPlanetChanged?.Invoke(planet);

            Debug.Log($"Tour planet: {planet}");
        }
    }
}
