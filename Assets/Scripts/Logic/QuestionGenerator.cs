using System;
using System.Collections.Generic;

namespace RSPP
{
    /// <summary>
    /// Creates second-grade math questions with simple C# rules. No external service or AI model is used.
    /// All numbers stay within 20, as recommended by our EDN 303 collaborators. Four small steps:
    ///   Level 1: small numbers within 10      (3 + 2, 5 - 1)          odd/even 1-10
    ///   Level 2: within 10                    (6 + 4, 9 - 3)          odd/even 1-10
    ///   Level 3: within 20, no crossing ten   (12 + 5, 17 - 4)        odd/even 11-20
    ///   Level 4: within 20, crossing ten      (8 + 7, 15 - 8)         odd/even 11-20
    /// </summary>
    public class QuestionGenerator
    {
        /// <summary>Largest number that can appear in an answer at this level.</summary>
        public static int MaxNumber(int level)
        {
            return level <= 2 ? 10 : 20;
        }

        private readonly Random rng;

        public QuestionGenerator() : this(new Random()) { }

        public QuestionGenerator(Random random)
        {
            rng = random;
        }

        /// <summary>Returns a question from a random category.</summary>
        public MathQuestion NextRandom(int level)
        {
            return Next((QuestionCategory)rng.Next(0, 3), level);
        }

        public MathQuestion Next(QuestionCategory category, int level)
        {
            if (level < AdaptiveDifficulty.MinLevel) level = AdaptiveDifficulty.MinLevel;
            if (level > AdaptiveDifficulty.MaxLevel) level = AdaptiveDifficulty.MaxLevel;

            switch (category)
            {
                case QuestionCategory.Addition: return MakeAddition(level);
                case QuestionCategory.Subtraction: return MakeSubtraction(level);
                default: return MakeOddEven(level);
            }
        }

        // ------------------------------------------------------------------
        // Addition
        // ------------------------------------------------------------------
        private MathQuestion MakeAddition(int level)
        {
            int a, b;
            switch (level)
            {
                case 1:
                    // Sum 2-6.
                    do { a = rng.Next(1, 6); b = rng.Next(1, 6); } while (a + b > 6);
                    break;
                case 2:
                    // Sum 7-10.
                    do { a = rng.Next(1, 10); b = rng.Next(1, 10); } while (a + b < 7 || a + b > 10);
                    break;
                case 3:
                    // Teen number + small number, the ones stay below ten (e.g. 12 + 5 = 17).
                    do { a = rng.Next(11, 19); b = rng.Next(1, 9); } while (a % 10 + b > 9);
                    break;
                default:
                    // Two one-digit numbers that cross ten (e.g. 8 + 7 = 15).
                    do { a = rng.Next(2, 10); b = rng.Next(2, 10); } while (a + b < 11);
                    break;
            }

            int sum = a + b;
            var q = new MathQuestion();
            q.Category = QuestionCategory.Addition;
            q.Level = level;
            q.A = a;
            q.B = b;
            q.Answer = sum;
            q.Prompt = a + " + " + b + " = ?";
            q.Explanation = Loc.F("exp_add", a, b, sum);
            BuildNumberChoices(q, sum, Math.Abs(a - b), MaxNumber(level));
            return q;
        }

        // ------------------------------------------------------------------
        // Subtraction
        // ------------------------------------------------------------------
        private MathQuestion MakeSubtraction(int level)
        {
            int a, b;
            switch (level)
            {
                case 1:
                    // From 2-6.
                    a = rng.Next(2, 7);
                    b = rng.Next(1, a);
                    break;
                case 2:
                    // From 7-10.
                    a = rng.Next(7, 11);
                    b = rng.Next(1, a);
                    break;
                case 3:
                    // Teen number minus a small number, no crossing ten (e.g. 17 - 4 = 13).
                    do { a = rng.Next(11, 20); b = rng.Next(1, 10); } while (b > a % 10);
                    break;
                default:
                    // Crossing ten (e.g. 15 - 8 = 7).
                    do { a = rng.Next(11, 19); b = rng.Next(2, 10); } while (b <= a % 10);
                    break;
            }

            int difference = a - b;
            var q = new MathQuestion();
            q.Category = QuestionCategory.Subtraction;
            q.Level = level;
            q.A = a;
            q.B = b;
            q.Answer = difference;
            q.Prompt = a + " - " + b + " = ?";
            q.Explanation = Loc.F("exp_sub", a, b, difference);
            BuildNumberChoices(q, difference, a + b, MaxNumber(level));
            return q;
        }

        // ------------------------------------------------------------------
        // Odd / even
        // ------------------------------------------------------------------
        private MathQuestion MakeOddEven(int level)
        {
            int min = level <= 2 ? 1 : 11;
            int max = level <= 2 ? 10 : 20;

            var q = new MathQuestion();
            q.Category = QuestionCategory.OddEven;
            q.Level = level;

            if (rng.Next(0, 2) == 0)
            {
                // Form A: "Is 7 odd or even?"
                int n = rng.Next(min, max + 1);
                bool even = n % 2 == 0;
                q.A = n;
                q.Answer = even ? 0 : 1;
                q.Prompt = Loc.F("q_is_oddeven", n);
                q.Choices = new[] { Loc.T("odd"), Loc.T("even") };
                q.CorrectIndex = even ? 1 : 0;
                q.Explanation = ParityExplanation(n);
            }
            else
            {
                // Form B: "Which number is even?" with exactly one matching number.
                bool wantEven = rng.Next(0, 2) == 0;
                int target = RandomWithParity(min, max, wantEven);

                var others = new List<int>();
                while (others.Count < 3)
                {
                    int candidate = RandomWithParity(min, max, !wantEven);
                    if (!others.Contains(candidate)) others.Add(candidate);
                }

                var all = new List<int>(others);
                all.Add(target);
                all.Sort();

                q.A = target;
                q.Answer = target % 2;
                q.Prompt = Loc.T(wantEven ? "q_which_even" : "q_which_odd");
                q.Choices = new string[all.Count];
                for (int i = 0; i < all.Count; i++)
                {
                    q.Choices[i] = all[i].ToString();
                    if (all[i] == target) q.CorrectIndex = i;
                }
                q.Explanation = ParityExplanation(target);
            }
            return q;
        }

        private static string ParityExplanation(int n)
        {
            return Loc.F(n % 2 == 0 ? "exp_even" : "exp_odd", n);
        }

        private int RandomWithParity(int min, int max, bool even)
        {
            // The ranges are tiny, so a simple retry loop is fine.
            while (true)
            {
                int n = rng.Next(min, max + 1);
                if ((n % 2 == 0) == even) return n;
            }
        }

        // ------------------------------------------------------------------
        // Answer choices for + and - questions
        // ------------------------------------------------------------------

        /// <summary>
        /// Fills in four number choices: the correct answer plus three believable wrong ones
        /// (off by one or two, and the "used the other operation" mistake).
        /// </summary>
        private void BuildNumberChoices(MathQuestion q, int correct, int otherOperationResult, int maxValue)
        {
            var candidates = new List<int> { correct + 1, correct - 1, correct + 2, correct - 2, otherOperationResult };
            Shuffle(candidates);

            var wrong = new List<int>();
            for (int i = 0; i < candidates.Count && wrong.Count < 3; i++)
            {
                int c = candidates[i];
                if (c < 0 || c > maxValue || c == correct || wrong.Contains(c)) continue;
                wrong.Add(c);
            }

            // Safety net: walk outwards from the correct answer until there are three wrong answers.
            for (int distance = 1; wrong.Count < 3 && distance <= maxValue; distance++)
            {
                int below = correct - distance;
                int above = correct + distance;
                if (below >= 0 && !wrong.Contains(below)) wrong.Add(below);
                if (wrong.Count < 3 && above <= maxValue && !wrong.Contains(above)) wrong.Add(above);
            }

            var all = new List<int>(wrong);
            all.Add(correct);
            all.Sort();

            q.Choices = new string[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                q.Choices[i] = all[i].ToString();
                if (all[i] == correct) q.CorrectIndex = i;
            }
        }

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
