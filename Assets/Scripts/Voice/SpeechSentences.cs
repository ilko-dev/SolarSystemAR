using System.Collections.Generic;
using System.Text;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Cuts narration into short pieces that are spoken (and shown as captions) one at a time.
    /// Plain C#, so it is covered by the dotnet tests.
    /// </summary>
    public static class SpeechSentences
    {
        /// <summary>Longer sentences are cut at a comma, dash or space so a caption fits in two or three lines.</summary>
        public const int MaxLength = 120;

        private const string SentenceEnds = ".!?…";
        private const string ClosingMarks = "»\"”)";

        public static List<string> Split(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            var current = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                current.Append(c);

                if (SentenceEnds.IndexOf(c) < 0)
                    continue;

                // Keep "?!", "..." and closing quotes with the sentence they end.
                while (i + 1 < text.Length && (SentenceEnds.IndexOf(text[i + 1]) >= 0 || ClosingMarks.IndexOf(text[i + 1]) >= 0))
                    current.Append(text[++i]);

                if (EndsSentence(text, i + 1))
                {
                    AddPiece(result, current.ToString());
                    current.Clear();
                }
            }

            AddPiece(result, current.ToString());
            return result;
        }

        /// <summary>Rough speaking time, used on PC (no real voice) and as a safety timeout on the phone.</summary>
        public static float EstimateSeconds(string piece)
        {
            int length = piece?.Length ?? 0;
            return length / 14f + 0.4f; // ~14 letters per second, plus a short pause
        }

        // A sentence ends before a space, unless the text goes on in lowercase or with a dash:
        // "227.9", "Скажіть «стоп!» — і я замовкну" and "т. д. і" are not ends.
        private static bool EndsSentence(string text, int next)
        {
            if (next >= text.Length)
                return true;
            if (!char.IsWhiteSpace(text[next]))
                return false;

            while (next < text.Length && char.IsWhiteSpace(text[next]))
                next++;

            return next >= text.Length || !(char.IsLower(text[next]) || "—–-,;:".IndexOf(text[next]) >= 0);
        }

        private static void AddPiece(List<string> result, string piece)
        {
            piece = piece.Trim();
            while (piece.Length > MaxLength)
            {
                int cut = FindCut(piece);
                result.Add(piece.Substring(0, cut).Trim());
                piece = piece.Substring(cut).Trim();
            }

            if (piece.Length > 0)
                result.Add(piece);
        }

        // Index to cut a too-long sentence at: after the last ", " / "; " / ": ", before the last " — ",
        // or at the last space, all within MaxLength. Never leaves a tiny first piece.
        private static int FindCut(string piece)
        {
            int minimum = MaxLength / 3;

            for (int i = MaxLength - 1; i >= minimum; i--)
            {
                if (piece[i] == ' ' && i > 0 && ",;:".IndexOf(piece[i - 1]) >= 0)
                    return i;
                if (piece[i] == ' ' && i + 1 < piece.Length && (piece[i + 1] == '—' || piece[i + 1] == '–'))
                    return i;
            }

            int space = piece.LastIndexOf(' ', MaxLength - 1);
            return space >= minimum ? space : MaxLength;
        }
    }
}
