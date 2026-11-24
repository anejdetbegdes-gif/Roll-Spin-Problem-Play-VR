using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RSPP
{
    /// <summary>
    /// The die. The player grabs it with the LEFT hand (grip) and rolls it by pressing the trigger
    /// or simply letting go.
    ///
    /// Design choice (simplest reliable solution): the roll is a short scripted tumble instead of a
    /// free physics throw. The result (1-6) is chosen at random, the die tumbles back onto its tray and
    /// lands with that number on top. This way the die can never fall off the table, get stuck on an
    /// edge, or be thrown across the room, and the top face always matches the number used by the game.
    /// </summary>
    public class Dice : MonoBehaviour
    {
        /// <summary>Raised when a roll begins.</summary>
        public event Action RollStarted;

        /// <summary>Raised when the die has landed. The argument is the number on top (1-6).</summary>
        public event Action<int> Rolled;

        /// <summary>When false, grabbing and releasing the die just puts it back on its tray.</summary>
        public bool RollEnabled { get; set; }

        public bool IsRolling { get; private set; }

        private XRGrabInteractable grab;
        private AudioManager audioManager;
        private AudioSource audioSource;
        private Transform homeParent;
        private Vector3 homeLocalPosition;
        private GameObject tray;
        private bool resetting;

        private static readonly Color TrayIdle = new Color(0.55f, 0.58f, 0.62f);
        private static readonly Color TrayActive = new Color(1.00f, 0.80f, 0.20f);

        // ------------------------------------------------------------------ Construction

        public static Dice Create(Transform playArea, AudioManager audio)
        {
            float size = GameConfig.DieSize;

            // Tray that marks where the die lives. It turns yellow when it is time to roll.
            Vector3 home = GameConfig.DieHome;
            GameObject trayObject = Shapes.Box("Die Tray", playArea, new Vector3(home.x, 0.003f, home.z),
                new Vector3(size * 1.9f, 0.006f, size * 1.9f), TrayIdle);

            var go = new GameObject("Die");
            go.transform.SetParent(playArea, false);
            go.transform.localPosition = home;

            Shapes.Box("Die Body", go.transform, Vector3.zero, Vector3.one * size, Color.white);
            for (int face = 1; face <= 6; face++) AddPips(go.transform, face, size);

            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one * size;

            // Kinematic: the die is moved by the hand or by the roll animation, never by physics forces.
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var dice = go.AddComponent<Dice>();
            dice.audioManager = audio;
            dice.audioSource = audio.CreateSpatialSource(go.transform);
            dice.homeParent = playArea;
            dice.homeLocalPosition = home;
            dice.tray = trayObject;

            dice.grab = go.AddComponent<XRGrabInteractable>();
            dice.grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            dice.grab.throwOnDetach = false;
            dice.grab.useDynamicAttach = true;      // the die stays where the hand grabbed it (no snapping)
            dice.grab.selectEntered.AddListener(dice.OnGrabbed);
            dice.grab.selectExited.AddListener(dice.OnReleased);
            dice.grab.activated.AddListener(dice.OnTriggerPressed);
            return dice;
        }

        /// <summary>Rotation that turns the die's local "up" towards the given face (1-6). Opposite faces add up to 7.</summary>
        private static Quaternion FaceRotation(int face)
        {
            switch (face)
            {
                case 1: return Quaternion.identity;              // +Y
                case 2: return Quaternion.Euler(90f, 0f, 0f);    // +Z
                case 3: return Quaternion.Euler(0f, 0f, -90f);   // +X
                case 4: return Quaternion.Euler(0f, 0f, 90f);    // -X
                case 5: return Quaternion.Euler(-90f, 0f, 0f);   // -Z
                default: return Quaternion.Euler(180f, 0f, 0f);  // -Y (6)
            }
        }

        private static void AddPips(Transform die, int face, float size)
        {
            // Pip positions on a 3 x 3 grid (-1, 0, 1).
            Vector2[] layout;
            switch (face)
            {
                case 1: layout = new[] { new Vector2(0, 0) }; break;
                case 2: layout = new[] { new Vector2(-1, -1), new Vector2(1, 1) }; break;
                case 3: layout = new[] { new Vector2(-1, -1), new Vector2(0, 0), new Vector2(1, 1) }; break;
                case 4: layout = new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 1) }; break;
                case 5: layout = new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(0, 0), new Vector2(1, -1), new Vector2(1, 1) }; break;
                default: layout = new[] { new Vector2(-1, -1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 0), new Vector2(1, 1) }; break;
            }

            Quaternion rotation = FaceRotation(face);
            float step = size * 0.27f;
            float pipDiameter = size * 0.19f;
            var pipColor = new Color(0.10f, 0.10f, 0.15f);

            foreach (Vector2 p in layout)
            {
                // Build the pip on the top (+Y) face, then rotate it onto the right face.
                Vector3 onTop = new Vector3(p.x * step, size * 0.5f + 0.0005f, p.y * step);
                GameObject pip = Shapes.Cylinder("Pip " + face, die, rotation * onTop, pipDiameter, 0.002f, pipColor);
                pip.transform.localRotation = rotation;
            }
        }

        // ------------------------------------------------------------------ Public control

        /// <summary>Highlights the tray to show that it is time to roll.</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (tray != null) Shapes.SetColor(tray, highlighted ? TrayActive : TrayIdle);
        }

        /// <summary>Stops any roll and puts the die back on its tray (used by Start / Restart).</summary>
        public void ResetToHome()
        {
            StopAllCoroutines();
            IsRolling = false;
            grab.enabled = true;

            // If the die is being held, take it out of the hand without starting a roll.
            resetting = true;
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            resetting = false;

            SnapHome(Quaternion.identity);
        }

        /// <summary>Desktop test mode: rolls the die as if it had been picked up and released.</summary>
        public void RollFromDesktop()
        {
            if (!RollEnabled || IsRolling || grab.isSelected) return;
            transform.position = homeParent.TransformPoint(homeLocalPosition + new Vector3(0.05f, 0.22f, -0.03f));
            StartCoroutine(RollRoutine());
        }

        // ------------------------------------------------------------------ XR events

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            Haptics.PulseLeft(0.3f, 0.05f);
        }

        /// <summary>Trigger pressed while holding the die: let go of it, which starts the roll.</summary>
        private void OnTriggerPressed(ActivateEventArgs args)
        {
            if (grab.isSelected) StartCoroutine(ReleaseNextFrame());
        }

        private IEnumerator ReleaseNextFrame()
        {
            yield return null;
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            if (resetting || IsRolling || !isActiveAndEnabled) return;

            if (RollEnabled) StartCoroutine(RollRoutine());
            else StartCoroutine(ReturnHomeNextFrame());
        }

        private IEnumerator ReturnHomeNextFrame()
        {
            yield return null;
            if (!grab.isSelected) SnapHome(transform.rotation);
        }

        // ------------------------------------------------------------------ Roll

        private IEnumerator RollRoutine()
        {
            IsRolling = true;

            // Wait one frame so the XR Interaction Toolkit finishes releasing the die,
            // then make it un-grabbable until it has landed.
            yield return null;
            grab.enabled = false;
            if (transform.parent != homeParent) transform.SetParent(homeParent, true);

            if (RollStarted != null) RollStarted();

            int result = UnityEngine.Random.Range(1, 7);    // 1..6

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 endPosition = homeParent.TransformPoint(homeLocalPosition);

            // Final pose: chosen face up, with a small random turn so it looks natural.
            Quaternion endRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-30f, 30f), 0f)
                                     * Quaternion.Inverse(FaceRotation(result));
            Vector3 spinAxis = UnityEngine.Random.onUnitSphere;

            // Tumble through the air onto the tray.
            const float flightTime = 0.85f;
            float arcHeight = Mathf.Max(0.10f, 0.22f - Mathf.Max(0f, startPosition.y - endPosition.y) * 0.5f);
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / flightTime);
                Vector3 p = Vector3.Lerp(startPosition, endPosition, t);
                p.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = p;

                float remaining = (1f - t) * (1f - t);
                Quaternion tumble = endRotation * Quaternion.AngleAxis(remaining * 1080f, spinAxis);
                transform.rotation = Quaternion.Slerp(startRotation, tumble, Mathf.Clamp01(t * 6f));
                yield return null;
            }

            audioManager.Play(Sfx.DiceLand, audioSource);
            Haptics.PulseLeft(0.6f, 0.1f);

            // One small bounce.
            const float bounceTime = 0.22f;
            t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / bounceTime);
                Vector3 p = endPosition;
                p.y += Mathf.Sin(t * Mathf.PI) * 0.03f;
                transform.position = p;
                yield return null;
            }
            audioManager.Play(Sfx.DiceLand, audioSource, 0.4f);

            transform.position = endPosition;
            transform.rotation = endRotation;
            yield return new WaitForSeconds(0.35f);

            IsRolling = false;
            grab.enabled = true;
            if (Rolled != null) Rolled(result);
        }

        private void SnapHome(Quaternion rotation)
        {
            if (transform.parent != homeParent) transform.SetParent(homeParent, true);
            transform.localPosition = homeLocalPosition;
            transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        }
    }
}
