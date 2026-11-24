using System;
using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// The entrance screen: the player stands in a school hallway in front of the classroom door.
    /// A welcome panel offers the language (English, Español, Türkçe, Français), Instructions,
    /// and "Enter the classroom". Entering fades the screen to black and shows the classroom,
    /// with the player already seated at the table (no walking).
    ///
    /// The hallway is built around the player's spot and shown INSTEAD of the classroom, so the
    /// player never has to move. Like everything else, it is made of simple shapes.
    /// </summary>
    public class EntranceScreen : MonoBehaviour
    {
        /// <summary>Raised when the player presses "Enter the classroom".</summary>
        public event Action EnterPressed;

        /// <summary>Parent of the welcome panel; the PlayAreaFitter moves it up or down like the table.</summary>
        public Transform Anchor { get; private set; }

        public bool IsVisible
        {
            get { return gameObject.activeSelf; }
        }

        private const float Width = 900f;
        private const float Height = 780f;

        private GameObject mainPage;
        private GameObject instructionsPage;
        private Text statusText;
        private readonly Button[] languageButtons = new Button[4];

        public static EntranceScreen Create(Camera eventCamera)
        {
            var go = new GameObject("Entrance (School Hallway)");
            var entrance = go.AddComponent<EntranceScreen>();
            entrance.BuildHallway(go.transform);

            // The panel hangs from an anchor that the PlayAreaFitter keeps at table height, like the classroom.
            entrance.Anchor = new GameObject("Entrance Anchor").transform;
            entrance.Anchor.SetParent(go.transform, false);
            entrance.Anchor.position = new Vector3(0f, GameConfig.DesktopEyeHeight - GameConfig.EyeHeightAboveTable, 0f);
            entrance.BuildPanel(entrance.Anchor, eventCamera);
            return entrance;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (visible) ShowInstructions(false);
        }

        /// <summary>Shows a small VR status line (headset and controllers) at the bottom of the panel.</summary>
        public void SetStatus(string status)
        {
            if (statusText != null) statusText.text = status;
        }

        // ------------------------------------------------------------------ Hallway

        private void BuildHallway(Transform root)
        {
            // A corridor 4 m wide around the player; the classroom door is 2.6 m in front.
            const float halfWidth = 2f, front = 2.6f, back = -2.5f, height = 3f, wall = 0.1f;
            float depth = front - back, centerZ = (front + back) * 0.5f;

            var floor = new Color(0.72f, 0.76f, 0.80f);
            var walls = new Color(0.97f, 0.91f, 0.76f);
            var ceiling = new Color(0.97f, 0.97f, 0.97f);

            Shapes.Box("Hall Floor", root, new Vector3(0f, -wall * 0.5f, centerZ), new Vector3(halfWidth * 2f, wall, depth), floor);
            Shapes.Box("Hall Ceiling", root, new Vector3(0f, height + wall * 0.5f, centerZ), new Vector3(halfWidth * 2f, wall, depth), ceiling);
            Shapes.Box("Hall Left Wall", root, new Vector3(-halfWidth, height * 0.5f, centerZ), new Vector3(wall, height, depth), walls);
            Shapes.Box("Hall Right Wall", root, new Vector3(halfWidth, height * 0.5f, centerZ), new Vector3(wall, height, depth), walls);
            Shapes.Box("Hall Back Wall", root, new Vector3(0f, height * 0.5f, back), new Vector3(halfWidth * 2f, height, wall), walls);
            Shapes.Box("Hall Front Wall", root, new Vector3(0f, height * 0.5f, front), new Vector3(halfWidth * 2f, height, wall), walls);
            Shapes.Box("Hall Stripe", root, new Vector3(0f, 0.9f, front - 0.06f), new Vector3(halfWidth * 2f, 0.12f, 0.02f), new Color(0.25f, 0.56f, 0.90f));

            // Classroom door with a little window and a sign.
            float doorZ = front - wall * 0.5f;
            Shapes.Box("Door Frame", root, new Vector3(0f, 1.1f, doorZ - 0.03f), new Vector3(1.25f, 2.25f, 0.05f), new Color(0.50f, 0.34f, 0.20f));
            Shapes.Box("Door", root, new Vector3(0f, 1.05f, doorZ - 0.06f), new Vector3(1.05f, 2.1f, 0.04f), new Color(0.66f, 0.46f, 0.29f));
            Shapes.Box("Door Window", root, new Vector3(0f, 1.6f, doorZ - 0.085f), new Vector3(0.45f, 0.35f, 0.01f), new Color(0.62f, 0.84f, 0.98f));
            Shapes.Sphere("Door Knob", root, new Vector3(0.40f, 1.0f, doorZ - 0.10f), 0.07f, new Color(1f, 0.80f, 0.20f));
            Shapes.Box("Door Sign", root, new Vector3(0f, 2.4f, doorZ - 0.06f), new Vector3(0.9f, 0.22f, 0.02f), Color.white);
            Canvas sign = UIFactory.WorldCanvas("Door Sign Text", root, new Vector3(0f, 2.4f, doorZ - 0.075f), Quaternion.identity, 900f, 220f, null, false);
            UIFactory.Localize(UIFactory.Label("Sign", sign.transform, "", 0f, 0f, 900f, 220f, 120, GameConfig.PanelHeaderColor), "class_sign");

            // Lockers along both side walls.
            Color[] lockerColors = { new Color(0.25f, 0.56f, 0.90f), new Color(0.93f, 0.33f, 0.31f) };
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (halfWidth - 0.25f);
                for (int i = 0; i < 6; i++)
                {
                    float z = -1.6f + i * 0.62f;
                    Color c = lockerColors[(i + (side > 0 ? 1 : 0)) % 2];
                    Shapes.Box("Locker", root, new Vector3(x, 0.95f, z), new Vector3(0.4f, 1.9f, 0.58f), c);
                    Shapes.Box("Locker Vent", root, new Vector3(x - side * 0.205f, 1.6f, z), new Vector3(0.01f, 0.12f, 0.3f), new Color(0.15f, 0.15f, 0.2f));
                    Shapes.Box("Locker Handle", root, new Vector3(x - side * 0.21f, 1.0f, z + 0.18f), new Vector3(0.02f, 0.12f, 0.03f), new Color(0.85f, 0.85f, 0.88f));
                }
            }

            // A colourful welcome banner above the lockers on each side.
            Shapes.Box("Banner Left", root, new Vector3(-halfWidth + 0.06f, 2.3f, 0.2f), new Vector3(0.02f, 0.35f, 2.4f), new Color(1f, 0.80f, 0.20f));
            Shapes.Box("Banner Right", root, new Vector3(halfWidth - 0.06f, 2.3f, 0.2f), new Vector3(0.02f, 0.35f, 2.4f), new Color(0.36f, 0.75f, 0.38f));
        }

        // ------------------------------------------------------------------ Welcome panel

        private void BuildPanel(Transform anchor, Camera eventCamera)
        {
            // Local to the anchor (table height): centre 0.5 m above the "table" level, 1.2 m in front.
            Canvas canvas = UIFactory.WorldCanvas("Welcome Panel", anchor, new Vector3(0f, 0.45f, 1.2f), Quaternion.identity,
                Width, Height, eventCamera, true);
            Transform root = canvas.transform;

            UIFactory.Panel("Background", root, 0f, 0f, Width, Height, GameConfig.PanelColor);
            UIFactory.Panel("Header", root, 0f, 0f, Width, 110f, GameConfig.PanelHeaderColor);
            UIFactory.Label("Title", root, "Roll, Spin, Problem, Play", 20f, 10f, Width - 40f, 90f, 62, GameConfig.TextLight);

            // ---- Main page: language, Enter, Instructions
            RectTransform mainRect = UIFactory.Group("Main Page", root, Width, Height);
            mainPage = mainRect.gameObject;

            UIFactory.Localize(UIFactory.Label("Choose Language", mainRect, "", 40f, 125f, Width - 80f, 60f, 42, GameConfig.TextDark), "choose_lang");
            for (int i = 0; i < languageButtons.Length; i++)
            {
                var language = (Language)i;
                float bx = 60f + (i % 2) * 400f;
                float by = 195f + (i / 2) * 100f;
                languageButtons[i] = UIFactory.MakeButton(language.ToString(), mainRect, Loc.NativeName(language), bx, by, 380f, 84f,
                    GameConfig.ButtonBlue, GameConfig.TextLight, 44, () => ChooseLanguage(language));
            }

            Button enter = UIFactory.MakeButton("Enter", mainRect, "", 150f, 410f, Width - 300f, 120f, GameConfig.ButtonGreen, GameConfig.TextLight, 56, () =>
            {
                if (EnterPressed != null) EnterPressed();
            });
            UIFactory.LocalizeButton(enter, "enter");

            Button instructions = UIFactory.MakeButton("Instructions", mainRect, "", 250f, 550f, Width - 500f, 84f, GameConfig.ButtonOrange, GameConfig.TextLight, 42,
                () => ShowInstructions(true));
            UIFactory.LocalizeButton(instructions, "instructions");

            statusText = UIFactory.Label("Status", mainRect, "", 30f, 650f, Width - 60f, 110f, 26, GameConfig.ButtonGrey);

            // ---- Instructions page
            RectTransform instructionsRect = UIFactory.Group("Instructions Page", root, Width, Height);
            instructionsPage = instructionsRect.gameObject;
            Text text = UIFactory.Label("Instructions Text", instructionsRect, "", 40f, 125f, Width - 80f, 520f, 38,
                GameConfig.TextDark, TextAnchor.UpperLeft, false);
            UIFactory.Localize(text, "instructions_text", GameConfig.HelperName);
            Button back = UIFactory.MakeButton("Back", instructionsRect, "", (Width - 260f) * 0.5f, 670f, 260f, 84f,
                GameConfig.ButtonBlue, GameConfig.TextLight, 44, () => ShowInstructions(false));
            UIFactory.LocalizeButton(back, "back");

            ShowInstructions(false);
            HighlightLanguage();
        }

        private void ChooseLanguage(Language language)
        {
            Loc.Set(language);
            HighlightLanguage();
        }

        /// <summary>The chosen language's button is dark blue; the others are lighter.</summary>
        private void HighlightLanguage()
        {
            for (int i = 0; i < languageButtons.Length; i++)
            {
                bool selected = (int)Loc.Current == i;
                UIFactory.SetButtonColor(languageButtons[i], selected ? GameConfig.PanelHeaderColor : GameConfig.ButtonBlue);
                UIFactory.SetButtonLabel(languageButtons[i], (selected ? "> " : "") + Loc.NativeName((Language)i));
            }
        }

        private void ShowInstructions(bool show)
        {
            if (mainPage == null) return;
            mainPage.SetActive(!show);
            instructionsPage.SetActive(show);
        }
    }
}
