using System;
using System.Collections.Generic;
using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;

namespace Lingyan.Core.Saves
{
    /// <summary>从角色创建草稿生成新档。</summary>
    public static class SaveFactory
    {
        /// <summary>开局日期：垂拱四年三月十七，巳时（688 年）。</summary>
        public static TangDate DefaultStart()
        {
            return new TangDate("chuigong", 4, 3, 17, 5);
        }

        public static SaveData NewGame(CharacterDraft draft, DateTime utcNow)
        {
            string error = CharacterCreationRules.ValidateFinal(draft);
            if (error != null)
            {
                throw new InvalidOperationException("草稿未通过校验: " + error);
            }

            ProtagonistDef def = draft.Def;
            TangDate start = DefaultStart();

            return new SaveData
            {
                SchemaVersion = SaveData.CurrentVersion,
                CreatedUtc = utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                ProtagonistKey = def.Key,
                CharacterName = draft.Name.Trim(),
                EntryPath = draft.Def.Id == ProtagonistId.BaiShen
                    ? draft.EntryPath.ToString()
                    : null,
                Attributes = new SaveAttributes
                {
                    Stamina = draft.Attributes.Stamina,
                    Health = draft.Attributes.Health,
                    Strength = draft.Attributes.Strength,
                    Wisdom = draft.Attributes.Wisdom
                },
                Offices = new SaveOffices
                {
                    ZhiShiId = null,
                    SanGuanId = null,
                    XunZhuan = 0,
                    JueId = null
                },
                Reputation = new SaveReputation
                {
                    GuanSheng = def.StartGuanSheng,
                    MinWang = def.StartMinWang,
                    JiangHu = def.StartJiangHu
                },
                ReputationLedger = new List<SaveLedgerEntry>(),
                MoneyWen = def.StartMoneyWen,
                Date = new SaveDate
                {
                    EraId = start.EraId,
                    EraYear = start.EraYear,
                    Month = start.Month,
                    Day = start.Day,
                    HourIndex = start.HourIndex
                },
                StoryFlags = new Dictionary<string, bool>(),
                Counters = new Dictionary<string, int>(),
                WantedLevel = 0,
                NpcStates = new Dictionary<string, SaveNpcState>(),
                Cases = new Dictionary<string, SaveCaseState>(),
                KaoKeGrades = new List<int>(),
                HousingId = "hut",
                CodexUnlocked = new List<string>
                {
                    // 开卷即识的常识词条
                    "fang", "xiaojin", "guan_currency", "wen_currency"
                }
            };
        }
    }
}
