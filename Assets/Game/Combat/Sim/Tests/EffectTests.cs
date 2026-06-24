using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #17 효과 시스템(effects[]): 레지스트리 · effect-구동 라우팅 · 멀티비트 카드 · 확장성(새 효과=컴포넌트 추가).
    public class EffectTests
    {
        static Unit U(int id, int team, int row, int col, float hp = 10f, int speed = 5)
            => new Unit { Id = new UnitId(id), Team = new TeamId(team), Pos = new Cell(row, col), Hp = hp, Arc = 0, Speed = speed };

        static BattleState State(params Unit[] units)
            => new BattleState(units.ToList(), new XorShiftRng(1), round: 0);

        static RoundInput Plan(int unitId, params CardData[] slots)
            => new RoundInput { Plans = new Dictionary<UnitId, CardData[]> { { new UnitId(unitId), slots } } };

        static CardData CardWith(Phase type, IReadOnlyList<Cell> pattern, params EffectSpec[] effects)
            => new CardData(type, arcCost: 0, speed: 0, moveOffset: default,
                attackPattern: pattern, effects: effects, animKey: null, kind: CardKind.Common);

        // ── 레지스트리 ──────────────────────────────────────────────
        [Test]
        public void Registry_ResolvesBaseEffectsToCorrectBeats()
        {
            Assert.AreEqual(Phase.Move, EffectRegistry.Resolve(EffectKeys.Move).Beat);
            Assert.AreEqual(Phase.Attack, EffectRegistry.Resolve(EffectKeys.Damage).Beat);
            Assert.AreEqual(Phase.Guard, EffectRegistry.Resolve(EffectKeys.Guard).Beat);
            Assert.AreEqual(Phase.Charge, EffectRegistry.Resolve(EffectKeys.Charge).Beat);
            Assert.IsTrue(EffectRegistry.IsRegistered(EffectKeys.Move));
            Assert.IsFalse(EffectRegistry.IsRegistered("정의되지_않은_키"));
        }

        [Test]
        public void Registry_UnknownKey_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => EffectRegistry.Resolve("정의되지_않은_키"));
        }

        // 카드가 미등록 EffectKey를 들고 있으면 해결 중 fail-fast(저작 오류 조기 발견).
        [Test]
        public void UnknownEffectKeyOnCard_ThrowsDuringResolve()
        {
            var s = State(U(1, 0, 1, 1), U(2, 1, 1, 3));
            var bad = CardWith(Phase.Charge, null, new EffectSpec("bogus_key", 1));
            Assert.Throws<InvalidOperationException>(
                () => new RoundResolver().ResolveRound(s, Plan(1, bad, null, null)));
        }

        // ── effect-구동 라우팅 ──────────────────────────────────────
        // Type=Attack이라도 effect가 없으면 어떤 비트에도 안 들어가 무동작(라우팅은 effect의 Phase).
        [Test]
        public void CardWithNoEffects_IsInert()
        {
            var s = State(U(1, 0, 1, 1), U(2, 1, 1, 2));
            var noFx = CardWith(Phase.Attack, new[] { new Cell(0, 1) }); // 패턴은 있으나 effect 없음
            var tl = new RoundResolver().ResolveRound(s, Plan(1, noFx, null, null));
            Assert.IsEmpty(tl, "effect 없는 카드는 어떤 이벤트도 만들지 않는다");
        }

        // ── 멀티비트 카드 ──────────────────────────────────────────
        // 한 카드 [이동, 피해] → 같은 슬롯서 이동(이동 비트) 후 '이동한 위치' 기준 공격(공격 비트).
        [Test]
        public void MultiBeatCard_MovesThenAttacksFromNewPosition()
        {
            var s = State(U(1, 0, 1, 1), U(2, 1, 1, 3)); // 거리 2 → 이동 1칸이면 (1,2), 인접 후 동쪽 패턴 명중
            var dash = CardWith(Phase.Attack, new[] { new Cell(0, 1) },
                new EffectSpec(EffectKeys.Move, 1), new EffectSpec(EffectKeys.Damage, 1));

            var tl = new RoundResolver().ResolveRound(s, Plan(1, dash, null, null));

            var move = tl.OfType<MoveEvent>().Single(e => e.Actor.Value == 1);
            Assert.AreEqual(new Cell(1, 1), move.From);
            Assert.AreEqual(new Cell(1, 2), move.To, "최근접 적 쪽 직교 1칸");

            var decl = tl.OfType<AttackDeclaredEvent>().Single();
            CollectionAssert.AreEqual(new[] { new Cell(1, 3) }, decl.StrikeCells, "이동한 위치(1,2) 기준 동쪽 타격");
            Assert.IsTrue(tl.OfType<HitEvent>().Any(h => h.Target.Value == 2), "이동 후 공격이 적 명중");
        }

        // ── 확장성(DoD): 새 효과 = 컴포넌트 추가 + 등록, resolver 무수정으로 동작 ──
        sealed class BigChargeEffect : IEffect
        {
            public Phase Beat => Phase.Charge;
            public void Apply(EffectContext ctx)
            {
                ctx.Self.Arc += 5;
                ctx.Timeline.Add(new ChargeEvent(ctx.Round, ctx.Slot, ctx.Self.Id, 5, ctx.Self.Arc));
            }
        }

        [Test]
        public void NewEffect_RegisteredComponent_RunsWithoutResolverChange()
        {
            EffectRegistry.Register("big_charge_test", new BigChargeEffect());

            var s = State(U(1, 0, 1, 1), U(2, 1, 1, 3));
            var u1 = s.Units.First(u => u.Id.Value == 1);
            int arc0 = u1.Arc;
            var card = CardWith(Phase.Charge, null, new EffectSpec("big_charge_test", 0));

            var tl = new RoundResolver().ResolveRound(s, Plan(1, card, null, null));

            Assert.AreEqual(arc0 + 5, u1.Arc, "신규 effect가 resolver 수정 없이 상태 변경");
            Assert.IsTrue(tl.OfType<ChargeEvent>().Any(e => e.Actor.Value == 1 && e.Slot == 0));
        }

        // ── 결정론: effect 경유해도 동일 시드 → 동일 타임라인 ──
        [Test]
        public void EffectDriven_SameSeed_SameTimeline()
        {
            var a = new RoundResolver().ResolveRound(BattleScenarios.TwoUnits(99), BattleScenarios.SampleInput());
            var b = new RoundResolver().ResolveRound(BattleScenarios.TwoUnits(99), BattleScenarios.SampleInput());
            Assert.AreEqual(BattleScenarios.Serialize(a), BattleScenarios.Serialize(b));
        }
    }
}
