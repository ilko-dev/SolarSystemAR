using System;
using System.Collections.Generic;

namespace SolarSystemAR.Voice
{
    /// <summary>
    /// Shared logic of both voices: splits the text into sentences, speaks them one after another
    /// and reports which sentence is being said (for captions and the guide's talking animation).
    ///
    /// The real voice reports "sentence N finished" through OnVoiceSentenceDone. Without a real voice
    /// (PC, or the phone's voice failed) the sentences advance by an estimated speaking time instead.
    /// </summary>
    public abstract class SentenceSpeechSynthesizer : ISpeechSynthesizer
    {
        public event Action<string> Problem;
        public event Action<string> SentenceStarted;
        public event Action Finished;

        private List<string> sentences; // null when silent
        private int index;
        private int session;            // bumped on every Speak/Stop, so late callbacks from old speech are ignored
        private bool timed;             // advance by estimated time instead of voice callbacks
        private float elapsed;          // seconds since the current sentence started

        public bool IsSpeaking => sentences != null;

        public void Speak(string text)
        {
            List<string> parts = SpeechSentences.Split(text);
            if (parts.Count == 0)
                return;

            session++;
            sentences = parts;
            index = 0;
            elapsed = 0f;
            timed = !StartVoice(parts, session);
            SentenceStarted?.Invoke(parts[0]);
        }

        public void Stop()
        {
            session++;
            StopVoice();

            if (sentences == null)
                return;

            sentences = null;
            Finished?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (sentences == null)
                return;

            elapsed += deltaTime;
            float expected = SpeechSentences.EstimateSeconds(sentences[index]);

            // With a real voice this is only a safety net, in case the phone never reports the end.
            if (timed ? elapsed >= expected : elapsed >= expected * 3f + 5f)
                Advance();
        }

        public virtual void Dispose()
        {
        }

        /// <summary>Start saying all sentences in order. Return false if there is no real voice.</summary>
        protected abstract bool StartVoice(IReadOnlyList<string> parts, int voiceSession);

        protected abstract void StopVoice();

        /// <summary>The real voice finished sentence <paramref name="sentenceIndex"/> of speech <paramref name="voiceSession"/>.</summary>
        protected void OnVoiceSentenceDone(int voiceSession, int sentenceIndex)
        {
            if (voiceSession == session && sentences != null && sentenceIndex == index)
                Advance();
        }

        /// <summary>The real voice can't say speech <paramref name="voiceSession"/>: keep the captions going by time.</summary>
        protected void OnVoiceUnavailable(int voiceSession)
        {
            if (voiceSession == session)
                timed = true;
        }

        protected void ReportProblem(string message)
        {
            Problem?.Invoke(message);
        }

        private void Advance()
        {
            index++;
            elapsed = 0f;

            if (index < sentences.Count)
            {
                SentenceStarted?.Invoke(sentences[index]);
                return;
            }

            sentences = null;
            Finished?.Invoke();
        }
    }
}
