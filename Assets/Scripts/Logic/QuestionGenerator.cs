using System;
using System.Collections.Generic;

namespace RSPP
{
    /// <summary>
    /// Creates math questions with simple C# rules. No external service or AI model is used.
    /// Levels 1-4 are the second-grade problem set from the original design: all numbers stay within 20,
    /// as recommended by our EDN 303 collaborators. Level 5 was added for players aged 10-12, because
    /// Meta Quest headsets are only for ages 10 and up (approved by our instructor).
    ///   Level 1: small numbers within 10      (3 + 2, 5 - 1)          odd/even 1-10
    ///   Level 2: within 10                    (6 + 4, 9 - 3)          odd/even 1-10
    ///   Level 3: within 20, no crossing ten   (12 + 5, 17 - 4)        odd/even 11-20
    ///   Level 4: within 20, crossing ten      (8 + 7, 15 - 8)         odd/even 11-20
    ///   Level 5: within 100, with regrouping  (47 + 28, 83 - 36)      odd/even of a product (6 x 7)
    /// </summary>
    public class QuestionGenerator
    {
        /// <summary>Largest number that can appear in an answer at this level.</summary>
        public static int MaxNumber(int level)
        {
            if (level <= 2) return 10;
            if (level <= 4) return 20;
            return 100;
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
                case 4:
                    // Two one-digit numbers that cross ten (e.g. 8 + 7 = 15).
                    do { a = rng.Next(2, 10); b = rng.Next(2, 10); } while (a + b < 11);
                    break;
                default:
                    // Two two-digit numbers, the ones make a new ten, sum below 100 (e.g. 47 + 28 = 75).
                    do { a = rng.Next(15, 80); b = rng.Next(12, 50); }
                    while (a % 10 + b % 10 < 10 || a + b > 99 || a % 10 == 0 || b % 10 == 0);
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
            if (level >= 5)
                // Typical mistakes with big numbers: forgetting the new ten (-10), or off by one.
                BuildNumberChoices(q, sum, new[] { sum - 10, sum + 10, sum + 1, sum - 1 }, MaxNumber(level));
            else
                BuildNumberChoices(q, sum, new[] { sum + 1, sum - 1, sum + 2, sum - 2, Math.Abs(a - b) }, MaxNumber(level));
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
                case 4:
                    // Crossing ten (e.g. 15 - 8 = 7).
                    do { a = rng.Next(11, 19); b = rng.Next(2, 10); } while (b <= a % 10);
                    break;
                default:
                    // Two two-digit numbers, a ten has to be broken up (e.g. 83 - 36 = 47).
                    do { a = rng.Next(31, 100); b = rng.Next(12, 70); }
                    while (b % 10 <= a % 10 || a - b < 10);
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
            if (level >= 5)
                // Typical mistakes: not breaking up the ten (+10), or off by one.
                BuildNumberChoices(q, difference, new[] { difference + 10, difference - 10, difference + 1, difference - 1 }, MaxNumber(level));
            else
                BuildNumberChoices(q, difference, new[] { difference + 1, difference - 1, difference + 2, difference - 2, a + b }, MaxNumber(level));
            return q;
        }

        // ------------------------------------------------------------------
        // Odd / even
        // ------------------------------------------------------------------
        private MathQuestion MakeOddEven(int level)
        {
            if (level >= 5) return MakeProductOddEven(level);

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

        /// <summary>
        /// Level 5: "Is 6 x 7 odd or even?" The player works out a times-table fact, then decides.
        /// A = the product, FactorA x FactorB = A.
        /// </summary>
        private MathQuestion MakeProductOddEven(int level)
        {
            int x = rng.Next(3, 10);
            int y = rng.Next(3, 10);
            // Half of the questions use two odd numbers, so "odd" answers are as common as "even" ones.
            if (rng.Next(0, 2) == 0) { x |= 1; y |= 1; }
            int product = x * y;
            bool even = product % 2 == 0;

            var q = new MathQuestion();
            q.Category = QuestionCategory.OddEven;
            q.Level = level;
            q.A = product;
            q.FactorA = x;
            q.FactorB = y;
            q.Answer = even ? 0 : 1;
            q.Prompt = Loc.F("q_product_oddeven", x + " × " + y);
            q.Choices = new[] { Loc.T("odd"), Loc.T("even") };
            q.CorrectIndex = even ? 1 : 0;
            q.Explanation = Loc.F(even ? "exp_product_even" : "exp_product_odd", x + " × " + y, product);
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
        /// Fills in four number choices: the correct answer plus three believable wrong ones,
        /// picked at random from <paramref name="mistakes"/> (typical mistakes such as off by one).
        /// </summary>
        private void BuildNumberChoices(MathQuestion q, int correct, int[] mistakes, int maxValue)
        {
            var candidates = new List<int>(mistakes);
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
