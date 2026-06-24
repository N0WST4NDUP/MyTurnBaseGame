using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // 책임 범위(#12 + #17):
    //   - 슬롯 1→2→3 순차 해결, 슬롯 내 비트 순서: 이동 → 가드 → 공격 → 충전.
    //   - 비트 분류 = effects[]의 Phase(#17). card.Type은 메타(UI/AI) — 실행 라우팅에 미사용.
    //     한 카드가 여러 비트의 effect 보유 가능(멀티비트 카드: 한 유닛이 여러 비트에 등장).
    //   - 공격 비트의 Speed 우선도 + 동률 시드 RNG 타이브레이크 + 선공 우선은 BeatResolver가 소유.
    //   - 상태 = 가변 in-place. 라운드 카운터(s.Round) 증가는 상위 전투 루프 몫.
    //
    // 범위 밖(PLACEHOLDER, 후속 이슈 교체):
    //   - 이동 방향(MoveOffset 소비)          : 후속(현재 최근접 적 1칸)
    //   - 데미지 공식·가드 경감·아크 cap·충전량 : E3(effect 내부)
    //
    // 결정론 가드레일(combat-sim.md): s.Units(순서 보장)만 순회 · Plans는 키 조회만 ·
    //   비트내 순서 = s.Units × card.Effects[] 배열순 · 동률 정렬은 s.Rng로만 · 레지스트리는 키 조회만.
    public sealed class RoundResolver : IBattleResolver
    {
        public IReadOnlyList<BattleEvent> ResolveRound(BattleState s, RoundInput input)
        {
            var tl = new List<BattleEvent>();
            int round = s.Round;

            for (int slot = 0; slot < 3; slot++)
            {
                // 슬롯 시작 시점의 '살아있는 유닛'을 effect의 Phase로 비트별 분류(동시성 스냅샷).
                var movers = new List<BeatEntry>();
                var guarders = new List<BeatEntry>();
                var attackers = new List<BeatEntry>();
                var chargers = new List<BeatEntry>();

                foreach (var u in s.Units)
                {
                    if (!ResolutionUtil.IsAlive(u)) continue;
                    if (!ResolutionUtil.TryGetCard(input, u.Id, slot, out var card)) continue;

                    bool m = false, g = false, a = false, c = false;
                    foreach (var spec in card.Effects)
                    {
                        switch (EffectRegistry.Resolve(spec.EffectKey).Beat) // 미등록 키 → throw(fail-fast)
                        {
                            case Phase.Move: m = true; break;
                            case Phase.Guard: g = true; break;
                            case Phase.Attack: a = true; break;
                            case Phase.Charge: c = true; break;
                        }
                    }

                    var entry = new BeatEntry(u, card);
                    if (m) movers.Add(entry);
                    if (g) guarders.Add(entry);
                    if (a) attackers.Add(entry);
                    if (c) chargers.Add(entry);
                }

                var ctx = new EffectContext
                {
                    State = s,
                    Round = round,
                    Slot = slot,
                    Timeline = tl,
                    Rng = s.Rng,
                    Guards = new Dictionary<UnitId, GuardState>(),
                };

                BeatResolver.ResolveMoveBeat(ctx, movers);
                BeatResolver.ResolveGuardBeat(ctx, guarders);
                BeatResolver.ResolveAttackBeat(ctx, attackers);
                BeatResolver.ResolveChargeBeat(ctx, chargers);
            }

            return tl;
        }
    }
}
