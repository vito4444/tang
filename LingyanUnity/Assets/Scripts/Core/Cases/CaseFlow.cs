using System.Collections.Generic;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Cases
{
    /// <summary>
    /// 案件流转：按序解锁，一次只办一案。
    /// 丝帛案开卷即可接（听过传闻）；枯井案须前案已具结且有官身（旧案卷宗非白身可调）。
    /// </summary>
    public static class CaseFlow
    {
        /// <summary>全部案件，按剧情顺序。</summary>
        public static readonly IReadOnlyList<CaseDef> All = new[]
        {
            SilkCase.Def,
            WellCase.Def
        };

        /// <summary>某案是否已具结（指认过人，无论对错）。</summary>
        public static bool IsClosed(SaveData save, CaseDef def)
        {
            return save.Cases.TryGetValue(def.Id, out SaveCaseState state)
                && state.Accused != null;
        }

        /// <summary>
        /// 当前应呈现的案件：第一件未具结的案。全部具结则返回最后一件（供回看结语）。
        /// </summary>
        public static CaseDef Current(SaveData save)
        {
            foreach (CaseDef def in All)
            {
                if (!IsClosed(save, def)) { return def; }
            }
            return All[All.Count - 1];
        }

        /// <summary>当前案的接案门槛是否已达（已接过的案恒可进线索板）。</summary>
        public static bool CanOpen(SaveData save, CaseDef def)
        {
            if (save.Cases.ContainsKey(def.Id)) { return true; }
            if (def.Id == SilkCase.CaseId)
            {
                return save.StoryFlags.TryGetValue("heard_silk_case", out bool heard) && heard;
            }
            if (def.Id == WellCase.CaseId)
            {
                return IsClosed(save, SilkCase.Def) && save.Offices.ZhiShiId != null;
            }
            return false;
        }
    }
}
