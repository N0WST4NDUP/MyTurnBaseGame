using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    public class RoundInput
    {
        public IReadOnlyDictionary<UnitId, Card[]> Plans;   // 각 배열 길이 = 3(슬롯)
    }
}
