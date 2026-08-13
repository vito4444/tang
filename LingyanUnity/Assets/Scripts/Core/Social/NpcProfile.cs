using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Social
{
    /// <summary>礼物类别（送礼要投其所好，送错反而减分）。</summary>
    public enum GiftTaste
    {
        Book = 0,       // 书籍
        WineFood = 1,   // 酒食
        Jade = 2,       // 玉器
        Silk = 3,       // 绢帛
        Exotic = 4      // 胡货
    }

    /// <summary>NPC 性格旗标（傲慢者受辱可触发隐藏支线，规格第六节）。</summary>
    [System.Flags]
    public enum Personality
    {
        None = 0,
        Proud = 1,      // 傲慢
        Timid = 2,      // 怯懦
        Shrewd = 4      // 精明
    }

    /// <summary>
    /// NPC 社交档案：与作息表（NpcScheduleDef）同键合用。
    /// 好感基准、性格、礼物喜好、警觉（防偷）、身手（切磋）、身上钱财。
    /// </summary>
    public sealed class NpcProfile
    {
        public string NpcId { get; }
        public int BaseAffinity { get; }
        public Personality Personality { get; }
        public IReadOnlyList<GiftTaste> Tastes { get; }

        /// <summary>警觉 0–100，偷窃难度。</summary>
        public int Alertness { get; }

        /// <summary>身手 0–100，切磋强度；null = 不应战。</summary>
        public int? Prowess { get; }

        /// <summary>身上钱（文），偷窃上限。</summary>
        public int PurseWen { get; }

        public NpcProfile(
            string npcId, int baseAffinity, Personality personality,
            IEnumerable<GiftTaste> tastes, int alertness, int? prowess, int purseWen)
        {
            NpcId = npcId;
            BaseAffinity = baseAffinity;
            Personality = personality;
            Tastes = tastes.ToList();
            Alertness = alertness;
            Prowess = prowess;
            PurseWen = purseWen;
        }
    }

    /// <summary>槐里坊三人的社交档案（与 SampleWard.Npcs 同键）。</summary>
    public static class NpcProfiles
    {
        public static readonly IReadOnlyDictionary<string, NpcProfile> All =
            new Dictionary<string, NpcProfile>
            {
                ["kang_san"] = new NpcProfile(
                    "kang_san", baseAffinity: 50, Personality.Shrewd,
                    new[] { GiftTaste.Exotic, GiftTaste.WineFood },
                    alertness: 55, prowess: 35, purseWen: 60),

                ["zheng_wu"] = new NpcProfile(
                    "zheng_wu", baseAffinity: 35, Personality.Proud,
                    new[] { GiftTaste.WineFood },
                    alertness: 80, prowess: 65, purseWen: 30),

                ["huan_fuzi"] = new NpcProfile(
                    "huan_fuzi", baseAffinity: 45, Personality.None,
                    new[] { GiftTaste.Book, GiftTaste.Silk },
                    alertness: 25, prowess: null, purseWen: 40)
            };

        public static NpcProfile Get(string npcId)
        {
            return All.TryGetValue(npcId, out NpcProfile profile) ? profile : null;
        }
    }
}
