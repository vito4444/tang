using Lingyan.Core.Economy;
using Lingyan.Core.Localization;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 市集：定时开市（午启酉散，《唐六典》），货品买入行囊；送礼消耗行囊。
    /// </summary>
    public static class MarketScreen
    {
        private static string _lastResultText;

        public static void Reset()
        {
            _lastResultText = null;
        }

        public static void Build(GameController c, RectTransform root)
        {
            SaveData save = c.ActiveSave;
            if (save == null)
            {
                c.GoMainMenu();
                return;
            }

            bool en = c.L10n.Locale == Locale.En;
            UiKit.InkBackground(root);

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.92f, 700, 66),
                "T", c.L10n.Tr("market.title"), 1.8f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.878f, 480, 0.24f);
            UiKit.Text(UiKit.At(root, "HoursNote", 0.5f, 0.845f, 1100, 42),
                "T", c.L10n.Tr("market.hours_note"), 0.95f,
                InkPalette.Faint, TextAlignmentOptions.Center);

            var purse = new Money(save.MoneyWen);
            UiKit.Text(UiKit.At(root, "Purse", 0.82f, 0.92f, 460, 50),
                "T", c.L10n.Tr("study.money") + " " + (en ? purse.ToEn() : purse.ToZh()),
                1.1f, InkPalette.Seal, TextAlignmentOptions.MidlineRight);

            if (!MarketService.IsOpenAt(save.Date.HourIndex))
            {
                // 正常入口在坊屏按市时门控，此处兜底（直接跳屏或时辰推进后残留）
                UiKit.Text(UiKit.At(root, "Closed", 0.5f, 0.5f, 900, 60),
                    "T", c.L10n.Tr("market.closed"), 1.3f,
                    InkPalette.Seal, TextAlignmentOptions.Center);
                UiKit.TextButton(UiKit.At(root, "BtnBack", 0.5f, 0.1f, 320, 56),
                    "Btn", c.L10n.Tr("market.back"), () => c.GoWard(save), 1.2f);
                return;
            }

            RectTransform panel = UiKit.Rect(root, "Panel",
                new Vector2(0.16f, 0.17f), new Vector2(0.84f, 0.80f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "Bg");

            float y = 0.90f;
            foreach (GiftDef goods in MarketService.Goods)
            {
                GiftDef captured = goods;
                var price = new Money(goods.PriceWen);
                int owned = MarketService.CountOf(save, goods.Id);
                bool affordable = save.MoneyWen >= goods.PriceWen;

                UiKit.Text(UiKit.At(panel, "Name_" + goods.Id, 0.22f, y, 420, 48),
                    "T", c.L10n.Tr(goods.NameKey), 1.1f,
                    InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
                UiKit.Text(UiKit.At(panel, "Price_" + goods.Id, 0.52f, y, 300, 48),
                    "T", en ? price.ToEn() : price.ToZh(), 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.Center);
                UiKit.Text(UiKit.At(panel, "Owned_" + goods.Id, 0.72f, y, 260, 48),
                    "T", c.L10n.TrF("market.owned", owned), 1.0f,
                    owned > 0 ? InkPalette.Good : InkPalette.Faint,
                    TextAlignmentOptions.Center);
                UiKit.TextButton(UiKit.At(panel, "Buy_" + goods.Id, 0.89f, y, 130, 48),
                    "Btn", c.L10n.Tr("market.buy"),
                    () => RunBuy(c, save, captured), 1.05f, affordable);

                y -= 0.125f;
            }

            if (_lastResultText != null)
            {
                UiKit.Text(UiKit.At(panel, "Result", 0.5f, 0.08f, 900, 46),
                    "T", _lastResultText, 1.0f,
                    InkPalette.Good, TextAlignmentOptions.Center);
            }

            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.5f, 0.09f, 320, 56),
                "Btn", c.L10n.Tr("market.back"), () => c.GoWard(save), 1.2f);
        }

        private static void RunBuy(GameController c, SaveData save, GiftDef goods)
        {
            MarketError error = MarketService.Buy(save, goods.Id);
            if (error == MarketError.None)
            {
                _lastResultText = c.L10n.TrF("market.bought", c.L10n.Tr(goods.NameKey));
                c.AutoSave();
            }
            else if (error == MarketError.NotEnoughMoney)
            {
                _lastResultText = c.L10n.Tr("market.no_money");
            }
            else if (error == MarketError.Closed)
            {
                _lastResultText = c.L10n.Tr("market.closed");
            }
            c.GoMarket();
        }
    }
}
