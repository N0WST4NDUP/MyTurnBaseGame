using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #11 DoD: "동일 입력 + 동일 시드 → 동일 결과" 결정론 회귀 방지.
    public class DeterminismTests
    {
        static string ResolveOnce(int seed)
        {
            var state = BattleScenarios.TwoUnits(seed);
            var tl = new StubBattleResolver().ResolveRound(state, BattleScenarios.SampleInput());
            return BattleScenarios.Serialize(tl);
        }

        [Test]
        public void SameSeedSameInput_ProducesIdenticalTimeline()
        {
            // 핵심 보장: 같은 입력 + 같은 시드 → 바이트 동일 타임라인.
            Assert.AreEqual(ResolveOnce(12345), ResolveOnce(12345));
        }

        [Test]
        public void Seed_ActuallyInfluencesOutput()
        {
            // 시드가 출력에 실제로 반영(=RNG가 배선)되는지 보장.
            // 고정 시드 2개라 결과는 결정적 → 항상 다름(우연 일치 아님).
            Assert.AreNotEqual(ResolveOnce(12345), ResolveOnce(99999));
        }

        [Test]
        public void Timeline_IsNonEmpty()
        {
            Assert.IsNotEmpty(ResolveOnce(1));
        }
    }
}
