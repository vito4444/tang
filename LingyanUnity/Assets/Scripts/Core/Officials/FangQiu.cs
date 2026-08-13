using Lingyan.Core.Calendar;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Officials
{
    /// <summary>一次防秋出征的结果。</summary>
    public sealed class FangQiuResult
    {
        public bool Went { get; set; }

        /// <summary>战报/驳文文本键。</summary>
        public string TextKey { get; set; }

        /// <summary>本次得转数（勋官轨）。</summary>
        public int ZhuanGained { get; set; }

        public int MeritGained { get; set; }

        public int MinWangDelta { get; set; }

        public int JiangHuDelta { get; set; }
    }

    /// <summary>
    /// 防秋点兵（武线中期）：唐制秋防——吐蕃、突厥趁秋高马肥犯边，
    /// 诸军岁发兵防秋。军线职事官秋季（七至九月）可应点出征，岁一次：
    /// - 体力 + 力量 ≥ 14：先登陷阵——勋加二转、功绩 +2、民望 +1、江湖 +2；
    /// - 不足：随军效力——勋加一转、江湖 +1。
    /// 役期一月（历期整推三十日），勋转封顶十二转（上柱国）。
    /// 勋官轨由此激活：书房四轨里的"勋"不再恒零。
    /// </summary>
    public static class FangQiuService
    {
        public const int VanguardThreshold = 14;

        public const int MaxZhuan = 12;

        /// <summary>秋季：七、八、九月。</summary>
        public static bool IsAutumn(int month)
        {
            return month >= 7 && month <= 9;
        }

        /// <summary>驳文键；可出征返回 null。</summary>
        public static string RefusalKey(SaveData save, int year)
        {
            OfficeDef office = OfficialLadders.Get(save.Offices.ZhiShiId);
            if (office == null || office.Line != CareerLine.Military)
            {
                return "fangqiu.refuse.not_military";
            }
            if (!IsAutumn(save.Date.Month))
            {
                return "fangqiu.refuse.not_autumn";
            }
            if (save.Counters.TryGetValue("fangqiu_year", out int last) && last >= year)
            {
                return "fangqiu.refuse.already";
            }
            return null;
        }

        /// <summary>应点出征：属性定档、勋转入档、历期整推三十日。</summary>
        public static FangQiuResult Go(SaveData save, int year)
        {
            var result = new FangQiuResult();
            string refusal = RefusalKey(save, year);
            if (refusal != null)
            {
                result.TextKey = refusal;
                return result;
            }

            bool vanguard =
                save.Attributes.Stamina + save.Attributes.Strength >= VanguardThreshold;
            result.Went = true;
            result.ZhuanGained = vanguard ? 2 : 1;
            result.MeritGained = vanguard ? 2 : 0;
            result.MinWangDelta = vanguard ? 1 : 0;
            result.JiangHuDelta = vanguard ? 2 : 1;
            result.TextKey = vanguard ? "fangqiu.result.vanguard" : "fangqiu.result.served";

            int zhuan = save.Offices.XunZhuan + result.ZhuanGained;
            save.Offices.XunZhuan = zhuan > MaxZhuan ? MaxZhuan : zhuan;

            save.Counters.TryGetValue("merit_points", out int merit);
            save.Counters["merit_points"] = merit + result.MeritGained;
            save.Counters["fangqiu_year"] = year;

            // 役期一月
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            date.AdvanceDays(30);
            save.Date.EraId = date.EraId;
            save.Date.EraYear = date.EraYear;
            save.Date.Month = date.Month;
            save.Date.Day = date.Day;
            save.Date.HourIndex = date.HourIndex;

            return result;
        }
    }
}
