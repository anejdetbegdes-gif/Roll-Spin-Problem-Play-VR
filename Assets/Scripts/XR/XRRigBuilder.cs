using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace RSPP
{
    /// <summary>References to the parts of the XR rig that other scripts need.</summary>
    public class GameRig
    {
        public GameObject Root;
        public XROrigin Origin;
        public Camera Camera;
        public GameObject LeftHand;
        public GameObject RightHand;
        public XRDirectInteractor DirectInteractor;
        public XRRayInteractor RayInteractor;
        public InputAction RecenterAction;
    }

    /// <summary>
    /// Builds the XR rig with the XR Interaction Toolkit (version 3.x):
    ///
    ///   LEFT hand  = XR Direct Interactor  (grip = grab, trigger = activate)  -> die and wheel handle
    ///   RIGHT hand = XR Ray Interactor     (trigger = UI click)               -> menu and answer buttons
    ///
    /// The input actions are created here in code with the standard XR controller bindings,
    /// so no Input Action asset has to be assigned by hand. The interactors never need to be toggled.
    /// </summary>
    public static class XRRigBuilder
    {
        public static GameRig Build()
        {
            var rig = new GameRig();

            // The Interaction Manager connects interactors (hands) with interactables (die, handle).
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            // The Event System with the XR UI Input Module lets the ray (and the mouse) click UI buttons.
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<XRUIInputModule>();

            // Build the rig while it is inactive, so every component is fully configured
            // before Unity calls Awake / OnEnable on it.
            rig.Root = new GameObject("XR Origin (XR Rig)");
            rig.Root.SetActive(false);

            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(rig.Root.transform, false);

            // ---------------------------------------------------------------- Head / camera
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            // Used only when no headset is tracking (desktop test mode).
            cameraObject.transform.localPosition = new Vector3(0f, GameConfig.DesktopEyeHeight, 0f);

            rig.Camera = cameraObject.AddComponent<Camera>();
            rig.Camera.nearClipPlane = 0.05f;
            rig.Camera.farClipPlane = 30f;
            rig.Camera.clearFlags = CameraClearFlags.SolidColor;
            rig.Camera.backgroundColor = new Color(0.75f, 0.88f, 0.98f);
            cameraObject.AddComponent<AudioListener>();

            var headPose = cameraObject.AddComponent<TrackedPoseDriver>();
            headPose.positionInput = new InputActionProperty(
                new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", null, null, "Vector3"));
            headPose.rotationInput = new InputActionProperty(
                new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", null, null, "Quaternion"));

            rig.Origin = rig.Root.AddComponent<XROrigin>();
            rig.Origin.Origin = rig.Root;
            rig.Origin.CameraFloorOffsetObject = cameraOffset;
            rig.Origin.Camera = rig.Camera;
            rig.Origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // ---------------------------------------------------------------- Left hand: Direct Interactor
            rig.LeftHand = CreateHand("Left Controller (Direct Interactor)", cameraOffset.transform, "{LeftHand}",
                "devicePosition", "deviceRotation");

            var grabZone = rig.LeftHand.AddComponent<SphereCollider>();
            grabZone.isTrigger = true;
            grabZone.radius = 0.09f;       // generous grab zone for small hands

            rig.DirectInteractor = rig.LeftHand.AddComponent<XRDirectInteractor>();
            rig.DirectInteractor.improveAccuracyWithSphereCollider = true;
            BindButton(rig.DirectInteractor.selectInput, "Left Grip", "<XRController>{LeftHand}/{GripButton}", "<XRController>{LeftHand}/{Grip}");
            BindButton(rig.DirectInteractor.activateInput, "Left Trigger", "<XRController>{LeftHand}/{TriggerButton}", "<XRController>{LeftHand}/{Trigger}");

            Haptics.Left = CreateHaptics(rig.LeftHand, "<XRController>{LeftHand}/*");
            CreateHandVisual(rig.LeftHand.transform, new Color(0.25f, 0.56f, 0.90f));

            // ---------------------------------------------------------------- Right hand: Ray Interactor
            rig.RightHand = CreateHand("Right Controller (Ray Interactor)", cameraOffset.transform, "{RightHand}",
                "pointerPosition", "pointerRotation");

            rig.RayInteractor = rig.RightHand.AddComponent<XRRayInteractor>();
            rig.RayInteractor.enableUIInteraction = true;
            rig.RayInteractor.maxRaycastDistance = 6f;
            // The ray is for UI only: it is on no interaction layer, so it cannot grab the die or the handle.
            rig.RayInteractor.interactionLayers = 0;
            rig.RayInteractor.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            rig.RayInteractor.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            BindButton(rig.RayInteractor.uiPressInput, "Right Trigger (UI Press)", "<XRController>{RightHand}/{TriggerButton}", "<XRController>{RightHand}/{Trigger}");

            var line = rig.RightHand.AddComponent<LineRenderer>();
            line.material = CreateLineMaterial();
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;

            var lineVisual = rig.RightHand.AddComponent<XRInteractorLineVisual>();
            lineVisual.lineWidth = 0.006f;
            lineVisual.overrideInteractorLineLength = true;
            lineVisual.lineLength = 3f;
            lineVisual.stopLineAtFirstRaycastHit = true;
            lineVisual.validColorGradient = SolidGradient(new Color(0.2f, 1f, 0.3f), 1f, 1f);
            lineVisual.invalidColorGradient = SolidGradient(Color.white, 0.9f, 0.15f);

            Haptics.Right = CreateHaptics(rig.RightHand, "<XRController>{RightHand}/*");
            CreateHandVisual(rig.RightHand.transform, new Color(0.93f, 0.50f, 0.08f));

            // ---------------------------------------------------------------- Supervisor convenience: recenter
            // Clicking either thumbstick re-centres the player in front of the board.
            rig.RecenterAction = new InputAction("Recenter", InputActionType.Button);
            rig.RecenterAction.AddBinding("<XRController>{LeftHand}/{Primary2DAxisClick}");
            rig.RecenterAction.AddBinding("<XRController>{RightHand}/{Primary2DAxisClick}");
            rig.RecenterAction.Enable();

            rig.Root.SetActive(true);
            return rig;
        }

        // ------------------------------------------------------------------ Helpers

        private static GameObject CreateHand(string name, Transform parent, string handUsage, string positionControl, string rotationControl)
        {
            var hand = new GameObject(name);
            hand.transform.SetParent(parent, false);

            var pose = hand.AddComponent<TrackedPoseDriver>();
            pose.positionInput = new InputActionProperty(new InputAction(name + " Position", InputActionType.Value,
                "<XRController>" + handUsage + "/" + positionControl, null, null, "Vector3"));
            pose.rotationInput = new InputActionProperty(new InputAction(name + " Rotation", InputActionType.Value,
                "<XRController>" + handUsage + "/" + rotationControl, null, null, "Quaternion"));
            return hand;
        }

        /// <summary>Points an XRI input reader at a controller button (pressed state + analog value).</summary>
        private static void BindButton(XRInputButtonReader reader, string name, string buttonBinding, string valueBinding)
        {
            reader.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
            reader.inputActionPerformed = new InputAction(name, InputActionType.Button, buttonBinding);
            reader.inputActionValue = new InputAction(name + " Value", InputActionType.Value, valueBinding, null, null, "Axis");
        }

        private static HapticImpulsePlayer CreateHaptics(GameObject hand, string hapticBinding)
        {
            var player = hand.AddComponent<HapticImpulsePlayer>();
            player.hapticOutput.inputSourceMode = XRInputHapticImpulseProvider.InputSourceMode.InputAction;
            player.hapticOutput.inputAction = new InputAction(hand.name + " Haptic", InputActionType.PassThrough, hapticBinding);
            return player;
        }

        /// <summary>A very simple controller stand-in so players can see where their hands are.</summary>
        private static void CreateHandVisual(Transform hand, Color color)
        {
            Shapes.Sphere("Hand Visual", hand, Vector3.zero, 0.07f, color);
            Shapes.Box("Hand Pointer", hand, new Vector3(0f, 0f, 0.05f), new Vector3(0.02f, 0.02f, 0.06f), Color.white);
        }

        private static Material CreateLineMaterial()
        {
            // "Sprites/Default" is always included in builds and supports the line's vertex colors.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) return new Material(shader);
            return Shapes.Mat(Color.white);
        }

        private static Gradient SolidGradient(Color color, float startAlpha, float endAlpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(endAlpha, 1f) });
            return gradient;
        }
    }
}
