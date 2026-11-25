using SolarSystemAR.Core;
using SolarSystemAR.Voice;
using Xunit;

namespace VoiceParser.Tests
{
    public class HelpAndIntroTests
    {
        [Theory]
        [InlineData("що ти вмієш", CommandType.ShowHelp)]
        [InlineData("допомога", CommandType.ShowHelp)]
        [InlineData("допоможи", CommandType.ShowHelp)]
        [InlineData("які команди", CommandType.ShowHelp)]
        [InlineData("що можна сказати", CommandType.ShowHelp)]
        [InlineData("які питання", CommandType.ListQuestions)]
        [InlineData("які запитання можна задати", CommandType.ListQuestions)]
        [InlineData("що можна запитати", CommandType.ListQuestions)]
        public void HelpPhrases(string text, CommandType type)
        {
            AppCommand cmd = VoiceCommandParser.Parse(text, new VoiceParseContext());
            Assert.NotNull(cmd);
            Assert.Equal(type, cmd.Type);
        }

        [Fact]
        public void ListQuestions_KeepsNamedPlanet()
        {
            AppCommand cmd = VoiceCommandParser.Parse("що можна запитати про Сатурн", new VoiceParseContext());
            Assert.Equal(CommandType.ListQuestions, cmd.Type);
            Assert.Equal(PlanetId.Saturn, cmd.Planet);
        }

        [Theory]
        [InlineData(1, "кілометр")]
        [InlineData(2, "кілометри")]
        [InlineData(4, "кілометри")]
        [InlineData(5, "кілометрів")]
        [InlineData(11, "кілометрів")]
        [InlineData(12, "кілометрів")]
        [InlineData(21, "кілометр")]
        [InlineData(22, "кілометри")]
        [InlineData(6779, "кілометрів")]
        public void Plural(int n, string expected)
        {
            Assert.Equal(expected, PlanetIntro.Plural(n, "кілометр", "кілометри", "кілометрів"));
        }

        [Fact]
        public void Intro_Sun_TalksAboutPlanetsNotMoons()
        {
            string text = PlanetIntro.Build("Сонце", "Зоря, жовтий карлик", 1392700, 0f,
                "приблизно за 25 земних діб", "", 0, isStar: true);

            Assert.StartsWith("Сонце — зоря, жовтий карлик. Діаметр — 1392700 кілометрів.", text);
            Assert.Contains("Навколо своєї осі обертається приблизно за 25 земних діб.", text);
            Assert.Contains("Навколо нього обертаються вісім планет.", text);
            Assert.DoesNotContain("Супутник", text);
            Assert.DoesNotContain("Відстань від Сонця", text);
        }

        [Fact]
        public void Intro_Mars()
        {
            string text = PlanetIntro.Build("Марс", "Планета земної групи", 6779, 227.9f,
                "24 години 37 хвилин", "687 земних діб", 2);
            Assert.Equal(
                "Марс — планета земної групи. Діаметр — 6779 кілометрів. " +
                "Відстань від Сонця — 227,9 мільйона кілометрів. " +
                "Доба триває 24 години 37 хвилин, рік — 687 земних діб. Має 2 супутники. " +
                "Скажіть «які питання», щоб дізнатися, що можна запитати.", text);
        }

        [Fact]
        public void Intro_WholeMillionsAndMoonCounts()
        {
            Assert.Contains("1432 мільйони кілометрів", PlanetIntro.Build("Сатурн", "", 1, 1432f, "", "", 274));
            Assert.Contains("Має 274 супутники", PlanetIntro.Build("Сатурн", "", 1, 1432f, "", "", 274));
            Assert.Contains("4515 мільйонів", PlanetIntro.Build("Нептун", "", 1, 4515f, "", "", 16));
            Assert.Contains("Супутників немає", PlanetIntro.Build("Меркурій", "", 1, 57.9f, "", "", 0));
            Assert.Contains("Має 1 супутник.", PlanetIntro.Build("Земля", "", 1, 149.6f, "", "", 1));
        }

        [Fact]
        public void Intro_EmptyDataSaysNothing()
        {
            Assert.Equal(string.Empty, PlanetIntro.Build("", "", 0, 0, "", "", 0));
        }
    }
}
