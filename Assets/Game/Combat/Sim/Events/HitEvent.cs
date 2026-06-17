using System;

namespace MyTurnBase.Combat.Sim
{
    // 공격이 대상에 명중(판정). 실제 피해 수치는 후속 DamageEvent로 분리.
    // Actor = 공격자, Target = 피격자.
    public sealed class HitEvent : BattleEvent
    {
        public readonly UnitId Target;
        public readonly Cell At;

        public HitEvent(int round, int slot, UnitId actor, UnitId target, Cell at)
            : base(round, slot, Phase.Attack, actor)
        {
            Target = target;
            At = at;
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} HIT t={Target.Value} @({At.Row},{At.Col})");
        }
    }
}
