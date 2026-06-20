using System;

namespace MyTurnBase.Combat.Sim
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int Row, Col;

        public Cell(int r, int c)
        {
            Row = r;
            Col = c;
        }

        public bool Equals(Cell other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => System.HashCode.Combine(Row, Col);
    }
}
