using System;

namespace Lingyan.Core.Officials
{
    /// <summary>考课九等，值越大等第越高。</summary>
    public enum NineGrade
    {
        XiaXia = 0,
        XiaZhong = 1,
        XiaShang = 2,
        ZhongXia = 3,
        ZhongZhong = 4,
        ZhongShang = 5,
        ShangXia = 6,
        ShangZhong = 7,
        ShangShang = 8
    }

    /// <summary>四善（《唐六典》）：德义有闻、清慎明著、公平可称、恪勤匪懈。</summary>
    [Flags]
    public enum SiShan
    {
        None = 0,
        DeYiYouWen = 1,
        QingShenMingZhu = 2,
        GongPingKeCheng = 4,
        KeQinFeiXie = 8
    }

    /// <summary>考课中触发下三等的劣迹。</summary>
    public enum Misconduct
    {
        None = 0,

        /// <summary>爱憎任情，处断乖理 → 下上。</summary>
        AiZengRenQing = 1,

        /// <summary>背公向私，职务废阙 → 下中。</summary>
        BeiGongXiangSi = 2,

        /// <summary>居官谄诈，贪浊有状 → 下下。</summary>
        TanZhuoYouZhuang = 3
    }

    /// <summary>年度考课的输入。规格：考课分 = f(功绩, 名誉, 官声, 案件/军功完成度)。</summary>
    public sealed class KaoKeInput
    {
        /// <summary>年度功绩点（破案、军功等累积）。</summary>
        public int MeritPoints { get; set; }

        /// <summary>案件/军功完成度，0–1。</summary>
        public double CompletionRatio { get; set; }

        public int GuanSheng { get; set; }

        public int MinWang { get; set; }

        /// <summary>本年是否酿成冤案。</summary>
        public bool WrongfulConviction { get; set; }

        /// <summary>本年是否受贿事发。</summary>
        public bool BriberyExposed { get; set; }

        public Misconduct Misconduct { get; set; }
    }

    public sealed class KaoKeResult
    {
        public NineGrade Grade { get; set; }
        public SiShan Shan { get; set; }
        public bool HasZui { get; set; }
        public int ShanCount { get; set; }
    }

    /// <summary>
    /// 考课评定。善、最的判据由游戏量化指标推导，
    /// 九等映射依《唐六典》：一最四善为上上；一最三善、无最四善为上中；
    /// 一最二善、无最三善为上下；一最一善、无最二善为中上；
    /// 一最、无最一善为中中；善最弗闻为中下；下三等以劣迹论。
    /// </summary>
    public static class KaoKeService
    {
        /// <summary>年度功绩达到此数视为得"最"（居官最优之考语）。</summary>
        public const int ZuiMeritThreshold = 30;

        public static SiShan DeriveShan(KaoKeInput input)
        {
            SiShan shan = SiShan.None;
            if (input.MinWang >= 70) { shan |= SiShan.DeYiYouWen; }
            if (input.GuanSheng >= 60 && !input.BriberyExposed) { shan |= SiShan.QingShenMingZhu; }
            if (!input.WrongfulConviction && input.MinWang >= 50) { shan |= SiShan.GongPingKeCheng; }
            if (input.CompletionRatio >= 0.8) { shan |= SiShan.KeQinFeiXie; }
            return shan;
        }

        public static int CountShan(SiShan shan)
        {
            int n = 0;
            if ((shan & SiShan.DeYiYouWen) != 0) { n++; }
            if ((shan & SiShan.QingShenMingZhu) != 0) { n++; }
            if ((shan & SiShan.GongPingKeCheng) != 0) { n++; }
            if ((shan & SiShan.KeQinFeiXie) != 0) { n++; }
            return n;
        }

        public static KaoKeResult Evaluate(KaoKeInput input)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            var result = new KaoKeResult();

            if (input.Misconduct != Misconduct.None)
            {
                result.Grade =
                    input.Misconduct == Misconduct.TanZhuoYouZhuang ? NineGrade.XiaXia :
                    input.Misconduct == Misconduct.BeiGongXiangSi ? NineGrade.XiaZhong :
                    NineGrade.XiaShang;
                return result;
            }

            SiShan shan = DeriveShan(input);
            int s = CountShan(shan);
            bool zui = input.MeritPoints >= ZuiMeritThreshold;

            result.Shan = shan;
            result.ShanCount = s;
            result.HasZui = zui;
            result.Grade = MapGrade(zui, s);
            return result;
        }

        /// <summary>(最, 善数) → 九等。公开供数据驱动测试逐行核对。</summary>
        public static NineGrade MapGrade(bool zui, int shanCount)
        {
            if (shanCount < 0) { shanCount = 0; }
            if (shanCount > 4) { shanCount = 4; }
            if (zui)
            {
                switch (shanCount)
                {
                    case 4: return NineGrade.ShangShang;
                    case 3: return NineGrade.ShangZhong;
                    case 2: return NineGrade.ShangXia;
                    case 1: return NineGrade.ZhongShang;
                    default: return NineGrade.ZhongZhong;
                }
            }
            switch (shanCount)
            {
                case 4: return NineGrade.ShangZhong;
                case 3: return NineGrade.ShangXia;
                case 2: return NineGrade.ZhongShang;
                case 1: return NineGrade.ZhongZhong;
                default: return NineGrade.ZhongXia;
            }
        }

        private static readonly string[] GradeZhNames =
        {
            "下下", "下中", "下上", "中下", "中中", "中上", "上下", "上中", "上上"
        };

        public static string GradeZh(NineGrade grade) { return GradeZhNames[(int)grade]; }

        private static readonly string[] GradeEnNames =
        {
            "Lower-lower", "Lower-middle", "Lower-upper",
            "Middle-lower", "Middle-middle", "Middle-upper",
            "Upper-lower", "Upper-middle", "Upper-upper"
        };

        public static string GradeEn(NineGrade grade) { return GradeEnNames[(int)grade]; }
    }
}
