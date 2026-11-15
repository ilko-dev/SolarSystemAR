using System.Collections.Generic;
using SolarSystemAR.Core;

namespace SolarSystemAR.Voice
{
    /// <summary>A prepared question the user can ask by voice (copied from PlanetData.questions).</summary>
    public class VoiceQuestion
    {
        public PlanetId Planet { get; set; }
        public string Id { get; set; }
        public string Text { get; set; }

        public VoiceQuestion(PlanetId planet, string id, string text)
        {
            Planet = planet;
            Id = id;
            Text = text;
        }
    }

    /// <summary>What the app looks like right now, so the parser can resolve "this planet" and questions.</summary>
    public class VoiceParseContext
    {
        public PlanetId? SelectedPlanet { get; set; }

        public IReadOnlyList<VoiceQuestion> Questions { get; set; } = new List<VoiceQuestion>();
    }
}
