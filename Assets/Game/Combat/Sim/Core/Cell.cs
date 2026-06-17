namespace MyTurnBase.Combat.Sim
{
    public readonly struct Cell
    {
        public readonly int Row, Col;

        public Cell(int r, int c)
        {
            Row = r;
            Col = c;
        }
    }
}
