using SolarSystemAR.Core;
using UnityEngine;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Adds the voice system automatically when a scene with an AppController starts,
    /// so nobody has to edit the shared scene file (avoids merge conflicts).
    /// </summary>
    public static class VoiceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateVoiceController()
        {
            if (Object.FindAnyObjectByType<AppController>() == null)
                return; // not the AR scene

            if (Object.FindAnyObjectByType<VoiceController>() != null)
                return; // someone already added it by hand

            new GameObject("Voice").AddComponent<VoiceController>();
        }
    }
}
