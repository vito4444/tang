using System;
using Lingyan.Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 代码构建 UI 的工厂。所有文字尺寸以设置里的基准字号（≥22px）为 1 倍，
    /// 小于 1 倍的系数不提供——中文小字难读，无障碍基线不许破。
    /// </summary>
    public static class UiKit
    {
        private static FontService _fonts;
        private static int _basePx = 22;

        private static Texture2D _inkTexture;
        private static Sprite _inkSprite;

        public static int BasePx { get { return _basePx; } }

        public static void Configure(FontService fonts, int basePx)
        {
            _fonts = fonts;
            _basePx = Mathf.Max(22, basePx);
        }

        public static int Px(float multiplier)
        {
            return Mathf.RoundToInt(_basePx * Mathf.Max(1f, multiplier));
        }

        // ---- 布局 ----

        public static RectTransform Rect(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        /// <summary>锚定中心点 + 尺寸的便捷矩形。anchor 取归一化画布坐标。</summary>
        public static RectTransform At(
            Transform parent, string name,
            float anchorX, float anchorY, float width, float height)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            var anchor = new Vector2(anchorX, anchorY);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        // ---- 元素 ----

        public static Image InkBackground(Transform parent)
        {
            RectTransform rect = Rect(parent, "InkBackground",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = InkSprite();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        public static Image PanelBox(Transform parent, string name)
        {
            RectTransform rect = Rect(parent, name,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = InkPalette.Panel;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(
            Transform parent, string name, string content,
            float sizeMul, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            _fonts?.Apply(text);
            text.text = content;
            text.fontSize = Px(sizeMul);
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        public static Button TextButton(
            Transform parent, string name, string label,
            Action onClick, float sizeMul = 1.1f, bool interactable = true)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var hitArea = go.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0.001f);
            hitArea.raycastTarget = true;

            TextMeshProUGUI text = Text(rect, "Label", label, sizeMul,
                Color.white, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);

            var button = go.AddComponent<Button>();
            button.targetGraphic = text;
            ColorBlock colors = button.colors;
            colors.normalColor = InkPalette.PaperText;
            colors.highlightedColor = InkPalette.Seal;
            colors.pressedColor = InkPalette.Faint;
            colors.selectedColor = InkPalette.Seal;
            colors.disabledColor = InkPalette.Disabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.interactable = interactable;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            return button;
        }

        public static Image Swatch(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_InputField Input(
            Transform parent, string name, string initial, int characterLimit)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var background = go.AddComponent<Image>();
            background.color = new Color(0.91f, 0.87f, 0.78f, 0.10f);
            background.raycastTarget = true;

            var input = go.AddComponent<TMP_InputField>();

            RectTransform viewport = Rect(rect, "TextArea",
                Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6));
            viewport.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI text = Text(viewport, "Text", "", 1.1f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);

            input.textViewport = viewport;
            input.textComponent = text;
            input.characterLimit = characterLimit;
            input.text = initial;
            input.caretColor = InkPalette.PaperText;
            input.customCaretColor = true;
            input.selectionColor = new Color(0.69f, 0.27f, 0.18f, 0.45f);
            return input;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ---- 程序化水墨底 ----

        private static Sprite InkSprite()
        {
            if (_inkSprite != null) { return _inkSprite; }

            const int size = 256;
            _inkTexture = new Texture2D(size, size, TextureFormat.RGB24, false);
            _inkTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                Color row = Color.Lerp(InkPalette.Void, InkPalette.VoidTop, t);
                for (int x = 0; x < size; x++)
                {
                    // 淡纸纹噪声 + 底部略沉，模拟宿墨纸面
                    float noise = (Mathf.PerlinNoise(x * 0.11f, y * 0.11f) - 0.5f) * 0.035f;
                    float vignette = 1f - 0.10f * Mathf.Abs(x / (float)size - 0.5f) * 2f;
                    var c = new Color(
                        Mathf.Clamp01(row.r * vignette + noise),
                        Mathf.Clamp01(row.g * vignette + noise),
                        Mathf.Clamp01(row.b * vignette + noise));
                    _inkTexture.SetPixel(x, y, c);
                }
            }
            _inkTexture.Apply();
            _inkSprite = Sprite.Create(_inkTexture, new UnityEngine.Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f));
            return _inkSprite;
        }
    }
}
