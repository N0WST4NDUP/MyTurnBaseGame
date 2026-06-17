namespace MyTurnBase.Combat.Sim
{
    // 추상 핸들. 실제 카드 데이터·효과는 #16(E2)에서.
    public readonly struct Card
    {
        public readonly int Value;

        public Card(int value)
        {
            Value = value;
        }
    }
}
