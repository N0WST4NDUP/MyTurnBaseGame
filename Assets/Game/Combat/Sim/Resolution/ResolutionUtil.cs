using System.Collections.Generic;
using System.Linq;

namespace MyTurnBase.Combat.Sim
{
    internal static class ResolutionUtil
    {
        public static bool IsAlive(Unit u) => u.Hp > 0f;

        // 카드 → 비트는 이제 CardData.Type이 직접 보유(#16). 임시 %4 매핑(PhaseOf) 제거.

        public static bool TryGetCard(RoundInput input, UnitId id, int slot, out CardData card)
        {
            if (input?.Plans != null
                && input.Plans.TryGetValue(id, out var cards) // 키 조회만(열거 순서 비의존)
                && cards != null && slot >= 0 && slot < cards.Length)
            {
                card = cards[slot];
                return card != null; // null 슬롯 = 행동 없음(참조형 CardData → NRE 방지; 구조체 시절과 동일하게 안전)
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

        // 최근접(맨해튼) 살아있는 적. 거리 동률 = Units 순서(결정론).
        // 이동 의도(MoveEffect)와 공격 facing(ResolveStrikeCells) 둘 다 사용.
        // (이름은 #11 잔재 — 이제 placeholder 아님. NearestEnemy로 개명해도 좋음.)
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

        // 공격 패턴의 기준 방향(forward 단위벡터) = 최근접 적 쪽 카디널 4방.
        // 우세 축으로 결정(동률 = 수평 우선), 적 없음·동일 칸 → 기본 동쪽(+col).
        private static Cell FacingToward(BattleState s, Unit attacker)
        {
            var enemy = PlaceholderTarget(s, attacker);
            if (enemy == null) return new Cell(0, 1);

            int dr = enemy.Pos.Row - attacker.Pos.Row;
            int dc = enemy.Pos.Col - attacker.Pos.Col;
            if (dr == 0 && dc == 0) return new Cell(0, 1); // 동일 칸 → 기본 동쪽
            int adr = dr < 0 ? -dr : dr; // (using System 회피용 인라인 abs)
            int adc = dc < 0 ? -dc : dc;
            if (adc >= adr) return new Cell(0, dc > 0 ? 1 : -1); // 수평 우선(동률 포함)
            return new Cell(dr > 0 ? 1 : -1, 0); // 수직
        }

        // 카드 공격 패턴(자기 기준 상대 오프셋) → 절대 타격 셀.
        // 패턴은 '동쪽(+col) 정면' 정준 프레임으로 저작 → facing 방향으로 90° 회전.
        //   forward=(fr,fc)일 때  (r,c) → (c*fr + r*fc,  c*fc - r*fr)
        //   동:(r,c) 서:(-r,-c) 남:(c,-r) 북:(-c,r)
        // 경계 밖 셀은 드롭, 중복 셀은 제거.
        public static List<Cell> ResolveStrikeCells(BattleState s, Unit attacker, CardData card)
        {
            var cells = new List<Cell>();
            if (card?.AttackPattern == null || card.AttackPattern.Count == 0) return cells;

            var f = FacingToward(s, attacker);
            int fr = f.Row, fc = f.Col;

            foreach (var off in card.AttackPattern)
            {
                int or = off.Col * fr + off.Row * fc;
                int oc = off.Col * fc - off.Row * fr;
                var cell = new Cell(attacker.Pos.Row + or, attacker.Pos.Col + oc);
                if (!Grid.InBounds(cell)) continue;
                if (!cells.Contains(cell)) cells.Add(cell);
            }
            return cells;
        }

        // 타격 셀에 있는 '적'을 결정론 순서(s.Units)로 수집 — 스택이라 한 셀에 여럿 가능.
        // 아군·자기 제외. 한 유닛 위치는 하나라 자연 de-dup.
        public static List<Unit> CollectVictims(BattleState s, Unit attacker, IReadOnlyList<Cell> strikeCells)
        {
            var victims = new List<Unit>();
            if (strikeCells == null || strikeCells.Count == 0) return victims;

            foreach (var v in s.Units)
            {
                if (!IsAlive(v) || v.Team.Value == attacker.Team.Value) continue;
                for (int i = 0; i < strikeCells.Count; i++)
                    if (v.Pos.Equals(strikeCells[i])) { victims.Add(v); break; }
            }
            return victims;
        }
    }
}
