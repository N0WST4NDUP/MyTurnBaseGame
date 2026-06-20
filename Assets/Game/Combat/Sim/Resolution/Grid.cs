using System;
using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // 그리드 기하 + 위치 질의. 이동(#13)·거리/인접·명중(#14)에서 공용.
    internal static class Grid
    {
        public static bool InBounds(Cell c) => (
            c.Row >= 0 && c.Row < BattleState.Rows &&
            c.Col >= 0 && c.Col < BattleState.Cols
        );

        public static int Manhattan(Cell a, Cell b) => Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col);

        public static bool AreAdjacent(Cell a, Cell b) => Manhattan(a, b) == 1;

        // 해당 셀의 살아있는 유닛들(스택 가능 → 복수). #14 명중 판정이 사용.
        public static IEnumerable<Unit> UnitsAt(BattleState s, Cell c)
        {
            foreach (var u in s.Units)
            {
                if (ResolutionUtil.IsAlive(u) && u.Pos.Equals(c))
                {
                    yield return u;
                }
            }
        }
    }
}
