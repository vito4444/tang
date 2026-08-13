using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lingyan.Core.Saves
{
    /// <summary>
    /// 存档根对象，schema v1。
    /// 关键字段一律 Required：迁移或格式出错时必须响亮失败，
    /// 绝不允许"照常打开但钱是 0、官阶不对、线索没了"。
    /// </summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 3;

        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; } = CurrentVersion;

        [JsonProperty("createdUtc", Required = Required.Always)]
        public string CreatedUtc { get; set; }

        [JsonProperty("protagonist", Required = Required.Always)]
        public string ProtagonistKey { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string CharacterName { get; set; }

        /// <summary>白身专属；其余主角为 null，但字段必须存在。</summary>
        [JsonProperty("entryPath", Required = Required.AllowNull)]
        public string EntryPath { get; set; }

        [JsonProperty("attributes", Required = Required.Always)]
        public SaveAttributes Attributes { get; set; }

        [JsonProperty("offices", Required = Required.Always)]
        public SaveOffices Offices { get; set; }

        [JsonProperty("reputation", Required = Required.Always)]
        public SaveReputation Reputation { get; set; }

        [JsonProperty("reputationLedger", Required = Required.Always)]
        public List<SaveLedgerEntry> ReputationLedger { get; set; } = new List<SaveLedgerEntry>();

        [JsonProperty("moneyWen", Required = Required.Always)]
        public long MoneyWen { get; set; }

        [JsonProperty("date", Required = Required.Always)]
        public SaveDate Date { get; set; }

        /// <summary>剧情分支持久化落点（第 4 阶段起写入）。</summary>
        [JsonProperty("storyFlags", Required = Required.Always)]
        public Dictionary<string, bool> StoryFlags { get; set; } = new Dictionary<string, bool>();

        [JsonProperty("counters", Required = Required.Always)]
        public Dictionary<string, int> Counters { get; set; } = new Dictionary<string, int>();

        /// <summary>通缉值（v2 起：偷窃败露等累积，武侯缉拿的依据）。</summary>
        [JsonProperty("wantedLevel", Required = Required.Always)]
        public int WantedLevel { get; set; }

        /// <summary>NPC 社交状态（v2 起：好感账本、相识、互动旗标）。</summary>
        [JsonProperty("npcStates", Required = Required.Always)]
        public Dictionary<string, SaveNpcState> NpcStates { get; set; }
            = new Dictionary<string, SaveNpcState>();

        /// <summary>案件进度（v3 起：查了一半的案子，线索一条都不能丢）。</summary>
        [JsonProperty("cases", Required = Required.Always)]
        public Dictionary<string, SaveCaseState> Cases { get; set; }
            = new Dictionary<string, SaveCaseState>();

        /// <summary>历年考课等第（NineGrade 整数值，v3 起；迁转判定的依据）。</summary>
        [JsonProperty("kaokeGrades", Required = Required.Always)]
        public List<int> KaoKeGrades { get; set; } = new List<int>();

        /// <summary>现居宅邸（v3 起；按官品解锁，见 HousingTable）。</summary>
        [JsonProperty("housing", Required = Required.Always)]
        public string HousingId { get; set; } = "hut";

        /// <summary>已解锁的 Codex 词条 id（v3 起；遇术语自动解锁，规格第十一节）。</summary>
        [JsonProperty("codex", Required = Required.Always)]
        public List<string> CodexUnlocked { get; set; } = new List<string>();
    }

    public sealed class SaveCaseState
    {
        [JsonProperty("status", Required = Required.Always)]
        public int Status { get; set; }

        [JsonProperty("opened", Required = Required.Always)]
        public string OpenedStamp { get; set; }

        [JsonProperty("deadline", Required = Required.Always)]
        public string DeadlineStamp { get; set; }

        [JsonProperty("clues", Required = Required.Always)]
        public List<string> Clues { get; set; } = new List<string>();

        [JsonProperty("inferences", Required = Required.Always)]
        public List<string> Inferences { get; set; } = new List<string>();

        [JsonProperty("accused", Required = Required.AllowNull)]
        public string Accused { get; set; }

        [JsonProperty("outcome", Required = Required.AllowNull)]
        public string OutcomeKey { get; set; }

        [JsonProperty("wrongful", Required = Required.Always)]
        public bool WrongfulConviction { get; set; }
    }

    public sealed class SaveNpcState
    {
        [JsonProperty("met", Required = Required.Always)]
        public bool Met { get; set; }

        [JsonProperty("lastGreetDay", Required = Required.AllowNull)]
        public string LastGreetDay { get; set; }

        [JsonProperty("ledger", Required = Required.Always)]
        public List<SaveAffinityEntry> Ledger { get; set; } = new List<SaveAffinityEntry>();

        [JsonProperty("flags", Required = Required.Always)]
        public List<string> Flags { get; set; } = new List<string>();
    }

    public sealed class SaveAffinityEntry
    {
        [JsonProperty("delta", Required = Required.Always)]
        public int Delta { get; set; }

        [JsonProperty("source", Required = Required.Always)]
        public string SourceKey { get; set; }

        [JsonProperty("param", Required = Required.AllowNull)]
        public string SourceParam { get; set; }

        [JsonProperty("date", Required = Required.Always)]
        public string DateStamp { get; set; }
    }

    public sealed class SaveAttributes
    {
        [JsonProperty("stamina", Required = Required.Always)]
        public int Stamina { get; set; }

        [JsonProperty("health", Required = Required.Always)]
        public int Health { get; set; }

        [JsonProperty("strength", Required = Required.Always)]
        public int Strength { get; set; }

        [JsonProperty("wisdom", Required = Required.Always)]
        public int Wisdom { get; set; }
    }

    /// <summary>四轨并行的当前身份。</summary>
    public sealed class SaveOffices
    {
        [JsonProperty("zhishi", Required = Required.AllowNull)]
        public string ZhiShiId { get; set; }

        [JsonProperty("sanguan", Required = Required.AllowNull)]
        public string SanGuanId { get; set; }

        [JsonProperty("xunZhuan", Required = Required.Always)]
        public int XunZhuan { get; set; }

        [JsonProperty("jue", Required = Required.AllowNull)]
        public string JueId { get; set; }
    }

    public sealed class SaveReputation
    {
        [JsonProperty("guansheng", Required = Required.Always)]
        public int GuanSheng { get; set; }

        [JsonProperty("minwang", Required = Required.Always)]
        public int MinWang { get; set; }

        [JsonProperty("jianghu", Required = Required.Always)]
        public int JiangHu { get; set; }
    }

    public sealed class SaveLedgerEntry
    {
        [JsonProperty("track", Required = Required.Always)]
        public string Track { get; set; }

        [JsonProperty("delta", Required = Required.Always)]
        public int Delta { get; set; }

        [JsonProperty("source", Required = Required.Always)]
        public string SourceKey { get; set; }

        [JsonProperty("date", Required = Required.Always)]
        public string DateStamp { get; set; }
    }

    public sealed class SaveDate
    {
        [JsonProperty("era", Required = Required.Always)]
        public string EraId { get; set; }

        [JsonProperty("eraYear", Required = Required.Always)]
        public int EraYear { get; set; }

        [JsonProperty("month", Required = Required.Always)]
        public int Month { get; set; }

        [JsonProperty("day", Required = Required.Always)]
        public int Day { get; set; }

        [JsonProperty("hourIndex", Required = Required.Always)]
        public int HourIndex { get; set; }
    }
}
