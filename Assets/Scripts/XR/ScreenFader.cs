using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// Fades the view to black and back, used when entering or leaving the classroom.
    /// The player never moves through the world (that could make children feel sick);
    /// the scene simply changes while the screen is dark.
    /// A black panel sits just in front of the camera, so it works in the headset and on the desktop.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private const float FadeTime = 0.35f;

        private Image black;
        private bool busy;

        public static ScreenFader Create(Camera head)
        {
            Canvas canvas = UIFactory.WorldCanvas("Screen Fader", head.transform, new Vector3(0f, 0f, 0.1f), Quaternion.identity,
                2000f, 2000f, null, false);
            canvas.sortingOrder = 1000;     // drawn over everything else

            var fader = canvas.gameObject.AddComponent<ScreenFader>();
            fader.black = UIFactory.Panel("Black", canvas.transform, 0f, 0f, 2000f, 2000f, new Color(0f, 0f, 0f, 0f));
            fader.black.sprite = null;      // plain rectangle
            fader.black.enabled = false;
            return fader;
        }

        /// <summary>Fades to black, runs <paramref name="whileDark"/>, then fades back in.</summary>
        public void FadeAndDo(Action whileDark)
        {
            if (busy) return;
            StartCoroutine(FadeRoutine(whileDark));
        }

        private IEnumerator FadeRoutine(Action whileDark)
        {
            busy = true;
            black.enabled = true;
            yield return Fade(0f, 1f);
            if (whileDark != null) whileDark();
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Fade(1f, 0f);
            black.enabled = false;
            busy = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / FadeTime);
                black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));
                yield return null;
            }
        }
    }
}
