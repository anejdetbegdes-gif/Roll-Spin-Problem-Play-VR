using UnityEngine;

namespace RSPP
{
    /// <summary>
    /// Quiet, cheerful background music for young players.
    /// The tune is an original 8-bar melody on the C major pentatonic scale (no wrong-sounding notes),
    /// built as a sound clip by code when the game starts, so there is no music file and no licence to worry about.
    /// It loops softly. The menu's Sound button mutes it together with all other sounds (AudioListener volume).
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const float BeatsPerMinute = 108f;

        // Melody as MIDI note numbers (72 = C5) and lengths in beats. 32 beats = 8 bars of 4/4.
        private static readonly int[] MelodyNotes =
        {
            72, 76, 79, 76,   81, 79, 76,   74, 76, 79, 74,   72, 67,
            72, 76, 79, 84,   81, 79, 76, 79,   74, 79, 76, 74,   72
        };
        private static readonly float[] MelodyBeats =
        {
            1, 1, 1, 1,   1, 1, 2,   1, 1, 1, 1,   2, 2,
            1, 1, 1, 1,   1, 1, 1, 1,   1, 1, 1, 1,   4
        };

        // One soft bass note per bar: C, A, G, C, C, A, G, C (two octaves lower).
        private static readonly int[] BassNotes = { 48, 45, 43, 48, 48, 45, 43, 48 };

        private AudioSource source;

        /// <summary>Music button on the in-game menu.</summary>
        public void SetMusicOn(bool on)
        {
            if (source != null) source.mute = !on;
        }

        public static MusicPlayer Create()
        {
            var go = new GameObject("Music");
            var player = go.AddComponent<MusicPlayer>();

            var source = go.AddComponent<AudioSource>();
            player.source = source;
            source.clip = BuildClip();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = GameConfig.MusicVolume;
            source.priority = 200;      // lower priority than the game's sound effects
            source.Play();
            return player;
        }

        private static AudioClip BuildClip()
        {
            float beat = 60f / BeatsPerMinute;
            int totalBeats = 32;
            int length = Mathf.CeilToInt(totalBeats * beat * SampleRate);
            var data = new float[length];

            // Melody: a soft "music box" tone (sine plus a little second harmonic) with a gentle decay.
            float time = 0f;
            for (int i = 0; i < MelodyNotes.Length; i++)
            {
                float duration = MelodyBeats[i] * beat;
                AddNote(data, MelodyNotes[i], time, duration, 0.32f, 3.5f, 0.25f);
                time += duration;
            }

            // Bass: one long, quiet note per bar.
            for (int bar = 0; bar < BassNotes.Length; bar++)
                AddNote(data, BassNotes[bar], bar * 4f * beat, 4f * beat, 0.20f, 1.2f, 0f);

            // Keep the peak safely below clipping.
            float peak = 0.0001f;
            for (int i = 0; i < length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float scale = 0.8f / peak;
            for (int i = 0; i < length; i++) data[i] *= scale;

            AudioClip clip = AudioClip.Create("Background Music", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Mixes one note into the buffer.</summary>
        private static void AddNote(float[] data, int midiNote, float startTime, float duration,
            float volume, float decay, float harmonic)
        {
            float frequency = 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
            int start = Mathf.FloorToInt(startTime * SampleRate);
            int count = Mathf.FloorToInt(duration * SampleRate);
            const float attack = 0.01f;
            const float release = 0.05f;

            for (int n = 0; n < count && start + n < data.Length; n++)
            {
                float t = n / (float)SampleRate;
                // Short fade in and out so notes never click.
                float envelope = Mathf.Min(1f, t / attack) * Mathf.Min(1f, (duration - t) / release) * Mathf.Exp(-decay * t);
                float phase = 2f * Mathf.PI * frequency * t;
                data[start + n] += volume * envelope * (Mathf.Sin(phase) + harmonic * Mathf.Sin(2f * phase));
            }
        }
    }
}
