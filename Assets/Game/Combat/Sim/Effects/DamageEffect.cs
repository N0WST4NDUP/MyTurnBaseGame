using System;

namespace MyTurnBase.Combat.Sim
{
    // 피해 효과(#17). 공격 비트서 프레임이 victim마다 호출(ctx.CurrentVictim 세팅 후).
    // 데미지 수치·공식(기본→가산→곱연산)·가드 경감 = PLACEHOLDER → E3(#20)가 채운다.
    // Spec.Magnitude 소비도 E3 — 지금은 검증용 고정 1.
    internal sealed class DamageEffect : IEffect
    {
        private const float PlaceholderHitDamage = 1f; // PLACEHOLDER = E3. defeat/선공우선 검증용 최소 양수.

        public Phase Beat => Phase.Attack;

        public void Apply(EffectContext ctx)
        {
            var victim = ctx.CurrentVictim;
            if (victim == null) return; // 방어적 — 프레임이 항상 세팅

            float amount = PlaceholderHitDamage;
            if (ctx.Guards != null && ctx.Guards.TryGetValue(victim.Id, out var g))
                amount = g.Full ? 0f : Math.Max(0f, amount - g.Reduction);

            victim.Hp -= amount;
            ctx.Timeline.Add(new DamageEvent(ctx.Round, ctx.Slot, ctx.Self.Id, victim.Id, amount, victim.Hp));
        }
    }
}
