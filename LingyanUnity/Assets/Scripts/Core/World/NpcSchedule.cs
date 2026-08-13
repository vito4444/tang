using System;
using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Reputation;

namespace Lingyan.Core.World
{
    /// <summary>
    /// 作息条目：自 FromHour（含）至 ToHour（不含）在某地做某事。
    /// 时段按十二时辰环形计（子=0…亥=11），允许跨子夜（如 戌→寅）。
    /// </summary>
    public sealed class ScheduleEntry
    {
        public int FromHour { get; }
        public int ToHour { get; }
        public string PlaceId { get; }

        /// <summary>活动名本地化键（摆摊、巡夜、课徒……）。</summary>
        public string ActivityKey { get; }

        public ScheduleEntry(int fromHour, int toHour, string placeId, string activityKey)
        {
            if (fromHour < 0 || fromHour > 11) { throw new ArgumentOutOfRangeException(nameof(fromHour)); }
            if (toHour < 0 || toHour > 11) { throw new ArgumentOutOfRangeException(nameof(toHour)); }
            if (fromHour == toHour) { throw new ArgumentException("空时段或全日时段须拆写", nameof(toHour)); }
            FromHour = fromHour;
            ToHour = toHour;
            PlaceId = placeId;
            ActivityKey = activityKey;
        }

        /// <summary>环形判含：from &lt;= h &lt; to（mod 12）。</summary>
        public bool Covers(int hourIndex)
        {
            if (FromHour < ToHour)
            {
                return hourIndex >= FromHour && hourIndex < ToHour;
            }
            return hourIndex >= FromHour || hourIndex < ToHour;
        }

        /// <summary>时段长度（时辰数）。</summary>
        public int Length
        {
            get { return (ToHour - FromHour + 12) % 12; }
        }
    }

    /// <summary>一名 NPC 的全日作息。白天摆摊、傍晚收摊、夜间归家，皆由此表驱动。</summary>
    public sealed class NpcScheduleDef
    {
        public string NpcId { get; }

        /// <summary>NPC 名本地化键。</summary>
        public string NameKey { get; }

        /// <summary>身份群体：决定三轨名誉在其眼中的权重（阶段 3 好感系统复用）。</summary>
        public NpcArchetype Archetype { get; }

        public IReadOnlyList<ScheduleEntry> Entries { get; }

        public NpcScheduleDef(
            string npcId, string nameKey, NpcArchetype archetype,
            IEnumerable<ScheduleEntry> entries)
        {
            NpcId = npcId;
            NameKey = nameKey;
            Archetype = archetype;
            Entries = entries.ToList();
            string error = Validate();
            if (error != null)
            {
                throw new ArgumentException("作息表非法（" + npcId + "）: " + error);
            }
        }

        /// <summary>十二时辰必须恰好全覆盖：无空档（NPC 凭空消失）、无重叠（分身两处）。</summary>
        private string Validate()
        {
            if (Entries.Count == 0) { return "作息为空"; }
            int totalLength = Entries.Sum(e => e.Length);
            if (totalLength != 12)
            {
                return "时段总长 " + totalLength + " ≠ 12";
            }
            for (int h = 0; h < 12; h++)
            {
                int hits = Entries.Count(e => e.Covers(h));
                if (hits != 1)
                {
                    return "时辰 " + Calendar.ShiChenTable.All[h].Zh + " 被覆盖 " + hits + " 次";
                }
            }
            return null;
        }

        /// <summary>此刻在哪、做什么。作息全覆盖，永有答案。</summary>
        public ScheduleEntry At(int hourIndex)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Covers(hourIndex)) { return Entries[i]; }
            }
            throw new InvalidOperationException("作息覆盖校验失效");
        }
    }
}
