using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    public interface IBattleResolver
    {
        IReadOnlyList<BattleEvent> ResolveRound(BattleState s, RoundInput input);
    }
}
