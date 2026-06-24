using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // 비트에 참여하는 (유닛, 카드) 한 쌍. RoundResolver가 비트별로 수집해 비트러너에 넘긴다.
    internal readonly struct BeatEntry
    {
        public readonly Unit Unit;
        public readonly CardData Card;
        public BeatEntry(Unit unit, CardData card) { Unit = unit; Card = card; }
    }

    // 비트 오케스트레이션(동시성·순서·선공우선)은 프레임이 소유. 실제 동작은 effects[]가 수행(#17).
    //   - 이동: 비트시작 스냅샷 → 동시성 / 공격: Speed순 + 선공우선 / 충전: 사망 스킵.
    // 수치(데미지·충전량·가드 경감)는 effect 내부 PLACEHOLDER → E3.
    internal static class BeatResolver
    {
        // ── 비트: 이동 ─────────────────────────────────────────────
        // 비트시작 위치를 스냅샷해 ctx에 실음 → MoveEffect가 스냅샷 기준으로 의도 계산(순차 적용도 동시성 유지).
        public static void ResolveMoveBeat(EffectContext ctx, List<BeatEntry> movers)
        {
            if (movers.Count == 0) return;

            var snapshot = new Dictionary<UnitId, Cell>();
            foreach (var u in ctx.State.Units) snapshot[u.Id] = u.Pos;
            ctx.MoveSnapshot = snapshot;

            foreach (var m in movers) ApplyBeat(ctx, m.Unit, m.Card, Phase.Move);

            ctx.MoveSnapshot = null;
        }

        // ── 비트: 가드 ─────────────────────────────────────────────
        // GuardEffect가 ctx.Guards에 슬롯 한정 상태 등록 → 같은 슬롯 공격 비트가 읽는다.
        public static void ResolveGuardBeat(EffectContext ctx, List<BeatEntry> guarders)
        {
            foreach (var g in guarders) ApplyBeat(ctx, g.Unit, g.Card, Phase.Guard);
        }

        // ── 비트: 공격 ─────────────────────────────────────────────
        // Speed 높은 쪽 먼저(동률 → 시드 RNG). 선공 우선: 처치된 공격자는 자기 공격을 못 한다.
        // 기하(패턴→타격셀→victim, #14)는 프레임. victim당 데미지는 effect(DamageEffect).
        public static void ResolveAttackBeat(EffectContext ctx, List<BeatEntry> attackers)
        {
            if (attackers.Count == 0) return;

            var s = ctx.State;
            var units = new List<Unit>(attackers.Count);
            var cardByUnit = new Dictionary<UnitId, CardData>(attackers.Count);
            foreach (var a in attackers) { units.Add(a.Unit); cardByUnit[a.Unit.Id] = a.Card; }

            var ordered = ResolutionUtil.OrderBySpeed(ctx.Rng, units);
            foreach (var attacker in ordered)
            {
                if (!ResolutionUtil.IsAlive(attacker)) continue; // 선공 우선: 이 비트에서 이미 처치됨

                var card = cardByUnit[attacker.Id];
                var strikeCells = ResolutionUtil.ResolveStrikeCells(s, attacker, card); // #14: 패턴→방향→절대 셀
                ctx.Timeline.Add(new AttackDeclaredEvent(ctx.Round, ctx.Slot, attacker.Id, card, strikeCells));

                var victims = ResolutionUtil.CollectVictims(s, attacker, strikeCells); // 스택 대응 다중 명중
                if (victims.Count == 0)
                {
                    ctx.Timeline.Add(new MissEvent(ctx.Round, ctx.Slot, attacker.Id));
                    continue;
                }

                ctx.Self = attacker;
                ctx.Card = card;
                foreach (var victim in victims)
                {
                    ctx.Timeline.Add(new HitEvent(ctx.Round, ctx.Slot, attacker.Id, victim.Id, victim.Pos));

                    ctx.CurrentVictim = victim;
                    ApplyEffectsOfBeat(ctx, card, Phase.Attack); // 데미지 등(DamageEvent emit)

                    if (!ResolutionUtil.IsAlive(victim))
                        ctx.Timeline.Add(new DefeatEvent(ctx.Round, ctx.Slot, victim.Id));
                }
                ctx.CurrentVictim = null;
            }
        }

        // ── 비트: 충전 ─────────────────────────────────────────────
        public static void ResolveChargeBeat(EffectContext ctx, List<BeatEntry> chargers)
        {
            foreach (var c in chargers)
            {
                if (!ResolutionUtil.IsAlive(c.Unit)) continue; // 공격 비트에서 처치됐을 수 있음
                ApplyBeat(ctx, c.Unit, c.Card, Phase.Charge);
            }
        }

        // 한 유닛의 해당 비트 효과들을 배열 순서로 적용(자기 대상 비트: 이동/가드/충전).
        private static void ApplyBeat(EffectContext ctx, Unit unit, CardData card, Phase beat)
        {
            ctx.Self = unit;
            ctx.Card = card;
            ApplyEffectsOfBeat(ctx, card, beat);
        }

        // ctx.Self/Card(공격은 CurrentVictim까지) 세팅 전제. card.Effects 중 해당 비트만 배열 순서로 Apply.
        private static void ApplyEffectsOfBeat(EffectContext ctx, CardData card, Phase beat)
        {
            foreach (var spec in card.Effects)
            {
                var effect = EffectRegistry.Resolve(spec.EffectKey); // 미등록 키 → throw(fail-fast)
                if (effect.Beat != beat) continue;
                ctx.Spec = spec;
                effect.Apply(ctx);
            }
        }
    }
}
