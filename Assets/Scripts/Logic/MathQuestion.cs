namespace RSPP
{
    /// <summary>The three math categories from the original Roll, Spin, Problem, Play design.</summary>
    public enum QuestionCategory
    {
        Addition,
        Subtraction,
        OddEven
    }

    /// <summary>
    /// One multiple-choice math question. Plain data, no Unity dependencies,
    /// so it can be tested outside of Unity.
    /// </summary>
    public class MathQuestion
    {
        public QuestionCategory Category;

        /// <summary>Difficulty level this question was generated for (1-4).</summary>
        public int Level;

        /// <summary>
        /// The numbers behind the question, used for the picture explanation (ten frames):
        ///   addition "A + B", subtraction "A - B", odd/even: A is the number that is explained.
        /// </summary>
        public int A;
        public int B;

        /// <summary>The correct result (sum or difference). For odd/even: 1 = odd, 0 = even.</summary>
        public int Answer;

        /// <summary>Text shown on the question panel, e.g. "8 + 5 = ?".</summary>
        public string Prompt;

        /// <summary>Answer button labels (2 to 4 entries).</summary>
        public string[] Choices;

        /// <summary>Index into <see cref="Choices"/> of the correct answer.</summary>
        public int CorrectIndex;

        /// <summary>Short explanation shown after an incorrect answer, e.g. "8 plus 5 equals 13."</summary>
        public string Explanation;

        public string CorrectText
        {
            get { return Choices[CorrectIndex]; }
        }
    }
}
