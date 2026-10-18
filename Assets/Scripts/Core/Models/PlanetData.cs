
using System;
using UnityEngine;

namespace SolarSystemAR.Core
{
    [CreateAssetMenu(
        fileName = "PlanetData",
        menuName = "Solar System/Planet Data")]
    public class PlanetData : ScriptableObject
    {
        public PlanetId id;
        public string displayName;
        public string planetType;

        public float diameterKm;
        public float distanceFromSunMillionKm;
        public string dayDuration;
        public string yearDuration;
        public int moonCount;

        [TextArea(3, 6)]
        public string interestingFact;

        [TextArea(3, 8)]
        public string discoveryHistory;

        public PlanetQuestion[] questions;
    }

    [Serializable]
    public class PlanetQuestion
    {
        public string id;
        public string question;

        [TextArea(2, 5)]
        public string answer;

        public AudioClip audio;
    }
}
