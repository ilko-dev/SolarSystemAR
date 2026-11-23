using System.Linq;
using SolarSystemAR.Voice;
using Xunit;

namespace VoiceParser.Tests
{
    public class SpeechSentencesTests
    {
        [Fact]
        public void SplitsAtSentenceEnds()
        {
            Assert.Equal(new[] { "Марс — планета земної групи.", "Діаметр — 6779 кілометрів.", "Чи знали ви?", "Так!" },
                SpeechSentences.Split("Марс — планета земної групи. Діаметр — 6779 кілометрів. Чи знали ви? Так!"));
        }

        [Fact]
        public void KeepsDecimalsQuotesAndEllipsis()
        {
            Assert.Equal(new[] { "Відстань — 227.9 мільйона кілометрів.", "Скажіть «стоп!» — і я замовкну…", "Добре?!" },
                SpeechSentences.Split("Відстань — 227.9 мільйона кілометрів. Скажіть «стоп!» — і я замовкну… Добре?!"));
        }

        [Fact]
        public void TextWithoutFinalDot()
        {
            Assert.Equal(new[] { "Перше.", "Друге без крапки" }, SpeechSentences.Split("  Перше.   Друге без крапки "));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyText(string text)
        {
            Assert.Empty(SpeechSentences.Split(text));
        }

        [Fact]
        public void LongSentence_IsCutAtCommaOrDash_AndNothingIsLost()
        {
            string text = "Я вмію показувати планети, розповідати цікаві факти та історію відкриття, " +
                          "відповідати на запитання про кожну планету — а ще можу провести вас на екскурсію " +
                          "всією Сонячною системою від Меркурія до самого Нептуна.";

            var pieces = SpeechSentences.Split(text);

            Assert.True(pieces.Count >= 2);
            Assert.All(pieces, p => Assert.InRange(p.Length, SpeechSentences.MaxLength / 3, SpeechSentences.MaxLength));
            Assert.Equal(text, string.Join(" ", pieces));
            Assert.True(pieces.Take(pieces.Count - 1).All(p => p.EndsWith(",") || p.EndsWith("планету")),
                string.Join(" | ", pieces));
        }

        [Fact]
        public void VeryLongWordlessText_IsStillCut()
        {
            string text = new string('а', 300);
            var pieces = SpeechSentences.Split(text);
            Assert.All(pieces, p => Assert.True(p.Length <= SpeechSentences.MaxLength));
            Assert.Equal(text, string.Concat(pieces));
        }
    }
}
