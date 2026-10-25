
using UnityEngine;

namespace SolarSystemAR.Core
{
    public class PlanetOrbit : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private PlanetId planetId;

        [Header("Movement")]
        [SerializeField] private float orbitSpeed = 10f;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private Transform sun;

        private bool isPaused;

        public PlanetId PlanetId => planetId;

        private void Update()
        {
            if (isPaused)
                return;

            float delta = Time.deltaTime;

            // Orbit around the Sun
            if (sun != null)
            {
                transform.RotateAround(
                    sun.position,
                    Vector3.up,
                    orbitSpeed * delta
                );
            }

            // Rotate around its own axis
            transform.Rotate(
                Vector3.up,
                rotationSpeed * delta,
                Space.Self
            );
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
    }
}
