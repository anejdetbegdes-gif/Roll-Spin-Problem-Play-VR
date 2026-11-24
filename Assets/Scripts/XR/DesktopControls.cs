using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace RSPP
{
    /// <summary>
    /// Lets you test the whole game in the Unity Editor WITHOUT a headset:
    ///   Mouse (left click)  = point and click UI buttons (replaces the right-hand ray)
    ///   Space               = grab and roll the die     (replaces the left hand)
    ///   Enter or S          = pull the SPIN handle      (replaces the left hand)
    ///   Hold right mouse    = look around
    ///   R                   = re-centre the view
    /// This component switches itself off when a headset is active.
    /// </summary>
    public class DesktopControls : MonoBehaviour
    {
        public GameRig Rig;
        public Dice Dice;
        public WheelHandle Handle;
        public PlayAreaFitter Fitter;
        public AIHelperRobot Robot;

        private float yaw;
        private float pitch = 12f;

        private void Start()
        {
            // Desktop mode is only for the Editor / PC without a headset. Never on the Quest itself.
            if (Application.isMobilePlatform || XRSettings.isDeviceActive || (Keyboard.current == null && Mouse.current == null))
            {
                enabled = false;
                return;
            }

            // No headset: hide the untracked controllers and tilt the camera down towards the board.
            SetHandsVisible(false);
            ApplyLook();

            Debug.Log("[RSPP] No headset detected: desktop test mode. Mouse = click buttons, Space = roll die, " +
                      "Enter or S = spin wheel, hold right mouse = look, R = recentre.");
        }

        private void SetHandsVisible(bool visible)
        {
            if (Rig == null) return;
            if (Rig.LeftHand != null) Rig.LeftHand.SetActive(visible);
            if (Rig.RightHand != null) Rig.RightHand.SetActive(visible);
        }

        private void Update()
        {
            // A headset was connected after Play was pressed (for example Quest Link): switch to VR.
            if (XRSettings.isDeviceActive)
            {
                SetHandsVisible(true);
                enabled = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame && Dice != null) Dice.RollFromDesktop();
                if ((keyboard.enterKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) && Handle != null) Handle.PullFromDesktop();
                if (keyboard.tKey.wasPressedThisFrame && Robot != null) Robot.TouchFromDesktop();
                if (keyboard.rKey.wasPressedThisFrame && Fitter != null)
                {
                    yaw = 0f;
                    pitch = 12f;
                    ApplyLook();
                    Fitter.FitToPlayer();
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw = Mathf.Clamp(yaw + delta.x * 0.15f, -110f, 110f);
                pitch = Mathf.Clamp(pitch - delta.y * 0.15f, -60f, 75f);
                ApplyLook();
            }
        }

        private void ApplyLook()
        {
            if (Rig != null && Rig.Camera != null)
                Rig.Camera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
