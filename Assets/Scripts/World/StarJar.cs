using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSPP
{
    /// <summary>
    /// A glass jar at the front-right corner of the table that fills up with stars.
    ///   - Every star the student earns flies from the game piece into the jar, so progress is visible.
    ///   - At FINISH the stars fly out of the jar one by one and form a shining frame around the
    ///     result panel, with a rising "ding" and a small vibration for each star.
    /// The reward is only for this game: no currency, no shop, nothing to unlock (proposal scope).
    /// </summary>
    public class StarJar : MonoBehaviour
    {
        private const float JarDiameter = 0.13f;
        private const float JarHeight = 0.18f;
        private const float StarInJarSize = 0.045f;
        private const float CelebrationStarSize = 0.075f;

        private readonly List<Transform> starsInJar = new List<Transform>();
        private readonly List<Transform> celebrating = new List<Transform>();     // stars that left the jar at FINISH
        private readonly HashSet<Transform> arrived = new HashSet<Transform>();   // ... and have reached the frame
        private Transform playArea;
        private AudioManager audioManager;
        private AudioSource source;

        public int Count
        {
            get { return starsInJar.Count; }
        }

        public static StarJar Create(Transform playArea, AudioManager audio)
        {
            var go = new GameObject("Star Jar");
            go.transform.SetParent(playArea, false);
            go.transform.localPosition = GameConfig.StarJarPosition;

            var jar = go.AddComponent<StarJar>();
            jar.playArea = playArea;
            jar.audioManager = audio;
            jar.source = audio.CreateSpatialSource(go.transform);
            jar.Build();
            return jar;
        }

        private void Build()
        {
            Shapes.Cylinder("Jar Base", transform, new Vector3(0f, 0.006f, 0f), JarDiameter + 0.01f, 0.012f, GameConfig.StarColor);

            // See-through glass: the "Sprites/Default" shader draws transparent colours and is always in the build.
            GameObject glass = Shapes.Cylinder("Jar Glass", transform, new Vector3(0f, 0.012f + JarHeight * 0.5f, 0f),
                JarDiameter, JarHeight, Color.white);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader);
                material.color = new Color(0.80f, 0.93f, 1.00f, 0.30f);
                glass.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            Shapes.Cylinder("Jar Rim", transform, new Vector3(0f, 0.012f + JarHeight, 0f), JarDiameter + 0.012f, 0.01f,
                new Color(0.25f, 0.56f, 0.90f));
        }

        // ------------------------------------------------------------------ During the game

        /// <summary>A new star flies from <paramref name="fromWorld"/> (the game piece) into the jar.</summary>
        public void AddStar(Vector3 fromWorld)
        {
            GameObject star = Shapes.Star("Jar Star", transform, Vector3.zero, StarInJarSize, GameConfig.StarColor);
            Vector3 from = transform.InverseTransformPoint(fromWorld);
            Vector3 to = SlotInJar(starsInJar.Count);
            starsInJar.Add(star.transform);
            StartCoroutine(Fly(star.transform, from, to, 0.75f, 0.25f, StarInJarSize, StarInJarSize, 0f));
        }

        /// <summary>Empties the jar (new game or back to the menu).</summary>
        public void ResetJar()
        {
            StopAllCoroutines();
            foreach (Transform t in starsInJar) if (t != null) Destroy(t.gameObject);
            foreach (Transform t in celebrating) if (t != null) Destroy(t.gameObject);
            starsInJar.Clear();
            celebrating.Clear();
            arrived.Clear();
            if (source != null) source.pitch = 1f;
        }

        /// <summary>Where the n-th star rests: small rings of six, stacked upwards.</summary>
        private static Vector3 SlotInJar(int n)
        {
            int layer = Mathf.Min(n / 6, 6);
            float angle = ((n % 6) * 60f + layer * 30f) * Mathf.Deg2Rad;
            const float radius = 0.033f;
            return new Vector3(Mathf.Cos(angle) * radius, 0.035f + layer * 0.022f, Mathf.Sin(angle) * radius);
        }

        // ------------------------------------------------------------------ At FINISH

        /// <summary>
        /// The stars leave the jar one by one and form a frame around a panel
        /// (<paramref name="center"/>, <paramref name="width"/> and <paramref name="height"/> are local to the play area).
        /// </summary>
        public void Celebrate(Vector3 center, float width, float height)
        {
            if (starsInJar.Count == 0) return;
            StartCoroutine(CelebrateRoutine(center, width, height));
        }

        private IEnumerator CelebrateRoutine(Vector3 center, float width, float height)
        {
            yield return new WaitForSeconds(0.8f);      // let the confetti start first

            int count = starsInJar.Count;
            for (int i = 0; i < count; i++)
            {
                // The top star leaves first. It moves to the "celebrating" list right away,
                // so ResetJar() can always find and remove it, even while it is flying.
                Transform star = starsInJar[starsInJar.Count - 1];
                starsInJar.RemoveAt(starsInJar.Count - 1);
                celebrating.Add(star);
                star.SetParent(playArea, true);
                Vector3 from = star.localPosition;
                Vector3 to = PointOnFrame(i / (float)count, center, width, height);
                StartCoroutine(Fly(star, from, to, 0.9f, 0.35f, StarInJarSize, CelebrationStarSize, i));
                yield return new WaitForSeconds(Mathf.Clamp(2.5f / count, 0.12f, 0.35f));
            }
        }

        /// <summary>A point on the outline of a rectangle, starting at the top centre and going clockwise.</summary>
        private static Vector3 PointOnFrame(float t, Vector3 center, float width, float height)
        {
            float perimeter = 2f * (width + height);
            float d = Mathf.Repeat(t, 1f) * perimeter;
            float halfW = width * 0.5f, halfH = height * 0.5f;
            Vector2 p;
            if (d < halfW) p = new Vector2(d, halfH);
            else if ((d -= halfW) < height) p = new Vector2(halfW, halfH - d);
            else if ((d -= height) < width) p = new Vector2(halfW - d, -halfH);
            else if ((d -= width) < height) p = new Vector2(-halfW, -halfH + d);
            else p = new Vector2(-halfW + (d - height), halfH);
            return center + new Vector3(p.x, p.y, 0f);
        }

        // ------------------------------------------------------------------ Animation

        /// <summary>
        /// Moves a star along an arc in its parent's space, spinning and changing size.
        /// At FINISH (the star grows) it plays a "ding" whose pitch rises with <paramref name="celebrationIndex"/>.
        /// </summary>
        private IEnumerator Fly(Transform star, Vector3 from, Vector3 to, float time, float arc,
            float startSize, float endSize, float celebrationIndex)
        {
            bool celebration = endSize > startSize;
            float t = 0f;
            while (t < 1f && star != null)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / time);
                float eased = 1f - (1f - t) * (1f - t);
                Vector3 p = Vector3.Lerp(from, to, eased);
                p.y += Mathf.Sin(t * Mathf.PI) * arc;
                star.localPosition = p;
                star.localRotation = Quaternion.Euler(0f, t * 720f, 0f);
                star.localScale = Vector3.one * Mathf.Lerp(startSize, endSize, t);
                yield return null;
            }
            if (star == null) yield break;

            if (celebration)
            {
                // A rising "ding" and a small buzz for every star that arrives.
                if (source != null) source.pitch = Mathf.Min(2f, 1f + celebrationIndex * 0.06f);
                audioManager.Play(Sfx.Star, source, 0.6f);
                Haptics.PulseBoth(0.25f, 0.04f);
                arrived.Add(star);
            }
        }

        private void Update()
        {
            // The finished frame of stars keeps spinning and twinkling.
            for (int i = 0; i < celebrating.Count; i++)
            {
                Transform star = celebrating[i];
                if (star == null || !arrived.Contains(star)) continue;
                star.localRotation = Quaternion.Euler(0f, Time.time * 90f + i * 25f, 0f);
                star.localScale = Vector3.one * CelebrationStarSize * (1f + 0.15f * Mathf.Sin(Time.time * 4f + i));
            }
        }
    }
}
