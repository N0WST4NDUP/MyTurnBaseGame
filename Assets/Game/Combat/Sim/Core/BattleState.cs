using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    public sealed class BattleState
    {
        public const int Rows = 3, Cols = 5; // 나중에 CSV나 여러가지로 확장 가능할듯
        public readonly IReadOnlyList<Unit> Units;
        public int Round, Slot;
        public readonly IRng Rng; // 시드 주입은 생성 시 new XorShiftRng(seed)

        public BattleState(IReadOnlyList<Unit> units, IRng rng, int round = 0, int slot = 0)
        {
            Units = units;
            Rng = rng;
            Round = round;
            Slot = slot;
        }
    }
}
