using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SolarSystemAR.Core;
using SolarSystemAR.Voice;
using Xunit;

namespace VoiceParser.Tests
{
    /// <summary>
    /// Uses the real questions from Assets/Data/Planets/*.asset, so adding or editing a question
    /// in Unity is checked against the parser too.
    /// </summary>
    public class RealPlanetContentTests
    {
        private static readonly List<VoiceQuestion> Questions = LoadQuestions();

        private static List<VoiceQuestion> LoadQuestions()
        {
            string dir = FindPlanetsFolder();
            var result = new List<VoiceQuestion>();

            foreach (string file in Directory.GetFiles(dir, "*.asset"))
            {
                string yaml = File.ReadAllText(file);
                var planet = (PlanetId)int.Parse(Regex.Match(yaml, @"^  id: (\d+)", RegexOptions.Multiline).Groups[1].Value);

                foreach (Match m in Regex.Matches(yaml, @"^  - id: (\S+)\r?\n    question: (.*?)\r?$", RegexOptions.Multiline))
                    result.Add(new VoiceQuestion(planet, m.Groups[1].Value, YamlString(m.Groups[2].Value.Trim())));
            }

            return result;
        }

        private static string YamlString(string raw)
        {
            // Unity writes non-ASCII text as a double-quoted string with \uXXXX escapes (JSON-compatible).
            return raw.StartsWith("\"") ? JsonSerializer.Deserialize<string>(raw) : raw;
        }

        private static string FindPlanetsFolder()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Data", "Planets")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return Path.Combine(dir.FullName, "Assets", "Data", "Planets");
        }

        private static AppCommand Parse(string text, PlanetId? selected = null)
        {
            return VoiceCommandParser.Parse(text, new VoiceParseContext { SelectedPlanet = selected, Questions = Questions });
        }

        [Fact]
        public void AllEightPlanetsAndTheSunHaveQuestions()
        {
            Assert.Equal(9, Questions.Select(q => q.Planet).Distinct().Count());
            Assert.Equal(28, Questions.Count);
        }

        [Fact]
        public void EveryQuestion_ReadAloud_MatchesItself()
        {
            foreach (VoiceQuestion q in Questions)
            {
                // Selected planet = the question's planet, as when the user is looking at it.
                AppCommand cmd = Parse(q.Text, q.Planet);
                Assert.True(cmd != null && cmd.Type == CommandType.AskQuestion && cmd.QuestionId == q.Id
                            && cmd.Planet == q.Planet,
                    $"\"{q.Text}\" -> {cmd?.Type} {cmd?.Planet} {cmd?.QuestionId}, expected {q.Id}");
            }
        }

        [Theory]
        [InlineData("чому марс червоний", null, "mars_red", PlanetId.Mars)]
        [InlineData("яка температура сонця", PlanetId.Mars, "sun_temperature", PlanetId.Sun)]
        [InlineData("скільки років сонцю", null, "sun_age", PlanetId.Sun)]
        [InlineData("яка температура на меркурії", PlanetId.Sun, "mercury_temperature", PlanetId.Mercury)]
        [InlineData("скільки супутників у юпітера", PlanetId.Mars, "jupiter_moons", PlanetId.Jupiter)]
        [InlineData("скільки супутників", PlanetId.Saturn, "saturn_moons", PlanetId.Saturn)]
        [InlineData("чи можна там жити", PlanetId.Venus, "venus_life", PlanetId.Venus)]
        [InlineData("чи можна жити на марсі", PlanetId.Earth, "mars_life", PlanetId.Mars)]
        [InlineData("що таке велика червона пляма", PlanetId.Mars, "jupiter_spot", PlanetId.Jupiter)]
        [InlineData("чому небо блакитне", PlanetId.Neptune, "earth_sky", PlanetId.Earth)]
        [InlineData("чому нептун синій", null, "neptune_blue", PlanetId.Neptune)]
        [InlineData("з чого складаються кільця", null, "saturn_rings", PlanetId.Saturn)]
        [InlineData("чому уран лежить на боці", null, "uranus_side", PlanetId.Uranus)]
        [InlineData("чому венера гаряча", null, "venus_hot", PlanetId.Venus)]
        [InlineData("яка температура на меркурії", null, "mercury_temperature", PlanetId.Mercury)]
        public void SpokenVariants_FindTheRightQuestion(string text, PlanetId? selected, string id, PlanetId planet)
        {
            AppCommand cmd = Parse(text, selected);
            Assert.NotNull(cmd);
            Assert.Equal(CommandType.AskQuestion, cmd.Type);
            Assert.Equal(id, cmd.QuestionId);
            Assert.Equal(planet, cmd.Planet);
        }

        [Theory]
        [InlineData("розкажи про Марс", CommandType.ShowFact, PlanetId.Mars)]
        [InlineData("розкажи про кільця Сатурна", CommandType.ShowFact, PlanetId.Saturn)]
        [InlineData("покажи Юпітер", CommandType.SelectPlanet, PlanetId.Jupiter)]
        [InlineData("червоний Марс", CommandType.SelectPlanet, PlanetId.Mars)]
        [InlineData("історія Нептуна", CommandType.ShowHistory, PlanetId.Neptune)]
        [InlineData("які питання про Марс", CommandType.ListQuestions, PlanetId.Mars)]
        [InlineData("що ти вмієш", CommandType.ShowHelp, null)]
        public void CommandsAreNotMistakenForQuestions(string text, CommandType type, PlanetId? planet)
        {
            AppCommand cmd = Parse(text, PlanetId.Earth);
            Assert.NotNull(cmd);
            Assert.Equal(type, cmd.Type);
            Assert.Equal(planet, cmd.Planet);
        }
    }
}
