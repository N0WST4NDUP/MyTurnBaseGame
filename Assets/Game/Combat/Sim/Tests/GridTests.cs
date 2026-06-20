using System.Linq;
using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #13 그리드 유틸: 경계 · 거리/인접 · 위치 질의(스택 가능).
    public class GridTests
    {
        [Test]
        public void InBounds_AcceptsInside_RejectsOutside()
        {
            Assert.IsTrue(Grid.InBounds(new Cell(0, 0)));
            Assert.IsTrue(Grid.InBounds(new Cell(BattleState.Rows - 1, BattleState.Cols - 1)));
            Assert.IsFalse(Grid.InBounds(new Cell(-1, 0)));
            Assert.IsFalse(Grid.InBounds(new Cell(0, -1)));
            Assert.IsFalse(Grid.InBounds(new Cell(BattleState.Rows, 0)));
            Assert.IsFalse(Grid.InBounds(new Cell(0, BattleState.Cols)));
        }

        [Test]
        public void Manhattan_And_Adjacency()
        {
            Assert.AreEqual(0, Grid.Manhattan(new Cell(1, 1), new Cell(1, 1)));
            Assert.AreEqual(3, Grid.Manhattan(new Cell(0, 0), new Cell(1, 2)));
            Assert.IsTrue(Grid.AreAdjacent(new Cell(1, 1), new Cell(1, 2)));   // 직교 인접
            Assert.IsTrue(Grid.AreAdjacent(new Cell(1, 1), new Cell(0, 1)));
            Assert.IsFalse(Grid.AreAdjacent(new Cell(1, 1), new Cell(1, 3)));  // 2칸
            Assert.IsFalse(Grid.AreAdjacent(new Cell(1, 1), new Cell(2, 2)));  // 대각 ≠ 인접
            Assert.IsFalse(Grid.AreAdjacent(new Cell(1, 1), new Cell(1, 1)));  // 자기 자신
        }

        [Test]
        public void UnitsAt_ReturnsOccupants_EmptyWhenNone()
        {
            var s = BattleScenarios.TwoUnits(1);                        // U1(1,0), U2(1,4)
            Assert.AreEqual(1, Grid.UnitsAt(s, new Cell(1, 0)).Single().Id.Value);
            Assert.IsEmpty(Grid.UnitsAt(s, new Cell(2, 2)));
        }
    }
}
