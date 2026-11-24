using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace RSPP
{
    /// <summary>
    /// Shows a short VR status line at the bottom of the entrance screen while it is visible:
    /// is VR active, and are both controllers found? This makes set-up problems visible
    /// inside the headset instead of the game silently not reacting. (English only: it is for the adult.)
    /// </summary>
    public class XRStatus : MonoBehaviour
    {
        public EntranceScreen Entrance;

        private IEnumerator Start()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                if (Entrance != null && Entrance.IsVisible) Entrance.SetStatus(Describe());
                yield return wait;
            }
        }

        /// <summary>One or two short lines describing the VR state.</summary>
        public static string Describe()
        {
#if !ENABLE_INPUT_SYSTEM
            return "SETUP PROBLEM: the Input System is switched off. In Unity run the menu RSPP > Set Up XR For Meta Quest and restart the Editor.";
#else
            if (!XRSettings.isDeviceActive)
            {
                // The XR plug-in is loaded but the headset session has not started yet (takes about a second with Link).
                XRGeneralSettings xr = XRGeneralSettings.Instance;
                if (xr != null && xr.Manager != null && xr.Manager.activeLoader != null)
                    return "Starting the headset... If this stays, put the headset on.";

                if (Application.isEditor)
                    return "No headset found: desktop test mode. Mouse = click, Space = roll, Enter = spin, T = touch Robo.";
                return "SETUP PROBLEM: VR is not active in this build. In Unity run the menu RSPP > Set Up XR For Meta Quest, then build again.";
            }

            bool left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid;
            bool right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand).isValid;
            string status = "VR: " + XRSettings.loadedDeviceName +
                            "   Left controller: " + (left ? "OK" : "NOT FOUND") +
                            "   Right controller: " + (right ? "OK" : "NOT FOUND");
            if (!left || !right)
                status += "\nPick up both controllers. If one stays NOT FOUND, run RSPP > Set Up XR For Meta Quest in Unity and build again.";
            return status;
#endif
        }
    }
}
