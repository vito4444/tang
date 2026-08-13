using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Social
{
    /// <summary>一件礼物。</summary>
    public sealed class GiftDef
    {
        public string Id { get; }

        /// <summary>名称本地化键。</summary>
        public string NameKey { get; }

        public GiftTaste Taste { get; }

        /// <summary>价格（文）。</summary>
        public int PriceWen { get; }

        public GiftDef(string id, string nameKey, GiftTaste taste, int priceWen)
        {
            Id = id;
            NameKey = nameKey;
            Taste = taste;
            PriceWen = priceWen;
        }
    }

    /// <summary>阶段 3 试制礼单（物价量级参照《唐会要》气口，正式经济表阶段 6 校）。</summary>
    public static class GiftCatalog
    {
        public static readonly IReadOnlyList<GiftDef> All = new[]
        {
            new GiftDef("gift_wenxuan", "gift.wenxuan", GiftTaste.Book, 800),
            new GiftDef("gift_jiu", "gift.jiu", GiftTaste.WineFood, 120),
            new GiftDef("gift_hubing", "gift.hubing", GiftTaste.WineFood, 15),
            new GiftDef("gift_yupei", "gift.yupei", GiftTaste.Jade, 2500),
            new GiftDef("gift_juan", "gift.juan", GiftTaste.Silk, 460),
            new GiftDef("gift_hujiao", "gift.hujiao", GiftTaste.Exotic, 300)
        };

        private static readonly Dictionary<string, GiftDef> ById = All.ToDictionary(g => g.Id);

        public static GiftDef Get(string id)
        {
            return id != null && ById.TryGetValue(id, out GiftDef gift) ? gift : null;
        }
    }
}
