using UnityEditor;
using UnityEditor.SceneManagement;

namespace RSPP.EditorTools
{
    /// <summary>
    /// EDITOR ONLY. Menu "RSPP > Capture README Screenshots": opens MainScene, presses Play, and lets
    /// RSPP.ScreenshotTour play one game by itself and save PNG screenshots into the Docs folder.
    /// </summary>
    public static class RSPPScreenshotTour
    {
        [MenuItem("RSPP/Capture README Screenshots")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            SessionState.SetBool(RSPP.ScreenshotTour.SessionKey, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
