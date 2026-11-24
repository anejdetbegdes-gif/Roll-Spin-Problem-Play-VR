using UnityEngine;
using UnityEngine.SceneManagement;

namespace RSPP
{
    /// <summary>
    /// ENTRY POINT. This component sits on the "RSPP Game" object in MainScene.
    /// When you press Play it builds the whole game from code and connects every part:
    ///
    ///   XR rig -> classroom and table -> board and token -> die, wheel and handle -> UI -> game manager
    ///
    /// Why from code? Every object and reference is created in plain, readable C#, so nothing
    /// depends on hand-wired Inspector links or prefabs that could break. To change a position,
    /// size, color or rule, edit GameConfig.cs.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const string MainSceneName = "MainScene";

        private bool built;

        private void Awake()
        {
            BuildGame();
        }

        private void BuildGame()
        {
            if (built) return;
            built = true;

            // Quest-friendly rendering: 4x anti-aliasing for readable text, no real-time shadows.
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.Disable;

            Shapes.ResetCaches();
            UIFactory.ResetCaches();
            Loc.ClearListeners();

            // Priority 1: XR rig and world.
            GameRig rig = XRRigBuilder.Build();
            Transform playArea = EnvironmentBuilder.Build();
            AudioManager audioManager = new GameObject("Audio Manager").AddComponent<AudioManager>();

            // Priority 2: board, token, die, wheel.
            BoardPath board = BoardPath.Create(playArea);
            PlayerToken token = PlayerToken.Create(board);
            Dice dice = Dice.Create(playArea, audioManager);
            SpinWheel wheel = SpinWheel.Create(playArea, audioManager);
            WheelHandle handle = WheelHandle.Create(playArea);

            // Priority 3 and 4: UI and the simulated AI helper.
            QuestionPanel panel = QuestionPanel.Create(playArea, rig.Camera);
            MainMenu menu = MainMenu.Create(playArea, rig.Camera);
            AIHelperRobot robot = AIHelperRobot.Create(playArea, rig.Camera);

            // Comfort: recentre the player and fit the table height.
            PlayAreaFitter fitter = gameObject.AddComponent<PlayAreaFitter>();
            fitter.Setup(rig.Origin, rig.Camera, playArea, rig.RecenterAction);

            // Debug (off by default, see GameConfig): logs head / rig / table positions every 3 s.
            if (GameConfig.ShowXRDiagnostics)
            {
                XRDiagnostics diagnostics = gameObject.AddComponent<XRDiagnostics>();
                diagnostics.Origin = rig.Origin;
                diagnostics.Head = rig.Camera;
                diagnostics.PlayArea = playArea;
            }

            // Celebration effects, quiet background music and English narration.
            Effects effects = Effects.Create(playArea);
            MusicPlayer music = MusicPlayer.Create();
            Narrator narrator = Narrator.Create();

            // Entrance screen (school hallway) and the fade used to enter / leave the classroom.
            EntranceScreen entrance = EntranceScreen.Create(rig.Camera);
            fitter.EntranceAnchor = entrance.Anchor;
            ScreenFader fader = ScreenFader.Create(rig.Camera);
            Transform room = EnvironmentBuilder.Room;

            // Game flow.
            GameManager game = gameObject.AddComponent<GameManager>();
            game.Board = board;
            game.Token = token;
            game.Dice = dice;
            game.Wheel = wheel;
            game.Handle = handle;
            game.Panel = panel;
            game.Robot = robot;
            game.Audio = audioManager;
            game.Fitter = fitter;
            game.Effects = effects;
            game.Narrator = narrator;
            game.Jar = StarJar.Create(playArea, audioManager);     // star reward jar on the table
            game.StarAudioSource = audioManager.CreateSpatialSource(panel.transform);
            game.Initialize();

            // Entrance: "Enter the classroom" -> fade -> classroom with a new game.
            // In-game menu: "Back to Menu" (confirmed) -> fade -> entrance screen.
            entrance.EnterPressed += () => fader.FadeAndDo(() =>
            {
                ShowClassroom(room, playArea, entrance, true);
                game.StartGame();
            });
            menu.BackToMenuConfirmed += () => fader.FadeAndDo(() =>
            {
                game.ReturnToMenu();
                ShowClassroom(room, playArea, entrance, false);
            });
            menu.MusicToggled += music.SetMusicOn;
            menu.SoundToggled += audioManager.SetSoundOn;

            // The game opens on the entrance screen.
            ShowClassroom(room, playArea, entrance, false);

            // Shows VR / controller status at the bottom of the entrance screen.
            XRStatus status = gameObject.AddComponent<XRStatus>();
            status.Entrance = entrance;

            // A tiny vibration on the pointing hand for every button press.
            UIFactory.AnyButtonClicked += OnAnyButtonClicked;

            // Editor testing without a headset (mouse and keyboard).
            DesktopControls desktop = gameObject.AddComponent<DesktopControls>();
            desktop.Rig = rig;
            desktop.Dice = dice;
            desktop.Handle = handle;
            desktop.Fitter = fitter;
            desktop.Robot = robot;
        }

        /// <summary>Shows either the classroom (with the table and game) or the entrance hallway.</summary>
        private static void ShowClassroom(Transform room, Transform playArea, EntranceScreen entrance, bool inClassroom)
        {
            if (room != null) room.gameObject.SetActive(inClassroom);
            playArea.gameObject.SetActive(inClassroom);
            entrance.SetVisible(!inClassroom);
        }

        private static void OnAnyButtonClicked()
        {
            Haptics.PulseRight(0.2f, 0.03f);
        }

        /// <summary>
        /// Safety net: if MainScene is opened but the "RSPP Game" object is missing
        /// (for example after someone deleted it by accident), create it automatically.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameExists()
        {
            if (SceneManager.GetActiveScene().name != MainSceneName) return;
            if (Object.FindAnyObjectByType<GameBootstrap>() != null) return;
            new GameObject("RSPP Game").AddComponent<GameBootstrap>();
        }
    }
}
