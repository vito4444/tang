using System;
using Lingyan.Core.Localization;
using Lingyan.Core.Terminology;
using UnityEngine;

namespace Lingyan.Game.Services
{
    /// <summary>双语目录 + 术语表的运行时载体。语言切换即时广播，界面重建取词。</summary>
    public sealed class LocalizationService
    {
        public LocalizationCatalog Catalog { get; private set; }
        public Glossary Glossary { get; private set; }
        public Locale Locale { get; private set; } = Locale.ZhHans;

        public event Action LocaleChanged;

        public void Load()
        {
            TextAsset strings = Resources.Load<TextAsset>("Data/strings");
            if (strings == null)
            {
                throw new InvalidOperationException("Resources/Data/strings.json 缺失，双语系统无法启动");
            }
            Catalog = LocalizationCatalog.Parse(strings.text);

            TextAsset glossary = Resources.Load<TextAsset>("Data/glossary");
            if (glossary == null)
            {
                throw new InvalidOperationException("Resources/Data/glossary.json 缺失，术语表无法启动");
            }
            Glossary = Glossary.Parse(glossary.text);
        }

        public void SetLocale(Locale locale)
        {
            if (Locale == locale) { return; }
            Locale = locale;
            LocaleChanged?.Invoke();
        }

        /// <summary>取词。缺键返回 ⟦key⟧ 哨兵（可见、可查日志）。</summary>
        public string Tr(string key)
        {
            return Catalog.Get(key, Locale);
        }

        public string TrF(string key, params object[] args)
        {
            return Catalog.Format(key, Locale, args);
        }

        /// <summary>官职双语名："县尉 District Defender" 风格由调用方拼装。</summary>
        public string OfficeEn(string zh)
        {
            string en = Glossary.EnFor(zh);
            if (en == null)
            {
                Debug.LogError("[Lingyan] 官职缺锁定英译: " + zh);
                return "\u27e6" + zh + "\u27e7";
            }
            return en;
        }
    }
}
