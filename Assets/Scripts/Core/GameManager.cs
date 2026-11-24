using System;
using System.Collections;
using UnityEngine;

namespace RSPP
{
    public enum GameState
    {
        Menu,               // on the entrance screen, no game running
        WaitingForRoll,     // the player should roll the die
        Rolling,            // the die is tumbling
        Moving,             // the token is hopping along the board
        WaitingForSpin,     // the player should pull the SPIN handle
        Spinning,           // the wheel is turning
        Question,           // a math question is waiting for an answer
        HelperAsk,          // the AI Helper has an idea; the player should touch the robot
        HelperCheck,        // the AI Helper made a suggestion; the player judges it
        Feedback,           // feedback or a result is shown; waiting for the Next button
        Finished            // the token reached FINISH
    }

    /// <summary>
    /// Controls the whole game loop:
    ///
    ///   roll the die -> the token moves automatically -> spin the wheel -> answer -> feedback -> next turn
    ///
    /// Rules (from the VR Game Proposal, with the changes listed in the README):
    ///   - Correct answer: earn a star. Incorrect answer: see the correct answer, a short explanation
    ///     and a picture (ten frames). There is no penalty and the game continues.
    ///   - Adaptive difficulty: see <see cref="AdaptiveDifficulty"/>.
    ///   - Star spaces and the wheel's MINI segment start a mini challenge of three short questions.
    ///     2 correct = 1 star. 3 correct = 2 stars. Robo takes one of the three questions.
    ///   - Every star flies into the star jar; at FINISH the stars fly out and frame the result panel.
    ///   - The wheel's BONUS segment gives a free star.
    ///   - Sometimes Robo (the simulated AI Helper) has an idea: the player touches Robo, hears the
    ///     suggestion and decides if it is right.
    ///   - The game ends when the token reaches FINISH (an exact roll is not needed).
    /// All sentences come from Loc (English, Spanish, Turkish, French). Narration is English only.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // References, assigned by GameBootstrap.
        public BoardPath Board;
        public PlayerToken Token;
        public Dice Dice;
        public SpinWheel Wheel;
        public WheelHandle Handle;
        public QuestionPanel Panel;
        public AIHelperRobot Robot;
        public AudioManager Audio;
        public PlayAreaFitter Fitter;
        public AudioSource StarAudioSource;
        public Effects Effects;
        public Narrator Narrator;
        public StarJar Jar;

        public GameState State { get; private set; }
        public int Stars { get; private set; }
        public int Position { get; private set; }

        /// <summary>The question on screen now (read by the Editor screenshot tour).</summary>
        public MathQuestion CurrentQuestion
        {
            get { return currentQuestion; }
        }

        /// <summary>Robo's current suggestion, or null (read by the Editor screenshot tour).</summary>
        public HelperSuggestion CurrentSuggestion
        {
            get { return currentSuggestion; }
        }

        private static string HelperName
        {
            get { return GameConfig.HelperName; }
        }

        private readonly QuestionGenerator questions = new QuestionGenerator();
        private readonly AdaptiveDifficulty difficulty = new AdaptiveDifficulty();
        private readonly AIHelperLogic helperLogic = new AIHelperLogic();

        private MathQuestion currentQuestion;
        private HelperSuggestion currentSuggestion;
        private Action afterNext;               // what the Next button does

        private bool miniChallengeActive;
        private int miniQuestionIndex;
        private int miniCorrectCount;
        private int miniHelperIndex;            // which of the mini challenge questions Robo takes

        // Numbers for the summary on the FINISH screen.
        private int mathAnswered;
        private int mathCorrect;
        private int helperChecks;
        private int helperGood;
        private int highestLevel;

        // ------------------------------------------------------------------ Setup

        /// <summary>Called once by GameBootstrap after all references are assigned.</summary>
        public void Initialize()
        {
            Dice.RollStarted += OnRollStarted;
            Dice.Rolled += OnDiceRolled;
            Handle.Pulled += OnHandlePulled;
            Wheel.Stopped += OnWheelStopped;
            Panel.AnswerSelected += OnAnswerSelected;
            Panel.NextPressed += OnNextPressed;
            Panel.ListenPressed += OnListenPressed;
            Robot.Judged += OnHelperJudged;
            Robot.Touched += OnRobotTouched;

            State = GameState.Menu;
            Dice.RollEnabled = false;
            Panel.ShowMessage("", "", null);
        }

        // ------------------------------------------------------------------ Entrance screen / menu actions

        /// <summary>Entrance screen: Enter the classroom. Starts a new game when none is running.</summary>
        public void StartGame()
        {
            if (State == GameState.Menu || State == GameState.Finished) BeginNewGame();
        }

        /// <summary>Starts again from the START space.</summary>
        public void RestartGame()
        {
            BeginNewGame();
        }

        /// <summary>In-game menu: Back to Menu. Stops the game; the entrance screen is shown again.</summary>
        public void ReturnToMenu()
        {
            StopAllCoroutines();
            if (Narrator != null) Narrator.Stop();
            State = GameState.Menu;
            afterNext = null;
            currentQuestion = null;
            currentSuggestion = null;
            Dice.ResetToHome();
            Dice.RollEnabled = false;
            Dice.SetHighlighted(false);
            Wheel.ResetWheel();
            Handle.SetHighlighted(false);
            Robot.ShowMessage("");
            Panel.ShowMessage("", "", null);
            if (Jar != null) Jar.ResetJar();
        }

        private void BeginNewGame()
        {
            StopAllCoroutines();
            if (Fitter != null) Fitter.FitToPlayer();

            Stars = 0;
            Position = 0;
            difficulty.Reset();
            difficulty.SetLevelCap(GameConfig.LevelCapForSpace(0, Board.LastIndex));
            helperLogic.Reset();
            mathAnswered = 0;
            mathCorrect = 0;
            helperChecks = 0;
            helperGood = 0;
            highestLevel = difficulty.Level;
            currentQuestion = null;
            currentSuggestion = null;
            afterNext = null;
            miniChallengeActive = false;

            Dice.ResetToHome();
            Wheel.ResetWheel();
            Token.PlaceOn(0);
            if (Jar != null) Jar.ResetJar();

            Panel.SetStars(Stars);
            Panel.SetLevel(difficulty.Level);
            BeginRollPhase();
        }

        // ------------------------------------------------------------------ Step 1: roll

        private void BeginRollPhase()
        {
            State = GameState.WaitingForRoll;

            Dice.RollEnabled = true;
            Dice.SetHighlighted(true);
            Handle.SetHighlighted(false);
            Robot.ShowMessage(Loc.F("robo_idle", HelperName));
            Panel.ShowMessage(Loc.T("your_turn_title"), Loc.T("your_turn_msg"), null);
            Say("roll_the_die");
        }

        private void StartNormalTurn()
        {
            BeginRollPhase();
        }

        private void OnRollStarted()
        {
            if (State != GameState.WaitingForRoll) return;
            State = GameState.Rolling;
            Dice.RollEnabled = false;
            Dice.SetHighlighted(false);
            Panel.ShowMessage(Loc.T("rolling"), "", null);
        }

        private void OnDiceRolled(int number)
        {
            if (State != GameState.Rolling) return;
            State = GameState.Moving;
            Panel.ShowMessage(Loc.F("rolled_title", number),
                number == 1 ? Loc.T("rolled_one") : Loc.F("rolled_many", number), null);
            StartCoroutine(MoveTokenRoutine(number));
        }

        // ------------------------------------------------------------------ Step 2: automatic token movement

        private IEnumerator MoveTokenRoutine(int steps)
        {
            int target = Mathf.Min(Position + steps, Board.LastIndex);
            yield return Token.MoveAlong(Position, target);
            Position = target;
            yield return new WaitForSeconds(0.3f);
            OnTokenLanded();
        }

        private void OnTokenLanded()
        {
            // Further along the board, harder questions are allowed (see GameConfig.LevelCapPerBoardPart).
            int cap = GameConfig.LevelCapForSpace(Position, Board.LastIndex);
            if (cap > difficulty.LevelCap)
            {
                difficulty.SetLevelCap(cap);
                Robot.ShowMessage(Loc.T("robo_new_part"));
            }

            if (Position >= Board.LastIndex)
            {
                FinishGame();
            }
            else if (Board.IsStarSpace(Position))
            {
                IntroduceMiniChallenge(Loc.T("mini_landed"));
            }
            else
            {
                BeginSpinPhase();
            }
        }

        // ------------------------------------------------------------------ Step 3: spin

        private void BeginSpinPhase()
        {
            State = GameState.WaitingForSpin;
            Handle.SetHighlighted(true);
            Panel.ShowMessage(Loc.T("spin_title"), Loc.T("spin_msg"), null);
            Say("spin_the_wheel");
        }

        private void OnHandlePulled()
        {
            if (State != GameState.WaitingForSpin || Wheel.IsSpinning) return;
            State = GameState.Spinning;
            Handle.SetHighlighted(false);
            Panel.ShowMessage(Loc.T("spinning"), "", null);
            Wheel.Spin();
        }

        private void OnWheelStopped(WheelSegment segment)
        {
            if (State != GameState.Spinning) return;

            switch (segment)
            {
                case WheelSegment.Addition:
                    AskSingleQuestion(QuestionCategory.Addition);
                    break;
                case WheelSegment.Subtraction:
                    AskSingleQuestion(QuestionCategory.Subtraction);
                    break;
                case WheelSegment.OddEven:
                    AskSingleQuestion(QuestionCategory.OddEven);
                    break;
                case WheelSegment.MiniChallenge:
                    IntroduceMiniChallenge(Loc.T("mini_wheel"));
                    break;
                default:
                    AddStar();
                    ShowResultMessage(Loc.T("bonus_title"), Loc.T("bonus_msg"), Loc.T("next"), StartNormalTurn);
                    Say("bonus_star");
                    break;
            }
        }

        // ------------------------------------------------------------------ Step 4: questions

        private void AskSingleQuestion(QuestionCategory category)
        {
            currentQuestion = questions.Next(category, difficulty.Level);
            string title = GameConfig.CategoryTitle(category);

            if (helperLogic.ShouldAppear())
            {
                // First the student touches Robo (left hand) to hear the idea, then judges it.
                currentSuggestion = helperLogic.MakeSuggestion(currentQuestion);
                State = GameState.HelperAsk;
                Panel.ShowQuestion(title, currentQuestion, false, Loc.F("robo_idea", HelperName));
                Robot.ShowWaitingForTouch();
                SayQuestionThen("robo_idea");
            }
            else
            {
                currentSuggestion = null;
                State = GameState.Question;
                Panel.ShowQuestion(title, currentQuestion, true, null);
                SayQuestionThen(null);
            }
        }

        private void OnAnswerSelected(int choiceIndex)
        {
            if (State != GameState.Question || currentQuestion == null) return;

            bool correct = choiceIndex == currentQuestion.CorrectIndex;
            int levelChange = difficulty.RecordAnswer(correct);
            mathAnswered++;
            if (correct) mathCorrect++;
            highestLevel = Mathf.Max(highestLevel, difficulty.Level);
            if (levelChange != 0) Panel.ShowLevelChange(difficulty.Level);
            else Panel.SetLevel(difficulty.Level);

            string feedback;
            if (correct)
            {
                Audio.Play(Sfx.Correct);
                Haptics.PulseRight(0.5f, 0.1f);
                if (miniChallengeActive)
                {
                    miniCorrectCount++;
                    feedback = Loc.T("correct_mini");
                }
                else
                {
                    AddStar();
                    feedback = Loc.T("correct_star");
                }
                Say("correct");
            }
            else
            {
                Audio.Play(Sfx.Incorrect);
                Haptics.PulseRight(0.25f, 0.25f);
                feedback = Loc.F("not_quite", currentQuestion.Explanation);
                if (Narrator != null) Narrator.SayExplanation(currentQuestion, "not_quite");
            }

            if (levelChange > 0) feedback += Loc.F("level_up", difficulty.Level);
            else if (levelChange < 0) feedback += Loc.F("level_down", difficulty.Level);

            State = GameState.Feedback;
            if (miniChallengeActive) afterNext = ContinueMiniChallenge;
            else afterNext = StartNormalTurn;
            Panel.ShowFeedback(correct, feedback, choiceIndex, currentQuestion.CorrectIndex, Loc.T("next"));
            if (!correct) Panel.ShowVisual(currentQuestion);     // picture explanation with ten frames
        }

        private void OnListenPressed()
        {
            if (currentQuestion != null && Narrator != null) Narrator.SayQuestion(currentQuestion);
        }

        // ------------------------------------------------------------------ Robo, the simulated AI Helper

        private void OnRobotTouched()
        {
            if (State != GameState.HelperAsk || currentSuggestion == null) return;
            State = GameState.HelperCheck;
            Robot.ShowSuggestion(currentSuggestion.Text);
            Panel.SetHint(Loc.F("robo_check", HelperName));
            Say("robo_check");      // the narrator reads the instruction; Robo itself has no voice
        }

        private void OnHelperJudged(bool playerSaidCorrect)
        {
            if (State != GameState.HelperCheck || currentQuestion == null || currentSuggestion == null) return;

            bool helperWasRight = currentSuggestion.IsCorrect;
            bool goodJudgment = playerSaidCorrect == helperWasRight;
            helperChecks++;
            if (goodJudgment) helperGood++;

            string feedback;
            if (goodJudgment)
            {
                Audio.Play(Sfx.Correct);
                Haptics.PulseRight(0.5f, 0.1f);
                if (miniChallengeActive)
                {
                    // In a mini challenge a good check counts as one of the right answers (no separate star).
                    miniCorrectCount++;
                    feedback = Loc.F(helperWasRight ? "helper_right_good_mini" : "helper_wrong_good_mini", HelperName, currentQuestion.Explanation);
                }
                else
                {
                    AddStar();
                    feedback = Loc.F(helperWasRight ? "helper_right_good" : "helper_wrong_good", HelperName, currentQuestion.Explanation);
                }
            }
            else
            {
                Audio.Play(Sfx.Incorrect);
                Haptics.PulseRight(0.25f, 0.25f);
                feedback = Loc.F(helperWasRight ? "helper_right_bad" : "helper_wrong_bad", HelperName, currentQuestion.Explanation);
            }

            Robot.ShowMessage(Loc.T(helperWasRight ? "robo_was_right" : "robo_was_wrong"));
            if (Narrator != null) Narrator.SayExplanation(currentQuestion, goodJudgment ? "great_checking" : "lets_check");

            State = GameState.Feedback;
            if (miniChallengeActive) afterNext = ContinueMiniChallenge;
            else afterNext = StartNormalTurn;
            Panel.ShowFeedback(goodJudgment, feedback, -1, -1, Loc.T("next"));

            // A picture explanation whenever the helper or the student got it wrong.
            if (!helperWasRight || !goodJudgment) Panel.ShowVisual(currentQuestion);
        }

        // ------------------------------------------------------------------ Mini challenge

        private void IntroduceMiniChallenge(string reason)
        {
            Handle.SetHighlighted(false);
            ShowResultMessage(Loc.T("mini_title"),
                Loc.F("mini_intro", reason, GameConfig.MiniChallengeQuestions,
                    GameConfig.MiniChallengeCorrectForStar, GameConfig.MiniChallengeCorrectForTwoStars),
                Loc.T("go"), StartMiniChallenge);
            Say("mini_challenge");
        }

        private void StartMiniChallenge()
        {
            miniChallengeActive = true;
            miniQuestionIndex = 0;
            miniCorrectCount = 0;
            // Robo always takes exactly one of the mini challenge questions (a random one).
            miniHelperIndex = UnityEngine.Random.Range(0, GameConfig.MiniChallengeQuestions);
            AskMiniQuestion();
        }

        private void AskMiniQuestion()
        {
            currentQuestion = questions.NextRandom(difficulty.Level);
            string title = Loc.F("mini_q", miniQuestionIndex + 1, GameConfig.MiniChallengeQuestions);

            if (miniQuestionIndex == miniHelperIndex)
            {
                currentSuggestion = helperLogic.MakeSuggestion(currentQuestion);
                State = GameState.HelperAsk;
                Panel.ShowQuestion(title, currentQuestion, false, Loc.F("robo_idea", HelperName));
                Robot.ShowWaitingForTouch();
                SayQuestionThen("robo_idea");
            }
            else
            {
                currentSuggestion = null;
                State = GameState.Question;
                Panel.ShowQuestion(title, currentQuestion, true, null);
                SayQuestionThen(null);
            }
        }

        private void ContinueMiniChallenge()
        {
            miniQuestionIndex++;
            if (miniQuestionIndex < GameConfig.MiniChallengeQuestions) AskMiniQuestion();
            else FinishMiniChallenge();
        }

        private void FinishMiniChallenge()
        {
            miniChallengeActive = false;

            bool winsStar = miniCorrectCount >= GameConfig.MiniChallengeCorrectForStar;
            bool winsTwoStars = miniCorrectCount >= GameConfig.MiniChallengeCorrectForTwoStars;
            string score = Loc.F("mini_score", miniCorrectCount, GameConfig.MiniChallengeQuestions);

            if (winsStar) AddStar();
            if (winsTwoStars) StartCoroutine(AddStarLater(0.5f));

            if (winsTwoStars)
                ShowResultMessage(Loc.T("mini_title"), score + "\n" + Loc.T("mini_perfect"), Loc.T("next"), StartNormalTurn);
            else if (winsStar)
                ShowResultMessage(Loc.T("mini_title"), score + "\n" + Loc.T("mini_star"), Loc.T("next"), StartNormalTurn);
            else
                ShowResultMessage(Loc.T("mini_title"), score + "\n" + Loc.T("mini_try"), Loc.T("next"), StartNormalTurn);
        }

        // ------------------------------------------------------------------ Finish

        private void FinishGame()
        {
            State = GameState.Finished;
            Dice.RollEnabled = false;
            Dice.SetHighlighted(false);
            Handle.SetHighlighted(false);
            Audio.Play(Sfx.Star, StarAudioSource);
            Haptics.PulseBoth(0.6f, 0.3f);
            if (Effects != null) Effects.Confetti();

            // The stars fly out of the jar and frame the result panel.
            if (Jar != null)
                Jar.Celebrate(GameConfig.QuestionPanelPosition + new Vector3(0f, 0f, -0.06f), 1.04f, 0.82f);

            afterNext = BeginNewGame;
            Robot.ShowMessage(Loc.T("robo_done"));
            Say("you_did_it");

            string summary = Loc.F("finish_summary", Stars, mathCorrect, mathAnswered, HelperName,
                helperGood, helperChecks, highestLevel);
            Panel.ShowMessage(Loc.T("finish_title"), summary, Loc.T("play_again"));
        }

        // ------------------------------------------------------------------ Shared helpers

        private void AddStar()
        {
            Stars++;
            Panel.SetStars(Stars);
            Panel.PulseStars();
            // The new star flies from the game piece into the star jar.
            Vector3 from = Token.transform.position + Vector3.up * 0.10f;
            if (Jar != null) Jar.AddStar(from);
            else if (Effects != null) Effects.StarPop(from);
            StartCoroutine(StarChimeRoutine());
        }

        private IEnumerator AddStarLater(float delay)
        {
            yield return new WaitForSeconds(delay);
            AddStar();
        }

        private IEnumerator StarChimeRoutine()
        {
            // A short delay so the star chime does not hide the "correct" sound.
            yield return new WaitForSeconds(0.35f);
            Audio.Play(Sfx.Star, StarAudioSource);
            Haptics.PulseRight(0.4f, 0.08f);
        }

        /// <summary>Shows a message with a button and remembers what the button should do.</summary>
        private void ShowResultMessage(string title, string message, string buttonLabel, Action onNext)
        {
            State = GameState.Feedback;
            afterNext = onNext;
            Panel.ShowMessage(title, message, buttonLabel);
        }

        private void OnNextPressed()
        {
            if (State != GameState.Feedback && State != GameState.Finished) return;

            Action action = afterNext;
            afterNext = null;
            if (action != null) action();
        }

        // ------------------------------------------------------------------ Narration (English only)

        private void Say(params string[] clips)
        {
            if (Narrator != null) Narrator.Say(clips);
        }

        private void SayQuestionThen(string followUpClip)
        {
            if (Narrator == null || currentQuestion == null) return;
            Narrator.SayQuestion(currentQuestion);
            if (followUpClip != null) Narrator.Queue(followUpClip);
        }
    }
}
