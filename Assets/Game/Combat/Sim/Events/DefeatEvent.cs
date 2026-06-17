using System;

namespace MyTurnBase.Combat.Sim
{
    // 유닛 사망(HP <= 0). Actor = 쓰러진 유닛(이 이벤트의 '주체').
    // MVP: 사망 트리거는 공격뿐 → Phase.Attack 고정. 다른 페이즈 사망이 생기면 phase 파라미터화.
    public sealed class DefeatEvent : BattleEvent
    {
        public DefeatEvent(int round, int slot, UnitId actor)
            : base(round, slot, Phase.Attack, actor)
        {
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} DEFEAT");
        }
    }
}
