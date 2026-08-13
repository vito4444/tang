using System.Collections.Generic;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;

namespace Lingyan.Core.Economy
{
    public enum MarketError
    {
        None = 0,

        /// <summary>市未开（午时启、酉时闭，见《唐六典》市制）。</summary>
        Closed = 1,

        NotEnoughMoney = 2,

        UnknownGoods = 3,

        /// <summary>行囊里没这件东西（送礼消耗时用）。</summary>
        NotInInventory = 4,
    }

    /// <summary>
    /// 市集：定时开闭、买入行囊、行囊消耗。
    /// 考据口径：《唐六典》卷二十——"凡市，以日中击鼓三百声而众以会；
    /// 日入前七刻，击钲三百声而众以散"。游戏时辰粒度取午、未、申三时开市，
    /// 酉（日入）即散，宵禁时段自然不开。
    /// </summary>
    public static class MarketService
    {
        /// <summary>开市时辰下标（午=6、未=7、申=8）。</summary>
        public static readonly IReadOnlyList<int> OpenHourIndexes = new[] { 6, 7, 8 };

        public static bool IsOpenAt(int hourIndex)
        {
            for (int i = 0; i < OpenHourIndexes.Count; i++)
            {
                if (OpenHourIndexes[i] == hourIndex) { return true; }
            }
            return false;
        }

        /// <summary>市上有售的货品（现阶段 = 全部礼品目录）。</summary>
        public static IReadOnlyList<GiftDef> Goods
        {
            get { return GiftCatalog.All; }
        }

        /// <summary>买一件入行囊。失败不动钱不动货。</summary>
        public static MarketError Buy(SaveData save, string goodsId)
        {
            if (!IsOpenAt(save.Date.HourIndex)) { return MarketError.Closed; }

            GiftDef goods = GiftCatalog.Get(goodsId);
            if (goods == null) { return MarketError.UnknownGoods; }

            if (save.MoneyWen < goods.PriceWen) { return MarketError.NotEnoughMoney; }

            save.MoneyWen -= goods.PriceWen;
            save.Inventory.TryGetValue(goodsId, out int count);
            save.Inventory[goodsId] = count + 1;
            return MarketError.None;
        }

        /// <summary>行囊里某物件数。</summary>
        public static int CountOf(SaveData save, string goodsId)
        {
            return save.Inventory.TryGetValue(goodsId, out int count) ? count : 0;
        }

        /// <summary>从行囊取走一件（送礼消耗）。件数归零就移除键，存档不留 0 条目。</summary>
        public static MarketError TakeOne(SaveData save, string goodsId)
        {
            if (GiftCatalog.Get(goodsId) == null) { return MarketError.UnknownGoods; }
            if (!save.Inventory.TryGetValue(goodsId, out int count) || count < 1)
            {
                return MarketError.NotInInventory;
            }
            if (count == 1) { save.Inventory.Remove(goodsId); }
            else { save.Inventory[goodsId] = count - 1; }
            return MarketError.None;
        }
    }
}
