# 🧩 결정론 전투 sim 아키텍처 (E1 #11)

> 전투 시뮬레이션의 **코드 구조·결정론 계약**. 게임 규칙은 `combat-rules.md`, 정체성은 `GDD.md`.
> 위치: `Assets/Game/Combat/Sim/`. 어셈블리: `MyTurnBase.Combat.Sim` (`noEngineReferences` — UnityEngine 비참조).

## 설계 원칙
- **헤드리스·순수 C#** — UnityEngine 비참조. 외부 머신 구현·단위테스트·RL 이식을 컴파일러가 강제.
- **결정론** — 동일 입력 + 동일 시드 → 동일 이벤트 타임라인.
- **확장 대비** — 특정 유닛 A/B 하드코딩 금지(유닛 리스트). n대n/태깅 확장 여지.

## 코드 배치
```
Assets/Game/Combat/Sim/
  MyTurnBase.Combat.Sim.asmdef     (noEngineReferences)
  Core/        Cell · UnitId · TeamId · Unit · BattleState · Card · RoundInput
  Random/      IRng · XorShiftRng
  Events/      Phase · BattleEvent · (이벤트 8종)
  Resolution/  IBattleResolver · StubBattleResolver
  Tests/       (EditMode) BattleScenarios · DeterminismTests · RngTests
```

## 상태 모델
- `Unit` — `UnitId Id · TeamId Team · Cell Pos · float Hp · int Arc · int Speed`. (런타임 전투 상태. 캐릭터 '정체성/데이터'는 E4의 별도 레이어.)
- `BattleState` — `3×5 그리드(Rows/Cols 상수) · IReadOnlyList<Unit> Units · int Round, Slot · IRng Rng`.
- `Cell{int Row,Col}` · `UnitId{int Value}` · `TeamId{int Value}` — UnityEngine 비참조용 자체 값타입(`IEquatable` 구현 → Dictionary 키 박싱 방지).

## 결정론 / RNG
- `interface IRng { int NextInt(int maxExclusive); }`.
- `XorShiftRng` — 자체 xorshift32. **`System.Random` 미사용**(런타임/버전 간 시퀀스 재현 비보장 회피).
  - seed 0 → 비영(非零) 보정. `maxExclusive <= 0` → `ArgumentOutOfRangeException`.
- 시드는 `BattleState` 생성 시 주입.
- **보장 범위 = 동일 빌드 내.** HP가 float라 머신/아키텍처가 다른 PvP의 정확 재현은 후속 과제 — 데미지 수식을 한 곳에 모아 추후 고정소수점 교체 여지를 남긴다.

## 입력 / 해결 계약
- `RoundInput` — 유닛별 3슬롯 계획: `IReadOnlyDictionary<UnitId, Card[]> Plans` (각 배열 길이 3).
- `Card{int Value}` — 추상 카드 핸들. 실제 카드 데이터·효과는 E2(#16).
- `interface IBattleResolver { IReadOnlyList<BattleEvent> ResolveRound(BattleState, RoundInput); }`.
- **결정론 가드레일**: 해결기는 순서 있는 `Units`만 순회하고 `Plans`(Dictionary)는 **키 조회만**(열거 순서 의존 금지).

## 이벤트 타임라인
- 출력 = `IReadOnlyList<BattleEvent>`. 각 이벤트: `Round · Slot · Phase · Actor(UnitId)` + 페이로드.
- `enum Phase { Move, Guard, Attack, Charge }` — 비트 순서와 1:1.
- **선언·판정·피해 분리** — 한 공격: `AttackDeclared`(타격 셀) → `Hit`/`Miss` → `Damage` → (HP≤0) `Defeat`.
- 이벤트 8종: `Move · Guard · AttackDeclared · Hit · Miss · Damage · Charge · Defeat`.
- 각 이벤트 `ToString()` = **InvariantCulture 안정 1줄 표현**(결정론 비교 + 리플레이/디버그 텍스트).
- (참고) 라운드 단위 마커(RoundStart/End)는 미도입 — 턴종료 +1 아크는 `ChargeEvent`로 표현.

## #11 범위 / 다음
- #11 = **타입 + 계약 + 결정론 골격**. `StubBattleResolver`는 자리표시(실제 규칙 아님).
- 실제 해결: 슬롯 루프·비트 순서·Speed 우선도 = **#12**, 이동 = **#13**, 명중 판정 = **#14**.

## 테스트 (EditMode)
- `DeterminismTests` — 동일 입력+시드 → 동일 타임라인 / 시드가 출력에 반영됨 / 타임라인 비어있지 않음.
- `RngTests` — 같은 시드 같은 시퀀스 / 범위 / `NextInt(<=0)` throw / seed 0 비잠금.
- 실행: Unity **Test Runner → EditMode**.
