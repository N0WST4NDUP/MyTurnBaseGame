using System;

namespace MyTurnBase.Combat.Sim
{
    // 아크 충전. 기모으기 카드(+n) 또는 턴종료 +1 등 '아크 증가'를 기록. 비트: Charge.
    public sealed class ChargeEvent : BattleEvent
    {
        public readonly int Amount;    // 이번에 충전된 양(+)
        public readonly int ArcAfter;  // 적용 후 아크 칸

        public ChargeEvent(int round, int slot, UnitId actor, int amount, int arcAfter)
            : base(round, slot, Phase.Charge, actor)
        {
            Amount = amount;
            ArcAfter = arcAfter;
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} CHARGE +{Amount} arc={ArcAfter}");
        }
    }
}
