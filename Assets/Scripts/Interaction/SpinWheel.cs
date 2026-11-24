using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// The spinning wheel. It visibly rotates, slows down, and stops with one of its eight
    /// segments under the pointer at the top. The segment decides what happens next:
    /// a math category, a mini challenge, or a bonus.
    /// </summary>
    public class SpinWheel : MonoBehaviour
    {
        /// <summary>Raised when the wheel has stopped.</summary>
        public event Action<WheelSegment> Stopped;

        public bool IsSpinning { get; private set; }

        private const float SpinTime = 3.2f;
        private const float PointerAngle = 90f;     // the pointer sits at the top of the wheel

        private Transform disc;
        private float angle;                        // current rotation of the disc around its Z axis
        private AudioManager audioManager;
        private AudioSource audioSource;

        // ------------------------------------------------------------------ Construction

        public static SpinWheel Create(Transform playArea, AudioManager audio)
        {
            Vector3 position = GameConfig.WheelPosition;
            float radius = GameConfig.WheelRadius;
            WheelSegment[] layout = GameConfig.WheelLayout;
            float segmentAngle = 360f / layout.Length;

            // The wheel's local -Z side faces the player.
            var go = new GameObject("Spin Wheel");
            go.transform.SetParent(playArea, false);
            go.transform.localPosition = position;
            go.transform.localRotation = UIFactory.FacePlayer(position);

            var wheel = go.AddComponent<SpinWheel>();
            wheel.audioManager = audio;
            wheel.audioSource = audio.CreateSpatialSource(go.transform);

            // Stand.
            var standColor = new Color(0.40f, 0.28f, 0.20f);
            Shapes.Cylinder("Wheel Pole", go.transform, new Vector3(0f, -position.y * 0.5f, 0.03f), 0.03f, position.y, standColor);
            Shapes.Box("Wheel Foot", go.transform, new Vector3(0f, -position.y + 0.01f, 0.03f), new Vector3(0.20f, 0.02f, 0.14f), standColor);

            // Rotating disc.
            var discObject = new GameObject("Disc");
            discObject.transform.SetParent(go.transform, false);
            wheel.disc = discObject.transform;

            GameObject backPlate = Shapes.Cylinder("Back Plate", wheel.disc, new Vector3(0f, 0f, 0.009f), radius * 2f + 0.02f, 0.014f, new Color(0.20f, 0.20f, 0.28f));
            backPlate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            for (int i = 0; i < layout.Length; i++)
            {
                Mesh wedge = Shapes.WedgeMesh(i * segmentAngle, (i + 1) * segmentAngle, radius, 6);
                Shapes.Make("Segment " + i + " (" + layout[i] + ")", wheel.disc, wedge, Vector3.zero, Vector3.one, GameConfig.WheelColor(layout[i]));
            }

            GameObject hub = Shapes.Sphere("Hub", wheel.disc, new Vector3(0f, 0f, -0.004f), 0.04f, Color.white);
            hub.transform.localScale = new Vector3(0.04f, 0.04f, 0.012f);

            // Labels rotate with the disc.
            float canvasSize = radius * 2f / UIFactory.CanvasScale;
            Canvas canvas = UIFactory.WorldCanvas("Wheel Labels", wheel.disc, new Vector3(0f, 0f, -0.002f), Quaternion.identity,
                canvasSize, canvasSize, null, false);
            for (int i = 0; i < layout.Length; i++)
            {
                bool symbol = layout[i] == WheelSegment.Addition || layout[i] == WheelSegment.Subtraction;
                Text label = UIFactory.Label("Label " + i, canvas.transform, "", 0f, 0f, 96f, 84f,
                    symbol ? 34 : 22, layout[i] == WheelSegment.MiniChallenge ? GameConfig.TextDark : Color.white);
                label.resizeTextMinSize = 14;    // "Take away" shrinks to fit its slice
                UIFactory.Localize(label, GameConfig.WheelLabelKey(layout[i]));

                float centreAngle = (i + 0.5f) * segmentAngle;
                float rad = centreAngle * Mathf.Deg2Rad;
                float distance = radius * 0.64f / UIFactory.CanvasScale;

                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(Mathf.Cos(rad) * distance, Mathf.Sin(rad) * distance);
                rect.localRotation = Quaternion.Euler(0f, 0f, centreAngle - 90f);   // upright when at the top
            }

            // Fixed red pointer at the top (does not rotate).
            Mesh pointerMesh = Shapes.WedgeMesh(65f, 115f, 0.075f, 2);
            Shapes.Make("Pointer", go.transform, pointerMesh, new Vector3(0f, radius - 0.02f, -0.012f), Vector3.one, new Color(0.85f, 0.10f, 0.10f));

            return wheel;
        }

        // ------------------------------------------------------------------ Control

        /// <summary>Starts a spin. Does nothing if the wheel is already spinning.</summary>
        public void Spin()
        {
            if (IsSpinning) return;
            StartCoroutine(SpinRoutine());
        }

        /// <summary>Stops any spin in progress (used by Start / Restart).</summary>
        public void ResetWheel()
        {
            StopAllCoroutines();
            IsSpinning = false;
        }

        private IEnumerator SpinRoutine()
        {
            IsSpinning = true;

            WheelSegment[] layout = GameConfig.WheelLayout;
            float segmentAngle = 360f / layout.Length;

            // Choose the result first, then rotate the disc so that segment ends under the pointer.
            int target = UnityEngine.Random.Range(0, layout.Length);
            float insideSegment = (target + 0.5f + UnityEngine.Random.Range(-0.3f, 0.3f)) * segmentAngle;
            float finalAngle = Mathf.Repeat(PointerAngle - insideSegment, 360f);

            float startAngle = Mathf.Repeat(angle, 360f);
            float travel = 360f * 3f + Mathf.Repeat(finalAngle - startAngle, 360f);    // at least three full turns

            int lastTick = Mathf.FloorToInt((startAngle - PointerAngle) / segmentAngle);
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / SpinTime);
                float eased = 1f - Mathf.Pow(1f - t, 3f);       // fast at first, then slows down
                angle = startAngle + travel * eased;
                disc.localRotation = Quaternion.Euler(0f, 0f, angle);

                // Tick each time a segment border passes the pointer.
                int tick = Mathf.FloorToInt((angle - PointerAngle) / segmentAngle);
                if (tick != lastTick)
                {
                    lastTick = tick;
                    audioManager.Play(Sfx.WheelTick, audioSource, 0.7f);
                }
                yield return null;
            }

            angle = finalAngle;
            disc.localRotation = Quaternion.Euler(0f, 0f, angle);
            audioManager.Play(Sfx.WheelStop, audioSource);
            Haptics.PulseLeft(0.5f, 0.15f);

            yield return new WaitForSeconds(0.5f);
            IsSpinning = false;
            if (Stopped != null) Stopped(layout[target]);
        }
    }
}
