using System;
using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Cases
{
    /// <summary>一条线索。</summary>
    public sealed class ClueDef
    {
        public string Id { get; }

        /// <summary>线索名与内容的本地化键（.name / .body 后缀）。</summary>
        public string Key { get; }

        /// <summary>获取途径提示键（线索板上未获得时显示去向）。</summary>
        public string HintKey { get; }

        public ClueDef(string id, string key, string hintKey)
        {
            Id = id;
            Key = key;
            HintKey = hintKey;
        }
    }

    /// <summary>两条线索组合出的推论。</summary>
    public sealed class InferenceDef
    {
        public string Id { get; }
        public string Key { get; }
        public string ClueA { get; }
        public string ClueB { get; }

        public InferenceDef(string id, string key, string clueA, string clueB)
        {
            Id = id;
            Key = key;
            ClueA = clueA;
            ClueB = clueB;
        }

        public bool Matches(string a, string b)
        {
            return (ClueA == a && ClueB == b) || (ClueA == b && ClueB == a);
        }
    }

    /// <summary>嫌疑人。</summary>
    public sealed class SuspectDef
    {
        public string Id { get; }
        public string NameKey { get; }

        public SuspectDef(string id, string nameKey)
        {
            Id = id;
            NameKey = nameKey;
        }
    }

    /// <summary>关键证人开口的门槛：智慧达标，或好感达标（二选一即可）。</summary>
    public sealed class WitnessGate
    {
        public string NpcId { get; }
        public int MinWisdom { get; }
        public int MinAffinity { get; }

        public WitnessGate(string npcId, int minWisdom, int minAffinity)
        {
            NpcId = npcId;
            MinWisdom = minWisdom;
            MinAffinity = minAffinity;
        }
    }

    /// <summary>案件静态定义。</summary>
    public sealed class CaseDef
    {
        public string Id { get; }
        public string TitleKey { get; }
        public string BriefKey { get; }
        public IReadOnlyList<ClueDef> Clues { get; }
        public IReadOnlyList<InferenceDef> Inferences { get; }
        public IReadOnlyList<SuspectDef> Suspects { get; }
        public WitnessGate Witness { get; }
        public string TrueCulpritId { get; }

        /// <summary>期限（游戏日）。时间在流逝，慢有代价（规格第七节）。</summary>
        public int DeadlineDays { get; }

        /// <summary>详审指认所需的最少线索数（含推论）。</summary>
        public int ThoroughEvidenceCount { get; }

        public CaseDef(
            string id, string titleKey, string briefKey,
            IEnumerable<ClueDef> clues, IEnumerable<InferenceDef> inferences,
            IEnumerable<SuspectDef> suspects, WitnessGate witness,
            string trueCulpritId, int deadlineDays, int thoroughEvidenceCount)
        {
            Id = id;
            TitleKey = titleKey;
            BriefKey = briefKey;
            Clues = clues.ToList();
            Inferences = inferences.ToList();
            Suspects = suspects.ToList();
            Witness = witness;
            TrueCulpritId = trueCulpritId;
            DeadlineDays = deadlineDays;
            ThoroughEvidenceCount = thoroughEvidenceCount;
        }

        public ClueDef Clue(string id)
        {
            return Clues.FirstOrDefault(c => c.Id == id);
        }
    }

    public enum CaseStatus
    {
        NotStarted = 0,
        Active = 1,

        /// <summary>已结案（对错与方式见 OutcomeKey）。</summary>
        Closed = 2,

        /// <summary>逾期未结，上官收回（失败也是内容：吃考课）。</summary>
        Expired = 3
    }

    /// <summary>结案方式（每个案子至少两种结局，规格第七节）。</summary>
    public enum AccuseMethod
    {
        /// <summary>严刑速破：快，官声升民望降，可能抓错人。</summary>
        Forced = 0,

        /// <summary>缓查详审：慢（吃游戏内时间），民望升，真相更准。</summary>
        Thorough = 1
    }

    /// <summary>指认结算。</summary>
    public sealed class AccusationOutcome
    {
        public bool CorrectCulprit { get; set; }
        public AccuseMethod Method { get; set; }
        public string OutcomeKey { get; set; }
        public int GuanShengDelta { get; set; }
        public int MinWangDelta { get; set; }

        /// <summary>计入考课的功绩点。</summary>
        public int MeritPoints { get; set; }

        /// <summary>冤案旗标（后期反转找上门的钩子；也进考课"公平可称"判定）。</summary>
        public bool WrongfulConviction { get; set; }
    }
}
