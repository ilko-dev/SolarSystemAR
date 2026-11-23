using System.Globalization;
using System.Text;
using UnityEngine;

namespace SolarSystemAR.Core
{
    public enum PlanetInfoSection { Introduction, Fact, History }

    /// <summary>Immutable card snapshot, independent of speech and mutable AppState.</summary>
    public sealed class PlanetInfoContent
    {
        public PlanetId Planet { get; private set; }
        public string Name { get; private set; }
        public Sprite Image { get; private set; }
        public string Heading { get; private set; }
        public string Text { get; private set; }

        public static PlanetInfoContent Create(PlanetId id, PlanetData data, PlanetInfoSection section)
        {
            string name = PlanetNames.Ukrainian(id);
            string text = data == null ? "Дані про цю планету ще не підготовлено."
                : section == PlanetInfoSection.Fact ? data.interestingFact
                : section == PlanetInfoSection.History ? data.discoveryHistory : Introduction(data);
            return new PlanetInfoContent
            {
                Planet = id, Name = name, Image = data != null ? data.image : null,
                Heading = section == PlanetInfoSection.Fact ? "ЦІКАВИЙ ФАКТ"
                    : section == PlanetInfoSection.History ? "ІСТОРІЯ ДОСЛІДЖЕННЯ" : "ПРО ПЛАНЕТУ",
                Text = string.IsNullOrWhiteSpace(text) ? "Ця інформація ще не підготовлена." : text
            };
        }

        private static string Introduction(PlanetData data)
        {
            var text = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(data.planetType)) text.AppendLine(data.planetType);
            if (data.diameterKm > 0) text.AppendLine($"Діаметр: {Number(data.diameterKm)} км");
            if (!string.IsNullOrWhiteSpace(data.yearDuration)) text.Append($"Рік: {data.yearDuration}");
            return text.ToString().Trim();
        }

        private static string Number(float value) =>
            value.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
