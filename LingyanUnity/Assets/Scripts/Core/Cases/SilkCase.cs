namespace Lingyan.Core.Cases
{
    /// <summary>
    /// 首案：丝帛血案（明镜线开卷案，坊内传闻与桓夫子对话已埋线）。
    /// 案情：商队在城南折货折人。三名嫌疑：同行的行商、坊内游手、商队账房。
    /// 真凶：账房——路引与巡夜记录对不上，账目亏空杀人灭口。
    /// </summary>
    public static class SilkCase
    {
        public const string CaseId = "silk_case";

        public static readonly CaseDef Def = new CaseDef(
            CaseId,
            "case.silk.title",
            "case.silk.brief",
            new[]
            {
                new ClueDef("dossier", "case.silk.clue.dossier", "case.silk.clue.dossier.hint"),
                new ClueDef("permit", "case.silk.clue.permit", "case.silk.clue.permit.hint"),
                new ClueDef("patrol_log", "case.silk.clue.patrol_log", "case.silk.clue.patrol_log.hint"),
                new ClueDef("torn_silk", "case.silk.clue.torn_silk", "case.silk.clue.torn_silk.hint")
            },
            new[]
            {
                // 路引（申时出城）× 巡夜记录（戌时方过南门）——两个时辰对不上
                new InferenceDef("time_gap", "case.silk.inf.time_gap", "permit", "patrol_log"),
                // 卷宗（货值三百匹）× 撕裂的绢帛（掺了次绢）——账实不符
                new InferenceDef("ledger_gap", "case.silk.inf.ledger_gap", "dossier", "torn_silk")
            },
            new[]
            {
                new SuspectDef("merchant_peer", "case.silk.suspect.merchant_peer"),
                new SuspectDef("idler", "case.silk.suspect.idler"),
                new SuspectDef("bookkeeper", "case.silk.suspect.bookkeeper")
            },
            witness: new WitnessGate("kang_san", minWisdom: 10, minAffinity: 55),
            trueCulpritId: "bookkeeper",
            deadlineDays: 10,
            thoroughEvidenceCount: 4);
    }
}
