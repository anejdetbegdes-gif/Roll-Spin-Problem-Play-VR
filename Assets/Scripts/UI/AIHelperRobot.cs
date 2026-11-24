using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RSPP
{
    /// <summary>
    /// "Robo", the friendly robot at the front-left corner of the table, and its speech bubble.
    ///
    /// This is a SIMULATED AI helper for teaching AI literacy. It only displays text that
    /// AIHelperLogic prepared with basic C# rules (sometimes right, sometimes wrong on purpose).
    /// There is no real AI model, no network connection, and no voice.
    ///
    /// When Robo has an idea, the student TOUCHES Robo with the LEFT hand (just reach into it) to hear
    /// the suggestion, then judges it with the Correct / Incorrect buttons (right-hand ray).
    /// </summary>
    public class AIHelperRobot : MonoBehaviour
    {
        /// <summary>Raised when the player judges a suggestion. True = the player pressed "Correct".</summary>
        public event Action<bool> Judged;

        /// <summary>Raised when the player touches Robo with the left hand (or presses T in desktop mode).</summary>
        public event Action Touched;

        private const float BubbleWidth = 540f;
        private const float BubbleHeight = 400f;

        private static readonly Color LightIdle = new Color(0.20f, 0.85f, 0.95f);
        private static readonly Color LightAsking = new Color(1.00f, 0.80f, 0.20f);

        private Text bubbleText;
        private Text askText;
        private Button correctButton;
        private Button incorrectButton;
        private GameObject antennaLight;
        private Transform upperBody;
        private Transform rightArm;

        private bool waitingForTouch;
        private float bounceTime = -10f;
        private float lastTouchTime = -10f;

        // ------------------------------------------------------------------ Construction

        public static AIHelperRobot Create(Transform playArea, Camera eventCamera)
        {
            Vector3 position = GameConfig.RobotPosition;

            var go = new GameObject("AI Helper Robot (" + GameConfig.HelperName + ")");
            go.transform.SetParent(playArea, false);
            go.transform.localPosition = position;
            // The robot's face (+Z side) looks at the player.
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(-position.x, 0f, -position.z).normalized, Vector3.up);

            var robot = go.AddComponent<AIHelperRobot>();
            robot.BuildBody(go.transform);
            robot.BuildBubble(playArea, eventCamera);
            robot.MakeTouchable(go);
            robot.ShowMessage("");
            return robot;
        }

        private void BuildBody(Transform root)
        {
            Color white = new Color(0.96f, 0.97f, 1.00f);
            Color blue = new Color(0.25f, 0.56f, 0.90f);
            Color dark = new Color(0.10f, 0.12f, 0.25f);

            Shapes.Cylinder("Base", root, new Vector3(0f, 0.01f, 0f), 0.15f, 0.02f, blue);

            // Everything above the base bobs gently (see Update).
            upperBody = new GameObject("Upper Body").transform;
            upperBody.SetParent(root, false);

            Shapes.Box("Body", upperBody, new Vector3(0f, 0.10f, 0f), new Vector3(0.13f, 0.14f, 0.10f), white);
            Shapes.Sphere("Chest Light", upperBody, new Vector3(0f, 0.11f, 0.05f), 0.035f, LightIdle);
            Shapes.Box("Left Arm", upperBody, new Vector3(-0.085f, 0.10f, 0f), new Vector3(0.03f, 0.10f, 0.03f), blue);

            // The right arm hangs from a shoulder pivot so it can wave.
            rightArm = new GameObject("Right Shoulder").transform;
            rightArm.SetParent(upperBody, false);
            rightArm.localPosition = new Vector3(0.085f, 0.15f, 0f);
            Shapes.Box("Right Arm", rightArm, new Vector3(0f, -0.05f, 0f), new Vector3(0.03f, 0.10f, 0.03f), blue);

            Shapes.Box("Head", upperBody, new Vector3(0f, 0.245f, 0f), new Vector3(0.18f, 0.13f, 0.12f), white);
            Shapes.Box("Face", upperBody, new Vector3(0f, 0.245f, 0.058f), new Vector3(0.14f, 0.08f, 0.01f), dark);
            Shapes.Sphere("Left Eye", upperBody, new Vector3(-0.034f, 0.252f, 0.062f), 0.03f, LightIdle);
            Shapes.Sphere("Right Eye", upperBody, new Vector3(0.034f, 0.252f, 0.062f), 0.03f, LightIdle);
            Shapes.Box("Smile", upperBody, new Vector3(0f, 0.222f, 0.064f), new Vector3(0.04f, 0.008f, 0.004f), LightIdle);

            GameObject leftEar = Shapes.Cylinder("Left Ear", upperBody, new Vector3(-0.098f, 0.245f, 0f), 0.05f, 0.02f, blue);
            leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject rightEar = Shapes.Cylinder("Right Ear", upperBody, new Vector3(0.098f, 0.245f, 0f), 0.05f, 0.02f, blue);
            rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            Shapes.Cylinder("Antenna", upperBody, new Vector3(0f, 0.335f, 0f), 0.008f, 0.05f, dark);
            antennaLight = Shapes.Sphere("Antenna Light", upperBody, new Vector3(0f, 0.372f, 0f), 0.03f, LightIdle);
        }

        private void BuildBubble(Transform playArea, Camera eventCamera)
        {
            Vector3 position = GameConfig.HelperBubblePosition;
            Canvas canvas = UIFactory.WorldCanvas("AI Helper Bubble", playArea, position, UIFactory.FacePlayer(position),
                BubbleWidth, BubbleHeight, eventCamera, true);
            Transform root = canvas.transform;

            UIFactory.Panel("Background", root, 0f, 0f, BubbleWidth, BubbleHeight, GameConfig.PanelColor);
            UIFactory.Panel("Header", root, 0f, 0f, BubbleWidth, 66f, GameConfig.PanelHeaderColor);
            UIFactory.Localize(UIFactory.Label("Name", root, "", 20f, 6f, BubbleWidth - 40f, 54f, 38, GameConfig.TextLight),
                "robo_name", GameConfig.HelperName);

            bubbleText = UIFactory.Label("Bubble Text", root, "", 24f, 76f, BubbleWidth - 48f, 150f, 48, GameConfig.TextDark);
            askText = UIFactory.Label("Ask Text", root, "", 24f, 228f, BubbleWidth - 48f, 50f, 36, GameConfig.PanelHeaderColor);
            UIFactory.Localize(askText, "robo_ask", GameConfig.HelperName);

            float buttonWidth = (BubbleWidth - 72f) * 0.5f;
            correctButton = UIFactory.MakeButton("Correct Button", root, "", 24f, 286f, buttonWidth, 98f,
                GameConfig.ButtonGreen, GameConfig.TextLight, 42, () => OnJudged(true));
            incorrectButton = UIFactory.MakeButton("Incorrect Button", root, "", 48f + buttonWidth, 286f, buttonWidth, 98f,
                GameConfig.ButtonRed, GameConfig.TextLight, 42, () => OnJudged(false));
            UIFactory.LocalizeButton(correctButton, "correct_btn");
            UIFactory.LocalizeButton(incorrectButton, "incorrect_btn");
        }

        /// <summary>Lets the LEFT hand (Direct Interactor) touch Robo. The right ray is UI-only, so it cannot.</summary>
        private void MakeTouchable(GameObject go)
        {
            var zone = go.AddComponent<BoxCollider>();
            zone.center = new Vector3(0f, 0.19f, 0f);
            zone.size = new Vector3(0.22f, 0.40f, 0.16f);

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var interactable = go.AddComponent<XRSimpleInteractable>();
            interactable.hoverEntered.AddListener(args => OnTouched());     // reaching into Robo is enough
            interactable.selectEntered.AddListener(args => OnTouched());    // gripping Robo also works
        }

        // ------------------------------------------------------------------ Views

        /// <summary>Shows plain text in the bubble (no buttons).</summary>
        public void ShowMessage(string message)
        {
            waitingForTouch = false;
            bubbleText.text = message;
            SetJudgeControlsVisible(false);
            Shapes.SetColor(antennaLight, LightIdle);
        }

        /// <summary>Robo has an idea and waits to be touched: blinking antenna and a wave.</summary>
        public void ShowWaitingForTouch()
        {
            waitingForTouch = true;
            bubbleText.text = Loc.T("robo_touch");
            SetJudgeControlsVisible(false);
        }

        /// <summary>Shows an answer suggestion with the "Correct" / "Incorrect" buttons.</summary>
        public void ShowSuggestion(string suggestion)
        {
            waitingForTouch = false;
            bubbleText.text = suggestion;
            SetJudgeControlsVisible(true);
            Shapes.SetColor(antennaLight, LightAsking);
        }

        /// <summary>Desktop test mode: acts as if the left hand touched Robo.</summary>
        public void TouchFromDesktop()
        {
            OnTouched();
        }

        private void SetJudgeControlsVisible(bool visible)
        {
            askText.gameObject.SetActive(visible);
            correctButton.gameObject.SetActive(visible);
            incorrectButton.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------ Touch and animation

        private void OnTouched()
        {
            // Hover and grip can both fire for one touch: ignore repeats within half a second.
            if (Time.time - lastTouchTime < 0.5f) return;
            lastTouchTime = Time.time;

            bounceTime = Time.time;
            Haptics.PulseLeft(0.4f, 0.08f);
            if (Touched != null) Touched();
        }

        private void Update()
        {
            if (upperBody == null) return;

            // Gentle idle bob, plus a small hop right after a touch.
            float bob = Mathf.Sin(Time.time * 2f) * 0.006f;
            float sinceBounce = Time.time - bounceTime;
            if (sinceBounce < 0.35f) bob += Mathf.Sin(sinceBounce / 0.35f * Mathf.PI) * 0.03f;
            upperBody.localPosition = new Vector3(0f, bob, 0f);

            if (waitingForTouch)
            {
                // Wave the right arm and blink the antenna to say "touch me".
                rightArm.localRotation = Quaternion.Euler(0f, 0f, 150f + Mathf.Sin(Time.time * 8f) * 25f);
                bool on = Mathf.Repeat(Time.time, 0.6f) < 0.3f;
                Shapes.SetColor(antennaLight, on ? LightAsking : Color.white);
            }
            else
            {
                rightArm.localRotation = Quaternion.identity;
            }
        }

        private void OnJudged(bool saidCorrect)
        {
            if (Judged != null) Judged(saidCorrect);
        }
    }
}
