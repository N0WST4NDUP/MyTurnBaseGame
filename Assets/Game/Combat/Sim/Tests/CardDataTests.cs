using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #16 카드 데이터 모델: 생성자 정규화. #17 이후 비트 분기는 effects[]의 Phase가 구동(Type은 메타).
    public class CardDataTests
    {
        [Test]
        public void Ctor_NullCollections_NormalizedToEmpty()
        {
            var c = new CardData(Phase.Attack, arcCost: 1, speed: 0, moveOffset: default,
                attackPattern: null, effects: null, animKey: null, kind: CardKind.Unique);

            Assert.IsNotNull(c.AttackPattern);
            Assert.IsNotNull(c.Effects);
            Assert.AreEqual(0, c.AttackPattern.Count);
            Assert.AreEqual(0, c.Effects.Count);
            Assert.AreEqual(string.Empty, c.AnimKey, "null AnimKey → 빈 문자열");
        }

        [Test]
        public void Ctor_PreservesFields()
        {
            var pattern = new List<Cell> { new Cell(0, 1) };
            var fx = new List<EffectSpec> { new EffectSpec("dmg", 3) };
            var c = new CardData(Phase.Move, arcCost: 2, speed: 7, moveOffset: new Cell(1, 0),
                attackPattern: pattern, effects: fx, animKey: "swing", kind: CardKind.Unique);

            Assert.AreEqual(Phase.Move, c.Type);
            Assert.AreEqual(2, c.ArcCost);
            Assert.AreEqual(7, c.Speed);
            Assert.AreEqual(new Cell(1, 0), c.MoveOffset);
            Assert.AreEqual(new Cell(0, 1), c.AttackPattern[0]);
            Assert.AreEqual("dmg", c.Effects[0].EffectKey);
            Assert.AreEqual(3, c.Effects[0].Magnitude);
            Assert.AreEqual("swing", c.AnimKey);
            Assert.AreEqual(CardKind.Unique, c.Kind);
        }

        // effects[]의 Phase가 실제로 해당 비트 이벤트를 만든다(위치 무관한 Guard/Charge로 검증).
        [Test]
        public void Effect_RoutesToMatchingBeat()
        {
            var s = BattleScenarios.TwoUnits(1);
            var input = new RoundInput
            {
                Plans = new Dictionary<UnitId, CardData[]>
                {
                    { new UnitId(1), new[] { BattleScenarios.GuardCard(), BattleScenarios.ChargeCard(), BattleScenarios.GuardCard() } },
                    { new UnitId(2), new[] { BattleScenarios.ChargeCard(), BattleScenarios.GuardCard(), BattleScenarios.ChargeCard() } },
                }
            };

            var tl = new RoundResolver().ResolveRound(s, input);

            // Guard/Charge만 계획 → AttackDeclared·Move 이벤트 없어야 함.
            Assert.IsEmpty(tl.OfType<AttackDeclaredEvent>(), "공격 카드 없음 → 공격 선언 없음");
            Assert.IsEmpty(tl.OfType<MoveEvent>(), "이동 카드 없음 → 이동 이벤트 없음");

            // U1 슬롯0=Guard → GuardEvent, 슬롯1=Charge → ChargeEvent.
            Assert.IsTrue(tl.Any(e => e is GuardEvent && e.Slot == 0 && e.Actor.Value == 1));
            Assert.IsTrue(tl.Any(e => e is ChargeEvent && e.Slot == 1 && e.Actor.Value == 1));
            // U2 슬롯0=Charge → ChargeEvent.
            Assert.IsTrue(tl.Any(e => e is ChargeEvent && e.Slot == 0 && e.Actor.Value == 2));
        }

        // 참조형 CardData → 배열에 null 슬롯이 와도 NRE 없이 '행동 없음'으로 스킵돼야 한다.
        [Test]
        public void NullCardSlot_SkippedWithoutThrow()
        {
            var s = BattleScenarios.TwoUnits(1);
            var input = new RoundInput
            {
                Plans = new Dictionary<UnitId, CardData[]>
                {
                    // U1: 슬롯0 null(행동 없음) → 슬롯1 Charge → 슬롯2 null.
                    { new UnitId(1), new CardData[] { null, BattleScenarios.ChargeCard(), null } },
                    // U2: 전부 null.
                    { new UnitId(2), new CardData[] { null, null, null } },
                }
            };

            IReadOnlyList<BattleEvent> tl = null;
            Assert.DoesNotThrow(() => tl = new RoundResolver().ResolveRound(s, input), "null 슬롯에서 NRE 금지");

            // null 슬롯은 이벤트를 만들지 않음 — 유일한 이벤트는 U1 슬롯1의 Charge.
            Assert.AreEqual(1, tl.Count);
            Assert.IsTrue(tl[0] is ChargeEvent && tl[0].Slot == 1 && tl[0].Actor.Value == 1);
        }
    }
}
