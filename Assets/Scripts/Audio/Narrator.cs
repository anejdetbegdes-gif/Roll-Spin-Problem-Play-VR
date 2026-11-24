using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSPP
{
    /// <summary>
    /// Reads questions and short instructions aloud, for students who find reading hard
    /// (suggested by our EDN 303 collaborator for students with dyslexia or dyscalculia).
    ///
    /// English only. The clips in Resources/Voice were made offline with the Windows built-in
    /// text-to-speech voice (Tools/make_voice.ps1) and are joined word by word, for example
    /// "what is" + "eight" + "plus" + "five". No internet and no speech engine are needed on the headset.
    /// Robo (the AI Helper) has no voice: the narrator only reads questions, instructions and feedback.
    /// The Sound button mutes the narrator too.
    /// </summary>
    public class Narrator : MonoBehaviour
    {
        private AudioSource source;
        private readonly Queue<AudioClip> queue = new Queue<AudioClip>();
        private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        private Coroutine playing;

        public static Narrator Create()
        {
            var narrator = new GameObject("Narrator").AddComponent<Narrator>();
            narrator.source = narrator.gameObject.AddComponent<AudioSource>();
            narrator.source.playOnAwake = false;
            narrator.source.spatialBlend = 0f;
            narrator.source.priority = 10;      // more important than effects and music
            return narrator;
        }

        /// <summary>Narration only runs when the language is English.</summary>
        private static bool Active
        {
            get { return Loc.Current == Language.English; }
        }

        /// <summary>Stops whatever is being said and says these clips instead.</summary>
        public void Say(params string[] clips)
        {
            Stop();
            Queue(clips);
        }

        /// <summary>Adds clips after what is being said now.</summary>
        public void Queue(params string[] clips)
        {
            if (!Active || clips == null) return;
            foreach (string name in clips)
            {
                AudioClip clip = Load(name);
                if (clip != null) queue.Enqueue(clip);
            }
            if (playing == null && queue.Count > 0) playing = StartCoroutine(PlayQueue());
        }

        public void Stop()
        {
            queue.Clear();
            if (playing != null) StopCoroutine(playing);
            playing = null;
            if (source != null) source.Stop();
        }

        /// <summary>Reads a question, e.g. "What is eight plus five?".</summary>
        public void SayQuestion(MathQuestion q)
        {
            Say(QuestionClips(q));
        }

        /// <summary>Says an intro (e.g. "Not quite.") and then the worked answer, e.g. "eight plus five equals thirteen".</summary>
        public void SayExplanation(MathQuestion q, string intro)
        {
            Say(intro);
            Queue(ExplanationClips(q));
        }

        // ------------------------------------------------------------------ Building sentences from clips

        /// <summary>
        /// Clips for a number from 0 to 99: n0 ... n20 are single clips, bigger numbers are joined
        /// from a tens clip and a ones clip, e.g. 47 = "n40" + "n7" ("forty" "seven").
        /// </summary>
        private static string[] Number(int n)
        {
            if (n <= 20 || n > 99) return new[] { "n" + n };
            if (n % 10 == 0) return new[] { "n" + n };
            return new[] { "n" + (n / 10 * 10), "n" + (n % 10) };
        }

        /// <summary>Joins words and numbers into one list of clip names.</summary>
        private static string[] Join(params object[] parts)
        {
            var clips = new List<string>();
            foreach (object part in parts)
            {
                if (part is string[]) clips.AddRange((string[])part);
                else clips.Add((string)part);
            }
            return clips.ToArray();
        }

        private static string[] QuestionClips(MathQuestion q)
        {
            switch (q.Category)
            {
                case QuestionCategory.Addition: return Join("what_is", Number(q.A), "plus", Number(q.B));
                case QuestionCategory.Subtraction: return Join("what_is", Number(q.A), "minus", Number(q.B));
                default:
                    // Level 5: "Is six times seven odd, or even?"
                    if (q.IsProduct) return Join("is", Number(q.FactorA), "times", Number(q.FactorB), "odd_or_even");
                    // "Is 7 odd or even?" has two answer buttons; "Which number is even?" has four.
                    if (q.Choices.Length == 2) return Join("is", Number(q.A), "odd_or_even");
                    return new[] { q.Answer == 0 ? "which_number_is_even" : "which_number_is_odd" };
            }
        }

        private static string[] ExplanationClips(MathQuestion q)
        {
            switch (q.Category)
            {
                case QuestionCategory.Addition: return Join(Number(q.A), "plus", Number(q.B), "equals", Number(q.Answer));
                case QuestionCategory.Subtraction: return Join(Number(q.A), "minus", Number(q.B), "equals", Number(q.Answer));
                default:
                    string parity = q.A % 2 == 0 ? "is_even" : "is_odd";
                    if (q.IsProduct)
                        return Join(Number(q.FactorA), "times", Number(q.FactorB), "equals", Number(q.A), Number(q.A), parity);
                    return Join(Number(q.A), parity);
            }
        }

        // ------------------------------------------------------------------ Playback

        private AudioClip Load(string name)
        {
            AudioClip clip;
            if (cache.TryGetValue(name, out clip)) return clip;
            clip = Resources.Load<AudioClip>("Voice/" + name);
            if (clip == null) Debug.LogWarning("[RSPP] Narration clip not found: Resources/Voice/" + name);
            cache[name] = clip;
            return clip;
        }

        private IEnumerator PlayQueue()
        {
            while (queue.Count > 0)
            {
                AudioClip clip = queue.Dequeue();
                source.clip = clip;
                source.Play();
                yield return new WaitForSeconds(clip.length + 0.04f);
            }
            playing = null;
        }
    }
}
