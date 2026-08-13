using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 水墨基调设计色板（占位美术，第 9 阶段正式落地画面方向）。
    /// 深墨底、宣纸字、朱砂点缀；好恶另有 ✔/✘ 符号双编码，不单靠颜色。
    /// </summary>
    public static class InkPalette
    {
        /// <summary>底色：近黑的暖墨。</summary>
        public static readonly Color Void = Hex("14120F");

        /// <summary>渐变顶部略暖。与 tools/render_mockups.py 的 PALETTE 同值，改必同步。</summary>
        public static readonly Color VoidTop = Hex("221D16");

        /// <summary>印面纸色（印章白文、点缀）。</summary>
        public static readonly Color SealPaper = Hex("EDE4D2");

        /// <summary>主文字：宣纸色。</summary>
        public static readonly Color PaperText = Hex("D9CFBA");

        /// <summary>次要文字。</summary>
        public static readonly Color Faint = Hex("8F846D");

        /// <summary>朱砂（印章、强调、悬停）。</summary>
        public static readonly Color Seal = Hex("B0442F");

        /// <summary>面板底（极淡的纸色蒙层）。</summary>
        public static readonly Color Panel = new Color(0.91f, 0.87f, 0.78f, 0.05f);

        /// <summary>禁用。</summary>
        public static readonly Color Disabled = Hex("5C554A");

        /// <summary>增益 ✔。</summary>
        public static readonly Color Good = Hex("7A9E71");

        /// <summary>减损 ✘。</summary>
        public static readonly Color Bad = Hex("A8524A");

        public static Color Hex(string rrggbb)
        {
            return new Color(
                int.Parse(rrggbb.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                int.Parse(rrggbb.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                int.Parse(rrggbb.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f);
        }
    }
}
