using System;
using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // 슬롯 한정 가드 상태(PLACEHOLDER 값).
    internal readonly struct GuardState
    {
        public readonly bool Full;
        public readonly float Reduction;
        public GuardState(bool full, float reduction) { Full = full; Reduction = reduction; }
    }

    internal static class BeatResolver
    {
        private const float PlaceholderHitDamage = 1f; // PLACEHOLDER 데미지 — 실제 공식 = E3. defeat 파이프라인/선공우선 검증용 최소 양수값.
        private const int PlaceholderChargeAmount = 1; // PLACEHOLDER 기본 충전량 — 실제 n(캐릭터별)·cap = #16/E3.

        // ── 비트: 이동 ─────────────────────────────────────────────
        // 동시 이동: 비트 시작 위치 기준 목표를 먼저 모은 뒤 일괄 적용.
        public static void ResolveMoveBeat(BattleState s, int round, int slot, List<Unit> movers, List<BattleEvent> tl)
        {
            if (movers.Count == 0) return;

            var targets = new List<Cell>(movers.Count);
            foreach (var u in movers)
            {
                targets.Add(ResolutionUtil.PlaceholderMoveIntent(s, u)); // PLACEHOLDER #16: 카드가 방향 결정
            }

            for (int i = 0; i < movers.Count; i++)
            {
                var u = movers[i];
                var to = targets[i];
                if (to.Equals(u.Pos) || !Grid.InBounds(to)) continue; // 제자리(이동 없음) or 경계 밖 → 이벤트 없음
                var from = u.Pos;
                u.Pos = to;
                tl.Add(new MoveEvent(round, slot, u.Id, from, to));
            }
        }

        // ── 비트: 가드 ─────────────────────────────────────────────
        // 가드는 '이 슬롯' 공격 비트에만 영향(슬롯 한정). 적용 값은 PLACEHOLDER(E3/#16).
        public static Dictionary<UnitId, GuardState> ResolveGuardBeat(int round, int slot, List<Unit> guarders, List<BattleEvent> tl)
        {
            var guards = new Dictionary<UnitId, GuardState>();
            foreach (var u in guarders)
            {
                // PLACEHOLDER: 완전/기본 구분과 경감 수치는 카드 데이터(#16)·밸런스(E3).
                var g = new GuardState(full: false, reduction: 0f);
                guards[u.Id] = g;
                tl.Add(new GuardEvent(round, slot, u.Id, g.Full, g.Reduction));
            }
            return guards;
        }

        // ── 비트: 공격 ─────────────────────────────────────────────
        // Speed 높은 쪽 먼저(동률 → 시드 RNG). 선공 우선: 처치된 대상은 이후 자기 공격을 못 한다.
        public static void ResolveAttackBeat(BattleState s, int round, int slot, List<Unit> attackers,
            RoundInput input, Dictionary<UnitId, GuardState> guards, List<BattleEvent> tl)
        {
            if (attackers.Count == 0) return;

            var ordered = ResolutionUtil.OrderBySpeed(s.Rng, attackers);
            foreach (var u in ordered)
            {
                if (!ResolutionUtil.IsAlive(u)) continue; // 이 비트에서 이미 처치됨(선공 우선)

                ResolutionUtil.TryGetCard(input, u.Id, slot, out var card);       // 버킷 단계에서 존재 확인됨
                var target = ResolutionUtil.PlaceholderTarget(s, u);              // PLACEHOLDER #14: 패턴→타격 셀→명중
                var strikeCells = target != null ? new[] { target.Pos } : Array.Empty<Cell>();

                tl.Add(new AttackDeclaredEvent(round, slot, u.Id, card, strikeCells));

                if (target == null)
                {
                    tl.Add(new MissEvent(round, slot, u.Id));
                    continue;
                }

                tl.Add(new HitEvent(round, slot, u.Id, target.Id, target.Pos));

                // PLACEHOLDER 데미지(E3). 완전가드면 0, 기본가드면 고정 경감(와이어링만).
                float amount = PlaceholderHitDamage;
                if (guards.TryGetValue(target.Id, out var g))
                    amount = g.Full ? 0f : Math.Max(0f, amount - g.Reduction);

                target.Hp -= amount;
                tl.Add(new DamageEvent(round, slot, u.Id, target.Id, amount, target.Hp));

                if (!ResolutionUtil.IsAlive(target))
                    tl.Add(new DefeatEvent(round, slot, target.Id));
            }
        }

        // ── 비트: 충전 ─────────────────────────────────────────────
        public static void ResolveChargeBeat(int round, int slot, List<Unit> chargers, List<BattleEvent> tl)
        {
            foreach (var u in chargers)
            {
                if (!ResolutionUtil.IsAlive(u)) continue; // 공격 비트에서 처치됐을 수 있음
                int amount = PlaceholderChargeAmount; // PLACEHOLDER #16/E3: 캐릭터별 n·cap
                u.Arc += amount;
                tl.Add(new ChargeEvent(round, slot, u.Id, amount, u.Arc));
            }
        }
    }
}
