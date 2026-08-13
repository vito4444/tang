using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Officials
{
    /// <summary>贬谪结果。</summary>
    public sealed class DemotionResult
    {
        public bool Demoted { get; set; }

        /// <summary>触因文本键（通缉滔天 / 考课连殿）。</summary>
        public string ReasonKey { get; set; }

        public OfficeDef NewOffice { get; set; }
    }

    /// <summary>
    /// 贬谪而非 Game Over（规格第十一节）：犯错被贬，剧情继续，另有翻身线。
    /// 触发：通缉值 ≥ 30，或最近两考皆在中下及以下。
    /// 后果：职事官沿本线降两阶（保底最低阶）、散官随新职回落、
    /// 通缉清零（既已问罪）、立旗标 demoted_lingnan 供翻身线剧情读取。
    /// </summary>
    public static class DemotionService
    {
        public const int WantedThreshold = 30;

        public static string ShouldDemote(SaveData save)
        {
            if (save.Offices.ZhiShiId == null)
            {
                return null; // 白身无官可贬（另有刑责线，归内容阶段）
            }
            if (save.WantedLevel >= WantedThreshold)
            {
                return "demotion.reason.wanted";
            }
            List<NineGrade> grades = save.KaoKeGrades
                .Select(g => (NineGrade)g).ToList();
            if (grades.Count >= 2
                && grades[grades.Count - 1] <= NineGrade.ZhongXia
                && grades[grades.Count - 2] <= NineGrade.ZhongXia)
            {
                return "demotion.reason.grades";
            }
            return null;
        }

        public static DemotionResult Apply(SaveData save, string reasonKey)
        {
            var result = new DemotionResult { ReasonKey = reasonKey };
            OfficeDef current = OfficialLadders.Get(save.Offices.ZhiShiId);
            if (current == null)
            {
                return result;
            }

            IReadOnlyList<OfficeDef> ladder =
                current.Line == CareerLine.Military
                    ? OfficialLadders.Military
                    : OfficialLadders.Civil;
            int newIndex = current.LadderIndex - 2;
            if (newIndex < 0) { newIndex = 0; }
            OfficeDef demotedTo = ladder[newIndex];

            save.Offices.ZhiShiId = demotedTo.Id;
            if (demotedTo.Grade != null)
            {
                SanGuanDef sanguan = SanGuanTable.InitialFor(
                    demotedTo.Grade.Value, civil: current.Line != CareerLine.Military);
                save.Offices.SanGuanId = sanguan.Id;
            }
            save.WantedLevel = 0;
            save.StoryFlags["demoted_lingnan"] = true;

            result.Demoted = true;
            result.NewOffice = demotedTo;
            return result;
        }
    }
}
