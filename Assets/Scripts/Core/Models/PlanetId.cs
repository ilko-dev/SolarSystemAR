namespace SolarSystemAR.Core
{
    public enum PlanetId
    {
        Mercury,
        Venus,
        Earth,
        Mars,
        Jupiter,
        Saturn,
        Uranus,
        Neptune,

        // Kept last: Unity saves these as numbers (Mercury = 0 ... Neptune = 7, Sun = 8).
        Sun
    }
}
