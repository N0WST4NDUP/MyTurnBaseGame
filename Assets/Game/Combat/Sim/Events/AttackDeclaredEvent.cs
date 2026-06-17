using System;
using System.Collections.Generic;
using System.Linq;

namespace MyTurnBase.Combat.Sim
{
    // 공격 '선언'(windup) — 어느 셀을 노리는지. 명중 판정(Hit/Miss) '전'에 발생.
    // 패턴(상대 좌표)→절대 셀 변환은 #14. 본 이벤트는 변환된 타격 셀을 기록.
    // StrikeCells: 비어 있을 수는 있으나 null 금지(resolver가 항상 리스트 제공).
    public sealed class AttackDeclaredEvent : BattleEvent
    {
        public readonly Card Card;
        public readonly IReadOnlyList<Cell> StrikeCells;

        public AttackDeclaredEvent(int round, int slot, UnitId actor, Card card, IReadOnlyList<Cell> strikeCells)
            : base(round, slot, Phase.Attack, actor)
        {
            Card = card;
            StrikeCells = strikeCells;
        }

        public override string ToString()
        {
            var cells = StrikeCells == null
                ? ""
                : string.Join(";", StrikeCells.Select(c => FormattableString.Invariant($"({c.Row},{c.Col})")));
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} ATK_DECL card={Card.Value} cells=[{cells}]");
        }
    }
}
