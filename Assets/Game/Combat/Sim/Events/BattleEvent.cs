namespace MyTurnBase.Combat.Sim
{
    public abstract class BattleEvent
    {
        public readonly int Round, Slot;
        public readonly Phase Phase;
        public readonly UnitId Actor;

        protected BattleEvent(int round, int slot, Phase phase, UnitId actor)
        {
            Round = round;
            Slot = slot;
            Phase = phase;
            Actor = actor;
        }

        public abstract override string ToString(); // 결정론 비교 + 리플레이용
    }
}
