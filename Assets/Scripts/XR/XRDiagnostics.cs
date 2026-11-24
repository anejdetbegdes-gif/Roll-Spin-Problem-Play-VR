using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace RSPP
{
    /// <summary>
    /// DEBUG HELPER. While a headset is active, writes one "[RSPP VR]" line to the Console every 3 seconds
    /// with the head (camera), rig and table positions. This shows where the player really is
    /// when the headset view looks wrong. It changes nothing in the game and can be removed later.
    /// </summary>
    public class XRDiagnostics : MonoBehaviour
    {
        public XROrigin Origin;
        public Camera Head;
        public Transform PlayArea;

        private IEnumerator Start()
        {
            var wait = new WaitForSecondsRealtime(3f);
            while (true)
            {
                if (XRSettings.isDeviceActive && Origin != null && Head != null) Debug.Log(Describe());
                yield return wait;
            }
        }

        private string Describe()
        {
            Transform cam = Head.transform;
            Transform offset = Origin.CameraFloorOffsetObject != null ? Origin.CameraFloorOffsetObject.transform : null;

            return "[RSPP VR] camera world " + cam.position.ToString("F2") +
                   " local " + cam.localPosition.ToString("F2") +
                   " forward " + cam.forward.ToString("F2") +
                   " | rig " + Origin.transform.position.ToString("F2") +
                   " rot " + Origin.transform.eulerAngles.ToString("F0") +
                   " | camera offset " + (offset != null ? offset.localPosition.ToString("F2") : "none") +
                   " | tracking " + Origin.CurrentTrackingOriginMode +
                   " | table " + (PlayArea != null ? PlayArea.position.ToString("F2") : "none") +
                   " | device head " + DeviceHeadPosition() +
                   " | fov " + Head.fieldOfView.ToString("F0") + " stereo " + Head.stereoEnabled +
                   " | focused " + Application.isFocused + " frame " + Time.frameCount +
                   " | " + SystemInfo.graphicsDeviceType + " " + XRSettings.stereoRenderingMode;
        }

        /// <summary>The head position read straight from the XR device (not through the camera), to compare.</summary>
        private static string DeviceHeadPosition()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            Vector3 position;
            if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
                return position.ToString("F2");
            return "none";
        }
    }
}
