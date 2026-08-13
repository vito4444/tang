using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;
using Lingyan.Core.Officials;

namespace Lingyan.Core.Saves
{
    /// <summary>
    /// 语义校验：字段都在，还要值都对。
    /// 断言真正的契约（引用得解析、区间得成立），不是"能反序列化"。
    /// </summary>
    public static class SaveValidator
    {
        public static void Validate(SaveData data)
        {
            if (data.SchemaVersion != SaveData.CurrentVersion)
            {
                throw new SaveCorruptException("校验时版本不是当前版本: " + data.SchemaVersion);
            }

            if (ProtagonistCatalog.GetByKey(data.ProtagonistKey) == null)
            {
                throw new SaveCorruptException("未知主角: " + data.ProtagonistKey);
            }

            if (string.IsNullOrWhiteSpace(data.CharacterName))
            {
                throw new SaveCorruptException("角色名为空");
            }

            ValidateAttribute(data.Attributes.Stamina, "体力");
            ValidateAttribute(data.Attributes.Health, "生命");
            ValidateAttribute(data.Attributes.Strength, "力量");
            ValidateAttribute(data.Attributes.Wisdom, "智慧");

            if (data.MoneyWen < 0)
            {
                throw new SaveCorruptException("钱为负: " + data.MoneyWen);
            }

            if (data.Offices.ZhiShiId != null && OfficialLadders.Get(data.Offices.ZhiShiId) == null)
            {
                throw new SaveCorruptException("未知职事官: " + data.Offices.ZhiShiId);
            }
            if (data.Offices.SanGuanId != null && SanGuanTable.Get(data.Offices.SanGuanId) == null)
            {
                throw new SaveCorruptException("未知散官: " + data.Offices.SanGuanId);
            }
            if (data.Offices.XunZhuan < 0 || data.Offices.XunZhuan > 12)
            {
                throw new SaveCorruptException("勋转出界: " + data.Offices.XunZhuan);
            }
            if (data.Offices.JueId != null && JueTable.Get(data.Offices.JueId) == null)
            {
                throw new SaveCorruptException("未知封爵: " + data.Offices.JueId);
            }

            ValidateReputation(data.Reputation.GuanSheng, "官声");
            ValidateReputation(data.Reputation.MinWang, "民望");
            ValidateReputation(data.Reputation.JiangHu, "江湖名望");

            if (data.WantedLevel < 0)
            {
                throw new SaveCorruptException("通缉值为负: " + data.WantedLevel);
            }

            if (Economy.HousingTable.Get(data.HousingId) == null)
            {
                throw new SaveCorruptException("未知宅邸: " + data.HousingId);
            }

            foreach (var pair in data.Inventory)
            {
                if (Social.GiftCatalog.Get(pair.Key) == null)
                {
                    throw new SaveCorruptException("行囊里有未知物品: " + pair.Key);
                }
                if (pair.Value < 1)
                {
                    throw new SaveCorruptException(
                        "行囊件数非法: " + pair.Key + "=" + pair.Value);
                }
            }

            if (EraTable.Get(data.Date.EraId) == null)
            {
                throw new SaveCorruptException("未知年号: " + data.Date.EraId);
            }
            if (data.Date.EraYear < 1
                || data.Date.Month < 1 || data.Date.Month > TangDate.MonthsPerYear
                || data.Date.Day < 1 || data.Date.Day > TangDate.DaysPerMonth
                || data.Date.HourIndex < 0 || data.Date.HourIndex > 11)
            {
                throw new SaveCorruptException("日期出界: " + data.Date.EraId + " "
                    + data.Date.EraYear + "/" + data.Date.Month + "/" + data.Date.Day
                    + " h" + data.Date.HourIndex);
            }
        }

        private static void ValidateAttribute(int value, string label)
        {
            if (value < 1 || value > 20)
            {
                throw new SaveCorruptException("属性出界: " + label + "=" + value);
            }
        }

        private static void ValidateReputation(int value, string label)
        {
            if (value < 0 || value > 100)
            {
                throw new SaveCorruptException("名誉出界: " + label + "=" + value);
            }
        }
    }
}
