using System;
using System.Collections.Generic;

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
        /// <summary>
        /// Suggestions come in groups of this size, and exactly one in each group is wrong
        /// (in a random position). Our EDN 303 collaborator suggested the helper should be wrong about
        /// one third of the time, so correct answers are reinforced and students stay confident.
        /// Using groups (instead of a dice roll) guarantees that students really see Robo's mistakes.
        /// </summary>
        public const int GroupSize = 3;

        private readonly Random rng;
        private readonly List<bool> group = new List<bool>();   // true = this suggestion is correct
        private bool helperWasLast;
        private bool lastSuggestionCorrect;

        public AIHelperLogic() : this(new Random()) { }

        public AIHelperLogic(Random random)
        {
            rng = random;
            Reset();
        }

        public void Reset()
        {
            group.Clear();
            helperWasLast = false;
            lastSuggestionCorrect = true;
        }

        /// <summary>
        /// Decides if the helper makes a suggestion for the next normal question:
        /// every second normal question (mini challenges choose their own helper question).
        /// </summary>
        public bool ShouldAppear()
        {
            bool appear = !helperWasLast;
            helperWasLast = appear;
            return appear;
        }

        /// <summary>Builds a suggestion that is sometimes right and sometimes deliberately wrong.</summary>
        public HelperSuggestion MakeSuggestion(MathQuestion question)
        {
            if (group.Count == 0) RefillGroup();
            bool correct = group[0];
            group.RemoveAt(0);
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

        /// <summary>Makes a new group: GroupSize - 1 correct suggestions and one wrong one, shuffled.</summary>
        private void RefillGroup()
        {
            int wrongPosition = rng.Next(0, GroupSize);
            // Never wrong twice in a row: if the last suggestion was wrong, the new group cannot start with one.
            if (!lastSuggestionCorrect && wrongPosition == 0) wrongPosition = rng.Next(1, GroupSize);
            for (int i = 0; i < GroupSize; i++) group.Add(i != wrongPosition);
        }
    }
}
