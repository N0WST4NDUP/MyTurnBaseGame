namespace MyTurnBase.Combat.Sim
{
    // 이동 효과(#17). 비트시작 스냅샷 기준 최근접 적 쪽 직교 1칸 의도 → 경계/제자리면 무이벤트.
    // 스냅샷에서 위치를 읽으므로 순차 Apply여도 동시성(일괄적용) 유지.
    // 방향 결정은 PLACEHOLDER(추후 MoveOffset 소비로 교체).
    internal sealed class MoveEffect : IEffect
    {
        public Phase Beat => Phase.Move;

        public void Apply(EffectContext ctx)
        {
            var from = StartPos(ctx, ctx.Self.Id, ctx.Self.Pos);
            var to = Intent(ctx, from);
            if (to.Equals(from) || !Grid.InBounds(to)) return; // 제자리/경계 밖 → 이벤트 없음
            ctx.Self.Pos = to;
            ctx.Timeline.Add(new MoveEvent(ctx.Round, ctx.Slot, ctx.Self.Id, from, to));
        }

        // 최근접(맨해튼) 살아있는 적 쪽 직교 1칸. 위치/거리는 스냅샷 기준(동시성).
        private static Cell Intent(EffectContext ctx, Cell from)
        {
            bool found = false;
            Cell enemyPos = default;
            int bestDist = int.MaxValue;
            foreach (var u in ctx.State.Units)
            {
                if (u.Hp <= 0f || u.Team.Value == ctx.Self.Team.Value) continue;
                var pos = StartPos(ctx, u.Id, u.Pos);
                int d = Grid.Manhattan(from, pos);
                if (d < bestDist) { bestDist = d; enemyPos = pos; found = true; }
            }
            if (!found) return from;

            int row = from.Row, col = from.Col;
            if (enemyPos.Col != col) col += enemyPos.Col > col ? 1 : -1;
            else if (enemyPos.Row != row) row += enemyPos.Row > row ? 1 : -1;
            return new Cell(row, col);
        }

        private static Cell StartPos(EffectContext ctx, UnitId id, Cell live)
            => ctx.MoveSnapshot != null && ctx.MoveSnapshot.TryGetValue(id, out var p) ? p : live;
    }
}
