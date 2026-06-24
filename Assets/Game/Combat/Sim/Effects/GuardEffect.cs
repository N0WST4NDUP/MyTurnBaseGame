namespace MyTurnBase.Combat.Sim
{
    // 가드 효과(#17). 슬롯 한정 가드 상태를 ctx.Guards에 등록 → 같은 슬롯 공격 비트가 읽는다.
    // 완전/기본 구분·경감 수치 = PLACEHOLDER → E3.
    internal sealed class GuardEffect : IEffect
    {
        public Phase Beat => Phase.Guard;

        public void Apply(EffectContext ctx)
        {
            var g = new GuardState(full: false, reduction: 0f); // PLACEHOLDER = E3
            if (ctx.Guards != null) ctx.Guards[ctx.Self.Id] = g;
            ctx.Timeline.Add(new GuardEvent(ctx.Round, ctx.Slot, ctx.Self.Id, g.Full, g.Reduction));
        }
    }
}
