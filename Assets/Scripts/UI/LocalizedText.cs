using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// Keeps a fixed label in the chosen language: it re-reads its sentence from Loc
    /// whenever it is shown and whenever the language changes.
    /// Add it with UIFactory.Localize(text, "key").
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string Key;
        public object[] Args;

        private Text text;

        public void Set(string key, params object[] args)
        {
            Key = key;
            Args = args;
            Refresh();
        }

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
        }

        public void Refresh()
        {
            if (text == null) text = GetComponent<Text>();
            if (text == null || string.IsNullOrEmpty(Key)) return;
            text.text = Args == null || Args.Length == 0 ? Loc.T(Key) : Loc.F(Key, Args);
        }
    }
}
