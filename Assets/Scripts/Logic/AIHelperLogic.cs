using System;

namespace RSPP
{
    /// <summary>What the simulated helper says about one question.</summary>
    public class HelperSuggestion
    {
        /// <summary>Index of the answer choice the helper suggests.</summary>
        public int SuggestedIndex;

        /// <summary>True when the helper's suggestion is actually the right answer.</summary>
        public bool IsCorrect;

        /// <summary>Speech bubble text, e.g. "I think the answer is 14!"</summary>
        public string Text;
    }

    /// <summary>
    /// SIMULATED "AI Helper". This is NOT real AI: it is a few lines of C# that pick either
    /// the correct answer or a wrong answer on purpose, so students practice checking
    /// an AI suggestion instead of trusting it automatically.
    /// </summary>
    public class AIHelperLogic
    {
        /// <summary>Chance that the helper speaks up on a normal question.</summary>
        public const double AppearChance = 0.4;

        /// <summary>The helper always appears after this many normal questions without it.</summary>
        public const int MaxQuestionsWithoutHelper = 2;

        /// <summary>
        /// Chance that a suggestion is correct. Our EDN 303 collaborator suggested the helper should be
        /// wrong about one third of the time, so correct answers are reinforced and students stay confident.
        /// </summary>
        public const double CorrectChance = 2.0 / 3.0;

        /// <summary>After this many correct suggestions in a row the next one is wrong, so students see both cases.</summary>
        public const int MaxCorrectInARow = 3;

        private readonly Random rng;
        private int questionsSinceHelper;
        private bool helperWasLast;
        private bool lastSuggestionCorrect;
        private int correctStreak;

        public AIHelperLogic() : this(new Random()) { }

        public AIHelperLogic(Random random)
        {
            rng = random;
            Reset();
        }

        public void Reset()
        {
            questionsSinceHelper = 0;
            helperWasLast = false;
            lastSuggestionCorrect = true;
            correctStreak = 0;
        }

        /// <summary>
        /// Decides if the helper makes a suggestion for the next normal question.
        /// "Occasionally": never twice in a row, but at least every third question.
        /// </summary>
        public bool ShouldAppear()
        {
            bool appear;
            if (helperWasLast) appear = false;
            else if (questionsSinceHelper >= MaxQuestionsWithoutHelper) appear = true;
            else appear = rng.NextDouble() < AppearChance;

            if (appear)
            {
                questionsSinceHelper = 0;
                helperWasLast = true;
            }
            else
            {
                questionsSinceHelper++;
                helperWasLast = false;
            }
            return appear;
        }

        /// <summary>Builds a suggestion that is sometimes right and sometimes deliberately wrong.</summary>
        public HelperSuggestion MakeSuggestion(MathQuestion question)
        {
            bool correct = rng.NextDouble() < CorrectChance;

            // Never wrong twice in a row (keeps students confident),
            // and never right more than MaxCorrectInARow times in a row (so they still see mistakes).
            if (!lastSuggestionCorrect) correct = true;
            else if (correctStreak >= MaxCorrectInARow) correct = false;

            correctStreak = correct ? correctStreak + 1 : 0;
            lastSuggestionCorrect = correct;

            int index = question.CorrectIndex;
            if (!correct)
            {
                // Pick any choice except the correct one.
                int offset = rng.Next(1, question.Choices.Length);
                index = (question.CorrectIndex + offset) % question.Choices.Length;
            }

            var suggestion = new HelperSuggestion();
            suggestion.SuggestedIndex = index;
            suggestion.IsCorrect = correct;
            suggestion.Text = Loc.F("suggest", question.Choices[index]);
            return suggestion;
        }
    }
}
