using System.Collections.Generic;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Endings
{
    /// <summary>结局档位（规格第十一节：多结局，身份即命运）。</summary>
    public enum EndingId
    {
        /// <summary>紫袍金鱼：三品及以上致仕，位极人臣。</summary>
        ZiPao = 0,

        /// <summary>青云直上：五品及以上（绯袍以上），仕途有成。</summary>
        QingYun = 1,

        /// <summary>循吏传：民望 ≥ 70 且无冤案在身——史笔留名的好官。</summary>
        XunLi = 2,

        /// <summary>江湖有名：江湖名望 ≥ 70，庙堂之外自有天地。</summary>
        JiangHuMing = 3,

        /// <summary>岭南瘴雨：带着贬谪之身收场，未及翻身。</summary>
        LingnanRain = 4,

        /// <summary>布衣终老：无官身收场，市井里过完一生。</summary>
        BuYi = 5,

        /// <summary>薄宦萧然：有官身但品低无名，浮沉下僚。</summary>
        BoHuan = 6
    }

    /// <summary>结局判定结果 + 生涯回顾素材。</summary>
    public sealed class EndingResult
    {
        public EndingId Id { get; set; }

        /// <summary>结局标题/尾声文本键。</summary>
        public string TitleKey { get; set; }

        public string EpilogueKey { get; set; }

        /// <summary>主角专属尾声一句（五主角各一句）。</summary>
        public string ProtagonistLineKey { get; set; }

        /// <summary>终任职事官 id（呈现层按语言取名）；白身为 null。</summary>
        public string FinalOfficeId { get; set; }

        /// <summary>回顾行（键 + 参数），呈现层直接铺。</summary>
        public List<(string Key, object[] Args)> RecapLines { get; }
            = new List<(string, object[])>();
    }

    /// <summary>
    /// 挂冠致仕触发结局判定。优先级：贬谪未雪 > 紫袍 > 青云 > 循吏 > 江湖 > 薄宦/布衣。
    /// 冤案在身者永不得入「循吏传」（污点回避，考据口径同考课"公平可称"）。
    /// </summary>
    public static class EndingService
    {
        public static EndingResult Evaluate(SaveData save)
        {
            var result = new EndingResult();

            bool demotedUnredeemed =
                save.StoryFlags.TryGetValue("demoted_lingnan", out bool demoted) && demoted;
            bool wrongful = HasWrongfulConviction(save);
            RankGrade? grade = CurrentGrade(save);

            if (demotedUnredeemed)
            {
                result.Id = EndingId.LingnanRain;
            }
            else if (grade != null && grade.Value.Band <= 3)
            {
                result.Id = EndingId.ZiPao;
            }
            else if (grade != null && grade.Value.Band <= 5)
            {
                result.Id = EndingId.QingYun;
            }
            else if (save.Reputation.MinWang >= 70 && !wrongful)
            {
                result.Id = EndingId.XunLi;
            }
            else if (save.Reputation.JiangHu >= 70)
            {
                result.Id = EndingId.JiangHuMing;
            }
            else if (grade != null)
            {
                result.Id = EndingId.BoHuan;
            }
            else
            {
                result.Id = EndingId.BuYi;
            }

            string suffix = KeySuffix(result.Id);
            result.TitleKey = "ending.title." + suffix;
            result.EpilogueKey = "ending.epilogue." + suffix;
            result.ProtagonistLineKey = "ending.hero." + save.ProtagonistKey;

            BuildRecap(save, grade, wrongful, result);
            return result;
        }

        private static string KeySuffix(EndingId id)
        {
            switch (id)
            {
                case EndingId.ZiPao: return "zipao";
                case EndingId.QingYun: return "qingyun";
                case EndingId.XunLi: return "xunli";
                case EndingId.JiangHuMing: return "jianghu";
                case EndingId.LingnanRain: return "lingnan";
                case EndingId.BuYi: return "buyi";
                default: return "bohuan";
            }
        }

        private static bool HasWrongfulConviction(SaveData save)
        {
            foreach (KeyValuePair<string, SaveCaseState> pair in save.Cases)
            {
                if (pair.Value.WrongfulConviction) { return true; }
            }
            return false;
        }

        private static RankGrade? CurrentGrade(SaveData save)
        {
            OfficeDef office = OfficialLadders.Get(save.Offices.ZhiShiId);
            return office?.Grade;
        }

        private static void BuildRecap(
            SaveData save, RankGrade? grade, bool wrongful, EndingResult result)
        {
            result.FinalOfficeId = grade != null ? save.Offices.ZhiShiId : null;
            if (grade == null)
            {
                result.RecapLines.Add(("ending.recap.no_office", new object[0]));
            }

            result.RecapLines.Add(("ending.recap.reputation", new object[]
            {
                save.Reputation.GuanSheng, save.Reputation.MinWang, save.Reputation.JiangHu
            }));

            int closedCases = 0;
            foreach (KeyValuePair<string, SaveCaseState> pair in save.Cases)
            {
                if (pair.Value.Accused != null) { closedCases++; }
            }
            result.RecapLines.Add(("ending.recap.cases",
                new object[] { closedCases, wrongful ? 1 : 0 }));

            result.RecapLines.Add(("ending.recap.kaoke",
                new object[] { save.KaoKeGrades.Count }));

            result.RecapLines.Add((save.Marriage.Married
                ? "ending.recap.married"
                : "ending.recap.single", new object[0]));

            result.RecapLines.Add(("ending.recap.codex",
                new object[] { save.CodexUnlocked.Count }));
        }
    }
}
