
using UnityEngine;

namespace SolarSystemAR.Core
{
    public class SolarSystemController : MonoBehaviour
    {
        [SerializeField]
        private PlanetOrbit[] planets;

        public void Pause()
        {
            SetPaused(true);
        }

        public void Resume()
        {
            SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            foreach (var planet in planets)
            {
                if (planet != null)
                    planet.SetPaused(paused);
            }
        }

        public PlanetOrbit GetPlanet(PlanetId id)
        {
            foreach (var planet in planets)
            {
                if (planet != null &&
                    planet.PlanetId == id)
                {
                    return planet;
                }
            }

            return null;
        }
    }
}
