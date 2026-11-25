using System.Collections.Generic;
using SolarSystemAR.Core;
using SolarSystemAR.Voice;
using Xunit;

namespace VoiceParser.Tests
{
    public class VoiceCommandParserTests
    {
        // Invented fixture questions (the real ones live in PlanetData assets).
        private static readonly List<VoiceQuestion> Questions = new List<VoiceQuestion>
        {
            new VoiceQuestion(PlanetId.Mars, "mars_red", "Чому Марс червоний?"),
            new VoiceQuestion(PlanetId.Mars, "mars_moons", "Скільки супутників у Марса?"),
            new VoiceQuestion(PlanetId.Mars, "mars_water", "Чи є на Марсі вода?"),
            new VoiceQuestion(PlanetId.Jupiter, "jupiter_moons", "Скільки супутників у Юпітера?"),
            new VoiceQuestion(PlanetId.Saturn, "saturn_rings", "З чого складаються кільця Сатурна?"),
        };

        private static AppCommand Parse(string text, PlanetId? selected = null)
        {
            return VoiceCommandParser.Parse(text, new VoiceParseContext { SelectedPlanet = selected, Questions = Questions });
        }

        private static void AssertCommand(AppCommand cmd, CommandType type, PlanetId? planet = null)
        {
            Assert.NotNull(cmd);
            Assert.Equal(type, cmd.Type);
            Assert.Equal(planet, cmd.Planet);
        }

        [Theory]
        [InlineData("покажи Юпітер", PlanetId.Jupiter)]
        [InlineData("перейди до Венери", PlanetId.Venus)]
        [InlineData("Марс", PlanetId.Mars)]
        [InlineData("покажи Марса", PlanetId.Mars)]
        [InlineData("Земля", PlanetId.Earth)]
        [InlineData("покажи Землю", PlanetId.Earth)]
        [InlineData("що на Землі", PlanetId.Earth)]
        [InlineData("Меркурій", PlanetId.Mercury)]
        [InlineData("покажи Сонце", PlanetId.Sun)]
        [InlineData("Сонце", PlanetId.Sun)]
        [InlineData("до Сонця", PlanetId.Sun)]
        [InlineData("покажи Уран", PlanetId.Uranus)]
        [InlineData("перейди до Урана", PlanetId.Uranus)]
        [InlineData("Сатурн", PlanetId.Saturn)]
        [InlineData("Нептун.", PlanetId.Neptune)]
        public void PlanetName_SelectsPlanet(string text, PlanetId planet)
        {
            AssertCommand(Parse(text), CommandType.SelectPlanet, planet);
        }

        [Theory]
        [InlineData("почни екскурсію", CommandType.StartTour)]
        [InlineData("тур", CommandType.StartTour)]
        [InlineData("далі", CommandType.NextPlanet)]
        [InlineData("наступна планета", CommandType.NextPlanet)]
        [InlineData("назад", CommandType.PreviousPlanet)]
        [InlineData("попередня", CommandType.PreviousPlanet)]
        [InlineData("пауза", CommandType.PauseOrbits)]
        [InlineData("зупини планети", CommandType.PauseOrbits)]
        [InlineData("продовж", CommandType.ResumeOrbits)]
        [InlineData("запусти", CommandType.ResumeOrbits)]
        [InlineData("огляд", CommandType.ShowOverview)]
        [InlineData("покажи всю систему", CommandType.ShowOverview)]
        [InlineData("вся система", CommandType.ShowOverview)]
        [InlineData("скасувати", CommandType.Cancel)]
        [InlineData("цікавий факт", CommandType.ShowFact)]
        [InlineData("історія", CommandType.ShowHistory)]
        [InlineData("хто відкрив", CommandType.ShowHistory)]
        [InlineData("стоп", CommandType.StopNarration)]
        [InlineData("тихо", CommandType.StopNarration)]
        [InlineData("замовкни!", CommandType.StopNarration)]
        [InlineData("повтори", CommandType.RepeatNarration)]
        [InlineData("ще раз", CommandType.RepeatNarration)]
        [InlineData("детальніше", CommandType.EnterDetailView)]
        [InlineData("ближче", CommandType.EnterDetailView)]
        [InlineData("вийти", CommandType.ExitDetailView)]
        [InlineData("повернутись", CommandType.ExitDetailView)]
        [InlineData("повернись назад", CommandType.ExitDetailView)]
        public void Keywords_MapToCommands(string text, CommandType type)
        {
            AssertCommand(Parse(text), type);
        }

        // Every example phrase from README.md must keep working.
        [Theory]
        [InlineData("Юпітер", CommandType.SelectPlanet, PlanetId.Jupiter)]
        [InlineData("розкажи", CommandType.ShowFact, null)]
        [InlineData("подорож", CommandType.StartTour, null)]
        [InlineData("вперед", CommandType.NextPlanet, null)]
        [InlineData("зупини", CommandType.PauseOrbits, null)]
        [InlineData("заморозь", CommandType.PauseOrbits, null)]
        [InlineData("віднови", CommandType.ResumeOrbits, null)]
        [InlineData("поїхали", CommandType.ResumeOrbits, null)]
        [InlineData("всі планети", CommandType.ShowOverview, null)]
        [InlineData("сонячна система", CommandType.ShowOverview, null)]
        [InlineData("наблизь", CommandType.EnterDetailView, null)]
        [InlineData("збільш", CommandType.EnterDetailView, null)]
        [InlineData("детальніше про Сатурн", CommandType.EnterDetailView, PlanetId.Saturn)]
        [InlineData("вихід", CommandType.ExitDetailView, null)]
        [InlineData("повернись", CommandType.ExitDetailView, null)]
        [InlineData("досить", CommandType.StopNarration, null)]
        [InlineData("припини", CommandType.StopNarration, null)]
        [InlineData("відміна", CommandType.Cancel, null)]
        [InlineData("покажи, будь ласка, Юпітер", CommandType.SelectPlanet, PlanetId.Jupiter)]
        [InlineData("покажи Марс", CommandType.SelectPlanet, PlanetId.Mars)]
        public void ReadmeExamples_Work(string text, CommandType type, PlanetId? planet)
        {
            AssertCommand(Parse(text), type, planet);
        }

        [Fact]
        public void TellAboutPlanet_IsFactForThatPlanet()
        {
            AssertCommand(Parse("розкажи про Марс"), CommandType.ShowFact, PlanetId.Mars);
        }

        [Fact]
        public void HistoryWithPlanet_KeepsPlanet()
        {
            AssertCommand(Parse("розкажи історію Юпітера"), CommandType.ShowHistory, PlanetId.Jupiter);
        }

        [Fact]
        public void StopWins_EvenWithOtherWords()
        {
            AssertCommand(Parse("стоп, покажи Марс"), CommandType.StopNarration);
        }

        [Fact]
        public void PunctuationCaseAndApostrophes_AreIgnored()
        {
            AssertCommand(Parse("  ПОКАЖИ,   юпітер!!! "), CommandType.SelectPlanet, PlanetId.Jupiter);
            Assert.Equal(VoiceCommandParser.Normalize("пам'ять"), VoiceCommandParser.Normalize("пам’ять"));
        }

        [Fact]
        public void Question_MatchedWithDifferentEndings()
        {
            var cmd = Parse("чому марс червона");
            AssertCommand(cmd, CommandType.AskQuestion, PlanetId.Mars);
            Assert.Equal("mars_red", cmd.QuestionId);
        }

        [Fact]
        public void Question_WaterOnMars()
        {
            var cmd = Parse("чи є вода на марсі");
            AssertCommand(cmd, CommandType.AskQuestion, PlanetId.Mars);
            Assert.Equal("mars_water", cmd.QuestionId);
        }

        [Fact]
        public void Question_NamedPlanetPicksThatPlanetsQuestion()
        {
            var cmd = Parse("скільки супутників у Юпітера", selected: PlanetId.Mars);
            AssertCommand(cmd, CommandType.AskQuestion, PlanetId.Jupiter);
            Assert.Equal("jupiter_moons", cmd.QuestionId);
        }

        [Fact]
        public void Question_NoPlanetNamed_PrefersSelectedPlanetOnTie()
        {
            var jupiter = Parse("скільки супутників", selected: PlanetId.Jupiter);
            AssertCommand(jupiter, CommandType.AskQuestion, PlanetId.Jupiter);
            Assert.Equal("jupiter_moons", jupiter.QuestionId);

            var mars = Parse("скільки супутників", selected: PlanetId.Mars);
            AssertCommand(mars, CommandType.AskQuestion, PlanetId.Mars);
            Assert.Equal("mars_moons", mars.QuestionId);
        }

        [Fact]
        public void Question_NoPlanetNamed_FindsQuestionOfAnotherPlanet()
        {
            var cmd = Parse("з чого складаються кільця", selected: PlanetId.Mars);
            AssertCommand(cmd, CommandType.AskQuestion, PlanetId.Saturn);
            Assert.Equal("saturn_rings", cmd.QuestionId);
        }

        [Fact]
        public void Question_PartialMatchIsNotEnough()
        {
            // Only "червоний" of "чому ... червоний" -> not the question, just select Mars.
            AssertCommand(Parse("червоний Марс"), CommandType.SelectPlanet, PlanetId.Mars);
        }

        [Fact]
        public void NoQuestionsInContext_StillParsesCommands()
        {
            var cmd = VoiceCommandParser.Parse("покажи Сатурн", new VoiceParseContext());
            AssertCommand(cmd, CommandType.SelectPlanet, PlanetId.Saturn);
            Assert.NotNull(VoiceCommandParser.Parse("далі", null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("яка сьогодні погода")]
        [InlineData("привіт")]
        public void Gibberish_ReturnsNull(string text)
        {
            Assert.Null(Parse(text));
        }

        [Fact]
        public void PlanetIndependentCommands_HaveNoPlanet()
        {
            AssertCommand(Parse("наступна планета після Марса"), CommandType.NextPlanet);
        }

        [Fact]
        public void SolarSystem_IsOverview_NotTheSun()
        {
            AssertCommand(Parse("покажи всю сонячну систему"), CommandType.ShowOverview);
        }

        [Theory]
        [InlineData("покажи Марс біля Сонця", PlanetId.Mars)]
        [InlineData("покажи Сонце і Юпітер", PlanetId.Jupiter)]
        public void RealPlanet_WinsOverTheSun(string text, PlanetId expected)
        {
            AssertCommand(Parse(text), CommandType.SelectPlanet, expected);
        }
    }
}
