using System;

namespace MyTurnBase.Combat.Sim
{
    public sealed class DamageEvent : BattleEvent
    {
        public readonly UnitId Target;
        public readonly float Amount, HpAfter;

        public DamageEvent(int round, int slot, UnitId actor, UnitId target, float amount, float hpAfter)
            : base(round, slot, Phase.Attack, actor)
        {
            Target = target;
            Amount = amount;
            HpAfter = hpAfter;
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} DMG t={Target.Value} amt={Amount:F3} hp={HpAfter:F3}");
        }
    }
}
