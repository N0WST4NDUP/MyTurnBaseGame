using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // effect 실행에 필요한 모든 입출력 통로. 프레임(BeatResolver)이 채워 effect.Apply에 건넨다.
    // effect는 무상태 → 여기서 읽고(State/Self/Card/Spec) 여기로 쓴다(상태 변경 + Timeline emit).
    internal sealed class EffectContext
    {
        public BattleState State;          // 전투 전체 상태(유닛/그리드/RNG) 읽기
        public Unit Self;                  // 이 호출의 주체 유닛
        public CardData Card;              // 발동 카드(AttackPattern 등 카드 필드 참조)
        public EffectSpec Spec;            // 이 effect의 저작 파라미터(Key+Magnitude). 소비는 E3.
        public int Round;
        public int Slot;
        public List<BattleEvent> Timeline; // 결과 이벤트 출력처
        public IRng Rng;

        // ── 비트별 스크래치 ──
        public Dictionary<UnitId, GuardState> Guards;          // 슬롯 가드맵(가드 write → 공격 read)
        public IReadOnlyDictionary<UnitId, Cell> MoveSnapshot; // 이동 비트시작 위치(동시성 보존)
        public Unit CurrentVictim;                             // 공격 비트 per-victim 대상
    }
}
