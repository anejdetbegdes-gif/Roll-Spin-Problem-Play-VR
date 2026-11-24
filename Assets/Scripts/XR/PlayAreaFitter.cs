using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace RSPP
{
    /// <summary>
    /// Keeps the game comfortable for a stationary player, seated or standing:
    ///   - moves the player's viewpoint in front of the board and turns it towards the board (re-centre), and
    ///   - raises or lowers the table so it sits a little below the player's eyes.
    /// This replaces locomotion: the player never has to walk.
    ///
    /// Why this checks continuously (v0.3 fix): the headset can start tracking a moment AFTER the
    /// game starts (Quest Link in the Editor does this). The old version decided once, at start, and then
    /// left the player wherever the headset's own tracking origin was, which can be outside the classroom,
    /// looking at nothing but the sky colour. Now the player is re-centred as soon as tracking starts,
    /// after the Quest's own recentre, and whenever the view ends up far away from the board.
    /// </summary>
    public class PlayAreaFitter : MonoBehaviour
    {
        /// <summary>If the head is further than this from the player spot (metres), re-centre automatically.</summary>
        private const float MaxDistanceFromSpot = 1.2f;

        private const float CheckInterval = 0.25f;

        private XROrigin origin;
        private Camera head;
        private Transform playArea;
        private InputAction recenterAction;

        private bool fittedForDesktop;
        private bool fittedForVR;
        private bool recenterRequested;
        private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();

        /// <summary>Called once by GameBootstrap right after this component is created.</summary>
        public void Setup(XROrigin xrOrigin, Camera headCamera, Transform playAreaRoot, InputAction recenter)
        {
            origin = xrOrigin;
            head = headCamera;
            playArea = playAreaRoot;
            recenterAction = recenter;
            if (recenterAction != null) recenterAction.performed += OnRecenterPressed;
        }

        private void OnDestroy()
        {
            if (recenterAction != null) recenterAction.performed -= OnRecenterPressed;
            foreach (XRInputSubsystem subsystem in inputSubsystems)
            {
                if (subsystem != null) subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            }
            inputSubsystems.Clear();
        }

        private IEnumerator Start()
        {
            var wait = new WaitForSecondsRealtime(CheckInterval);
            while (true)
            {
                if (XRSettings.isDeviceActive)
                {
                    if (!fittedForVR)
                    {
                        yield return WaitForHeadTracking();
                        SubscribeToTrackingOriginChanges();
                        RecenterFull();
                        fittedForVR = true;
                        Debug.Log("[RSPP] Headset tracking started: player re-centred in front of the board.");
                    }
                    else if (recenterRequested || HeadIsLost())
                    {
                        recenterRequested = false;
                        RecenterFull();
                    }
                }
                else
                {
                    fittedForVR = false;
                    if (!fittedForDesktop && !Application.isMobilePlatform)
                    {
                        RecenterFull();
                        fittedForDesktop = true;
                    }
                }
                yield return wait;
            }
        }

        /// <summary>Waits until the headset pose has really arrived (at most 5 seconds), plus a short settle time.</summary>
        private IEnumerator WaitForHeadTracking()
        {
            Vector3 startPosition = head.transform.localPosition;
            float waited = 0f;
            while (waited < 5f && head.transform.localPosition == startPosition)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        /// <summary>True when the view is broken (NaN) or far away from the player spot in front of the board.</summary>
        private bool HeadIsLost()
        {
            Vector3 p = head.transform.position;
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) return true;
            var horizontal = new Vector2(p.x, p.z);
            return horizontal.magnitude > MaxDistanceFromSpot;
        }

        private void SubscribeToTrackingOriginChanges()
        {
            if (inputSubsystems.Count > 0) return;
            SubsystemManager.GetSubsystems(inputSubsystems);
            foreach (XRInputSubsystem subsystem in inputSubsystems)
            {
                if (subsystem != null) subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
            }
        }

        /// <summary>The Quest's own recentre (holding the Meta button) moves the tracking origin: follow it.</summary>
        private void OnTrackingOriginUpdated(XRInputSubsystem subsystem)
        {
            recenterRequested = true;
        }

        private void OnRecenterPressed(InputAction.CallbackContext context)
        {
            RecenterFull();
        }

        /// <summary>Turns and moves the player to face the board, then fits the table height.</summary>
        public void RecenterFull()
        {
            if (origin == null || head == null) return;
            ResetIfBroken();
            origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            FitToPlayer();
        }

        /// <summary>
        /// Moves the player in front of the board and fits the table height, without turning the view.
        /// Used by Start Game and Restart Game (the player is looking at the menu at that moment).
        /// </summary>
        public void FitToPlayer()
        {
            if (origin == null || head == null || playArea == null) return;
            ResetIfBroken();

            float eyeHeight = head.transform.position.y;
            if (float.IsNaN(eyeHeight) || eyeHeight < 0.3f || eyeHeight > 2.5f) eyeHeight = GameConfig.DesktopEyeHeight;

            origin.MoveCameraToWorldLocation(new Vector3(0f, head.transform.position.y, 0f));

            float tableHeight = Mathf.Clamp(eyeHeight - GameConfig.EyeHeightAboveTable,
                GameConfig.MinTableHeight, GameConfig.MaxTableHeight);
            playArea.position = new Vector3(0f, tableHeight, 0f);
            if (EntranceAnchor != null) EntranceAnchor.position = playArea.position;
        }

        /// <summary>The entrance screen's panel anchor; kept at the same height as the table.</summary>
        public Transform EntranceAnchor;

        /// <summary>If the rig itself has an invalid position or rotation, put it back to zero.</summary>
        private void ResetIfBroken()
        {
            Transform rig = origin.transform;
            Vector3 p = rig.position;
            Quaternion r = rig.rotation;
            bool broken = float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                          float.IsNaN(r.x) || float.IsNaN(r.y) || float.IsNaN(r.z) || float.IsNaN(r.w);
            if (broken)
            {
                rig.position = Vector3.zero;
                rig.rotation = Quaternion.identity;
            }
        }
    }
}
