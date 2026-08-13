namespace Lingyan.Core.Cases
{
    /// <summary>
    /// 第二案：枯井藏骨（中期案，接于丝帛案结案且有官身之后）。
    /// 案情：淘井人于井底得骸骨与一枚鎏金带銙。十一年前坊正王某"携款潜逃"，
    /// 旧牍却与淘井簿对不上——人根本没出坊。
    /// 真凶：赌坊主——坊正上门讨赌债，争执失手害命，伪报潜逃。
    /// 传闻池 rumor.well_tale（井有旧事）自阶段 3 起已埋线。
    /// </summary>
    public static class WellCase
    {
        public const string CaseId = "well_case";

        public static readonly CaseDef Def = new CaseDef(
            CaseId,
            "case.well.title",
            "case.well.brief",
            new[]
            {
                new ClueDef("bone_belt", "case.well.clue.bone_belt", "case.well.clue.bone_belt.hint"),
                new ClueDef("well_ledger", "case.well.clue.well_ledger", "case.well.clue.well_ledger.hint"),
                new ClueDef("missing_roll", "case.well.clue.missing_roll", "case.well.clue.missing_roll.hint"),
                new ClueDef("old_neighbor", "case.well.clue.old_neighbor", "case.well.clue.old_neighbor.hint")
            },
            new[]
            {
                // 带銙刻「王」× 坊正失踪旧牍——骸骨身份对上了
                new InferenceDef("identity_match", "case.well.inf.identity_match",
                    "bone_belt", "missing_roll"),
                // 淘井簿（上次淘井十二年前）× 失踪旧牍（十一年前）——尸在井中，人没出坊，"潜逃"是伪报
                new InferenceDef("no_flight", "case.well.inf.no_flight",
                    "well_ledger", "missing_roll"),
                // 老邻证言（失踪前夜井边争执提赌债）× 带銙——争执当夜即井边，为债起衅
                new InferenceDef("debt_quarrel", "case.well.inf.debt_quarrel",
                    "old_neighbor", "bone_belt")
            },
            new[]
            {
                new SuspectDef("gambler_boss", "case.well.suspect.gambler_boss"),
                new SuspectDef("widow", "case.well.suspect.widow"),
                new SuspectDef("well_wright", "case.well.suspect.well_wright")
            },
            // 老邻嘴紧：旧案翻出怕报复，须智高盘问或交情深（桓夫子牵线）
            witness: new WitnessGate("huan_fuzi", minWisdom: 12, minAffinity: 50),
            trueCulpritId: "gambler_boss",
            deadlineDays: 15,
            thoroughEvidenceCount: 5);
    }
}
