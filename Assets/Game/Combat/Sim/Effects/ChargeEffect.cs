namespace MyTurnBase.Combat.Sim
{
    // 충전 효과(#17). 아크 가산. 충전량 n(캐릭터별)·cap = PLACEHOLDER → E3(#19).
    // Spec.Magnitude 소비도 E3 — 지금은 고정 1.
    internal sealed class ChargeEffect : IEffect
    {
        private const int PlaceholderChargeAmount = 1; // PLACEHOLDER = E3

        public Phase Beat => Phase.Charge;

        public void Apply(EffectContext ctx)
        {
            int amount = PlaceholderChargeAmount;
            ctx.Self.Arc += amount;
            ctx.Timeline.Add(new ChargeEvent(ctx.Round, ctx.Slot, ctx.Self.Id, amount, ctx.Self.Arc));
        }
    }
}
