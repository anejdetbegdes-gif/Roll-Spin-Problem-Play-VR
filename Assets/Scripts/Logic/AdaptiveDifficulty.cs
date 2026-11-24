namespace RSPP
{
    /// <summary>
    /// Rule-based adaptive difficulty with five levels (see QuestionGenerator):
    ///   every game starts at Level 3 (GameConfig.StartLevel), because players are 10-12 years old
    ///   2 correct answers in a row -> one level up   (but never above the current level cap)
    ///   2 wrong answers in a row   -> one level down (down to Level 1, the easiest numbers)
    /// The level cap rises as the token moves along the board (GameConfig.LevelCapPerBoardPart),
    /// so the hardest questions can only appear near the FINISH.
    /// Only basic variables and if-statements. This is NOT machine learning.
    /// </summary>
    public class AdaptiveDifficulty
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 5;
        public const int CorrectStreakToLevelUp = 2;
        public const int WrongStreakToLevelDown = 2;

        public int Level { get; private set; }
        public int LevelCap { get; private set; }
        public int CorrectStreak { get; private set; }
        public int WrongStreak { get; private set; }

        private readonly int startLevel;

        public AdaptiveDifficulty() : this(MinLevel) { }

        public AdaptiveDifficulty(int startLevel)
        {
            this.startLevel = startLevel < MinLevel ? MinLevel : (startLevel > MaxLevel ? MaxLevel : startLevel);
            Reset();
        }

        public void Reset()
        {
            Level = startLevel;
            LevelCap = MaxLevel;
            CorrectStreak = 0;
            WrongStreak = 0;
        }

        /// <summary>Sets the highest level that may be reached right now (1-5).</summary>
        public void SetLevelCap(int cap)
        {
            if (cap < MinLevel) cap = MinLevel;
            if (cap > MaxLevel) cap = MaxLevel;
            LevelCap = cap;
            if (Level > LevelCap) Level = LevelCap;
        }

        /// <summary>
        /// Call once for every answered math question.
        /// Returns +1 if the level went up, -1 if it went down, 0 if it stayed the same.
        /// </summary>
        public int RecordAnswer(bool correct)
        {
            if (correct)
            {
                CorrectStreak++;
                WrongStreak = 0;

                if (CorrectStreak >= CorrectStreakToLevelUp && Level < LevelCap)
                {
                    CorrectStreak = 0;
                    Level++;
                    return 1;
                }
            }
            else
            {
                WrongStreak++;
                CorrectStreak = 0;

                if (WrongStreak >= WrongStreakToLevelDown)
                {
                    WrongStreak = 0;
                    if (Level > MinLevel)
                    {
                        Level--;
                        return -1;
                    }
                }
            }
            return 0;
        }
    }
}
