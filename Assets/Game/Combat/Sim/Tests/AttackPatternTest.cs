using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #14 공격 패턴 판정: 패턴(자기 기준 오프셋) → 적 방향 4방 회전 → 절대 셀 → 스택 대응 명중.
    public class AttackPatternTests
    {
        static Unit U(int id, int team, int row, int col, float hp = 10f, int speed = 5)
            => new Unit { Id = new UnitId(id), Team = new TeamId(team), Pos = new Cell(row, col), Hp = hp, Arc = 0, Speed = speed };

        static BattleState State(params Unit[] units)
            => new BattleState(units.ToList(), new XorShiftRng(1), round: 0);

        // 공격자 1명만 slot0에 공격 카드(나머지 슬롯·유닛 무행동).
        static IReadOnlyList<BattleEvent> Run(BattleState s, int attackerId, CardData card)
            => new RoundResolver().ResolveRound(s,
                new RoundInput { Plans = new Dictionary<UnitId, CardData[]> { { new UnitId(attackerId), new[] { card, null, null } } } });

        // 적 우/좌 → 전방 패턴이 +col/-col로 정렬(4방 회전 수평 케이스).
        [Test]
        public void Pattern_OrientsTowardEnemy_RightAndLeft()
        {
            var right = State(U(1, 0, 1, 1), U(2, 1, 1, 4));
            var declR = Run(right, 1, BattleScenarios.AttackCard()).OfType<AttackDeclaredEvent>().First();
            CollectionAssert.AreEqual(new[] { new Cell(1, 2) }, declR.StrikeCells);

            var left = State(U(1, 0, 1, 3), U(2, 1, 1, 0));
            var declL = Run(left, 1, BattleScenarios.AttackCard()).OfType<AttackDeclaredEvent>().First();
            CollectionAssert.AreEqual(new[] { new Cell(1, 2) }, declL.StrikeCells);
        }

        // 통과(p2-p1): 적이 왼쪽 인접 → 공격이 좌측으로 반전돼 명중.
        [Test]
        public void Pattern_HitsEnemyBehindAfterPassing()
        {
            var s = State(U(1, 0, 1, 2), U(2, 1, 1, 1));
            var hit = Run(s, 1, BattleScenarios.AttackCard()).OfType<HitEvent>().SingleOrDefault();
            Assert.IsNotNull(hit, "지나친 적도 방향이 반전돼 명중해야");
            Assert.AreEqual(2, hit.Target.Value);
        }

        // 스택: 한 셀에 겹친 두 적 모두 명중(Grid.UnitsAt 복수).
        [Test]
        public void Pattern_MultiHitsStackedEnemiesInOneCell()
        {
            var s = State(U(1, 0, 1, 1), U(2, 1, 1, 2), U(3, 1, 1, 2));
            var hitIds = Run(s, 1, BattleScenarios.AttackCard())
                .OfType<HitEvent>().Select(h => h.Target.Value).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { 2, 3 }, hitIds);
        }

        // 다중 셀 패턴(라인) → 여러 셀의 적 모두 명중.
        [Test]
        public void Pattern_MultiCellLine_HitsAllCells()
        {
            var s = State(U(1, 0, 1, 0), U(2, 1, 1, 1), U(3, 1, 1, 2));
            var hitIds = Run(s, 1, BattleScenarios.AttackCard(new Cell(0, 1), new Cell(0, 2)))
                .OfType<HitEvent>().Select(h => h.Target.Value).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { 2, 3 }, hitIds);
        }

        // 빈 패턴 → 타격 셀 없음 → Miss.
        [Test]
        public void EmptyPattern_Misses()
        {
            var s = State(U(1, 0, 1, 1), U(2, 1, 1, 2));
            var tl = Run(s, 1, BattleScenarios.AttackCard(new Cell[0]));
            Assert.IsEmpty(tl.OfType<HitEvent>());
            Assert.IsNotEmpty(tl.OfType<MissEvent>());
        }

        // 경계 밖 오프셋 드롭(동일 칸 적 → 기본 동쪽 → 우측 off-grid).
        [Test]
        public void OutOfBoundsOffset_Dropped()
        {
            var s = State(U(1, 0, 1, 4), U(2, 1, 1, 4));
            var decl = Run(s, 1, BattleScenarios.AttackCard()).OfType<AttackDeclaredEvent>().First();
            Assert.IsEmpty(decl.StrikeCells, "경계 밖 셀은 드롭");
        }

        // 아군은 타격 셀에 있어도 비명중(같은 팀 제외).
        [Test]
        public void Allies_AreNotHit()
        {
            var s = State(U(1, 0, 1, 1), U(2, 0, 1, 2), U(3, 1, 1, 3)); // (1,2)=아군, 적=(1,3)
            var tl = Run(s, 1, BattleScenarios.AttackCard());           // facing East → strike (1,2)=아군
            Assert.IsEmpty(tl.OfType<HitEvent>(), "아군 비명중");
            Assert.IsNotEmpty(tl.OfType<MissEvent>());
        }
    }
}