using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MyTurnBase.Combat.Sim;
using MyTurnBase.Cards;

namespace MyTurnBase.Cards.Tests
{
    // #16 DoD: 에디터 SO 작성 → sim이 CardData로 소비. ToData() 변환을 검증.
    // 인스펙터 입력을 모사하기 위해 [SerializeField] private 필드를 리플렉션으로 주입.
    public class CardSOTests
    {
        static void SetField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"필드 누락: {name}");
            f.SetValue(target, value);
        }

        [Test]
        public void ToData_MapsAllAuthoredFields()
        {
            var so = ScriptableObject.CreateInstance<CardSO>();
            try
            {
                SetField(so, "type", Phase.Attack);
                SetField(so, "kind", CardKind.Unique);
                SetField(so, "arcCost", 3);
                SetField(so, "speed", 9);
                SetField(so, "moveOffset", new CardSO.CellOffset { row = 1, col = -1 });
                SetField(so, "attackPattern", new List<CardSO.CellOffset>
                {
                    new CardSO.CellOffset { row = 0, col = 1 },
                    new CardSO.CellOffset { row = 0, col = 2 },
                });
                SetField(so, "effects", new List<CardSO.EffectEntry>
                {
                    new CardSO.EffectEntry { effectKey = "dmg", magnitude = 5 },
                });
                SetField(so, "animKey", "slash");

                CardData data = so.ToData();

                Assert.AreEqual(Phase.Attack, data.Type);
                Assert.AreEqual(CardKind.Unique, data.Kind);
                Assert.AreEqual(3, data.ArcCost);
                Assert.AreEqual(9, data.Speed);
                Assert.AreEqual(new Cell(1, -1), data.MoveOffset);

                Assert.AreEqual(2, data.AttackPattern.Count);
                Assert.AreEqual(new Cell(0, 1), data.AttackPattern[0]);
                Assert.AreEqual(new Cell(0, 2), data.AttackPattern[1]);

                Assert.AreEqual(1, data.Effects.Count);
                Assert.AreEqual("dmg", data.Effects[0].EffectKey);
                Assert.AreEqual(5, data.Effects[0].Magnitude);

                Assert.AreEqual("slash", data.AnimKey);
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        // 비어 있는 SO도 안전하게 변환(빈 리스트·기본값).
        [Test]
        public void ToData_DefaultSO_ProducesEmptyCollections()
        {
            var so = ScriptableObject.CreateInstance<CardSO>();
            try
            {
                CardData data = so.ToData();
                Assert.IsNotNull(data.AttackPattern);
                Assert.IsNotNull(data.Effects);
                Assert.AreEqual(0, data.AttackPattern.Count);
                Assert.AreEqual(0, data.Effects.Count);
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }
    }
}
