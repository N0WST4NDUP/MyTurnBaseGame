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
  Effects/     IEffect · EffectContext · EffectRegistry · EffectKeys · GuardState · MoveEffect · DamageEffect · GuardEffect · ChargeEffect  (#17)
  Resolution/  IBattleResolver · RoundResolver · BeatResolver(+BeatEntry) · ResolutionUtil · Grid · DamagePipeline
  Tests/       (EditMode) BattleScenarios · DeterminismTests · ResolutionTests · MovementTests · GridTests · RngTests · CardDataTests · AttackPatternTests · EffectTests · DamagePipelineTests

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
  - `MoveOffset` 필드만 정의 — 이동 방향 **소비는 #13**. `AttackPattern` 필드만 정의 — 명중 판정은 **#14**. `Effects` = **효과 실행(#17 완료, 아래 「효과 시스템」)**. `Type`은 #17 이후 **메타(UI/AI 분류)** — 실행 라우팅은 effects[]가 구동.
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
- 공격 **타겟팅·명중**(패턴→타격 셀) = **#14 완료**(아래 「공격 패턴·명중 판정」). 데미지 **공식 구조**(기본→가산→곱연산) = **#20 완료**(`DamagePipeline`, 아래). · 이동 **방향**(`PlaceholderMoveIntent` = 최근접 적, `MoveOffset` 미소비) = 후속 · 데미지·가드 **수치**·아크 cap·충전량 n = **E3**.

## 이동 / 그리드 (#13 `Grid` · `ResolveMoveBeat`)
- **직교 1칸**(상하좌우) per 비트. 동시 이동 = 비트 시작 위치로 의도 수집 → 일괄 적용.
- **점유 = 비배타(스택 허용)** — 여러 유닛이 같은 칸에 겹침 가능. **막는 건 경계뿐**(off-grid); pass-through/swap 허용.
- **`MoveEvent` = 실제 칸 변화만**(to==from → 이벤트 없음; 제자리 가드).
- **`Grid`**(internal): `InBounds` · `Manhattan` · `AreAdjacent` · **`UnitsAt`**(복수 — 스택 대응, #14 명중이 사용).
- 이동 *방향*은 PLACEHOLDER(`PlaceholderMoveIntent` = 최근접 적 1칸) → 카드 데이터 = **#16**.
- `Cell : IEquatable` 추가, 테스트용 `InternalsVisibleTo`(`AssemblyInfo.cs`). 테스트: `GridTests` · `MovementTests`.

## 공격 패턴·명중 판정 (#14 `ResolveStrikeCells` · `CollectVictims`)
- **공격 = `card.AttackPattern`(자기 기준 상대 오프셋) → orientation 회전 → 절대 타격 셀.** `PlaceholderTarget` 단일 타깃을 대체.
- **orientation = 최근접 적 카디널 4방**(`FacingToward`): 우세 축으로 결정(동률 = 수평 우선), 적 없음·동일 칸 → 기본 동쪽(+col). *매 판정 시 현재 위치로 재산출 → 유닛이 서로 지나쳐도(p2-p1) 방향 모호성 없음.*
  - 패턴은 '동쪽(+col) 정면' 정준 프레임 저작 → forward `(fr,fc)`로 90° 회전: `(r,c) → (c·fr + r·fc, c·fc − r·fr)`. 경계 밖 셀 드롭·중복 셀 제거.
- **명중 = `CollectVictims`**: `s.Units` 순회로 타격 셀에 있는 **적**을 수집 — **스택이면 한 셀 다수, 여러 셀 = AoE(다중 명중)**. 아군·자기 제외, 위치는 하나라 자연 de-dup, 순서는 `Units`(결정론).
- **이벤트**: `AttackDeclared`(타격 셀들) → 피격마다 `Hit`+`Damage`(데미지=PLACEHOLDER/E3)+(HP≤0)`Defeat`. 적 0 → `Miss`. **선공 우선**(처치된 공격자 skip) 유지.
- `PlaceholderTarget`은 이제 이동 의도·공격 facing 공용 '최근접 적' 헬퍼(placeholder 아님). 방향 *수동 조절*(소켓)은 orientation 파라미터로 후속 주입 = 포스트-MVP.

## 효과 시스템 (#17 `effects[]`)
- **카드 동작 = effects[] 컴포넌트**. 각 `IEffect`가 `Phase Beat`(이동/가드/공격/충전) 보유 → **effect의 Phase가 실행 비트를 라우팅**(`card.Type`은 메타, 실행 미사용). 한 카드가 여러 비트의 effect 보유 = **멀티비트 카드**(예 대시-공격 = `[move, damage]` → 같은 슬롯서 이동 후 이동한 위치 기준 공격).
- **`IEffect { Phase Beat; void Apply(EffectContext ctx); }`** — **무상태**(레지스트리 싱글톤 공유). 모든 입출력은 `EffectContext` 경유(State·Self·Card·Spec·Round·Slot·Timeline·Rng + 비트 스크래치: `Guards`(슬롯 가드맵)·`MoveSnapshot`(이동 비트시작 위치)·`CurrentVictim`(공격 per-victim)).
- **레지스트리** `EffectRegistry`: `EffectKey→IEffect` 정적 dict(키 조회만 = 결정론). 미등록 키 → `InvalidOperationException`(fail-fast). **새 효과 = `IEffect` 구현 + `Register` → resolver/비트러너 무수정**(확장성 핵심). 기본4 키 = `EffectKeys`(move/damage/guard/charge).
- **오케스트레이션은 비트러너(BeatResolver)가 소유, effect는 동작만**: 이동=비트시작 스냅샷 일괄적용 / 공격=Speed순·선공우선·패턴(#14)·victim수집은 프레임, victim당 데미지만 effect / 충전=사망 스킵. → 기존 #12/#13/#14 동작·이벤트 타임라인 **보존**(회귀 테스트 그대로 통과).
- **이벤트 분리 유지**: 프레임이 `AttackDeclared`·`Hit`·`Miss`·`Defeat`(기하/판정), `DamageEffect`가 `Damage`(피해). `MoveEffect`=`Move`, `GuardEffect`=`Guard`, `ChargeEffect`=`Charge`.
- **수치는 PLACEHOLDER → E3**: `EffectSpec.Magnitude` 소비·충전량·가드 경감 수치 = E3(#19/#21). **데미지 공식 구조(기본→가산→곱연산)는 #20이 `DamageEffect` 내부에 채움**(`DamagePipeline`, 아래) — 수치는 여전히 E3.
- **후속(국소 변경)**: ①`Magnitude=int` → 데미지 float 곱연산 시 필드 확장(#20). ②4비트 밖 효과(회복·상태이상) → MVP는 기존 비트 재사용(회복=Charge, 상태부여=Attack rider), 진짜 새 타이밍은 `Phase`+beatOrder 확장. ③빈 effects[] 카드=무동작 → CardSO 검증(#18).

## 데미지 파이프라인 (#20 `DamagePipeline`)
- **구조 = 기본 → 가산 보정 → 곱연산 배율**: `Compute(기본, 가산, 곱연산) = (기본 + 가산) × 곱연산`, 0 미만 클램프. `ApplyGuard`: 완전 → 0, 기본 → `max(0, dmg − 고정 N)`. 데미지 수식을 **한 곳에 모음** → 추후 크로스플랫폼 결정론용 고정소수점 교체 단일 지점.
- **통합 = `DamageEffect.Apply`(#17)** — `DamageEffect`의 placeholder를 `DamagePipeline.Compute/ApplyGuard`로 교체(공격 비트 victim당 호출). 옛 인라인(`ResolveAttackBeat`)이 아니라 효과 안에서.
- **가산0·곱1·기본=PLACEHOLDER 스텁**(소스 = 효과 `EffectSpec`(#17) · 수치 = 밸런스 E3) — 하드코딩 밸런스 없음.
- **HP 미변경**: `Unit.Hp`(float) 그대로 차감. `MaxHp`·상한·HP floor·회복 = **E4/회복 이슈**(`IsAlive(>0)`가 음수도 죽음 처리 → floor 불요).
- 테스트 `DamagePipelineTests`(가산→곱연산 순서·곱연산 반영·음수 클램프·완전/기본 가드).

## 골격(#11) / 다음
- #11 = 타입 + 계약 + 결정론 골격(`StubBattleResolver`는 #12에서 `RoundResolver`로 대체·삭제).
- 다음: 승패 **#15**(HP≤0 판정·무승부 엣지).

## 테스트 (EditMode)
- `DeterminismTests` — 동일 입력+시드 → 동일 타임라인 / 시드가 출력에 반영됨 / 타임라인 비어있지 않음.
- `RngTests` — 같은 시드 같은 시퀀스 / 범위 / `NextInt(<=0)` throw / seed 0 비잠금.
- `AttackPatternTests` — orientation 좌/우·통과(p2-p1) · 스택 다중 명중 · 라인 AoE · 빈 패턴 Miss · 경계 드롭 · 아군 제외.
- `EffectTests` — 레지스트리(기본4 비트·미등록 throw) · effect-구동 라우팅(effect 없는 카드 무동작) · 멀티비트 카드(이동→신위치 공격) · 확장성(신규 effect 등록·resolver 무수정) · 결정론.
- `DamagePipelineTests` — 기본→가산→곱연산 순서 · 곱연산 반영 · 음수 클램프 · 완전/기본 가드.
- 실행: Unity **Test Runner → EditMode**.
