using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Officials;

namespace Lingyan.Core.Marriage
{
    /// <summary>
    /// 六礼（规格第九节）：纳采（雁礼）→ 问名 → 纳吉 → 纳征（聘财）→ 请期 → 亲迎（却扇）。
    /// </summary>
    public enum RiteStep
    {
        NaCai = 0,
        WenMing = 1,
        NaJi = 2,
        NaZheng = 3,
        QingQi = 4,
        QinYing = 5
    }

    public static class RiteSteps
    {
        public static readonly IReadOnlyList<(RiteStep step, string key)> All = new[]
        {
            (RiteStep.NaCai, "rite.step.nacai"),
            (RiteStep.WenMing, "rite.step.wenming"),
            (RiteStep.NaJi, "rite.step.naji"),
            (RiteStep.NaZheng, "rite.step.nazheng"),
            (RiteStep.QingQi, "rite.step.qingqi"),
            (RiteStep.QinYing, "rite.step.qinying")
        };
    }

    /// <summary>
    /// 门第（士庶不婚；五姓七望门第极高）。
    /// 数值越大门第越高：庶人 0，寒门 1，官户 2，士族 3，五姓七望 4。
    /// </summary>
    public enum ClanRank
    {
        Commoner = 0,
        ModestFamily = 1,
        OfficialFamily = 2,
        Gentry = 3,
        FiveSurnames = 4
    }

    /// <summary>可议亲对象的静态档案。</summary>
    public sealed class MatchDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public string ClanKey { get; }
        public ClanRank Clan { get; }

        /// <summary>结亲最低好感。</summary>
        public int MinAffinity { get; }

        /// <summary>门第对求亲者的官品要求（null = 不问官身）。</summary>
        public RankGrade? MinGrade { get; }

        /// <summary>聘财（文）。</summary>
        public long BetrothalWen { get; }

        public MatchDef(
            string id, string nameKey, string clanKey, ClanRank clan,
            int minAffinity, RankGrade? minGrade, long betrothalWen)
        {
            Id = id;
            NameKey = nameKey;
            ClanKey = clanKey;
            Clan = clan;
            MinAffinity = minAffinity;
            MinGrade = minGrade;
            BetrothalWen = betrothalWen;
        }
    }

    /// <summary>议亲判定结果（一次说清所有不许，不挤牙膏）。</summary>
    public sealed class ProposalCheck
    {
        public bool Allowed { get { return Reasons.Count == 0; } }

        /// <summary>不许的理由键列表（好感/官品/聘财/婚约）。</summary>
        public List<string> Reasons { get; } = new List<string>();
    }

    /// <summary>
    /// 议亲规则：成亲条件 = 好感 ≥ 阈值 AND 官品 ≥ 对方门第要求
    /// AND 聘财 ≥ 数额 AND 无婚约（规格第九节原文）。
    /// 门第是身份门槛：钱与情皆到而门第不及，照样不许——这正是唐婚的味道。
    /// </summary>
    public static class MarriageService
    {
        public static ProposalCheck CheckProposal(
            MatchDef match, int affinity, RankGrade? sanGuanGrade,
            long purseWen, bool betrothed)
        {
            var check = new ProposalCheck();
            if (betrothed)
            {
                check.Reasons.Add("marriage.deny.betrothed");
            }
            if (affinity < match.MinAffinity)
            {
                check.Reasons.Add("marriage.deny.affinity");
            }
            if (match.MinGrade != null
                && (sanGuanGrade == null
                    || !sanGuanGrade.Value.AtLeast(match.MinGrade.Value)))
            {
                check.Reasons.Add("marriage.deny.rank");
            }
            if (purseWen < match.BetrothalWen)
            {
                check.Reasons.Add("marriage.deny.betrothal");
            }
            return check;
        }

        /// <summary>推进一步六礼；纳征时扣聘财。返回该步文本键。</summary>
        public static string AdvanceRite(
            Saves.SaveData save, string matchId, MatchDef match)
        {
            Saves.SaveMarriageState state = save.Marriage;
            state.MatchId = matchId;
            var (step, key) = RiteSteps.All[state.RiteStep];
            if (step == RiteStep.NaZheng)
            {
                Social.OutcomeApplier.ApplyMoney(save, -match.BetrothalWen);
            }
            state.RiteStep++;
            if (state.RiteStep >= RiteSteps.All.Count)
            {
                state.Married = true;
            }
            return key;
        }
    }

    /// <summary>阶段 10 首位可议亲者：坊中绢行掌事之女（官户，示范全链）。</summary>
    public static class MatchCatalog
    {
        public static readonly IReadOnlyList<MatchDef> All = new[]
        {
            new MatchDef(
                "silk_daughter", "match.silk_daughter", "match.silk_daughter.clan",
                ClanRank.OfficialFamily,
                minAffinity: 60,
                minGrade: RankGrade.CongLower(9),   // 入流即可议
                betrothalWen: 80_000),
            new MatchDef(
                "cui_lady", "match.cui_lady", "match.cui_lady.clan",
                ClanRank.FiveSurnames,
                minAffinity: 75,
                minGrade: RankGrade.CongUpper(5),   // 五姓七望：五品以上方敢遣媒
                betrothalWen: 600_000)
        };

        public static MatchDef Get(string id)
        {
            return All.FirstOrDefault(m => m.Id == id);
        }
    }
}
