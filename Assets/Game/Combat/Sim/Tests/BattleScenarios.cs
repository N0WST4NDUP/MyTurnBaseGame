using System.Collections.Generic;
using System.Linq;

namespace MyTurnBase.Combat.Sim.Tests
{
    // 테스트용 공통 시나리오 빌더.
    // 매 호출 새 인스턴스(특히 새 XorShiftRng)를 만들어, 두 런이 독립적으로
    // 동일 결과를 내는지(결정론) 비교할 수 있게 한다.
    internal static class BattleScenarios
    {
        // 1행 양 끝에 마주 선 2유닛(팀 0 vs 팀 1).
        public static BattleState TwoUnits(int seed)
        {
            var units = new List<Unit>
            {
                new Unit { Id = new UnitId(1), Team = new TeamId(0), Pos = new Cell(1, 0), Hp = 10f, Arc = 2, Speed = 5 },
                new Unit { Id = new UnitId(2), Team = new TeamId(1), Pos = new Cell(1, 4), Hp = 10f, Arc = 2, Speed = 4 },
            };
            return new BattleState(units, new XorShiftRng(seed), round: 0);
        }

        // 유닛별 3슬롯 계획(카드값으로 스텁 분기를 다양하게 태움).
        public static RoundInput SampleInput()
        {
            return new RoundInput
            {
                Plans = new Dictionary<UnitId, Card[]>
                {
                    { new UnitId(1), new[] { new Card(0), new Card(1), new Card(2) } },
                    { new UnitId(2), new[] { new Card(2), new Card(3), new Card(0) } },
                }
            };
        }

        // 타임라인 → 결정론 비교용 안정적 문자열(이벤트 ToString을 줄바꿈 join).
        public static string Serialize(IReadOnlyList<BattleEvent> tl)
        {
            return string.Join("\n", tl.Select(e => e.ToString()));
        }
    }
}
