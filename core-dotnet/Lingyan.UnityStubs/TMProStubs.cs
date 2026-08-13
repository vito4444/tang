// TMPro API 桩，仅供编译检查。
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft = 257,
        Top = 258,
        TopRight = 260,
        MidlineLeft = 4097,
        Center = 514,
        Midline = 4098,
        MidlineRight = 4100,
        Left = 513,
        Right = 516
    }

    public class TMP_FontAsset : Object
    {
        public static TMP_FontAsset CreateFontAsset(Font font) { return null; }
    }

    public abstract class TMP_Text : MaskableGraphic
    {
        public string text { get; set; }
        public float fontSize { get; set; }
        public TMP_FontAsset font { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public float characterSpacing { get; set; }
        public float lineSpacing { get; set; }
    }

    public class TextMeshProUGUI : TMP_Text { }

    public class TMP_InputField : Selectable
    {
        public class OnChangeEvent : UnityEvent<string> { }
        public class SubmitEvent : UnityEvent<string> { }

        public RectTransform textViewport { get; set; }
        public TMP_Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public int characterLimit { get; set; }
        public string text { get; set; }
        public Color caretColor { get; set; }
        public bool customCaretColor { get; set; }
        public Color selectionColor { get; set; }
        public OnChangeEvent onValueChanged { get { return new OnChangeEvent(); } }
        public SubmitEvent onEndEdit { get { return new SubmitEvent(); } }
    }
}
