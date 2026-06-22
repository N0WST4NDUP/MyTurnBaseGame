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
  Core/        Cell · UnitId · TeamId · Unit · BattleState · CardData · CardKind · EffectSpec · RoundInput
  Random/      IRng · XorShiftRng
  Events/      Phase · BattleEvent · (이벤트 8종)
  Resolution/  IBattleResolver · RoundResolver · BeatResolver · ResolutionUtil · Grid
  Tests/       (EditMode) BattleScenarios · DeterminismTests · ResolutionTests · MovementTests · GridTests · RngTests · CardDataTests

Assets/Game/Cards/                 (Unity 어셈블리 MyTurnBase.Cards → refs Sim, noEngineReferences=false)
  CardSO.cs                        (ScriptableObject 저작 · CreateAssetMenu "MyTurnBase/Card" · ToData())
  Tests/       (EditMode) CardSOTests
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
- `RoundInput` — 유닛별 3슬롯 계획: `IReadOnlyDictionary<UnitId, CardData[]> Plans` (각 배열 길이 3).
- `CardData` — 런타임 카드 데이터(순수 C#·불변): `Phase Type · int ArcCost · int Speed · Cell MoveOffset · IReadOnlyList<Cell> AttackPattern · IReadOnlyList<EffectSpec> Effects · string AnimKey · CardKind Kind`. 저작은 Unity `CardSO`(별도 어셈블리 `MyTurnBase.Cards`) → `ToData()`로 변환. (#16, E2)
  - `MoveOffset` 필드만 정의 — 이동 방향 **소비는 #13**. `AttackPattern` 필드만 정의 — 명중 판정은 **#14**. `Effects` 슬롯만 정의 — 효과 실행은 **#17**.
- `interface IBattleResolver { IReadOnlyList<BattleEvent> ResolveRound(BattleState, RoundInput); }`.
- **결정론 가드레일**: 해결기는 순서 있는 `Units`만 순회하고 `Plans`(Dictionary)는 **키 조회만**(열거 순서 의존 금지).

## 이벤트 타임라인
- 출력 = `IReadOnlyList<BattleEvent>`. 각 이벤트: `Round · Slot · Phase · Actor(UnitId)` + 페이로드.
- `enum Phase { Move, Guard, Attack, Charge }` — 비트 순서와 1:1.
- **선언·판정·피해 분리** — 한 공격: `AttackDeclared`(타격 셀) → `Hit`/`Miss` → `Damage` → (HP≤0) `Defeat`.
- 이벤트 8종: `Move · Guard · AttackDeclared · Hit · Miss · Damage · Charge · Defeat`.
- 각 이벤트 `ToString()` = **InvariantCulture 안정 1줄 표현**(결정론 비교 + 리플레이/디버그 텍스트).
- (참고) 라운드 단위 마커(RoundStart/End)는 미도입 — 턴종료 +1 아크는 `ChargeEvent`로 표현.

## 라운드 해결 (#12 `RoundResolver`)
- **슬롯 1→2→3 순차.** 슬롯 내 비트 순서 = **이동 → 가드 → 공격 → 충전**(유닛 입력 순서가 아니라 비트로 재정렬).
- **동시성**: 같은 비트는 '비트 시작 상태' 기준(이동은 목표를 먼저 모아 일괄 적용). **공격만** Speed 순차.
- **공격 우선도**: Speed 내림차순, **정확 동률 → 시드 RNG**(Fisher-Yates, 동률 그룹만 소비). 근접 동률 '임계치'는 밸런스 = E3.
- **선공 우선**: 빠른 쪽이 먼저 해결, 대상이 HP≤0이면 그 유닛은 이후 공격 불가(반격 무효).
- **상태 = 가변 in-place**: 해결기가 `Unit.Hp/Arc/Pos`를 직접 변경(불변/클론 아님). 라운드 카운터(`s.Round`) 증가는 상위 전투 루프 몫.
- **결정론 가드레일**: `Units`만 순회 · `Plans`는 키 조회만 · 동률은 RNG로만.
- **구조**: `RoundResolver`(오케스트레이터, public) → `BeatResolver`(비트 적용) + `ResolutionUtil`(순수 헬퍼·정렬·PLACEHOLDER) — 뒤 둘은 `internal`.

### PLACEHOLDER (후속 이슈가 교체)
- ~~카드→비트 매핑~~ → `CardData.Type`이 직접 보유(**#16 완료**, `PhaseOf` %4 임시매핑 제거).
- 이동 **방향**(`PlaceholderMoveIntent`) = **#13** (`CardData.MoveOffset` 소비) · 타겟팅·명중(`PlaceholderTarget`) = **#14** · 데미지 공식·가드 경감·아크 cap·충전량 n = **E3**.

## 이동 / 그리드 (#13 `Grid` · `ResolveMoveBeat`)
- **직교 1칸**(상하좌우) per 비트. 동시 이동 = 비트 시작 위치로 의도 수집 → 일괄 적용.
- **점유 = 비배타(스택 허용)** — 여러 유닛이 같은 칸에 겹침 가능. **막는 건 경계뿐**(off-grid); pass-through/swap 허용.
- **`MoveEvent` = 실제 칸 변화만**(to==from → 이벤트 없음; 제자리 가드).
- **`Grid`**(internal): `InBounds` · `Manhattan` · `AreAdjacent` · **`UnitsAt`**(복수 — 스택 대응, #14 명중이 사용).
- 이동 *방향*은 PLACEHOLDER(`PlaceholderMoveIntent` = 최근접 적 1칸) → 카드 데이터 = **#16**.
- `Cell : IEquatable` 추가, 테스트용 `InternalsVisibleTo`(`AssemblyInfo.cs`). 테스트: `GridTests` · `MovementTests`.

## 골격(#11) / 다음
- #11 = 타입 + 계약 + 결정론 골격(`StubBattleResolver`는 #12에서 `RoundResolver`로 대체·삭제).
- 다음: 명중 판정 **#14** · 승패 **#15**.

## 테스트 (EditMode)
- `DeterminismTests` — 동일 입력+시드 → 동일 타임라인 / 시드가 출력에 반영됨 / 타임라인 비어있지 않음.
- `RngTests` — 같은 시드 같은 시퀀스 / 범위 / `NextInt(<=0)` throw / seed 0 비잠금.
- 실행: Unity **Test Runner → EditMode**.
