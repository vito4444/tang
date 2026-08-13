using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;
using Lingyan.Core.Economy;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 书房：开局落脚点。展示角色的四维、四轨官身、服色、三轨名声、
    /// 囊中钱与唐历时日——数据层的活体验收面。
    /// </summary>
    public static class StudyScreen
    {
        private static string _noticeText;

        public static void Build(GameController c, RectTransform root)
        {
            UiKit.InkBackground(root);
            SaveData save = c.ActiveSave;
            if (save == null)
            {
                c.GoMainMenu();
                return;
            }

            ProtagonistDef def = ProtagonistCatalog.GetByKey(save.ProtagonistKey);
            bool en = c.L10n.Locale == Locale.En;

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.94f, 700, 64),
                "T", c.L10n.Tr("study.title"), 1.8f,
                InkPalette.PaperText, TextAlignmentOptions.Center);

            UiKit.Text(UiKit.At(root, "Who", 0.5f, 0.875f, 1200, 48),
                "T", save.CharacterName + " · " + c.L10n.Tr(def.TitleKey)
                    + " · " + c.L10n.Tr(def.StartStatusKey),
                1.15f, InkPalette.Faint, TextAlignmentOptions.Center);

            if (_noticeText != null)
            {
                UiKit.Text(UiKit.At(root, "Notice", 0.5f, 0.835f, 1500, 44),
                    "T", _noticeText, 1.0f, InkPalette.Seal, TextAlignmentOptions.Center);
            }

            BuildCareerActions(c, root, save);

            // 左列：四维 + 名声
            RectTransform left = UiKit.Rect(root, "LeftPanel",
                new Vector2(0.06f, 0.16f), new Vector2(0.47f, 0.82f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(left, "PanelBg");

            float y = 0.89f;
            SectionHead(c, left, "AttrHead", y, en ? "Attributes" : "四维");
            y -= 0.105f;
            AttrRow(left, "A1", y, c.L10n.Tr("attr.stamina"), save.Attributes.Stamina);
            y -= 0.082f;
            AttrRow(left, "A2", y, c.L10n.Tr("attr.health"), save.Attributes.Health);
            y -= 0.082f;
            AttrRow(left, "A3", y, c.L10n.Tr("attr.strength"), save.Attributes.Strength);
            y -= 0.082f;
            AttrRow(left, "A4", y, c.L10n.Tr("attr.wisdom"), save.Attributes.Wisdom);

            y -= 0.115f;
            SectionHead(c, left, "RepHead", y, c.L10n.Tr("study.reputation"));
            y -= 0.105f;
            RepRow(left, "R1", y, c.L10n.Tr("rep.guansheng"), save.Reputation.GuanSheng);
            y -= 0.082f;
            RepRow(left, "R2", y, c.L10n.Tr("rep.minwang"), save.Reputation.MinWang);
            y -= 0.082f;
            RepRow(left, "R3", y, c.L10n.Tr("rep.jianghu"), save.Reputation.JiangHu);

            // 右列：四轨 + 服色 + 钱 + 时日
            RectTransform right = UiKit.Rect(root, "RightPanel",
                new Vector2(0.53f, 0.16f), new Vector2(0.94f, 0.82f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(right, "PanelBg");

            OfficeDef zhishi = OfficialLadders.Get(save.Offices.ZhiShiId);
            SanGuanDef sanguan = SanGuanTable.Get(save.Offices.SanGuanId);
            XunGuanDef xun = XunGuanTable.ForZhuan(save.Offices.XunZhuan);
            JueDef jue = JueTable.Get(save.Offices.JueId);

            float ry = 0.90f;
            SectionHead(c, right, "TrackHead", ry, c.L10n.Tr("study.four_tracks"));
            ry -= 0.095f;
            Row(right, "T1", ry, c.L10n.Tr("track.zhishi"),
                zhishi == null
                    ? c.L10n.Tr("study.none")
                    : OfficeLabel(c, zhishi.Zh, zhishi.Grade, en));
            ry -= 0.075f;
            Row(right, "T2", ry, c.L10n.Tr("track.sanguan"),
                sanguan == null
                    ? c.L10n.Tr("study.none")
                    : OfficeLabel(c, sanguan.Zh, sanguan.Grade, en, sanguan.Pinyin));
            ry -= 0.075f;
            Row(right, "T3", ry, c.L10n.Tr("track.xunguan"),
                xun == null
                    ? c.L10n.Tr("study.no_xun")
                    : c.L10n.TrF("study.xun_zhuan", en ? xun.En : xun.Zh, xun.Zhuan));
            ry -= 0.075f;
            Row(right, "T4", ry, c.L10n.Tr("track.jue"),
                jue == null
                    ? c.L10n.Tr("study.none")
                    : OfficeLabel(c, jue.Zh, jue.Grade, en));

            // 服色（符号 + 颜色 + 文字三重编码）：随散官品；宫官无散官，随职事品（舆服"八品九品服青"）
            ry -= 0.11f;
            RobeColor robe = RobeColors.FromGrade(sanguan?.Grade ?? zhishi?.Grade);
            UiKit.Text(UiKit.At(right, "RobeLabel", 0.22f, ry, 300, 46),
                "T", c.L10n.Tr("study.robe"), 1.05f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            var swatch = UiKit.Swatch(UiKit.At(right, "RobeSwatch", 0.42f, ry, 42, 42),
                "Box", InkPalette.Hex(RobeColors.UiHex(robe)));
            UiKit.Stretch(swatch.rectTransform);
            UiKit.Text(UiKit.At(right, "RobeName", 0.68f, ry, 300, 46),
                "T", c.L10n.Tr(RobeKey(robe)), 1.1f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);

            ry -= 0.11f;
            var money = new Money(save.MoneyWen);
            Row(right, "Money", ry, c.L10n.Tr("study.money"), en ? money.ToEn() : money.ToZh());

            ry -= 0.075f;
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            Row(right, "Date", ry, c.L10n.Tr("study.date"), en ? date.ToEn() : date.ToZh());
            ry -= 0.075f;
            Row(right, "Term", ry, c.L10n.Tr("study.solar_term"),
                en ? date.SolarTerm.En : date.SolarTerm.Zh);
            ry -= 0.075f;
            Row(right, "Housing", ry, c.L10n.Tr("study.housing"),
                c.L10n.Tr(Lingyan.Core.Economy.HousingTable.Get(save.HousingId).NameKey));
            if (save.WantedLevel > 0)
            {
                ry -= 0.075f;
                Row(right, "Wanted", ry, c.L10n.Tr("study.wanted"),
                    save.WantedLevel.ToString());
            }

            // 底部动作
            UiKit.TextButton(UiKit.At(root, "BtnSave", 0.22f, 0.075f, 460, 56),
                "Btn", c.L10n.Tr("study.save_and_menu"),
                () =>
                {
                    c.Saves.Write(save);
                    c.GoMainMenu();
                }, 1.1f);

            UiKit.TextButton(UiKit.At(root, "BtnWard", 0.44f, 0.075f, 300, 56),
                "Btn", c.L10n.Tr("study.go_out"),
                () => c.GoWard(save), 1.15f);

            // 案牍：案件按序流转（丝帛案→枯井案），门槛未达灰显
            Lingyan.Core.Cases.CaseDef currentCase = Lingyan.Core.Cases.CaseFlow.Current(save);
            bool caseOpen = save.Cases.ContainsKey(currentCase.Id);
            bool canTake = Lingyan.Core.Cases.CaseFlow.CanOpen(save, currentCase);
            UiKit.TextButton(UiKit.At(root, "BtnCase", 0.66f, 0.075f, 320, 56),
                "Btn", c.L10n.Tr(caseOpen ? "study.case_board" : "study.case_take"),
                () =>
                {
                    if (!caseOpen)
                    {
                        var now = new TangDate(save.Date.EraId, save.Date.EraYear,
                            save.Date.Month, save.Date.Day, save.Date.HourIndex);
                        Lingyan.Core.Cases.CaseService.Open(save, currentCase, now);
                        // 开卷旗标：对话树以此解锁案件相关话头（如桓夫子忆旧）
                        save.StoryFlags["case_opened_" + currentCase.Id] = true;
                        Lingyan.Core.Terminology.CodexService.OnEvent(
                            save, Lingyan.Core.Terminology.CodexEvent.CaseOpened);
                        Lingyan.Core.Terminology.CodexService.OnEvent(
                            save, Lingyan.Core.Terminology.CodexEvent.HeardSilkCase);
                    }
                    CaseScreen.Reset();
                    c.GoCase();
                }, 1.1f, caseOpen || canTake);

            UiKit.TextButton(UiKit.At(root, "BtnCodex", 0.845f, 0.075f, 200, 56),
                "Btn", c.L10n.Tr("study.codex"),
                () => { CodexScreen.Reset(); c.GoCodex(); }, 1.05f);

            // 挂冠致仕：随时可收束此生，结局按身份与名声定档（存档不销毁）
            UiKit.TextButton(UiKit.At(root, "BtnRetire", 0.955f, 0.075f, 200, 56),
                "Btn", c.L10n.Tr(save.Offices.ZhiShiId != null
                    ? "study.retire" : "study.retire_commoner"),
                () => { c.AutoSave(); c.GoEnding(); }, 1.0f);

            UiKit.TextButton(UiKit.At(root, "BtnSettings", 0.94f, 0.94f, 180, 48),
                "Btn", c.L10n.Tr("menu.settings"), c.GoSettings, 1.0f);
        }

        private static string OfficeLabel(
            GameController c, string zh, RankGrade? grade, bool en, string enOverride = null)
        {
            string name = en ? (enOverride ?? c.L10n.OfficeEn(zh)) : zh;
            if (grade == null) { return name; }
            return name + " · " + (en ? grade.Value.ToEn() : grade.Value.ToZh());
        }

        private static void SectionHead(
            GameController c, RectTransform panel, string name, float y, string text)
        {
            UiKit.Text(UiKit.At(panel, name, 0.5f, y, 640, 50),
                "T", text, 1.25f, InkPalette.Seal, TextAlignmentOptions.Center);
            UiKit.Hairline(panel, name + "_Rule", 0.5f, y - 0.042f, 420, 0.17f);
        }

        private static void Row(
            RectTransform panel, string name, float y, string label, string value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.24f, y, 330, 46),
                "T", label, 1.05f, InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.68f, y, 560, 46),
                "T", value, 1.1f, InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
        }

        /// <summary>四维行：标签 + 数值 + 十二格双编码。</summary>
        private static void AttrRow(
            RectTransform panel, string name, float y, string label, int value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.17f, y, 220, 46),
                "T", label, 1.05f, InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.36f, y, 80, 46),
                "T", value.ToString(), 1.15f, InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Cells(panel, name + "_Cells", 0.60f, y, value);
        }

        /// <summary>名声行：标签 + 数值 + 朱砂进度条（0–100）。</summary>
        private static void RepRow(
            RectTransform panel, string name, float y, string label, int value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.17f, y, 220, 46),
                "T", label, 1.05f, InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.36f, y, 80, 46),
                "T", value.ToString(), 1.15f, InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Bar(panel, name + "_Bar", 0.66f, y, 300, value / 100f);
        }

        /// <summary>仕途动作：破案后铨选授官；任上岁末赴考，考成迁转（服色随散官变）。</summary>
        private static void BuildCareerActions(GameController c, RectTransform root, SaveData save)
        {
            var now = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);

            // 入仕分化（其余主角线开局）：五线各有门槛与去处，按钮常亮、未达门槛点开见驳文
            if (save.Offices.ZhiShiId == null && save.Offices.SanGuanId == null)
            {
                EntryOffer offer = CareerEntryService.Evaluate(save);
                UiKit.TextButton(UiKit.At(root, "BtnAppoint", 0.94f, 0.875f, 220, 50),
                    "Btn", c.L10n.Tr(offer.ActionKey),
                    () =>
                    {
                        if (!CareerEntryService.Apply(save, offer))
                        {
                            _noticeText = c.L10n.Tr(offer.GateKey);
                            c.GoStudy(save);
                            return;
                        }
                        Lingyan.Core.Terminology.CodexService.OnEvent(
                            save, Lingyan.Core.Terminology.CodexEvent.Appointed);
                        bool en2 = c.L10n.Locale == Locale.En;
                        string text = c.L10n.Tr(offer.NoticeKey);
                        OfficeDef granted = OfficialLadders.Get(offer.ZhiShiOfficeId);
                        if (granted != null)
                        {
                            SanGuanDef given = SanGuanTable.Get(offer.SanGuanId);
                            string sanguanText = given == null
                                ? c.L10n.Tr("career.sanguan_none")
                                : (en2 ? given.Pinyin : given.Zh);
                            text += "\n" + c.L10n.TrF("career.appointed",
                                en2 ? c.L10n.OfficeEn(granted.Zh) : granted.Zh,
                                sanguanText);
                        }
                        _noticeText = text;
                        c.AutoSave();
                        c.GoStudy(save);
                    }, 1.0f);
                return;
            }

            // 岁末赴考：任上且当年未考
            if (save.Offices.ZhiShiId != null)
            {
                int year = EraTable.ToGregorianYear(save.Date.EraId, save.Date.EraYear);
                save.Counters.TryGetValue("last_kaoke_year", out int lastYear);
                bool canExam = year > lastYear;
                UiKit.TextButton(UiKit.At(root, "BtnKaoke", 0.94f, 0.875f, 220, 50),
                    "Btn", c.L10n.Tr("career.kaoke"),
                    () => RunKaoKe(c, save, now, year), 1.0f, canExam);

                // 支取月俸：每月一次
                int yearMonth = year * 100 + save.Date.Month;
                save.Counters.TryGetValue("last_salary_ym", out int lastSalary);
                bool canDraw = yearMonth > lastSalary;
                UiKit.TextButton(UiKit.At(root, "BtnSalary", 0.94f, 0.825f, 220, 50),
                    "Btn", c.L10n.Tr("career.salary"),
                    () => DrawSalary(c, save, yearMonth), 1.0f, canDraw);

                // 遣媒议亲：有官身方有门第可讲
                UiKit.TextButton(UiKit.At(root, "BtnMarriage", 0.94f, 0.775f, 220, 50),
                    "Btn", c.L10n.Tr("study.marriage"),
                    () => { MarriageScreen.Reset(); c.GoMarriage(); }, 1.0f);
            }
        }

        private static void DrawSalary(GameController c, SaveData save, int yearMonth)
        {
            OfficeDef office = OfficialLadders.Get(save.Offices.ZhiShiId);
            if (office?.Grade == null)
            {
                _noticeText = c.L10n.Tr("career.salary_none");
                c.GoStudy(save);
                return;
            }
            Lingyan.Core.Economy.MonthlyPay pay =
                Lingyan.Core.Economy.SalaryTable.For(office.Grade.Value);
            Lingyan.Core.Social.OutcomeApplier.ApplyMoney(save, pay.TotalWen);
            save.Counters["last_salary_ym"] = yearMonth;
            Lingyan.Core.Terminology.CodexService.OnEvent(
                save, Lingyan.Core.Terminology.CodexEvent.SalaryDrawn);

            bool en = c.L10n.Locale == Locale.En;
            string F(long wen)
            {
                var money = new Money(wen);
                return en ? money.ToEn() : money.ToZh();
            }
            _noticeText = c.L10n.TrF("career.salary_detail",
                F(pay.SalaryWen), pay.RiceShi,
                F((long)System.Math.Round(pay.RiceShi
                    * Lingyan.Core.Economy.SalaryTable.RicePricePerShi)),
                F(pay.FieldRentWen), F(pay.TotalWen));

            // 佣钱随俸结算：付得起扣钱，付不起长随当场辞工掉好感
            var payDate = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            bool paid = Lingyan.Core.Social.RetainerService.SettleMonthlyWage(
                save, payDate, out string retainerId);
            if (retainerId != null)
            {
                string retainerName = c.L10n.Tr(Lingyan.Core.World.SampleWard.Npcs
                    .First(n => n.NpcId == retainerId).NameKey);
                int wage = Lingyan.Core.Social.NpcProfiles.Get(retainerId).HireWageWen.Value;
                _noticeText += "\n" + (paid
                    ? c.L10n.TrF("career.salary_wage", F(wage), retainerName)
                    : c.L10n.TrF("career.salary_wage_broke", retainerName));
            }
            c.GoStudy(save);
        }

        private static void RunKaoKe(GameController c, SaveData save, TangDate now, int year)
        {
            save.Counters.TryGetValue("merit_points", out int merit);
            save.Counters.TryGetValue("cases_closed", out int closed);
            int opened = save.Cases.Count;
            // 冤案扫全部案卷（不再只看丝帛案）——一案含冤，考课"公平可称"即失
            bool wrongful = save.Cases.Values.Any(cs => cs.WrongfulConviction);

            var input = new KaoKeInput
            {
                MeritPoints = merit,
                CompletionRatio = opened == 0 ? 0 : (double)closed / opened,
                GuanSheng = save.Reputation.GuanSheng,
                MinWang = save.Reputation.MinWang,
                WrongfulConviction = wrongful
            };
            KaoKeResult result = KaoKeService.Evaluate(input);
            save.KaoKeGrades.Add((int)result.Grade);
            save.Counters["last_kaoke_year"] = year;
            save.Counters["merit_points"] = 0; // 功绩计入本考，翻篇

            var grades = save.KaoKeGrades.Select(g => (NineGrade)g).ToList();
            OfficeDef current = OfficialLadders.Get(save.Offices.ZhiShiId);
            PromotionDecision decision = PromotionService.Evaluate(grades, current);

            bool en = c.L10n.Locale == Locale.En;
            string text = c.L10n.TrF("career.kaoke_result",
                en ? KaoKeService.GradeEn(result.Grade) : KaoKeService.GradeZh(result.Grade))
                + "　" + c.L10n.Tr(decision.ReasonKey);

            if (decision.Eligible && decision.NextOffice != null)
            {
                save.Offices.ZhiShiId = decision.NextOffice.Id;
                // 散官随迁：文线文散、军线武散；宫官品阶自成体系，不带散官
                if (decision.NextOffice.Grade != null
                    && decision.NextOffice.Line != CareerLine.Palace)
                {
                    SanGuanDef sanguan = SanGuanTable.InitialFor(
                        decision.NextOffice.Grade.Value,
                        civil: decision.NextOffice.Line != CareerLine.Military);
                    save.Offices.SanGuanId = sanguan.Id;
                }
                text += "　" + c.L10n.TrF("career.promoted",
                    en ? c.L10n.OfficeEn(decision.NextOffice.Zh) : decision.NextOffice.Zh);
            }

            // 量移（翻身线）：贬谪之身岁课中上及以上，贬籍洗雪
            if (DemotionService.TryRedeem(save, result.Grade))
            {
                text += "　" + c.L10n.Tr("career.liangyi");
            }

            // 贬谪而非 Game Over：通缉滔天或考课连殿，贬官降阶、剧情继续
            string demotionReason = DemotionService.ShouldDemote(save);
            if (demotionReason != null)
            {
                DemotionResult demotion = DemotionService.Apply(save, demotionReason);
                if (demotion.Demoted)
                {
                    text += "　" + c.L10n.Tr(demotionReason) + "　"
                        + c.L10n.TrF("career.demoted",
                            en ? c.L10n.OfficeEn(demotion.NewOffice.Zh)
                               : demotion.NewOffice.Zh);
                }
            }
            _noticeText = text;
            c.AutoSave();
            c.GoStudy(save);
        }

        private static string RobeKey(RobeColor robe)
        {
            switch (robe)
            {
                case RobeColor.Purple: return "robe.purple";
                case RobeColor.Scarlet: return "robe.scarlet";
                case RobeColor.Green: return "robe.green";
                case RobeColor.Qing: return "robe.qing";
                default: return "robe.white";
            }
        }
    }
}
