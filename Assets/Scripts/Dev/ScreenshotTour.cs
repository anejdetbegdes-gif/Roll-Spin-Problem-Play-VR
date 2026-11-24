#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// EDITOR ONLY. Plays one game by itself in desktop mode and saves README screenshots to Docs/:
    /// the entrance hallway, the classroom, a wrong answer with the ten-frame picture, Robo's suggestion,
    /// and the FINISH celebration with the star jar.
    /// Started by the menu "RSPP > Capture README Screenshots" or in batch mode with
    ///   Unity.exe -batchmode -projectPath . -executeMethod RSPP.EditorTools.RSPPScreenshotTour.Run
    /// It is not part of the Quest build (the whole file is inside #if UNITY_EDITOR).
    /// </summary>
    public class ScreenshotTour : MonoBehaviour
    {
        public const string SessionKey = "RSPP_ScreenshotTour";
        private const float TimeoutSeconds = 300f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (!UnityEditor.SessionState.GetBool(SessionKey, false)) return;
            UnityEditor.SessionState.SetBool(SessionKey, false);
            var go = new GameObject("Screenshot Tour");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenshotTour>();
        }

        private GameManager game;
        private Camera cam;
        private string outDir;

        private IEnumerator Start()
        {
            outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs"));
            Directory.CreateDirectory(outDir);
            float started = Time.realtimeSinceStartup;

            yield return new WaitForSecondsRealtime(3f);
            game = FindAnyObjectByType<GameManager>();
            cam = Camera.main;
            if (game == null || cam == null) { Finish("game or camera not found"); yield break; }

            // 1. Entrance hallway.
            yield return Shot(0f, 4f, "screenshot_entrance.png");

            // 2. Enter the classroom (fade), then the classroom overview.
            Click("Enter");
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot(-8f, 24f, "screenshot_classroom.png");

            // 3. Play the game automatically and capture the interesting moments.
            bool gotWrongAnswer = false, gotRobo = false;
            Time.timeScale = 3f;
            Dice dice = FindAnyObjectByType<Dice>();
            WheelHandle handle = FindAnyObjectByType<WheelHandle>();
            AIHelperRobot robot = FindAnyObjectByType<AIHelperRobot>();

            while (game.State != GameState.Finished)
            {
                if (Time.realtimeSinceStartup - started > TimeoutSeconds) { Finish("timeout"); yield break; }

                switch (game.State)
                {
                    case GameState.WaitingForRoll:
                        dice.RollFromDesktop();
                        break;
                    case GameState.WaitingForSpin:
                        handle.PullFromDesktop();
                        break;
                    case GameState.Question:
                        yield return new WaitForSecondsRealtime(0.3f);
                        MathQuestion q = game.CurrentQuestion;
                        if (q == null) break;
                        if (!gotWrongAnswer && q.Category != QuestionCategory.OddEven)
                        {
                            // One wrong answer to show the ten-frame picture.
                            Click("Answer " + ((q.CorrectIndex + 1) % q.Choices.Length));
                            Time.timeScale = 1f;
                            yield return new WaitForSecondsRealtime(1f);
                            yield return Shot(0f, 10f, "screenshot_wrong_answer.png");
                            Time.timeScale = 3f;
                            gotWrongAnswer = true;
                        }
                        else
                        {
                            Click("Answer " + q.CorrectIndex);
                        }
                        break;
                    case GameState.HelperAsk:
                        robot.TouchFromDesktop();
                        break;
                    case GameState.HelperCheck:
                        yield return new WaitForSecondsRealtime(0.3f);
                        if (!gotRobo)
                        {
                            Time.timeScale = 1f;
                            yield return new WaitForSecondsRealtime(0.5f);
                            yield return Shot(-34f, 16f, "screenshot_robo.png");
                            Time.timeScale = 3f;
                            gotRobo = true;
                        }
                        HelperSuggestion s = game.CurrentSuggestion;
                        Click(s != null && s.IsCorrect ? "Correct Button" : "Incorrect Button");
                        break;
                    case GameState.Feedback:
                        yield return new WaitForSecondsRealtime(0.4f);
                        Click("Next Button");
                        break;
                }
                yield return new WaitForSecondsRealtime(0.3f);
            }

            // 4. FINISH: confetti and the stars leaving the jar.
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(7f);
            yield return Shot(0f, 6f, "screenshot_finish.png");
            Finish("done");
        }

        /// <summary>Presses the active UI button with this name (like a click with the ray or the mouse).</summary>
        private static void Click(string buttonName)
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (b.name == buttonName && b.isActiveAndEnabled && b.interactable)
                {
                    b.onClick.Invoke();
                    return;
                }
            }
            Debug.LogWarning("[RSPP Tour] Button not found: " + buttonName);
        }

        /// <summary>Turns the head (yaw = left/right, pitch = down) and saves a 1920 x 1080 PNG.</summary>
        private IEnumerator Shot(float yaw, float pitch, string fileName)
        {
            cam.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            yield return new WaitForEndOfFrame();

            var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 };
            RenderTexture previous = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            cam.targetTexture = null;
            RenderTexture.active = previous;
            rt.Release();

            string path = Path.Combine(outDir, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("[RSPP Tour] Saved " + path);
        }

        private static void Finish(string reason)
        {
            Time.timeScale = 1f;
            Debug.Log("[RSPP Tour] Finished: " + reason);
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
            else UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
