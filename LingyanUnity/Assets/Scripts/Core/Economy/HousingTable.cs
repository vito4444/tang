using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Officials;

namespace Lingyan.Core.Economy
{
    /// <summary>
    /// 宅邸等级。唐宅第有品级限制——庶人不得起乌头门（规格第八节）：
    /// 门第不是钱的问题，是身份的问题，天然的进阶门槛。
    /// </summary>
    public sealed class HousingDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public long PriceWen { get; }

        /// <summary>购置所需最低散官品阶；null = 无门槛。</summary>
        public RankGrade? MinGrade { get; }

        /// <summary>档次序（0 最低），只升不降。</summary>
        public int Tier { get; }

        public HousingDef(string id, string nameKey, long priceWen, RankGrade? minGrade, int tier)
        {
            Id = id;
            NameKey = nameKey;
            PriceWen = priceWen;
            MinGrade = minGrade;
            Tier = tier;
        }
    }

    public enum HousingDenial
    {
        None = 0,

        /// <summary>官品不及——钱再多也不行（礼制门槛）。</summary>
        RankTooLow = 1,

        /// <summary>钱不够。</summary>
        CannotAfford = 2,

        /// <summary>不升反降或原地。</summary>
        NotAnUpgrade = 3
    }

    public static class HousingTable
    {
        public static readonly IReadOnlyList<HousingDef> All = new[]
        {
            new HousingDef("hut", "housing.hut", 0, null, 0),
            new HousingDef("courtyard", "housing.courtyard", 30_000, null, 1),
            new HousingDef("two_court", "housing.two_court", 120_000,
                RankGrade.CongLower(9), 2),
            // 五品以上（含从五品下）方得起乌头门；出处待对照《营缮令》复原条文复核
            new HousingDef("wutou", "housing.wutou", 400_000,
                RankGrade.CongLower(5), 3)
        };

        private static readonly Dictionary<string, HousingDef> ById = All.ToDictionary(h => h.Id);

        public static HousingDef Get(string id)
        {
            return id != null && ById.TryGetValue(id, out HousingDef def) ? def : null;
        }

        /// <summary>购置判定：先问身份，再问钱，最后问是否升等。</summary>
        public static HousingDenial CanBuy(
            HousingDef target, string currentId, RankGrade? sanGuanGrade, long purseWen)
        {
            if (target.MinGrade != null)
            {
                if (sanGuanGrade == null || !sanGuanGrade.Value.AtLeast(target.MinGrade.Value))
                {
                    return HousingDenial.RankTooLow;
                }
            }
            HousingDef current = Get(currentId) ?? All[0];
            if (target.Tier <= current.Tier)
            {
                return HousingDenial.NotAnUpgrade;
            }
            if (purseWen < target.PriceWen)
            {
                return HousingDenial.CannotAfford;
            }
            return HousingDenial.None;
        }
    }
}
