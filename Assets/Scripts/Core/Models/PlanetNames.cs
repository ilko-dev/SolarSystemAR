namespace SolarSystemAR.Core
{
    /// <summary>Ukrainian planet names for spoken messages when no PlanetData is available.</summary>
    public static class PlanetNames
    {
        public static string Ukrainian(PlanetId id)
        {
            switch (id)
            {
                case PlanetId.Mercury: return "Меркурій";
                case PlanetId.Venus: return "Венера";
                case PlanetId.Earth: return "Земля";
                case PlanetId.Mars: return "Марс";
                case PlanetId.Jupiter: return "Юпітер";
                case PlanetId.Saturn: return "Сатурн";
                case PlanetId.Uranus: return "Уран";
                case PlanetId.Neptune: return "Нептун";
                case PlanetId.Sun: return "Сонце";
                default: return id.ToString();
            }
        }
    }
}
