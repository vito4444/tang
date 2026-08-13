using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lingyan.Core.Terminology
{
    /// <summary>
    /// 术语词条。官职（category = "office"）英译锁定
    /// Charles O. Hucker, A Dictionary of Official Titles in Imperial China (1985)。
    /// Verified = false 表示译名尚未对照原书逐字复核（见 docs/GLOSSARY.md 的复核流程），
    /// 但一致性校验照常生效——全局只此一个来源。
    /// </summary>
    public sealed class GlossaryEntry
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("zh", Required = Required.Always)]
        public string Zh { get; set; }

        [JsonProperty("en", Required = Required.Always)]
        public string En { get; set; }

        /// <summary>office / institution / exam / rite / architecture / military / system / currency。</summary>
        [JsonProperty("category", Required = Required.Always)]
        public string Category { get; set; }

        /// <summary>官职词条是否已对照 Hucker (1985) 原书逐字核对。</summary>
        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }
    }

    public sealed class Glossary
    {
        private readonly Dictionary<string, GlossaryEntry> _byId =
            new Dictionary<string, GlossaryEntry>(StringComparer.Ordinal);

        private readonly Dictionary<string, GlossaryEntry> _byZh =
            new Dictionary<string, GlossaryEntry>(StringComparer.Ordinal);

        public IReadOnlyCollection<GlossaryEntry> Entries { get { return _byId.Values; } }

        public static Glossary Parse(string json)
        {
            var entries = JsonConvert.DeserializeObject<List<GlossaryEntry>>(json);
            if (entries == null) { throw new FormatException("术语表 JSON 解析为空"); }
            var glossary = new Glossary();
            foreach (GlossaryEntry entry in entries)
            {
                if (glossary._byId.ContainsKey(entry.Id))
                {
                    throw new FormatException("术语表 id 重复: " + entry.Id);
                }
                if (glossary._byZh.ContainsKey(entry.Zh))
                {
                    throw new FormatException("术语表中文名重复: " + entry.Zh);
                }
                glossary._byId[entry.Id] = entry;
                glossary._byZh[entry.Zh] = entry;
            }
            return glossary;
        }

        public GlossaryEntry ById(string id)
        {
            return id != null && _byId.TryGetValue(id, out var e) ? e : null;
        }

        public GlossaryEntry ByZh(string zh)
        {
            return zh != null && _byZh.TryGetValue(zh, out var e) ? e : null;
        }

        /// <summary>官职中文名 → 锁定英译；无词条返回 null（调用侧必须视为错误）。</summary>
        public string EnFor(string zh)
        {
            GlossaryEntry entry = ByZh(zh);
            return entry == null ? null : entry.En;
        }
    }
}
