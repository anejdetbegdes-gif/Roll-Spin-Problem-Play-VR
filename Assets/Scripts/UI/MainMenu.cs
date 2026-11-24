using System;
using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// The small in-game menu to the RIGHT of the board (used with the right-hand ray):
    ///   Back to Menu (asks "Leave the game?" first), Music on/off, Sound on/off.
    /// Start, Instructions and the language choice are on the entrance screen (EntranceScreen).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        /// <summary>Raised when the player confirms "Leave the game? Yes".</summary>
        public event Action BackToMenuConfirmed;

        /// <summary>Raised when the music button is pressed. The argument is the new state (true = on).</summary>
        public event Action<bool> MusicToggled;

        /// <summary>Raised when the sound button is pressed. The argument is the new state (true = on).</summary>
        public event Action<bool> SoundToggled;

        private const float Width = 520f;
        private const float Height = 500f;

        private GameObject menuPage;
        private GameObject confirmPage;
        private Button musicButton;
        private Button soundButton;
        private bool musicOn = true;
        private bool soundOn = true;

        public static MainMenu Create(Transform playArea, Camera eventCamera)
        {
            Vector3 position = GameConfig.MenuPanelPosition;
            Canvas canvas = UIFactory.WorldCanvas("Menu", playArea, position, UIFactory.FacePlayer(position),
                Width, Height, eventCamera, true);

            var menu = canvas.gameObject.AddComponent<MainMenu>();
            menu.Build(canvas.transform);
            return menu;
        }

        private void Build(Transform root)
        {
            UIFactory.Panel("Background", root, 0f, 0f, Width, Height, GameConfig.PanelColor);
            UIFactory.Panel("Header", root, 0f, 0f, Width, 90f, GameConfig.PanelHeaderColor);
            UIFactory.Localize(UIFactory.Label("Title", root, "", 20f, 8f, Width - 40f, 74f, 48, GameConfig.TextLight), "menu_title");

            const float x = 50f;
            const float w = Width - 100f;
            const float h = 96f;

            // ---- Page 1: the three menu buttons
            RectTransform menuRect = UIFactory.Group("Menu Page", root, Width, Height);
            menuPage = menuRect.gameObject;

            Button back = UIFactory.MakeButton("Back to Menu", menuRect, "", x, 120f, w, h, GameConfig.ButtonOrange, GameConfig.TextLight, 46,
                () => ShowConfirm(true));
            UIFactory.LocalizeButton(back, "back_to_menu");

            musicButton = UIFactory.MakeButton("Music", menuRect, "", x, 236f, w, h, GameConfig.ButtonBlue, GameConfig.TextLight, 46, ToggleMusic);
            UIFactory.LocalizeButton(musicButton, "music_on");

            soundButton = UIFactory.MakeButton("Sound", menuRect, "", x, 352f, w, h, GameConfig.ButtonGrey, GameConfig.TextLight, 46, ToggleSound);
            UIFactory.LocalizeButton(soundButton, "sound_on");

            // ---- Page 2: "Leave the game?" so a child cannot lose the game by accident
            RectTransform confirmRect = UIFactory.Group("Confirm Page", root, Width, Height);
            confirmPage = confirmRect.gameObject;

            UIFactory.Localize(UIFactory.Label("Question", confirmRect, "", 30f, 130f, Width - 60f, 120f, 56, GameConfig.TextDark), "leave_q");
            Button yes = UIFactory.MakeButton("Yes", confirmRect, "", 40f, 290f, 200f, 110f, GameConfig.ButtonRed, GameConfig.TextLight, 50, () =>
            {
                ShowConfirm(false);
                if (BackToMenuConfirmed != null) BackToMenuConfirmed();
            });
            UIFactory.LocalizeButton(yes, "yes");
            Button no = UIFactory.MakeButton("No", confirmRect, "", Width - 240f, 290f, 200f, 110f, GameConfig.ButtonGreen, GameConfig.TextLight, 50,
                () => ShowConfirm(false));
            UIFactory.LocalizeButton(no, "no");

            ShowConfirm(false);
        }

        private void ShowConfirm(bool show)
        {
            menuPage.SetActive(!show);
            confirmPage.SetActive(show);
        }

        private void ToggleMusic()
        {
            musicOn = !musicOn;
            UIFactory.LocalizeButton(musicButton, musicOn ? "music_on" : "music_off");
            if (MusicToggled != null) MusicToggled(musicOn);
        }

        private void ToggleSound()
        {
            soundOn = !soundOn;
            UIFactory.LocalizeButton(soundButton, soundOn ? "sound_on" : "sound_off");
            if (SoundToggled != null) SoundToggled(soundOn);
        }
    }
}
