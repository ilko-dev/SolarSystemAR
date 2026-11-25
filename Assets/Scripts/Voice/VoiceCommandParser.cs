using System;
using System.Collections.Generic;
using System.Text;
using SolarSystemAR.Core;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Turns recognized speech ("покажи Юпітер") into an AppCommand.
    /// Plain C# with no Unity dependencies, so it is unit-tested outside Unity (see Tests/VoiceParser.Tests).
    ///
    /// Order of checks:
    ///   1. stop / cancel words
    ///   2. a prepared planet question (PlanetData.questions)
    ///   3. command keywords (VoiceKeywords.Intents, in order)
    ///   4. just a planet name -> SelectPlanet
    /// Returns null when nothing matched.
    /// </summary>
    public static class VoiceCommandParser
    {
        // Share of a question's meaningful words that must be heard to count as asking it.
        private const float QuestionMatchThreshold = 0.6f;

        public static AppCommand Parse(string text, VoiceParseContext context)
        {
            string[] words = Normalize(text);
            if (words.Length == 0)
                return null;

            context = context ?? new VoiceParseContext();
            PlanetId? planet = FindPlanet(words);

            if (ContainsAny(words, VoiceKeywords.StopNarration))
                return new AppCommand { Type = CommandType.StopNarration };

            if (ContainsAny(words, VoiceKeywords.Cancel))
                return new AppCommand { Type = CommandType.Cancel };

            VoiceQuestion question = FindQuestion(words, planet, context);
            if (question != null)
                return new AppCommand { Type = CommandType.AskQuestion, Planet = question.Planet, QuestionId = question.Id };

            foreach (var intent in VoiceKeywords.Intents)
            {
                if (!ContainsAny(words, intent.Value))
                    continue;

                var command = new AppCommand { Type = intent.Key };
                if (VoiceKeywords.PlanetAwareCommands.Contains(intent.Key))
                    command.Planet = planet;
                return command;
            }

            if (planet.HasValue)
                return new AppCommand { Type = CommandType.SelectPlanet, Planet = planet };

            return null;
        }

        /// <summary>Lowercase, unify apostrophes (removed), drop punctuation, split into words.</summary>
        public static string[] Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new string[0];

            var sb = new StringBuilder(text.Length);
            foreach (char raw in text.ToLowerInvariant())
            {
                char c = raw == 'ё' ? 'е' : raw;
                if (IsApostrophe(c))
                    continue;
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsApostrophe(char c)
        {
            return c == '\'' || c == '’' || c == 'ʼ' || c == '`' || c == '‘' || c == 'ʻ';
        }

        private static PlanetId? FindPlanet(string[] words)
        {
            // The first planet mentioned wins. The Sun counts only when no planet is named,
            // so "як далеко Марс від Сонця" is about Mars.
            bool sunNamed = false;
            for (int i = 0; i < words.Length; i++)
            {
                foreach (var entry in VoiceKeywords.PlanetStems)
                {
                    foreach (string stem in entry.Value)
                    {
                        if (!WordMatches(words[i], stem))
                            continue;
                        if (entry.Key != PlanetId.Sun)
                            return entry.Key;
                        sunNamed = true;
                    }
                }
            }

            return sunNamed ? PlanetId.Sun : (PlanetId?)null;
        }

        private static bool IsPlanetWord(string word)
        {
            foreach (var entry in VoiceKeywords.PlanetStems)
            {
                foreach (string stem in entry.Value)
                {
                    if (WordMatches(word, stem))
                        return true;
                }
            }

            return false;
        }

        // ---------- keyword phrases ----------

        private static bool ContainsAny(string[] words, string[] phrases)
        {
            foreach (string phrase in phrases)
            {
                if (ContainsPhrase(words, phrase))
                    return true;
            }

            return false;
        }

        private static bool ContainsPhrase(string[] words, string phrase)
        {
            string[] parts = phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int start = 0; start + parts.Length <= words.Length; start++)
            {
                bool all = true;
                for (int k = 0; k < parts.Length; k++)
                {
                    if (!WordMatches(words[start + k], parts[k]))
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                    return true;
            }

            return false;
        }

        private static bool WordMatches(string word, string pattern)
        {
            if (pattern.EndsWith("*", StringComparison.Ordinal))
                return word.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.Ordinal);
            return word == pattern;
        }

        // ---------- prepared questions ----------

        private static VoiceQuestion FindQuestion(string[] words, PlanetId? namedPlanet, VoiceParseContext context)
        {
            if (context.Questions == null || context.Questions.Count == 0)
                return null;

            List<string> spoken = MeaningfulWords(words);
            if (spoken.Count == 0)
                return null;

            VoiceQuestion best = null;
            float bestScore = 0f;

            foreach (VoiceQuestion q in context.Questions)
            {
                if (q == null || string.IsNullOrEmpty(q.Id) || string.IsNullOrEmpty(q.Text))
                    continue;

                // If the user named a planet, only that planet's questions count.
                if (namedPlanet.HasValue && q.Planet != namedPlanet.Value)
                    continue;

                List<string> expected = MeaningfulWords(Normalize(q.Text));
                if (expected.Count == 0)
                    continue;

                int matched = 0;
                foreach (string e in expected)
                {
                    foreach (string s in spoken)
                    {
                        if (SameWordRoot(e, s))
                        {
                            matched++;
                            break;
                        }
                    }
                }

                float score = (float)matched / expected.Count;
                if (score < QuestionMatchThreshold || matched < Math.Min(2, expected.Count))
                    continue;

                // On a tie, prefer the currently selected planet.
                bool better = score > bestScore + 0.0001f
                              || (Math.Abs(score - bestScore) <= 0.0001f && best != null
                                  && best.Planet != context.SelectedPlanet && q.Planet == context.SelectedPlanet);
                if (best == null || better)
                {
                    best = q;
                    bestScore = score;
                }
            }

            return best;
        }

        private static List<string> MeaningfulWords(string[] words)
        {
            var result = new List<string>();
            foreach (string w in words)
            {
                if (w.Length < 3 || VoiceKeywords.QuestionFillerWords.Contains(w) || IsPlanetWord(w))
                    continue;
                result.Add(w);
            }

            return result;
        }

        /// <summary>
        /// Rough Ukrainian "same word, different ending" check:
        /// червоний/червона, вода/води, кільця/кілець.
        /// </summary>
        private static bool SameWordRoot(string a, string b)
        {
            if (a == b)
                return true;

            int shorter = Math.Min(a.Length, b.Length);
            int common = 0;
            while (common < shorter && a[common] == b[common])
                common++;

            int needed = Math.Min(5, shorter - 1);
            return common >= 3 && common >= needed;
        }
    }
}
