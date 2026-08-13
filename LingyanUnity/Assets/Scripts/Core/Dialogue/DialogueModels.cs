using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lingyan.Core.Dialogue
{
    /// <summary>选项条件类型。</summary>
    public enum ConditionKind
    {
        /// <summary>与本 NPC 好感 ≥ 值。</summary>
        MinAffinity = 0,

        MinGuanSheng = 1,
        MinMinWang = 2,
        MinJiangHu = 3,

        /// <summary>剧情旗标已立。</summary>
        FlagSet = 4,

        /// <summary>剧情旗标未立。</summary>
        FlagNotSet = 5,

        /// <summary>时辰区间 [From, To)（环形，宵禁夜话之类）。</summary>
        HourBetween = 6,

        /// <summary>囊中钱 ≥ 值（文）。</summary>
        MinMoneyWen = 7
    }

    /// <summary>选项效果类型。</summary>
    public enum EffectKind
    {
        /// <summary>本 NPC 好感变动（入账本）。</summary>
        Affinity = 0,

        GuanSheng = 1,
        MinWang = 2,
        JiangHu = 3,

        /// <summary>立剧情旗标（分支后果持久化，后续剧情读得到）。</summary>
        SetFlag = 4,

        /// <summary>钱变动（文，可负）。</summary>
        MoneyWen = 5,

        Wanted = 6
    }

    public sealed class DialogueCondition
    {
        [JsonProperty("kind", Required = Required.Always)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ConditionKind Kind { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("value2")]
        public int Value2 { get; set; }

        [JsonProperty("flag")]
        public string Flag { get; set; }
    }

    public sealed class DialogueEffect
    {
        [JsonProperty("kind", Required = Required.Always)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public EffectKind Kind { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("flag")]
        public string Flag { get; set; }

        /// <summary>入账来源键（好感/名誉账本用；缺省给通用键）。</summary>
        [JsonProperty("source")]
        public string SourceKey { get; set; }
    }

    public sealed class DialogueChoice
    {
        [JsonProperty("text", Required = Required.Always)]
        public string TextKey { get; set; }

        [JsonProperty("conditions")]
        public List<DialogueCondition> Conditions { get; set; } = new List<DialogueCondition>();

        [JsonProperty("effects")]
        public List<DialogueEffect> Effects { get; set; } = new List<DialogueEffect>();

        /// <summary>下一节点 id；null = 对话结束。</summary>
        [JsonProperty("goto")]
        public string GotoId { get; set; }
    }

    public sealed class DialogueLine
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        /// <summary>台词文本键。</summary>
        [JsonProperty("text", Required = Required.Always)]
        public string TextKey { get; set; }

        [JsonProperty("choices")]
        public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
    }

    /// <summary>一棵对话树（一名 NPC 一个入口）。</summary>
    public sealed class DialogueTree
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("npc", Required = Required.Always)]
        public string NpcId { get; set; }

        [JsonProperty("entry", Required = Required.Always)]
        public string EntryId { get; set; }

        [JsonProperty("lines", Required = Required.Always)]
        public List<DialogueLine> Lines { get; set; } = new List<DialogueLine>();

        [JsonIgnore]
        private Dictionary<string, DialogueLine> _byId;

        public DialogueLine Line(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, DialogueLine>(StringComparer.Ordinal);
                foreach (DialogueLine line in Lines)
                {
                    _byId[line.Id] = line;
                }
            }
            return id != null && _byId.TryGetValue(id, out DialogueLine found) ? found : null;
        }

        public static DialogueTree Parse(string json)
        {
            DialogueTree tree = JsonConvert.DeserializeObject<DialogueTree>(json);
            if (tree == null)
            {
                throw new FormatException("对话树 JSON 解析为空");
            }
            return tree;
        }
    }
}
