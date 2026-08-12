using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Localization
{
    public enum Locale
    {
        ZhHans = 0,
        En = 1
    }

    public static class Locales
    {
        public static string Code(Locale locale)
        {
            return locale == Locale.En ? "en" : "zh-Hans";
        }

        public static Locale FromCode(string code)
        {
            return code == "en" ? Locale.En : Locale.ZhHans;
        }
    }

    /// <summary>
    /// 双语字符串目录。数据源为 JSON：{ "key": { "zh": "…", "en": "…" }, … }。
    /// 缺键不抛异常：返回 "⟦key⟧" 哨兵并记录，让缺失在画面上可见、在日志里可查，
    /// 而不是静默吞掉。
    /// </summary>
    public sealed class LocalizationCatalog
    {
        private readonly Dictionary<string, string[]> _entries =
            new Dictionary<string, string[]>(StringComparer.Ordinal);

        private readonly HashSet<string> _missing = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> MissingKeys { get { return _missing; } }

        public int Count { get { return _entries.Count; } }

        public IEnumerable<string> Keys { get { return _entries.Keys; } }

        public static LocalizationCatalog Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("本地化 JSON 为空", nameof(json));
            }
            var catalog = new LocalizationCatalog();
            JObject root = JObject.Parse(json);
            foreach (var pair in root)
            {
                if (!(pair.Value is JObject entry))
                {
                    throw new FormatException("本地化条目须为对象: " + pair.Key);
                }
                string zh = entry.Value<string>("zh");
                string en = entry.Value<string>("en");
                catalog._entries[pair.Key] = new[] { zh, en };
            }
            return catalog;
        }

        public void Add(string key, string zh, string en)
        {
            _entries[key] = new[] { zh, en };
        }

        public bool Has(string key) { return _entries.ContainsKey(key); }

        public bool TryGet(string key, Locale locale, out string value)
        {
            if (_entries.TryGetValue(key, out var pair))
            {
                value = locale == Locale.En ? pair[1] : pair[0];
                return value != null;
            }
            value = null;
            return false;
        }

        public string Get(string key, Locale locale)
        {
            if (TryGet(key, locale, out string value))
            {
                return value;
            }
            _missing.Add(key);
            return "\u27e6" + key + "\u27e7";
        }

        public string Format(string key, Locale locale, params object[] args)
        {
            return string.Format(Get(key, locale), args);
        }

        /// <summary>两语并齐检查：任一语言缺失或为空即为违规。</summary>
        public List<string> FindParityViolations()
        {
            var violations = new List<string>();
            foreach (var pair in _entries)
            {
                if (string.IsNullOrWhiteSpace(pair.Value[0]))
                {
                    violations.Add(pair.Key + ": zh 缺失");
                }
                if (string.IsNullOrWhiteSpace(pair.Value[1]))
                {
                    violations.Add(pair.Key + ": en 缺失");
                }
            }
            return violations;
        }
    }
}
