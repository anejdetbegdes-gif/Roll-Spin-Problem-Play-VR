using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RSPP
{
    /// <summary>
    /// The SPIN handle on the table. The player holds it with the LEFT hand (grip)
    /// and presses the trigger to spin the wheel.
    /// </summary>
    public class WheelHandle : MonoBehaviour
    {
        /// <summary>Raised when the player pulls the handle (trigger while holding it).</summary>
        public event Action Pulled;

        private static readonly Color KnobIdle = new Color(0.55f, 0.58f, 0.62f);
        private static readonly Color KnobActive = new Color(1.00f, 0.80f, 0.20f);
        private static readonly Color KnobHeld = new Color(0.20f, 0.80f, 0.30f);

        private GameObject knob;
        private bool highlighted;
        private bool held;

        public static WheelHandle Create(Transform playArea)
        {
            var go = new GameObject("Wheel Handle");
            go.transform.SetParent(playArea, false);
            go.transform.localPosition = GameConfig.HandlePosition;

            var dark = new Color(0.20f, 0.20f, 0.28f);
            Shapes.Box("Handle Base", go.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.10f, 0.02f, 0.10f), dark);
            Shapes.Cylinder("Handle Stem", go.transform, new Vector3(0f, 0.06f, 0f), 0.016f, 0.08f, dark);

            var handle = go.AddComponent<WheelHandle>();
            handle.knob = Shapes.Sphere("Handle Knob", go.transform, new Vector3(0f, 0.115f, 0f), 0.06f, KnobIdle);

            var grabZone = go.AddComponent<SphereCollider>();
            grabZone.center = new Vector3(0f, 0.10f, 0f);
            grabZone.radius = 0.05f;

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // Simple Interactable: it can be held (grip) and activated (trigger), but it does not move.
            var interactable = go.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(handle.OnHeld);
            interactable.selectExited.AddListener(handle.OnLetGo);
            interactable.activated.AddListener(handle.OnTriggerPressed);
            return handle;
        }

        /// <summary>Highlights the knob to show that it is time to spin.</summary>
        public void SetHighlighted(bool value)
        {
            highlighted = value;
            UpdateColor();
        }

        /// <summary>Desktop test mode: acts as if the handle had been pulled.</summary>
        public void PullFromDesktop()
        {
            if (Pulled != null) Pulled();
        }

        private void OnHeld(SelectEnterEventArgs args)
        {
            held = true;
            UpdateColor();
            Haptics.PulseLeft(0.3f, 0.05f);
        }

        private void OnLetGo(SelectExitEventArgs args)
        {
            held = false;
            UpdateColor();
        }

        private void OnTriggerPressed(ActivateEventArgs args)
        {
            Haptics.PulseLeft(0.5f, 0.08f);
            if (Pulled != null) Pulled();
        }

        private void UpdateColor()
        {
            if (knob == null) return;
            Shapes.SetColor(knob, held ? KnobHeld : (highlighted ? KnobActive : KnobIdle));
        }
    }
}
