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

        /// <summary>全局按钮点击回声（拨弦音等），TextButton 一律先走它再走业务回调。</summary>
        public static Action OnButtonClick;

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

        /// <summary>面板：淡纸蒙层 + 外裱边 + 内衬线（唐卷轴裱边的克制暗示）。</summary>
        public static Image PanelBox(Transform parent, string name)
        {
            RectTransform rect = Rect(parent, name,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = InkPalette.Panel;
            image.raycastTarget = false;
            Frame(rect, "FrameOuter", InkPalette.Faint, 0.25f, 0f);
            Frame(rect, "FrameInner", InkPalette.Faint, 0.10f, 6f);
            return image;
        }

        /// <summary>四条 1px 边线组成的框。</summary>
        public static void Frame(RectTransform parent, string name, Color color, float alpha, float inset)
        {
            var c = new Color(color.r, color.g, color.b, alpha);
            EdgeLine(parent, name + "_T",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(inset, -inset - 1), new Vector2(-inset, -inset), c);
            EdgeLine(parent, name + "_B",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(inset, inset), new Vector2(-inset, inset + 1), c);
            EdgeLine(parent, name + "_L",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(inset, inset), new Vector2(inset + 1, -inset), c);
            EdgeLine(parent, name + "_R",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-inset - 1, inset), new Vector2(-inset, -inset), c);
        }

        private static void EdgeLine(
            RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            RectTransform rect = Rect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>横向细线 + 中央菱形分隔（题字、小节标题之下用）。</summary>
        public static void Hairline(Transform parent, string name,
            float anchorX, float anchorY, float width, float alpha = 0.27f)
        {
            RectTransform line = At(parent, name, anchorX, anchorY, width, 1);
            var lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(InkPalette.Faint.r, InkPalette.Faint.g,
                InkPalette.Faint.b, alpha);
            lineImage.raycastTarget = false;

            RectTransform diamond = At(parent, name + "_Diamond", anchorX, anchorY, 7, 7);
            diamond.localRotation = Quaternion.Euler(0, 0, 45);
            var diamondImage = diamond.gameObject.AddComponent<Image>();
            diamondImage.color = new Color(InkPalette.Faint.r, InkPalette.Faint.g,
                InkPalette.Faint.b, Mathf.Clamp01(alpha + 0.12f));
            diamondImage.raycastTarget = false;
        }

        /// <summary>十二格数值条：格数 + 实心/空心双编码（不单靠颜色）。</summary>
        public static void Cells(Transform parent, string name,
            float anchorX, float anchorY, int value, int max = 12)
        {
            const float cellWidth = 14f;
            const float gap = 10f;
            float total = max * cellWidth + (max - 1) * gap;
            RectTransform row = At(parent, name, anchorX, anchorY, total, 20);
            for (int i = 0; i < max; i++)
            {
                var cell = Rect(row, "Cell" + i,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
                cell.sizeDelta = new Vector2(cellWidth, 18);
                cell.anchoredPosition = new Vector2(i * (cellWidth + gap) + cellWidth / 2, 0);
                var image = cell.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                if (i < value)
                {
                    image.color = new Color(InkPalette.PaperText.r, InkPalette.PaperText.g,
                        InkPalette.PaperText.b, 0.75f);
                }
                else
                {
                    image.color = new Color(InkPalette.Faint.r, InkPalette.Faint.g,
                        InkPalette.Faint.b, 0.22f);
                }
            }
        }

        /// <summary>名声进度条：细边框 + 朱砂填充（0–1）。</summary>
        public static void Bar(Transform parent, string name,
            float anchorX, float anchorY, float width, float value01)
        {
            RectTransform box = At(parent, name, anchorX, anchorY, width, 12);
            Frame(box, "Rim", InkPalette.Faint, 0.30f, 0f);
            float w = Mathf.Max(0f, Mathf.Min(1f, value01)) * (width - 4);
            var fill = Rect(box, "Fill",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            fill.sizeDelta = new Vector2(w, 8);
            fill.anchoredPosition = new Vector2(2 + w / 2, 0);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(InkPalette.Seal.r, InkPalette.Seal.g,
                InkPalette.Seal.b, 0.70f);
            fillImage.raycastTarget = false;
        }

        public static TextMeshProUGUI Text(
            Transform parent, string name, string content,
            float sizeMul, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            // 文字节点必须填满父容器：RectTransform 默认 100×100，
            // 不拉伸的话所有中文都会在 100px 宽里逐字断行（首轮引擎截图的竖排事故）。
            Stretch(rect);
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
            Stretch(rect); // 根节点同样要填满容器，否则按钮实际只有默认 100px 宽

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
                button.onClick.AddListener(() =>
                {
                    OnButtonClick?.Invoke();
                    onClick();
                });
            }
            if (interactable)
            {
                var brackets = go.AddComponent<HoverBrackets>();
                brackets.Bind(text, button);
            }
            return button;
        }

        public static Image Swatch(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
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
            Stretch(rect);

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
        // 与 tools/render_mockups.py 的 make_background 同构：
        // 宿墨渐变 + 三层平滑远山淡影（软脊线、向下渐隐）+ 纸纹颗粒 + 晕影。
        // 层参数与色值两边同值，改必同步。

        private static Sprite InkSprite()
        {
            if (_inkSprite != null) { return _inkSprite; }

            const int w = 960;
            const int h = 540;
            _inkTexture = new Texture2D(w, h, TextureFormat.RGB24, false);
            _inkTexture.wrapMode = TextureWrapMode.Clamp;

            float[] bases = { 0.680f, 0.765f, 0.845f };
            float[] roughs = { 0.20f, 0.30f, 0.44f };
            float[] deltas = { 6f / 255f, 4f / 255f, 2.2f / 255f };
            float[] seeds = { 11.7f, 23.3f, 37.9f };

            // 每层山脊线（自顶向下的像素高度），双八度 Perlin 天然圆缓
            var ridges = new float[3][];
            for (int layer = 0; layer < 3; layer++)
            {
                var ridge = new float[w];
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)w;
                    float noise =
                        Mathf.PerlinNoise(fx * 3.1f + seeds[layer], seeds[layer]) * 0.72f
                        + Mathf.PerlinNoise(fx * 9.7f + seeds[layer] * 2f, seeds[layer] + 5f) * 0.28f;
                    ridge[x] = (bases[layer] + (noise - 0.5f) * roughs[layer]) * h;
                }
                ridges[layer] = ridge;
            }

            var rng = new System.Random(7);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                // 纹理 y=0 在底部；fromTop 与设计稿的图像坐标对齐
                float fromTop = h - 1 - y;
                float t = fromTop / (h - 1);
                Color row = Color.Lerp(InkPalette.VoidTop, InkPalette.Void, t);

                for (int x = 0; x < w; x++)
                {
                    float r = row.r;
                    float g = row.g;
                    float b = row.b;

                    for (int layer = 0; layer < 3; layer++)
                    {
                        float below = fromTop - ridges[layer][x];
                        if (below <= 0f) { continue; }
                        float rise = below < 12f ? below / 12f : 1f;      // 软脊线（墨晕）
                        float fall = Mathf.Clamp01(below / (h * 0.55f));  // 山体向下渐隐
                        float strength = rise * (1f - fall * 0.85f);
                        float delta = deltas[layer] * strength;
                        r += delta;
                        g += delta;
                        b += delta;
                    }

                    // 纸纹颗粒
                    float grain = ((float)rng.NextDouble() - 0.5f) * (4.4f / 255f);
                    // 晕影
                    float dx = (x - w * 0.5f) / (w * 0.72f);
                    float dy = (fromTop - h * 0.46f) / (h * 0.72f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float vignette = 1f - Mathf.Clamp01(dist - 0.55f) * 0.22f;

                    pixels[y * w + x] = new Color(
                        Mathf.Clamp01((r + grain) * vignette),
                        Mathf.Clamp01((g + grain) * vignette),
                        Mathf.Clamp01((b + grain) * vignette));
                }
            }

            _inkTexture.SetPixels(pixels);
            _inkTexture.Apply();
            _inkSprite = Sprite.Create(_inkTexture, new UnityEngine.Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f));
            return _inkSprite;
        }
    }
}
