using System.Collections.Generic;
using UnityEngine;

namespace RSPP
{
    /// <summary>The simple sound effects used by the game.</summary>
    public enum Sfx
    {
        DiceLand,
        WheelTick,
        WheelStop,
        Correct,
        Incorrect,
        Star
    }

    /// <summary>
    /// Loads the sound effects from Resources/Audio and plays them.
    /// The game keeps working if a clip is missing or if sound is switched off.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public bool SoundOn { get; private set; }

        private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        private AudioSource uiSource;

        private void Awake()
        {
            SoundOn = true;
            AudioListener.volume = 1f;

            Load(Sfx.DiceLand, "Audio/dice_land");
            Load(Sfx.WheelTick, "Audio/wheel_tick");
            Load(Sfx.WheelStop, "Audio/wheel_stop");
            Load(Sfx.Correct, "Audio/correct");
            Load(Sfx.Incorrect, "Audio/incorrect");
            Load(Sfx.Star, "Audio/star");

            // Non-positional source for answer feedback.
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
        }

        private void Load(Sfx sfx, string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null) clips[sfx] = clip;
            else Debug.LogWarning("[RSPP] Sound not found in Resources: " + resourcePath);
        }

        /// <summary>Creates a 3D (positional) audio source attached to an object, e.g. the die or the wheel.</summary>
        public AudioSource CreateSpatialSource(Transform parent)
        {
            var go = new GameObject("Audio Source (3D)");
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.5f;
            source.maxDistance = 8f;
            source.dopplerLevel = 0f;
            return source;
        }

        /// <summary>Plays a sound. Pass a 3D source for positional sound, or leave it null for plain 2D sound.</summary>
        public void Play(Sfx sfx, AudioSource source = null, float volume = 1f)
        {
            if (!SoundOn) return;

            AudioClip clip;
            if (!clips.TryGetValue(sfx, out clip) || clip == null) return;

            AudioSource target = source != null ? source : uiSource;
            if (target != null && target.isActiveAndEnabled) target.PlayOneShot(clip, volume);
        }

        public void SetSoundOn(bool on)
        {
            SoundOn = on;
            AudioListener.volume = on ? 1f : 0f;
        }
    }
}
