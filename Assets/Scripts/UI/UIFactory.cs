using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace RSPP
{
    /// <summary>
    /// Helpers for building large, high-contrast world-space UI from code.
    /// One canvas unit is one millimetre (canvas scale 0.001), so a 900 x 520 canvas
    /// is a 0.90 m x 0.52 m panel in VR.
    /// Child positions use simple "top-left corner" coordinates: x to the right, y downwards.
    /// </summary>
    public static class UIFactory
    {
        public const float CanvasScale = 0.001f;

        /// <summary>Raised whenever any button made by this factory is clicked (used for haptics).</summary>
        public static event Action AnyButtonClicked;

        private static Font font;
        private static Sprite roundedSprite;

        public static void ResetCaches()
        {
            font = null;
            roundedSprite = null;
            circleSprite = null;
            AnyButtonClicked = null;
        }

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 48);
                return font;
            }
        }

        /// <summary>A white rounded rectangle that can be stretched (9-sliced) to any size.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite != null) return roundedSprite;

                const int size = 64;
                const int radius = 22;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "RoundedRect";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Distance from the nearest corner circle centre gives a soft rounded edge.
                        float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                        float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                        float dx = x + 0.5f - cx;
                        float dy = y + 0.5f - cy;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply();

                roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                roundedSprite.name = "RoundedRect";
                return roundedSprite;
            }
        }

        private static Sprite circleSprite;

        /// <summary>A white filled circle (used for the counters in the ten-frame pictures).</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null) return circleSprite;

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "Circle";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                var pixels = new Color32[size * size];
                float radius = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - radius;
                        float dy = y + 0.5f - radius;
                        float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply();

                circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                circleSprite.name = "Circle";
                return circleSprite;
            }
        }

        // ------------------------------------------------------------------ Canvas

        /// <summary>
        /// Creates a world-space canvas. Width and height are in canvas units (millimetres).
        /// Interactive canvases can be clicked by the right-hand ray (and by the mouse in the Editor).
        /// </summary>
        public static Canvas WorldCanvas(string name, Transform parent, Vector3 localPosition, Quaternion localRotation,
            float width, float height, Camera eventCamera, bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(width, height);
            rect.localPosition = localPosition;
            rect.localRotation = localRotation;
            rect.localScale = Vector3.one * CanvasScale;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;      // sharper text in VR
            scaler.referencePixelsPerUnit = 100f;

            if (interactive)
            {
                // Lets the XR ray interactor click this canvas.
                go.AddComponent<TrackedDeviceGraphicRaycaster>();

                // Lets a mouse click this canvas when testing in the Editor without a headset.
                if (Mouse.current != null) go.AddComponent<GraphicRaycaster>();
            }
            return canvas;
        }

        /// <summary>Rotation for a panel at <paramref name="panelPosition"/> so that it faces the player at the origin.</summary>
        public static Quaternion FacePlayer(Vector3 panelPosition)
        {
            var away = new Vector3(panelPosition.x, 0f, panelPosition.z);
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            return Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        // ------------------------------------------------------------------ Elements

        private static RectTransform NewRect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>An invisible container, used to show or hide several elements together.</summary>
        public static RectTransform Group(string name, Transform parent, float width, float height)
        {
            return NewRect(name, parent, 0f, 0f, width, height);
        }

        /// <summary>A rounded, single-color rectangle.</summary>
        public static Image Panel(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            var rect = NewRect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(string name, Transform parent, string content, float x, float y, float width, float height,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = true)
        {
            var rect = NewRect(name, parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1.05f;
            text.supportRichText = false;
            text.raycastTarget = false;

            // Shrinks long sentences so they always fit inside their box.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(24, fontSize);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        /// <summary>A big button with a text label. <paramref name="onClick"/> may be null and set later.</summary>
        public static Button MakeButton(string name, Transform parent, string label, float x, float y, float width, float height,
            Color background, Color textColor, int fontSize, Action onClick)
        {
            var rect = NewRect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = background;
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // Color tint multiplies the background color: slightly darker on hover, darker on press.
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.pressedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Label("Label", rect, label, 12f, 6f, width - 24f, height - 12f, fontSize, textColor);

            button.onClick.AddListener(() =>
            {
                if (AnyButtonClicked != null) AnyButtonClicked();
                if (onClick != null) onClick();
            });
            return button;
        }

        /// <summary>Makes a label follow the chosen language (see Loc). Returns the label.</summary>
        public static Text Localize(Text text, string key, params object[] args)
        {
            LocalizedText localized = text.GetComponent<LocalizedText>();
            if (localized == null) localized = text.gameObject.AddComponent<LocalizedText>();
            localized.Set(key, args);
            return text;
        }

        /// <summary>Makes a button's label follow the chosen language.</summary>
        public static void LocalizeButton(Button button, string key, params object[] args)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) Localize(text, key, args);
        }

        public static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
        }

        public static void SetButtonColor(Button button, Color color)
        {
            var image = button.targetGraphic as Image;
            if (image != null) image.color = color;
        }
    }
}
