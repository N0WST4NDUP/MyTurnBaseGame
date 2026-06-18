using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // 결정론 회귀 방지: "동일 입력 + 동일 시드 → 동일 타임라인".
    // (시드가 출력에 영향을 주는지는 동률 시나리오에서 ResolutionTests가 검증한다 —
    //  동률이 없으면 해결기가 RNG를 소비하지 않으므로 '시드 무관'이 정상 동작이다.)
    public class DeterminismTests
    {
        static string ResolveOnce(int seed)
        {
            var state = BattleScenarios.TwoUnits(seed);
            var tl = new RoundResolver().ResolveRound(state, BattleScenarios.SampleInput());
            return BattleScenarios.Serialize(tl);
        }

        [Test]
        public void SameSeedSameInput_ProducesIdenticalTimeline()
        {
            Assert.AreEqual(ResolveOnce(12345), ResolveOnce(12345));
        }

        [Test]
        public void Timeline_IsNonEmpty()
        {
            Assert.IsNotEmpty(ResolveOnce(1));
        }
    }
}
