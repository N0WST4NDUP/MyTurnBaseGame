namespace MyTurnBase.Combat.Sim
{
    public sealed class Unit
    {
        public UnitId Id;
        public TeamId Team;
        public Cell Pos;
        public float Hp;
        public int Arc;
        public int Speed;
    }

    public readonly struct UnitId : System.IEquatable<UnitId>
    {
        public readonly int Value;

        public UnitId(int value)
        {
            Value = value;
        }

        public bool Equals(UnitId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UnitId o && Equals(o);
        public override int GetHashCode() => Value;
    }

    public readonly struct TeamId : System.IEquatable<TeamId>
    {
        public readonly int Value;

        public TeamId(int value)
        {
            Value = value;
        }

        public bool Equals(TeamId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TeamId o && Equals(o);
        public override int GetHashCode() => Value;
    }
}
