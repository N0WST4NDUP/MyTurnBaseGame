using System;

namespace MyTurnBase.Combat.Sim
{
    // 공격이 아무도 못 맞춤(판정). 타격 셀에 대상 없음.
    public sealed class MissEvent : BattleEvent
    {
        public MissEvent(int round, int slot, UnitId actor)
            : base(round, slot, Phase.Attack, actor)
        {
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} MISS");
        }
    }
}
