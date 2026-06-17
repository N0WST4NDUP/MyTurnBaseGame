using System;

namespace MyTurnBase.Combat.Sim
{
    // 가드 설정. 비트: Guard.
    // Full=true  → 완전가드(데미지 0, 고유·아크 소모)
    // Full=false → 기본가드(고정 수치 Reduction 만큼 경감, 공용)
    // 경감 수치는 카드/밸런스(#16·E3)에서 결정. 본 이벤트는 '적용된 값'을 기록만.
    public sealed class GuardEvent : BattleEvent
    {
        public readonly bool Full;
        public readonly float Reduction;

        public GuardEvent(int round, int slot, UnitId actor, bool full, float reduction)
            : base(round, slot, Phase.Guard, actor)
        {
            Full = full;
            Reduction = reduction;
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} GUARD full={Full} red={Reduction:F3}");
        }
    }
}
