using System.Collections.Generic;
using System.Linq;

namespace MyTurnBase.Combat.Sim
{
    internal static class ResolutionUtil
    {
        public static bool IsAlive(Unit u) => u.Hp > 0f;

        // 카드 → 비트. PLACEHOLDER: 실제 매핑은 카드 데이터(E2 #16)가 결정.
        // enum Phase{Move=0,Guard=1,Attack=2,Charge=3}에 1:1 임시 매핑(음수 안전).
        public static Phase PhaseOf(Card card)
        {
            int k = ((card.Value % 4) + 4) % 4;
            return (Phase)k;
        }

        public static bool TryGetCard(RoundInput input, UnitId id, int slot, out Card card)
        {
            if (input?.Plans != null
                && input.Plans.TryGetValue(id, out var cards) // 키 조회만(열거 순서 비의존)
                && cards != null && slot >= 0 && slot < cards.Length)
            {
                card = cards[slot];
                return true;
            }
            card = default;
            return false;
        }

        // Speed 내림차순. 동률(정확히 같은 Speed)만 시드 RNG로 순서 결정.
        // (근접 동률 '임계치'는 밸런스 영역 = E3 → 지금은 정확 동률만.)
        public static List<Unit> OrderBySpeed(IRng rng, List<Unit> attackers)
        {
            var ordered = attackers.OrderByDescending(u => u.Speed).ToList(); // 안정 정렬: 동률 시 Units 순서 유지
            int i = 0;
            while (i < ordered.Count)
            {
                int j = i + 1;
                while (j < ordered.Count && ordered[j].Speed == ordered[i].Speed) j++;
                if (j - i > 1) ShuffleInPlace(rng, ordered, i, j - i); // 동률 그룹만 셔플
                i = j;
            }
            return ordered;
        }

        // Fisher-Yates(부분 범위). 동률이 있을 때만 호출 → 동률 없는 시나리오는 RNG 비소비.
        private static void ShuffleInPlace(IRng rng, List<Unit> list, int start, int count)
        {
            for (int k = count - 1; k >= 1; k--)
            {
                int r = rng.NextInt(k + 1); // [0, k]
                int a = start + k, b = start + r;
                var tmp = list[a]; list[a] = list[b]; list[b] = tmp;
            }
        }

        // PLACEHOLDER #16: 카드가 방향을 정할 때까지 '가장 가까운 적 쪽 직교 1칸' 의도만 생성.
        // 경계·점유·경합 판정은 BeatResolver.ResolveMoveBeat가 담당(여기선 raw intent).
        public static Cell PlaceholderMoveIntent(BattleState s, Unit u)
        {
            var enemy = PlaceholderTarget(s, u);
            if (enemy == null) return u.Pos;

            int row = u.Pos.Row, col = u.Pos.Col;
            if (enemy.Pos.Col != col) col += enemy.Pos.Col > col ? 1 : -1;
            else if (enemy.Pos.Row != row) row += enemy.Pos.Row > row ? 1 : -1;
            return new Cell(row, col);
        }

        // PLACEHOLDER #14: 가장 가까운(맨해튼) 살아있는 적. 거리 동률은 Units 순서. 실제 패턴·명중이 대체.
        public static Unit PlaceholderTarget(BattleState s, Unit self)
        {
            Unit best = null;
            int bestDist = int.MaxValue;
            foreach (var u in s.Units)
            {
                if (!IsAlive(u) || u.Team.Value == self.Team.Value) continue;
                int d = Grid.Manhattan(self.Pos, u.Pos);
                if (d < bestDist) { bestDist = d; best = u; }
            }
            return best;
        }
    }
}
