using UnityEngine;

namespace RSPP
{
    /// <summary>
    /// All tunable numbers, positions and colors in one place, so the game is easy to adjust.
    ///
    /// LAYOUT COORDINATES: every position below is LOCAL to the "Play Area" object.
    ///   x = right of the player, z = in front of the player, y = height above the TABLE TOP.
    ///   The player stands or sits at x = 0, z = 0 and looks towards +z.
    ///   The Play Area is moved up and down at runtime so the table suits the player's height.
    /// </summary>
    public static class GameConfig
    {
        // ---------------------------------------------------------------- Game rules
        /// <summary>Board spaces that start a mini challenge (index 0 is START).</summary>
        public static readonly int[] StarSpaces = { 4, 9, 14, 20, 25, 30 };

        /// <summary>
        /// Every game starts at this level. Meta Quest headsets are for ages 10 and up, so the game
        /// starts in the middle (numbers within 20) instead of at the easiest level. Two wrong answers
        /// in a row still move a player down to Levels 2 and 1 when they need easier numbers.
        /// </summary>
        public const int StartLevel = 3;

        /// <summary>
        /// The board is split into three equal parts (roughly its three rows). In each part the
        /// questions can climb up to this level, so the hardest questions (Level 5, for ages 10-12)
        /// appear near the FINISH. Adaptive difficulty still moves the level up and down inside this limit.
        /// </summary>
        public static readonly int[] LevelCapPerBoardPart = { 3, 4, 5 };

        /// <summary>The AI Helper robot's name (our EDN 303 collaborator suggested giving it a name).</summary>
        public const string HelperName = "Robo";

        /// <summary>Highest level allowed on the given board space.</summary>
        public static int LevelCapForSpace(int index, int lastIndex)
        {
            int parts = LevelCapPerBoardPart.Length;
            int part = Mathf.Clamp(index * parts / (lastIndex + 1), 0, parts - 1);
            return LevelCapPerBoardPart[part];
        }

        /// <summary>Background music volume (0-1). The Sound button switches music off too.</summary>
        public const float MusicVolume = 0.16f;

        /// <summary>Set to true to write "[RSPP VR]" head/rig positions to the Console every 3 seconds.</summary>
        public static readonly bool ShowXRDiagnostics = false;

        /// <summary>Number of short questions in one mini challenge.</summary>
        public const int MiniChallengeQuestions = 3;

        /// <summary>Correct answers needed in a mini challenge to win a star.</summary>
        public const int MiniChallengeCorrectForStar = 2;

        /// <summary>Correct answers needed in a mini challenge to win 2 stars instead of 1.</summary>
        public const int MiniChallengeCorrectForTwoStars = 3;

        /// <summary>The star jar stands at the front-right corner of the table.</summary>
        public static readonly Vector3 StarJarPosition = new Vector3(0.62f, 0f, 0.28f);

        /// <summary>The eight wheel segments, counter-clockwise starting at the right-hand side.</summary>
        public static readonly WheelSegment[] WheelLayout =
        {
            WheelSegment.Addition, WheelSegment.Subtraction, WheelSegment.OddEven, WheelSegment.MiniChallenge,
            WheelSegment.Addition, WheelSegment.Subtraction, WheelSegment.OddEven, WheelSegment.Bonus
        };

        // ---------------------------------------------------------------- Comfort / player fit
        /// <summary>The table top is placed this far below the player's eyes.</summary>
        public const float EyeHeightAboveTable = 0.45f;
        public const float MinTableHeight = 0.45f;
        public const float MaxTableHeight = 1.10f;

        /// <summary>Camera height used when no headset is connected (desktop test mode).</summary>
        public const float DesktopEyeHeight = 1.25f;

        // ---------------------------------------------------------------- Table and board
        public static readonly Vector3 TableCenter = new Vector3(0f, 0f, 0.60f);
        public static readonly Vector2 TableSize = new Vector2(1.60f, 0.80f);     // x, z

        public const int BoardColumns = 11;     // 3 rows of 11 + 2 connectors = 35 spaces
        public const float TileSpacing = 0.10f;
        public const float TileSize = 0.092f;
        public const float TileHeight = 0.012f;
        public const float BoardBaseHeight = 0.008f;
        public const float BoardFirstRowZ = 0.45f;
        public const float BoardRowGap = 0.20f;

        // ---------------------------------------------------------------- Interactive objects
        public const float DieSize = 0.07f;
        public static readonly Vector3 DieHome = new Vector3(-0.36f, DieSize * 0.5f + 0.006f, 0.30f);
        public static readonly Vector3 HandlePosition = new Vector3(-0.12f, 0f, 0.30f);

        // The wheel and the robot sit just outside the (longer) board.
        public static readonly Vector3 WheelPosition = new Vector3(-0.68f, 0.27f, 0.86f);
        public const float WheelRadius = 0.17f;

        // Robo stands at the front-LEFT corner of the table, within reach of the left hand.
        public static readonly Vector3 RobotPosition = new Vector3(-0.64f, 0f, 0.27f);

        // ---------------------------------------------------------------- Floating panels (centre positions)
        public static readonly Vector3 QuestionPanelPosition = new Vector3(0f, 0.67f, 1.10f);   // centre of a 0.92 x 0.70 m panel
        // LEFT side = things the left hand touches; RIGHT side = the menu for the right-hand ray.
        public static readonly Vector3 MenuPanelPosition = new Vector3(0.86f, 0.50f, 0.36f);
        public static readonly Vector3 HelperBubblePosition = new Vector3(-0.74f, 0.80f, 0.80f);   // above the wheel

        // ---------------------------------------------------------------- Colors
        public static readonly Color[] TileColors =
        {
            new Color(0.93f, 0.33f, 0.31f),   // red
            new Color(1.00f, 0.80f, 0.20f),   // yellow
            new Color(0.36f, 0.75f, 0.38f),   // green
            new Color(0.25f, 0.56f, 0.90f),   // blue
            new Color(0.62f, 0.42f, 0.83f)    // purple
        };

        public static readonly Color StartTileColor = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color FinishTileColor = new Color(1.00f, 0.60f, 0.10f);
        public static readonly Color StarTileColor = new Color(0.20f, 0.24f, 0.45f);
        public static readonly Color StarColor = new Color(1.00f, 0.84f, 0.10f);

        public static readonly Color PanelColor = new Color(1.00f, 0.99f, 0.95f);
        public static readonly Color PanelHeaderColor = new Color(0.16f, 0.25f, 0.50f);
        public static readonly Color TextDark = new Color(0.10f, 0.12f, 0.20f);
        public static readonly Color TextLight = Color.white;
        public static readonly Color ButtonBlue = new Color(0.16f, 0.42f, 0.85f);
        public static readonly Color ButtonGreen = new Color(0.13f, 0.60f, 0.25f);
        public static readonly Color ButtonRed = new Color(0.82f, 0.20f, 0.20f);
        public static readonly Color ButtonGrey = new Color(0.45f, 0.48f, 0.55f);
        public static readonly Color ButtonOrange = new Color(0.93f, 0.50f, 0.08f);

        public static Color WheelColor(WheelSegment segment)
        {
            switch (segment)
            {
                case WheelSegment.Addition: return new Color(0.36f, 0.75f, 0.38f);
                case WheelSegment.Subtraction: return new Color(0.25f, 0.56f, 0.90f);
                case WheelSegment.OddEven: return new Color(0.62f, 0.42f, 0.83f);
                case WheelSegment.MiniChallenge: return new Color(1.00f, 0.80f, 0.20f);
                default: return new Color(0.93f, 0.33f, 0.31f);
            }
        }

        /// <summary>Translation key (see Loc) for a wheel segment's label.</summary>
        public static string WheelLabelKey(WheelSegment segment)
        {
            switch (segment)
            {
                case WheelSegment.Addition: return "wheel_add";
                case WheelSegment.Subtraction: return "wheel_sub";
                case WheelSegment.OddEven: return "wheel_oddeven";
                case WheelSegment.MiniChallenge: return "wheel_mini";
                default: return "wheel_bonus";
            }
        }

        public static string CategoryTitle(QuestionCategory category)
        {
            switch (category)
            {
                case QuestionCategory.Addition: return Loc.T("cat_add");
                case QuestionCategory.Subtraction: return Loc.T("cat_sub");
                default: return Loc.T("cat_oddeven");
            }
        }
    }
}
