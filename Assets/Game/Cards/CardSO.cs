using System.Collections.Generic;
using UnityEngine;
using MyTurnBase.Combat.Sim;

namespace MyTurnBase.Cards
{
    // 카드 저작용 ScriptableObject(에디터). 런타임 sim 소비는 ToData()로 순수 C# CardData 변환.
    // sim 어셈블리는 UnityEngine 비참조 → 이 클래스는 별도 어셈블리(MyTurnBase.Cards)에 둔다.
    [CreateAssetMenu(fileName = "Card", menuName = "MyTurnBase/Card", order = 0)]
    public sealed class CardSO : ScriptableObject
    {
        // Cell·EffectSpec은 sim의 readonly struct라 인스펙터 저작 불가 → 직렬화용 래퍼.
        [System.Serializable]
        public struct CellOffset
        {
            public int row;
            public int col;
        }

        [System.Serializable]
        public struct EffectEntry
        {
            public string effectKey;
            public int magnitude;
        }

        [Header("분류")]
        [SerializeField] private Phase type = Phase.Move;     // 비트(Move/Guard/Attack/Charge)
        [SerializeField] private CardKind kind = CardKind.Common;

        [Header("코스트 / 스탯")]
        [SerializeField] private int arcCost = 0;
        [SerializeField] private int speed = 0;

        [Header("이동 (Type=Move일 때 의미 · 소비는 #13)")]
        [SerializeField] private CellOffset moveOffset;

        [Header("공격 패턴 (자기 기준 상대 오프셋 · 명중 판정 = #14)")]
        [SerializeField] private List<CellOffset> attackPattern = new List<CellOffset>();

        [Header("효과 슬롯 (실행 = #17)")]
        [SerializeField] private List<EffectEntry> effects = new List<EffectEntry>();

        [Header("연출")]
        [SerializeField] private string animKey = "";

        // SO → 런타임 순수 C# 변환. sim은 이 결과(CardData)만 소비.
        public CardData ToData()
        {
            var pattern = new List<Cell>(attackPattern.Count);
            foreach (var o in attackPattern)
                pattern.Add(new Cell(o.row, o.col));

            var fx = new List<EffectSpec>(effects.Count);
            foreach (var e in effects)
                fx.Add(new EffectSpec(e.effectKey, e.magnitude));

            return new CardData(
                type,
                arcCost,
                speed,
                new Cell(moveOffset.row, moveOffset.col),
                pattern,
                fx,
                animKey,
                kind);
        }
    }
}
