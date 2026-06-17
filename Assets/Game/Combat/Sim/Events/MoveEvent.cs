using System;

namespace MyTurnBase.Combat.Sim
{
    // 이동(기본 직교 1칸). 비트: Move.
    public sealed class MoveEvent : BattleEvent
    {
        public readonly Cell From, To;

        public MoveEvent(int round, int slot, UnitId actor, Cell from, Cell to)
            : base(round, slot, Phase.Move, actor)
        {
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return FormattableString.Invariant(
                $"R{Round} S{Slot} {Phase} a={Actor.Value} MOVE ({From.Row},{From.Col})->({To.Row},{To.Col})");
        }
    }
}
