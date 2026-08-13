using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Officials
{
    /// <summary>封爵一等。</summary>
    public sealed class JueDef
    {
        public string Id { get; }
        public string Zh { get; }

        /// <summary>术语表词条 id。</summary>
        public string GlossaryId { get; }

        public RankGrade Grade { get; }

        /// <summary>等第：1 = 亲王（最高），9 = 开国县男。</summary>
        public int Tier { get; }

        public JueDef(string id, string zh, string glossaryId, RankGrade grade, int tier)
        {
            Id = id;
            Zh = zh;
            GlossaryId = glossaryId;
            Grade = grade;
            Tier = tier;
        }
    }

    /// <summary>唐爵九等。罕见，重大结局奖励；影响结亲门第。</summary>
    public static class JueTable
    {
        public static readonly IReadOnlyList<JueDef> Tiers = new[]
        {
            new JueDef("qin_wang", "亲王", "qin_wang", RankGrade.Zheng(1), 1),
            new JueDef("si_wang", "嗣王", "si_wang", RankGrade.Cong(1), 2),
            new JueDef("jun_wang", "郡王", "jun_wang", RankGrade.Cong(1), 2),
            new JueDef("guo_gong", "国公", "guo_gong", RankGrade.Cong(1), 3),
            new JueDef("kaiguo_jungong", "开国郡公", "kaiguo_jungong", RankGrade.Zheng(2), 4),
            new JueDef("kaiguo_xiangong", "开国县公", "kaiguo_xiangong", RankGrade.Cong(2), 5),
            new JueDef("kaiguo_xianhou", "开国县侯", "kaiguo_xianhou", RankGrade.Cong(3), 6),
            new JueDef("kaiguo_xianbo", "开国县伯", "kaiguo_xianbo", RankGrade.ZhengUpper(4), 7),
            new JueDef("kaiguo_xianzi", "开国县子", "kaiguo_xianzi", RankGrade.ZhengUpper(5), 8),
            new JueDef("kaiguo_xiannan", "开国县男", "kaiguo_xiannan", RankGrade.CongUpper(5), 9)
        };

        private static readonly Dictionary<string, JueDef> ById = Tiers.ToDictionary(j => j.Id);

        public static JueDef Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var def) ? def : null;
        }
    }
}
