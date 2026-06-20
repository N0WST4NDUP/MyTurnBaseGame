using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #13 이동: 직교 1칸 · 스택 허용(배타 점유 없음) · 경계 제약 · 제자리 이벤트 없음.
    public class MovementTests
    {
        // 지정 유닛들이 3슬롯 전부 이동 카드(Card%4==0 → Move 비트).
        static RoundInput AllMove(params int[] unitIds)
        {
            var plans = new Dictionary<UnitId, Card[]>();
            foreach (var id in unitIds)
                plans[new UnitId(id)] = new[] { new Card(0), new Card(0), new Card(0) };
            return new RoundInput { Plans = plans };
        }

        // 떨어진 두 유닛이 접근 → 같은 칸에 겹친다(스택, 배타 점유 없음).
        // TwoUnits: U1(1,0) vs U2(1,4) → 중앙 (1,2)에서 합류.
        [Test]
        public void Move_UnitsConverge_StackOnSameCell()
        {
            var s = BattleScenarios.TwoUnits(1);
            new RoundResolver().ResolveRound(s, AllMove(1, 2));

            var u1 = s.Units.First(u => u.Id.Value == 1);
            var u2 = s.Units.First(u => u.Id.Value == 2);

            Assert.AreEqual(new Cell(1, 2), u1.Pos);
            Assert.AreEqual(new Cell(1, 2), u2.Pos);
            Assert.AreEqual(u1.Pos, u2.Pos, "같은 칸에 겹칠 수 있어야 한다(스택)");
            Assert.AreEqual(2, Grid.UnitsAt(s, new Cell(1, 2)).Count(), "겹친 유닛을 모두 반환");
        }

        // 한 비트 이동 = 직교 1칸(텔레포트·대각 없음).
        [Test]
        public void Move_StepsOneOrthogonalCellPerBeat()
        {
            var s = BattleScenarios.TwoUnits(1);   // U1(1,0), U2(1,4)
            var tl = new RoundResolver().ResolveRound(s, AllMove(1, 2));

            var slot0Moves = tl.OfType<MoveEvent>().Where(e => e.Slot == 0).ToList();
            Assert.AreEqual(2, slot0Moves.Count, "첫 비트에 두 유닛 모두 이동");
            foreach (var m in slot0Moves)
                Assert.AreEqual(1, Grid.Manhattan(m.From, m.To), "한 비트 이동은 직교 1칸");
        }

        // 불변식: MoveEvent는 실제로 칸이 바뀐 경우에만(From != To).
        // 스택 합류 후 적이 같은 칸 → intent=제자리 → 이벤트가 없어야 한다(제자리 가드 검증).
        [Test]
        public void Move_NeverEmitsNoOpMoveEvent()
        {
            var s = BattleScenarios.TwoUnits(1);
            var tl = new RoundResolver().ResolveRound(s, AllMove(1, 2));

            foreach (var m in tl.OfType<MoveEvent>())
                Assert.AreNotEqual(m.From, m.To, $"제자리 MoveEvent 금지: {m}");
        }
    }
}
