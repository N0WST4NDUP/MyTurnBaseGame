using System;
using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // 책임 범위(#12):
    //   - 슬롯 1→2→3 순차 해결, 슬롯 내 비트 순서: 이동 → 가드 → 공격 → 충전.
    //   - 공격 비트의 Speed 우선도(높은 쪽 먼저) + 동률 시 시드 RNG 타이브레이크.
    //   - 동시성(같은 비트는 비트 시작 상태 기준) + 사망(Defeat) 처리.
    //   - 확정 결정: 상태 = 가변 in-place / 공격 = 선공 우선(처치된 대상은 반격 못 함).
    //
    // 범위 밖(아래 'PLACEHOLDER' 지점을 후속 이슈가 교체):
    //   - 카드→비트 매핑(PhaseOf)           : 실제 카드 데이터 = E2 #16
    //   - 실제 이동 규칙(방향·사거리·충돌)  : #13
    //   - 타겟팅·명중 판정(패턴→타격 셀)    : #14
    //   - 데미지 공식·가드 경감·아크 cap    : E3
    //
    // 결정론 가드레일(combat-sim.md): s.Units(순서 보장)만 순회, Plans는 키 조회만,
    //   동률 정렬은 s.Rng로만 깬다. 라운드 카운터(s.Round) 증가는 상위 전투 루프 몫.
    public sealed class RoundResolver : IBattleResolver
    {
        public IReadOnlyList<BattleEvent> ResolveRound(BattleState s, RoundInput input)
        {
            var tl = new List<BattleEvent>();
            int round = s.Round;

            for (int slot = 0; slot < 3; slot++)
            {
                // 슬롯 시작 시점의 '살아있는 유닛'을 비트별로 분류(동시성 스냅샷).
                // 죽은 유닛은 어떤 비트에도 참여하지 않는다.
                var movers = new List<Unit>();
                var guarders = new List<Unit>();
                var attackers = new List<Unit>();
                var chargers = new List<Unit>();

                foreach (var u in s.Units)
                {
                    if (!ResolutionUtil.IsAlive(u)) continue;
                    if (!ResolutionUtil.TryGetCard(input, u.Id, slot, out var card)) continue;

                    var phase = ResolutionUtil.PhaseOf(card);
                    switch (phase)
                    {
                        case Phase.Move: movers.Add(u); break;
                        case Phase.Guard: guarders.Add(u); break;
                        case Phase.Attack: attackers.Add(u); break;
                        case Phase.Charge: chargers.Add(u); break;
                        default: throw new InvalidOperationException($"미처리 Phase: {phase}");
                    }
                }

                BeatResolver.ResolveMoveBeat(s, round, slot, movers, tl);
                var guards = BeatResolver.ResolveGuardBeat(round, slot, guarders, tl);
                BeatResolver.ResolveAttackBeat(s, round, slot, attackers, input, guards, tl);
                BeatResolver.ResolveChargeBeat(round, slot, chargers, tl);
            }

            return tl;
        }
    }
}
