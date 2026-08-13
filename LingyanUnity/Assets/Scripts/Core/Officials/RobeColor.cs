namespace Lingyan.Core.Officials
{
    /// <summary>
    /// 章服之制（本作 660–705 年窗口，按规格取四档）：
    /// 三品以上紫，四五品绯，六七品绿，八九品青；无品白衣。
    /// </summary>
    public enum RobeColor
    {
        /// <summary>白衣（庶人、未入流）。</summary>
        Commoner = 0,

        /// <summary>青袍（八、九品）。</summary>
        Qing = 1,

        /// <summary>绿袍（六、七品）。</summary>
        Green = 2,

        /// <summary>绯袍（四、五品）。</summary>
        Scarlet = 3,

        /// <summary>紫袍（三品以上）。</summary>
        Purple = 4
    }

    public static class RobeColors
    {
        /// <summary>服色由散官品阶决定；无散官即白衣。</summary>
        public static RobeColor FromGrade(RankGrade? grade)
        {
            if (grade == null)
            {
                return RobeColor.Commoner;
            }
            int band = grade.Value.Band;
            if (band <= 3) { return RobeColor.Purple; }
            if (band <= 5) { return RobeColor.Scarlet; }
            if (band <= 7) { return RobeColor.Green; }
            return RobeColor.Qing;
        }

        public static string ZhName(RobeColor color)
        {
            switch (color)
            {
                case RobeColor.Purple: return "紫";
                case RobeColor.Scarlet: return "绯";
                case RobeColor.Green: return "绿";
                case RobeColor.Qing: return "青";
                default: return "白";
            }
        }

        public static string EnName(RobeColor color)
        {
            switch (color)
            {
                case RobeColor.Purple: return "Purple";
                case RobeColor.Scarlet: return "Scarlet";
                case RobeColor.Green: return "Green";
                case RobeColor.Qing: return "Cyan-blue";
                default: return "White (commoner)";
            }
        }

        /// <summary>
        /// UI 展示用取色（设计值，非文物色卡实测；正式美术阶段再校色）。
        /// 返回 "RRGGBB"。
        /// </summary>
        public static string UiHex(RobeColor color)
        {
            switch (color)
            {
                case RobeColor.Purple: return "5B3A78";
                case RobeColor.Scarlet: return "A63A2B";
                case RobeColor.Green: return "4A6B3C";
                case RobeColor.Qing: return "33566B";
                default: return "E8E2D5";
            }
        }
    }
}
