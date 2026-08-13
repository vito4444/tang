using System;
using System.IO;
using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;
using Lingyan.Core.Economy;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Terminology;

namespace Lingyan.Demo
{
    /// <summary>
    /// 数据层端到端演示：建角 → 出档 → 读档 → 旧档迁移 → 坏档拒绝 → 考课迁转。
    /// 在无 Unity 环境下验证核心链路真实可跑。
    /// </summary>
    public static class Program
    {
        public static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string dataDir = FindDataDir();
            var catalog = LocalizationCatalog.Parse(File.ReadAllText(Path.Combine(dataDir, "strings.json")));
            var glossary = Glossary.Parse(File.ReadAllText(Path.Combine(dataDir, "glossary.json")));

            Line("=== 凌烟 · 数据层演示 ===");
            Line("");

            // 1) 建角：明镜
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData save = SaveFactory.NewGame(draft, DateTime.UtcNow);
            var migrator = SaveMigrator.CreateDefault();

            Line("— 新档（明镜线）—");
            PrintSheet(save, catalog, glossary);

            // 2) 序列化 → 反序列化
            string json = migrator.Serialize(save);
            SaveData restored = migrator.Load(json);
            Line("");
            Line("出档再读档：钱 = " + new Money(restored.MoneyWen).ToZh()
                + "，日期 = " + ToDate(restored).ToZh() + "  ← 与出档前一致");

            // 3) v0 旧档迁移
            const string v0 = "{ \"version\": 0, \"hero\": \"mingjing\", \"name\": \"沈知白\", "
                + "\"attrs\": { \"tili\": 6, \"shengming\": 6, \"liliang\": 4, \"zhihui\": 12 }, "
                + "\"money_guan\": 3.42, "
                + "\"rep\": { \"guan\": 30, \"min\": 40, \"jianghu\": 10 }, "
                + "\"date\": { \"era\": \"chuigong\", \"year\": 4, \"month\": 3, \"day\": 17, \"hour\": 5 } }";
            SaveData migrated = migrator.Load(v0);
            Line("");
            Line("— v0 旧档迁移 —");
            Line("v0 里 money_guan = 3.42（贯，浮点）");
            Line("迁移后 = " + new Money(migrated.MoneyWen).ToZh() + "（一文不丢）");

            // 4) 坏档拒绝（缺钱字段）
            const string broken = "{ \"version\": 0, \"hero\": \"mingjing\", \"name\": \"沈知白\", "
                + "\"attrs\": { \"tili\": 6, \"shengming\": 6, \"liliang\": 4, \"zhihui\": 12 }, "
                + "\"rep\": { \"guan\": 30, \"min\": 40, \"jianghu\": 10 }, "
                + "\"date\": { \"era\": \"chuigong\", \"year\": 4, \"month\": 3, \"day\": 17, \"hour\": 5 } }";
            Line("");
            Line("— 坏档拒绝 —");
            try
            {
                migrator.Load(broken);
                Line("!!! 不应到达此处");
                return 1;
            }
            catch (SaveException e)
            {
                Line("缺 money 字段的档被拒绝：" + e.GetType().Name);
                Line("给玩家看的话（本地化键 " + e.ReasonKey + "）：");
                Line("  zh: " + catalog.Get(e.ReasonKey, Locale.ZhHans));
                Line("  en: " + catalog.Get(e.ReasonKey, Locale.En));
            }

            // 5) 考课 → 迁转
            Line("");
            Line("— 考课演示（县尉任上四年）—");
            var years = new System.Collections.Generic.List<NineGrade>();
            for (int year = 1; year <= 4; year++)
            {
                var input = new KaoKeInput
                {
                    MeritPoints = year >= 3 ? 30 : 10,
                    CompletionRatio = 0.85,
                    GuanSheng = 45 + year * 6,
                    MinWang = 48 + year * 6
                };
                KaoKeResult result = KaoKeService.Evaluate(input);
                years.Add(result.Grade);
                Line("  第" + ZhNumerals.Number(year) + "考：善 " + result.ShanCount
                    + (result.HasZui ? "、有最" : "、无最")
                    + " → " + KaoKeService.GradeZh(result.Grade)
                    + " (" + KaoKeService.GradeEn(result.Grade) + ")");
            }
            OfficeDef xianwei = OfficialLadders.Get("xian_wei");
            PromotionDecision decision = PromotionService.Evaluate(years, xianwei);
            Line("  迁转判定：" + catalog.Get(decision.ReasonKey, Locale.ZhHans)
                + (decision.Eligible
                    ? " → " + decision.NextOffice.Zh + " ("
                        + glossary.EnFor(decision.NextOffice.Zh) + ", "
                        + decision.NextOffice.Grade.Value.ToZh() + ")"
                    : ""));

            Line("");
            Line("=== 演示完毕，全链路无静默缺损 ===");
            return 0;
        }

        private static void PrintSheet(SaveData save, LocalizationCatalog catalog, Glossary glossary)
        {
            ProtagonistDef def = ProtagonistCatalog.GetByKey(save.ProtagonistKey);
            TangDate date = ToDate(save);
            RobeColor robe = RobeColors.FromGrade(SanGuanTable.Get(save.Offices.SanGuanId)?.Grade);

            Line("姓名：" + save.CharacterName + "（" + catalog.Get(def.TitleKey, Locale.ZhHans) + " / "
                + catalog.Get(def.TitleKey, Locale.En) + "）");
            Line("出身：" + catalog.Get(def.StartStatusKey, Locale.ZhHans) + " / "
                + catalog.Get(def.StartStatusKey, Locale.En));
            Line("四维：体力 " + save.Attributes.Stamina + " · 生命 " + save.Attributes.Health
                + " · 力量 " + save.Attributes.Strength + " · 智慧 " + save.Attributes.Wisdom);
            Line("四轨：职事官 无 · 散官 无 · 勋 0 转 · 爵 无 → 服色 "
                + RobeColors.ZhName(robe) + " (" + RobeColors.EnName(robe) + ")");
            Line("名声：官声 " + save.Reputation.GuanSheng + " · 民望 " + save.Reputation.MinWang
                + " · 江湖 " + save.Reputation.JiangHu);
            Line("囊中：" + new Money(save.MoneyWen).ToZh() + " / " + new Money(save.MoneyWen).ToEn());
            Line("时日：" + date.ToZh() + " / " + date.ToEn());
            Line("节气：" + date.SolarTerm.Zh + " / " + date.SolarTerm.En);
        }

        private static TangDate ToDate(SaveData save)
        {
            return new TangDate(save.Date.EraId, save.Date.EraYear, save.Date.Month,
                save.Date.Day, save.Date.HourIndex);
        }

        private static void Line(string text) { Console.WriteLine(text); }

        private static string FindDataDir()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "LingyanUnity", "Assets", "Resources", "Data");
                if (File.Exists(Path.Combine(candidate, "strings.json"))) { return candidate; }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("找不到数据目录");
        }
    }
}
