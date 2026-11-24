using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// The large panel behind the board. It shows:
    ///   - the star counter and the current level,
    ///   - short instructions for the current step,
    ///   - the math question with 2-4 large answer buttons,
    ///   - immediate correct / incorrect feedback with a Next button.
    /// </summary>
    public class QuestionPanel : MonoBehaviour
    {
        /// <summary>Raised when the player selects an answer button. The argument is the choice index.</summary>
        public event Action<int> AnswerSelected;

        /// <summary>Raised when the player presses the Next / Go / Play Again button.</summary>
        public event Action NextPressed;

        /// <summary>Raised when the player presses the "Listen" button (reads the question aloud again).</summary>
        public event Action ListenPressed;

        private Button listenButton;

        private const float Width = 920f;
        private const float Height = 700f;

        // Picture explanation: two ten frames (2 rows x 5 cells each) with counters.
        private const float CellStep = 50f;
        private const float FrameTop = 486f;
        private static readonly float[] FrameLeft = { 40f, 314f };
        private static readonly Color CounterRed = new Color(0.90f, 0.22f, 0.20f);
        private static readonly Color CounterBlue = new Color(0.16f, 0.42f, 0.85f);
        private static readonly Color CounterGreen = new Color(0.13f, 0.60f, 0.25f);
        private static readonly Color CounterOrange = new Color(0.95f, 0.55f, 0.05f);
        private static readonly Color CounterTakenAway = new Color(0.60f, 0.62f, 0.68f, 0.35f);
        private const int MaxAnswers = 4;

        private static readonly Color GoodColor = new Color(0.05f, 0.45f, 0.15f);
        private static readonly Color BadColor = new Color(0.72f, 0.12f, 0.10f);

        private Text titleText;
        private Text levelText;
        private Text starsText;
        private Text promptText;
        private Text messageText;
        private Text feedbackText;
        private Button nextButton;
        private readonly Button[] answerButtons = new Button[MaxAnswers];
        private int visibleAnswers;

        private GameObject visualGroup;
        private readonly GameObject[] frames = new GameObject[2];
        private readonly Image[] counters = new Image[20];
        private Text visualText;

        // ------------------------------------------------------------------ Construction

        public static QuestionPanel Create(Transform playArea, Camera eventCamera)
        {
            Vector3 position = GameConfig.QuestionPanelPosition;
            Canvas canvas = UIFactory.WorldCanvas("Question Panel", playArea, position, UIFactory.FacePlayer(position),
                Width, Height, eventCamera, true);

            var panel = canvas.gameObject.AddComponent<QuestionPanel>();
            panel.Build(canvas.transform);
            return panel;
        }

        private void Build(Transform root)
        {
            UIFactory.Panel("Background", root, 0f, 0f, Width, Height, GameConfig.PanelColor);
            UIFactory.Panel("Header", root, 0f, 0f, Width, 84f, GameConfig.PanelHeaderColor);

            levelText = UIFactory.Label("Level", root, "Level 1", 28f, 10f, 210f, 64f, 40, GameConfig.TextLight, TextAnchor.MiddleLeft);
            titleText = UIFactory.Label("Title", root, "", 240f, 10f, 440f, 64f, 44, GameConfig.TextLight);
            starsText = UIFactory.Label("Stars", root, "Stars: 0", 682f, 10f, 210f, 64f, 40, GameConfig.StarColor, TextAnchor.MiddleRight);

            // Centre pivots so these two labels grow from their middle when they pulse.
            CenterPivot(levelText.rectTransform);
            CenterPivot(starsText.rectTransform);

            promptText = UIFactory.Label("Question", root, "", 30f, 98f, Width - 60f, 150f, 110, GameConfig.TextDark);
            messageText = UIFactory.Label("Message", root, "", 50f, 104f, Width - 100f, 350f, 58, GameConfig.TextDark);

            for (int i = 0; i < MaxAnswers; i++)
            {
                int index = i;      // captured by the click handler below
                answerButtons[i] = UIFactory.MakeButton("Answer " + i, root, "", 0f, 258f, 190f, 120f,
                    GameConfig.ButtonBlue, GameConfig.TextLight, 72, () => OnAnswerClicked(index));
            }

            feedbackText = UIFactory.Label("Feedback", root, "", 30f, 388f, Width - 60f, 92f, 40, GameConfig.TextDark);

            BuildVisual(root);

            // Read-aloud button (only shown while a question is on screen and the language is English).
            listenButton = UIFactory.MakeButton("Listen Button", root, "", Width - 190f, 98f, 170f, 60f,
                GameConfig.ButtonGrey, GameConfig.TextLight, 34, () => { if (ListenPressed != null) ListenPressed(); });
            UIFactory.LocalizeButton(listenButton, "listen");

            nextButton = UIFactory.MakeButton("Next Button", root, "Next", (Width - 260f) * 0.5f, 624f, 260f, 64f,
                GameConfig.ButtonOrange, GameConfig.TextLight, 40, OnNextClicked);

            ShowMessage("", "", null);
        }

        // ------------------------------------------------------------------ Header

        public void SetStars(int stars)
        {
            starsText.text = Loc.F("stars", stars);
        }

        public void SetLevel(int level)
        {
            levelText.text = Loc.F("level", level);
        }

        /// <summary>The star counter grows for a moment when a star is earned.</summary>
        public void PulseStars()
        {
            StartCoroutine(Pulse(starsText, GameConfig.StarColor));
        }

        /// <summary>Shows the new level and makes the level label flash so the change is easy to notice.</summary>
        public void ShowLevelChange(int level)
        {
            SetLevel(level);
            StartCoroutine(Pulse(levelText, GameConfig.TextLight));
        }

        private IEnumerator Pulse(Text label, Color normalColor)
        {
            const float duration = 0.6f;
            float t = 0f;
            label.color = GameConfig.StarColor;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / duration);
                label.rectTransform.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            label.rectTransform.localScale = Vector3.one;
            label.color = normalColor;
        }

        private static void CenterPivot(RectTransform rect)
        {
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition += new Vector2(rect.sizeDelta.x * 0.5f, -rect.sizeDelta.y * 0.5f);
        }

        // ------------------------------------------------------------------ Views

        /// <summary>Shows a short instruction or result. Pass a button label to show the Next button, or null to hide it.</summary>
        public void ShowMessage(string title, string message, string buttonLabel)
        {
            titleText.text = title;
            messageText.text = message;

            visualGroup.SetActive(false);
            listenButton.gameObject.SetActive(false);
            messageText.gameObject.SetActive(true);
            promptText.gameObject.SetActive(false);
            feedbackText.gameObject.SetActive(false);
            SetAnswerButtonsVisible(0);
            SetNextButton(buttonLabel);
        }

        /// <summary>
        /// Shows a math question. With <paramref name="showAnswerButtons"/> = false only the question is shown
        /// (used when the player has to judge the AI Helper's suggestion instead).
        /// </summary>
        public void ShowQuestion(string title, MathQuestion question, bool showAnswerButtons, string hint)
        {
            titleText.text = title;
            promptText.text = question.Prompt;

            visualGroup.SetActive(false);
            listenButton.gameObject.SetActive(Loc.Current == Language.English);    // narration is English only
            messageText.gameObject.SetActive(false);
            promptText.gameObject.SetActive(true);
            SetNextButton(null);

            feedbackText.color = GameConfig.TextDark;
            feedbackText.text = hint ?? "";
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(hint));

            int count = showAnswerButtons ? Mathf.Min(question.Choices.Length, MaxAnswers) : 0;
            SetAnswerButtonsVisible(count);
            for (int i = 0; i < count; i++)
            {
                UIFactory.SetButtonLabel(answerButtons[i], question.Choices[i]);
                UIFactory.SetButtonColor(answerButtons[i], GameConfig.ButtonBlue);
                answerButtons[i].interactable = true;
            }
        }

        /// <summary>Replaces the small instruction line under the question.</summary>
        public void SetHint(string hint)
        {
            feedbackText.color = GameConfig.TextDark;
            feedbackText.text = hint ?? "";
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        }

        /// <summary>
        /// Shows immediate feedback under the current question. The correct answer button turns green,
        /// a wrong selection turns red. Pass -1 for the indices when no answer buttons are shown.
        /// </summary>
        public void ShowFeedback(bool positive, string text, int selectedIndex, int correctIndex, string buttonLabel)
        {
            for (int i = 0; i < visibleAnswers; i++)
            {
                answerButtons[i].interactable = false;
                Color color = GameConfig.ButtonGrey;
                if (i == correctIndex) color = GameConfig.ButtonGreen;
                else if (i == selectedIndex) color = GameConfig.ButtonRed;
                UIFactory.SetButtonColor(answerButtons[i], color);
            }

            feedbackText.color = positive ? GoodColor : BadColor;
            feedbackText.text = text;
            feedbackText.gameObject.SetActive(true);
            SetNextButton(buttonLabel);
        }

        // ------------------------------------------------------------------ Picture explanation (ten frames)

        private void BuildVisual(Transform root)
        {
            visualGroup = UIFactory.Group("Picture Explanation", root, Width, Height).gameObject;
            Transform group = visualGroup.transform;

            for (int f = 0; f < 2; f++)
            {
                frames[f] = UIFactory.Panel("Ten Frame " + (f + 1), group, FrameLeft[f] - 6f, FrameTop - 6f,
                    5 * CellStep + 8f, 2 * CellStep + 8f, GameConfig.PanelHeaderColor).gameObject;
                for (int cell = 0; cell < 10; cell++)
                {
                    float x = (cell % 5) * CellStep + 2f;
                    float y = (cell / 5) * CellStep + 2f;
                    UIFactory.Panel("Cell", frames[f].transform, x + 4f, y + 4f, CellStep - 4f, CellStep - 4f, Color.white);
                }
            }

            for (int i = 0; i < counters.Length; i++)
            {
                Image counter = UIFactory.Panel("Counter " + i, group, 0f, 0f, 36f, 36f, CounterRed);
                counter.sprite = UIFactory.CircleSprite;
                counter.type = Image.Type.Simple;
                counters[i] = counter;
            }

            visualText = UIFactory.Label("Picture Text", group, "", 590f, FrameTop - 6f, Width - 610f, 2 * CellStep + 8f, 44,
                GameConfig.TextDark);
            visualGroup.SetActive(false);
        }

        /// <summary>
        /// Shows the question as a picture with counters in ten frames:
        ///   addition: red counters for the first number, blue for the second;
        ///   subtraction: the taken-away counters are greyed out;
        ///   odd/even: counters in pairs, a lone counter (orange) means odd.
        /// </summary>
        public void ShowVisual(MathQuestion q)
        {
            if (q == null) return;

            if (q.Level >= 5)
            {
                ShowBigNumberSteps(q);
                return;
            }

            for (int f = 0; f < frames.Length; f++) frames[f].SetActive(true);
            PlaceVisualText(590f, Width - 610f, 44);

            bool pairs = q.Category == QuestionCategory.OddEven;
            int total = q.Category == QuestionCategory.Addition ? q.A + q.B : q.A;
            total = Mathf.Clamp(total, 0, counters.Length);

            for (int i = 0; i < counters.Length; i++)
            {
                bool visible = i < total;
                counters[i].gameObject.SetActive(visible);
                if (!visible) continue;

                // Ten frames fill row by row; for odd/even they fill in pairs (top and bottom).
                int frame = i / 10;
                int j = i % 10;
                int col = pairs ? j / 2 : j % 5;
                int row = pairs ? j % 2 : j / 5;
                var rect = (RectTransform)counters[i].transform;
                rect.anchoredPosition = new Vector2(FrameLeft[frame] + col * CellStep + 7f, -(FrameTop + row * CellStep + 7f));

                Color color;
                if (q.Category == QuestionCategory.Addition) color = i < q.A ? CounterRed : CounterBlue;
                else if (q.Category == QuestionCategory.Subtraction) color = i < q.A - q.B ? CounterBlue : CounterTakenAway;
                else color = (q.A % 2 == 1 && i == q.A - 1) ? CounterOrange : CounterGreen;
                counters[i].color = color;
            }

            frames[1].SetActive(total > 10);

            if (q.Category == QuestionCategory.Addition) visualText.text = q.A + " + " + q.B + " = " + q.Answer;
            else if (q.Category == QuestionCategory.Subtraction) visualText.text = q.A + " - " + q.B + " = " + q.Answer;
            else visualText.text = Loc.F(q.A % 2 == 0 ? "visual_even" : "visual_odd", q.A);

            visualGroup.SetActive(true);
        }

        /// <summary>
        /// Level 5 numbers are too big for ten frames, so the answer is shown in small steps instead:
        ///   47 + 28:  40 + 20 = 60,  7 + 8 = 15,  60 + 15 = 75
        ///   83 - 36:  83 - 30 = 53,  53 - 6 = 47
        ///   6 × 7 = 42, 42 is even
        /// The steps use only numbers and symbols, so they need no translation.
        /// </summary>
        private void ShowBigNumberSteps(MathQuestion q)
        {
            for (int f = 0; f < frames.Length; f++) frames[f].SetActive(false);
            for (int i = 0; i < counters.Length; i++) counters[i].gameObject.SetActive(false);
            PlaceVisualText(30f, Width - 60f, 40);

            string steps;
            if (q.Category == QuestionCategory.Addition)
            {
                int tens = q.A / 10 * 10 + q.B / 10 * 10;
                int ones = q.A % 10 + q.B % 10;
                steps = q.A + " + " + q.B + " = " + q.Answer + "\n"
                    + (q.A / 10 * 10) + " + " + (q.B / 10 * 10) + " = " + tens + ",   "
                    + (q.A % 10) + " + " + (q.B % 10) + " = " + ones + ",   "
                    + tens + " + " + ones + " = " + q.Answer;
            }
            else if (q.Category == QuestionCategory.Subtraction)
            {
                int tensOfB = q.B / 10 * 10;
                int step = q.A - tensOfB;
                steps = q.A + " - " + q.B + " = " + q.Answer + "\n"
                    + q.A + " - " + tensOfB + " = " + step + ",   "
                    + step + " - " + (q.B % 10) + " = " + q.Answer;
            }
            else if (q.IsProduct)
            {
                steps = q.FactorA + " × " + q.FactorB + " = " + q.A + "\n"
                    + Loc.F(q.A % 2 == 0 ? "visual_even" : "visual_odd", q.A);
            }
            else
            {
                steps = Loc.F(q.A % 2 == 0 ? "visual_even" : "visual_odd", q.A);
            }

            visualText.text = steps;
            visualGroup.SetActive(true);
        }

        /// <summary>Moves the picture text: beside the ten frames (Levels 1-4) or across the whole panel (Level 5).</summary>
        private void PlaceVisualText(float x, float width, int fontSize)
        {
            var rect = visualText.rectTransform;
            rect.anchoredPosition = new Vector2(x, -(FrameTop - 6f));
            rect.sizeDelta = new Vector2(width, 2 * CellStep + 8f);
            visualText.fontSize = fontSize;
        }

        // ------------------------------------------------------------------ Helpers

        private void SetAnswerButtonsVisible(int count)
        {
            visibleAnswers = count;

            // Centre the visible buttons in one row.
            const float buttonWidth = 190f;
            const float gap = 22f;
            float totalWidth = count * buttonWidth + Mathf.Max(0, count - 1) * gap;
            float startX = (Width - totalWidth) * 0.5f;

            for (int i = 0; i < MaxAnswers; i++)
            {
                bool visible = i < count;
                answerButtons[i].gameObject.SetActive(visible);
                if (visible)
                {
                    var rect = (RectTransform)answerButtons[i].transform;
                    rect.anchoredPosition = new Vector2(startX + i * (buttonWidth + gap), rect.anchoredPosition.y);
                }
            }
        }

        private void SetNextButton(string label)
        {
            bool visible = !string.IsNullOrEmpty(label);
            nextButton.gameObject.SetActive(visible);
            if (visible) UIFactory.SetButtonLabel(nextButton, label);
        }

        private void OnAnswerClicked(int index)
        {
            if (index < visibleAnswers && AnswerSelected != null) AnswerSelected(index);
        }

        private void OnNextClicked()
        {
            if (NextPressed != null) NextPressed();
        }
    }
}
