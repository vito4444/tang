using Lingyan.Core.Reputation;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class ReputationTests
    {
        [Test]
        public void Apply_ClampsAndRecordsActualDelta()
        {
            var state = new ReputationState(98, 50, 50);
            int applied = state.Apply(ReputationTrack.GuanSheng, +5, "test.source", "chuigong:4:3:17:5");
            Assert.That(applied, Is.EqualTo(2), "98+5 夹取到 100，实际 +2");
            Assert.That(state.GuanSheng, Is.EqualTo(100));
            Assert.That(state.Ledger.Count, Is.EqualTo(1));
            Assert.That(state.Ledger[0].Delta, Is.EqualTo(2));
            Assert.That(state.Ledger[0].SourceKey, Is.EqualTo("test.source"));
            Assert.That(state.Ledger[0].DateStamp, Is.EqualTo("chuigong:4:3:17:5"));
        }

        [Test]
        public void Apply_FloorsAtZero()
        {
            var state = new ReputationState(3, 50, 50);
            int applied = state.Apply(ReputationTrack.GuanSheng, -10, "test.source", "s");
            Assert.That(applied, Is.EqualTo(-3));
            Assert.That(state.GuanSheng, Is.EqualTo(0));
        }

        [Test]
        public void SameDeeds_ReadDifferentlyByArchetype()
        {
            // 规格第六节：官员看官声、百姓看民望、游侠看江湖名望
            var state = new ReputationState(80, 20, 10);
            int byOfficial = ReputationWeights.WeightedOpinion(state, NpcArchetype.Official);
            int byCommoner = ReputationWeights.WeightedOpinion(state, NpcArchetype.Commoner);
            int byJianghu = ReputationWeights.WeightedOpinion(state, NpcArchetype.Jianghu);

            Assert.That(byOfficial, Is.GreaterThan(byCommoner));
            Assert.That(byCommoner, Is.GreaterThan(byJianghu));
            Assert.That(byOfficial, Is.EqualTo(61), "0.7*80+0.2*20+0.1*10 = 61");
        }

        [Test]
        public void Weights_SumToOne()
        {
            foreach (NpcArchetype archetype in System.Enum.GetValues(typeof(NpcArchetype)))
            {
                var (g, m, j) = ReputationWeights.For(archetype);
                Assert.That(g + m + j, Is.EqualTo(1.0).Within(1e-9), archetype.ToString());
            }
        }
    }
}
