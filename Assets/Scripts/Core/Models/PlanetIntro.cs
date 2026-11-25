using System;
using System.Globalization;

namespace SolarSystemAR.Core
{
    /// <summary>
    /// Builds the short spoken card for a selected planet, e.g.
    /// "Марс — планета земної групи. Діаметр — 6779 кілометрів. ..."
    /// Plain C# (no Unity), so it is unit-tested in Tests/VoiceParser.Tests.
    /// </summary>
    public static class PlanetIntro
    {
        public static string Build(string name, string type, float diameterKm, float distanceMillionKm,
            string day, string year, int moons, bool isStar = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var text = new System.Text.StringBuilder(name.Trim());

            if (!string.IsNullOrWhiteSpace(type))
                text.Append(" — ").Append(LowerFirst(type.Trim()));
            text.Append('.');

            if (diameterKm > 0f)
            {
                int d = (int)Math.Round(diameterKm);
                text.Append($" Діаметр — {d} {Plural(d, "кілометр", "кілометри", "кілометрів")}.");
            }

            if (distanceMillionKm > 0f)
                text.Append($" Відстань від Сонця — {Millions(distanceMillionKm)} кілометрів.");

            if (isStar)
            {
                // The Sun has no year or moons; planets go around it instead.
                if (!string.IsNullOrWhiteSpace(day))
                    text.Append($" Навколо своєї осі обертається {day.Trim()}.");
                text.Append(" Навколо нього обертаються вісім планет.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(day) && !string.IsNullOrWhiteSpace(year))
                    text.Append($" Доба триває {day.Trim()}, рік — {year.Trim()}.");

                text.Append(moons > 0
                    ? $" Має {moons} {Plural(moons, "супутник", "супутники", "супутників")}."
                    : " Супутників немає.");
            }

            text.Append(" Скажіть «які питання», щоб дізнатися, що можна запитати.");
            return text.ToString();
        }

        /// <summary>Ukrainian noun form after a number: 1 кілометр, 2 кілометри, 5 кілометрів.</summary>
        public static string Plural(int n, string one, string few, string many)
        {
            int mod10 = Math.Abs(n) % 10;
            int mod100 = Math.Abs(n) % 100;
            if (mod10 == 1 && mod100 != 11)
                return one;
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14))
                return few;
            return many;
        }

        // 227.9 -> "227,9 мільйона", 1432 -> "1432 мільйони", 4515 -> "4515 мільйонів"
        private static string Millions(float value)
        {
            double rounded = Math.Round(value, 1);
            if (Math.Abs(rounded - Math.Round(rounded)) < 0.05)
            {
                int whole = (int)Math.Round(rounded);
                return $"{whole} {Plural(whole, "мільйон", "мільйони", "мільйонів")}";
            }

            string number = rounded.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return $"{number} мільйона";
        }

        private static string LowerFirst(string s)
        {
            return s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
