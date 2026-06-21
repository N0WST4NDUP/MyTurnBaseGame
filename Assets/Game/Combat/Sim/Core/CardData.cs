using System;
using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // 런타임 카드 데이터(순수 C#·불변). 저작은 Unity CardSO → ToData()로 변환.
    // sim은 이 타입만 소비(UnityEngine 비참조 유지). 추상 핸들 Card를 대체(#16, E2).
    //
    // 비고:
    //   - Type = 비트(Move/Guard/Attack/Charge). 기존 Phase enum 재사용(비트와 1:1).
    //   - MoveOffset: 이동 카드의 상대 좌표. 필드만 정의 — 소비(이동 방향)는 #13 이동 규칙.
    //   - AttackPattern: 자기 기준 상대 오프셋. 절대 셀 변환·명중 판정 = #14.
    //   - Effects: effects[] 슬롯. 효과 실행 = #17.
    //   - AnimKey: sim 미사용(연출/뷰가 소비).
    //   - 충전량 n·아크 cap·데미지 공식·가드 경감 = E3(여기서 다루지 않음).
    public sealed class CardData
    {
        public readonly Phase Type;
        public readonly int ArcCost;
        public readonly int Speed;
        public readonly Cell MoveOffset;
        public readonly IReadOnlyList<Cell> AttackPattern;
        public readonly IReadOnlyList<EffectSpec> Effects;
        public readonly string AnimKey;
        public readonly CardKind Kind;

        public CardData(
            Phase type,
            int arcCost,
            int speed,
            Cell moveOffset,
            IReadOnlyList<Cell> attackPattern,
            IReadOnlyList<EffectSpec> effects,
            string animKey,
            CardKind kind)
        {
            Type = type;
            ArcCost = arcCost;
            Speed = speed;
            MoveOffset = moveOffset;
            AttackPattern = attackPattern ?? Array.Empty<Cell>(); // null 금지(AttackDeclaredEvent.StrikeCells 규약과 동일)
            Effects = effects ?? Array.Empty<EffectSpec>();
            AnimKey = animKey ?? string.Empty;
            Kind = kind;
        }
    }
}
