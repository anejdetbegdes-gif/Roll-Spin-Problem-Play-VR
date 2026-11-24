using System.Collections;
using UnityEngine;

namespace RSPP
{
    /// <summary>
    /// Small celebration effects:
    ///   - StarPop: a gold star pops up above the game piece whenever a star is earned.
    ///   - Confetti: a burst of colourful confetti over the board when the player reaches FINISH.
    /// Both are cheap (one mesh, one small particle burst) and need no extra assets.
    /// </summary>
    public class Effects : MonoBehaviour
    {
        private ParticleSystem confetti;

        public static Effects Create(Transform playArea)
        {
            var effects = new GameObject("Effects").AddComponent<Effects>();
            effects.transform.SetParent(playArea, false);
            effects.confetti = CreateConfetti(effects.transform);
            return effects;
        }

        // ------------------------------------------------------------------ Star pop

        /// <summary>A gold star rises from <paramref name="worldPosition"/>, grows, spins and disappears.</summary>
        public void StarPop(Vector3 worldPosition)
        {
            StartCoroutine(StarPopRoutine(worldPosition));
        }

        private IEnumerator StarPopRoutine(Vector3 start)
        {
            GameObject star = Shapes.Star("Star Pop", transform, Vector3.zero, 0.06f, GameConfig.StarColor);
            const float duration = 0.9f;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / duration);
                star.transform.position = start + Vector3.up * (0.28f * t);
                star.transform.rotation = Quaternion.Euler(0f, t * 360f, 0f);
                // Grow quickly, then shrink away at the end.
                float size = t < 0.7f ? Mathf.Lerp(0.06f, 0.11f, t / 0.7f) : Mathf.Lerp(0.11f, 0f, (t - 0.7f) / 0.3f);
                star.transform.localScale = Vector3.one * size;
                yield return null;
            }
            Destroy(star);
        }

        // ------------------------------------------------------------------ Confetti

        /// <summary>Shoots a burst of confetti up over the board.</summary>
        public void Confetti()
        {
            if (confetti == null) return;
            confetti.Clear();
            confetti.Play();
        }

        private static ParticleSystem CreateConfetti(Transform parent)
        {
            // Built inactive so the settings are applied before the system can start.
            var go = new GameObject("Confetti");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, GameConfig.TableCenter.z);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);    // the cone points up

            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 250;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Random bright colours from the board's tile colours.
            var gradient = new Gradient();
            gradient.mode = GradientMode.Fixed;
            Color[] colors = GameConfig.TileColors;
            var keys = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++) keys[i] = new GradientColorKey(colors[i], (i + 1f) / colors.Length);
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var startColor = new ParticleSystem.MinMaxGradient(gradient);
            startColor.mode = ParticleSystemGradientMode.RandomColor;
            main.startColor = startColor;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 180) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.05f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");    // always included in builds
            renderer.sharedMaterial = shader != null ? new Material(shader) : Shapes.Mat(Color.white);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            go.SetActive(true);
            return ps;
        }
    }
}
