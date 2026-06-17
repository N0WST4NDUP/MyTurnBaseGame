using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // ⚠️ STUB — 실제 전투 규칙이 아니다. #11 '결정론 파이프라인 + 이벤트 형태' 시연/검증용.
    //    실제 슬롯·비트 해결=#12, 이동=#13, 명중 판정=#14, 데미지 공식=E3 가 전부 이걸 대체한다.
    //    핵심 보장: (1) s.Rng를 실제로 소비 → 결정론 테스트가 의미 있음
    //              (2) s.Units 순회(n-safe)
    //              (3) 입력 카드값으로 분기 → 입력이 다르면 타임라인도 다름
    public class StubBattleResolver : IBattleResolver
    {
        public IReadOnlyList<BattleEvent> ResolveRound(BattleState s, RoundInput input)
        {
            var tl = new List<BattleEvent>();

            for (int slot = 0; slot < 3; slot++)            // 슬롯 1→2→3 (진짜 '동시 해결'은 #12)
            {
                foreach (var u in s.Units)                  // 리스트 순회(n-safe)
                {
                    Card card = CardAt(input, u.Id, slot);
                    int roll = s.Rng.NextInt(100);          // ← RNG 소비(결정론 검증 포인트)

                    switch (card.Value % 4)                 // 가짜 분기(실제론 카드 데이터가 결정 — #16)
                    {
                        case 0: // 가짜 이동
                            tl.Add(new MoveEvent(s.Round, slot, u.Id,
                                u.Pos, new Cell(u.Pos.Row, (u.Pos.Col + 1) % BattleState.Cols)));
                            break;

                        case 1: // 가짜 가드
                            tl.Add(new GuardEvent(s.Round, slot, u.Id,
                                full: roll % 5 == 0, reduction: roll % 5));
                            break;

                        case 2: // 가짜 공격: 선언 → (roll로 명중/빗맞) → 피해 → (드물게)사망
                            var atCell = new Cell(u.Pos.Row, (u.Pos.Col + 1) % BattleState.Cols);
                            tl.Add(new AttackDeclaredEvent(s.Round, slot, u.Id, card, new[] { atCell }));

                            UnitId? target = FirstEnemy(s, u.Team);
                            if (target.HasValue && roll % 2 == 0)
                            {
                                tl.Add(new HitEvent(s.Round, slot, u.Id, target.Value, atCell));
                                tl.Add(new DamageEvent(s.Round, slot, u.Id, target.Value,
                                    amount: roll % 7, hpAfter: roll % 11)); // 수치는 전부 가짜
                                if (roll == 0)                               // 사망 이벤트도 시연
                                    tl.Add(new DefeatEvent(s.Round, slot, target.Value));
                            }
                            else
                            {
                                tl.Add(new MissEvent(s.Round, slot, u.Id));
                            }
                            break;

                        default: // 가짜 충전(+1)
                            tl.Add(new ChargeEvent(s.Round, slot, u.Id, amount: 1, arcAfter: u.Arc + 1));
                            break;
                    }
                }
            }

            return tl;
        }

        // 입력에서 (유닛, 슬롯)의 카드 꺼내기. 없으면 Card(0).
        private static Card CardAt(RoundInput input, UnitId id, int slot)
        {
            if (input?.Plans != null
                && input.Plans.TryGetValue(id, out var cards)
                && cards != null && slot < cards.Length)
                return cards[slot];
            return new Card(0);
        }

        // 다른 팀의 첫 유닛(가짜 타겟팅). 실제 타겟팅·명중은 #14.
        private static UnitId? FirstEnemy(BattleState s, TeamId myTeam)
        {
            foreach (var u in s.Units)
                if (u.Team.Value != myTeam.Value)
                    return u.Id;
            return null;
        }
    }
}
