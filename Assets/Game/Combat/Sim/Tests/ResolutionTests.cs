using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #12 라운드·비트 해결: 비트 순서 · 선공 우선 · 동률 RNG 타이브레이크.
    public class ResolutionTests
    {
        // 한 슬롯 안 이벤트는 비트 순서(Move<Guard<Attack<Charge)대로 나와야 한다.
        // = 유닛 입력 순서가 아니라 '비트'로 재정렬됨을 검증.
        [Test]
        public void WithinSlot_EventsAreOrderedByBeat()
        {
            var s = BattleScenarios.TwoUnits(123);
            var tl = new RoundResolver().ResolveRound(s, BattleScenarios.SampleInput());

            foreach (var slotGroup in tl.GroupBy(e => (e.Round, e.Slot)))
            {
                int prev = -1;
                foreach (var e in slotGroup)
                {
                    int phase = (int)e.Phase;
                    Assert.GreaterOrEqual(phase, prev, $"슬롯 {slotGroup.Key} 비트 순서 위반: {e}");
                    prev = phase;
                }
            }
        }

        // 동률 Speed에서 서로 공격 → 선공(빠른 쪽)이 처치하면 느린 쪽은 반격 못 함
        // → 정확히 1명 생존. (동시 트레이드였다면 0명 생존이어야 하므로 둘을 구분한다.)
        [Test]
        public void EqualSpeed_FasterPreempts_ExactlyOneSurvives()
        {
            for (int seed = 1; seed <= 16; seed++)
            {
                var s = BattleScenarios.DuelEqualSpeed(seed);
                new RoundResolver().ResolveRound(s, BattleScenarios.BothAttack());
                Assert.AreEqual(1, s.Units.Count(u => u.Hp > 0f), $"seed {seed}: 선공 우선이면 생존자 1명");
            }
        }

        // 같은 시드 → 같은 생존자(결정론).
        [Test]
        public void EqualSpeed_SameSeed_SameSurvivor()
        {
            Assert.AreEqual(Survivor(7), Survivor(7));
            Assert.AreEqual(Survivor(40), Survivor(40));
        }

        // 시드에 따라 선공(생존자)이 갈린다 → RNG 타이브레이크가 실제로 동작.
        [Test]
        public void EqualSpeed_SeedDecidesInitiative()
        {
            var survivors = new HashSet<int>();
            for (int seed = 1; seed <= 64; seed++)
                survivors.Add(Survivor(seed));

            Assert.IsTrue(survivors.Contains(1) && survivors.Contains(2),
                "동률 Speed에서 시드에 따라 양쪽 모두 선공을 잡을 수 있어야 한다");
        }

        static int Survivor(int seed)
        {
            var s = BattleScenarios.DuelEqualSpeed(seed);
            new RoundResolver().ResolveRound(s, BattleScenarios.BothAttack());
            var alive = s.Units.Where(u => u.Hp > 0f).ToList();
            return alive.Count == 1 ? alive[0].Id.Value : -1;
        }
    }
}
