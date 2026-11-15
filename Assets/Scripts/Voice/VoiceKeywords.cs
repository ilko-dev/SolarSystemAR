using System.Collections.Generic;
using SolarSystemAR.Core;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// All words the voice parser understands, in one place.
    /// Edit this file to teach the app new phrases.
    ///
    /// Phrase syntax (after normalization: lowercase, no punctuation, no apostrophes):
    ///   "пауза"          - the word must match exactly
    ///   "екскурс*"       - any word starting with "екскурс" (екскурсія, екскурсію, ...)
    ///   "всю систем*"    - several words in a row, each matched as above
    /// </summary>
    public static class VoiceKeywords
    {
        // Word beginnings for every planet (covers Ukrainian case endings).
        public static readonly Dictionary<PlanetId, string[]> PlanetStems = new Dictionary<PlanetId, string[]>
        {
            { PlanetId.Mercury, new[] { "меркурі*" } },
            { PlanetId.Venus,   new[] { "венер*" } },
            { PlanetId.Earth,   new[] { "земл*" } },
            { PlanetId.Mars,    new[] { "марс*" } },
            { PlanetId.Jupiter, new[] { "юпітер*" } },
            { PlanetId.Saturn,  new[] { "сатурн*" } },
            { PlanetId.Uranus,  new[] { "уран", "урана", "урану", "ураном", "урані" } },
            { PlanetId.Neptune, new[] { "нептун*" } },
        };

        // Checked first: they must always work, even mid-sentence.
        public static readonly string[] StopNarration =
        {
            "стоп", "тихо", "замовкни", "замовчи", "помовч*", "досить", "зупинись", "припини*", "перестань",
        };

        public static readonly string[] Cancel =
        {
            "скасу*", "відміна", "відмініть", "відміни",
        };

        // Checked in this order after stop/cancel and question matching.
        // Order matters: e.g. "покажи всю систему" must hit Overview, not SelectPlanet;
        // "розкажи історію" must hit History, not Fact.
        public static readonly KeyValuePair<CommandType, string[]>[] Intents =
        {
            Intent(CommandType.ShowHelp, "допомо*", "довідк*", "що ти вмієш", "що вмієш", "що ти можеш",
                "що можна сказати", "які команди", "команди"),
            Intent(CommandType.ListQuestions, "які питання", "які запитання", "що можна запитати",
                "що можна спитати", "що запитати", "що спитати", "список питань", "список запитань"),
            Intent(CommandType.StartTour, "екскурс*", "тур", "подорож*"),
            Intent(CommandType.NextPlanet, "наступн*", "далі", "вперед"),
            Intent(CommandType.ShowOverview, "огляд*", "вся систем*", "всю систем*", "уся систем*", "усю систем*",
                "сонячн* систем*", "всі планети", "усі планети"),
            Intent(CommandType.ExitDetailView, "вийти", "вийди", "вихід", "поверн*"),
            Intent(CommandType.PreviousPlanet, "попередн*", "назад"),
            Intent(CommandType.PauseOrbits, "пауз*", "зупини", "зупиніть", "заморо*"),
            Intent(CommandType.ResumeOrbits, "продовж*", "запуст*", "віднови*", "поїхали"),
            Intent(CommandType.RepeatNarration, "повтори*", "ще раз"),
            Intent(CommandType.EnterDetailView, "детальн*", "ближче", "наблиз*", "збільш*"),
            Intent(CommandType.ShowHistory, "історі*", "хто відкрив", "відкритт*", "коли відкрили"),
            Intent(CommandType.ShowFact, "факт*", "цікав*", "розкажи*", "розкажіть"),
        };

        // Commands that use the planet named in the phrase (if any).
        public static readonly HashSet<CommandType> PlanetAwareCommands = new HashSet<CommandType>
        {
            CommandType.ShowFact, CommandType.ShowHistory, CommandType.EnterDetailView, CommandType.ListQuestions,
        };

        // Words ignored when comparing what was said with a prepared question.
        public static readonly HashSet<string> QuestionFillerWords = new HashSet<string>
        {
            "про", "для", "від", "при", "над", "під", "між", "через", "або", "але", "так", "також", "теж",
            "мені", "нам", "будь", "ласка", "розкажи", "розкажіть", "скажи", "скажіть", "покажи", "покажіть",
            "поясни", "поясніть", "цей", "ця", "це", "ці", "той", "та", "тут", "там", "його", "її", "їх",
            "планета", "планети", "планеті", "планету", "планетою",
            "такий", "така", "таке", "такі", "взагалі",
        };

        private static KeyValuePair<CommandType, string[]> Intent(CommandType type, params string[] phrases)
        {
            return new KeyValuePair<CommandType, string[]>(type, phrases);
        }
    }
}
