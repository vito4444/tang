using System.Linq;
using Lingyan.Core.Characters;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CreationTests
    {
        [Test]
        public void FiveProtagonists_AllTotal28()
        {
            Assert.That(ProtagonistCatalog.All.Count, Is.EqualTo(5));
            foreach (ProtagonistDef def in ProtagonistCatalog.All)
            {
                int total = def.Preset.Total
                    + (def.FreeAllocation ? CharacterCreationRules.FreePool : 0);
                Assert.That(total, Is.EqualTo(28), def.Key + " 四维总量应为 28");
            }
        }

        [Test]
        public void OnlyBaiShen_AllowsAllocation()
        {
            foreach (ProtagonistDef def in ProtagonistCatalog.All)
            {
                CharacterDraft draft = CharacterCreationRules.NewDraft(def.Id);
                AllocationError error = CharacterCreationRules.TryIncrease(draft, AttributeId.Wisdom);
                if (def.Id == ProtagonistId.BaiShen)
                {
                    Assert.That(error, Is.EqualTo(AllocationError.None));
                }
                else
                {
                    Assert.That(error, Is.EqualTo(AllocationError.NotAllocatable),
                        def.Key + " 为固定预设，不可加点");
                    Assert.That(draft.Attributes, Is.EqualTo(def.Preset), "预设值不得被改动");
                }
            }
        }

        [Test]
        public void BaiShen_PoolAndCaps()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.BaiShen);
            Assert.That(CharacterCreationRules.RemainingPool(draft), Is.EqualTo(12));

            // 智慧 4 → 12（上限），花 8 点
            for (int i = 0; i < 8; i++)
            {
                Assert.That(CharacterCreationRules.TryIncrease(draft, AttributeId.Wisdom),
                    Is.EqualTo(AllocationError.None));
            }
            Assert.That(CharacterCreationRules.TryIncrease(draft, AttributeId.Wisdom),
                Is.EqualTo(AllocationError.AtMax), "单项上限 12");

            // 余 4 点全给体力
            for (int i = 0; i < 4; i++)
            {
                CharacterCreationRules.TryIncrease(draft, AttributeId.Stamina);
            }
            Assert.That(CharacterCreationRules.RemainingPool(draft), Is.EqualTo(0));
            Assert.That(CharacterCreationRules.TryIncrease(draft, AttributeId.Health),
                Is.EqualTo(AllocationError.PoolExhausted));

            // 减回去、下限保护
            Assert.That(CharacterCreationRules.TryDecrease(draft, AttributeId.Health),
                Is.EqualTo(AllocationError.AtMin), "不得低于基础值 4");
            Assert.That(CharacterCreationRules.TryDecrease(draft, AttributeId.Stamina),
                Is.EqualTo(AllocationError.None));
            Assert.That(CharacterCreationRules.RemainingPool(draft), Is.EqualTo(1));
        }

        [Test]
        public void ValidateFinal_BaiShenNeedsEntryPathAndFullSpend()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.BaiShen);
            Assert.That(CharacterCreationRules.ValidateFinal(draft),
                Is.EqualTo("creation.error.points_unspent"));

            for (int i = 0; i < 12; i++)
            {
                foreach (AttributeId attr in AttributeSet.AllIds)
                {
                    if (CharacterCreationRules.TryIncrease(draft, attr) == AllocationError.None)
                    {
                        break;
                    }
                }
            }
            Assert.That(CharacterCreationRules.RemainingPool(draft), Is.EqualTo(0));

            draft.EntryPath = null;
            Assert.That(CharacterCreationRules.ValidateFinal(draft),
                Is.EqualTo("creation.error.entry_path_missing"));

            draft.EntryPath = EntryPath.KejuJinShi;
            Assert.That(CharacterCreationRules.ValidateFinal(draft), Is.Null);

            draft.Name = "  ";
            Assert.That(CharacterCreationRules.ValidateFinal(draft),
                Is.EqualTo("creation.error.name_empty"));
        }

        [Test]
        public void PresetsMatchSpecIntent()
        {
            // 明镜智高力弱；戍卒力体高；胡商钱多无官身
            ProtagonistDef mingjing = ProtagonistCatalog.Get(ProtagonistId.MingJing);
            Assert.That(mingjing.Preset.Wisdom, Is.GreaterThanOrEqualTo(12));
            Assert.That(mingjing.Preset.Strength, Is.LessThanOrEqualTo(4));

            ProtagonistDef shuzu = ProtagonistCatalog.Get(ProtagonistId.ShuZu);
            Assert.That(shuzu.Preset.Strength + shuzu.Preset.Stamina, Is.GreaterThanOrEqualTo(16));

            ProtagonistDef hushang = ProtagonistCatalog.Get(ProtagonistId.HuShang);
            Assert.That(hushang.StartMoneyWen,
                Is.GreaterThan(ProtagonistCatalog.All.Where(p => p.Id != ProtagonistId.HuShang)
                    .Max(p => p.StartMoneyWen) * 10),
                "胡商金钱起点应显著高于其他人");
        }
    }
}
