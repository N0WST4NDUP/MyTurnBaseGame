namespace MyTurnBase.Combat.Sim
{
    // 카드 효과의 '데이터' 슬롯(컴포넌트형 effects[]의 직렬화 표현).
    // 실제 효과 실행 시스템(IEffect)·sim 적용 = #17(E2). 본 타입은 키+범용 파라미터만 보유.
    public readonly struct EffectSpec
    {
        public readonly string EffectKey;  // 효과 식별 문자열(의미 부여 = #17)
        public readonly int Magnitude;     // 범용 파라미터(데미지량·이동칸 등, 해석 = #17)

        public EffectSpec(string effectKey, int magnitude)
        {
            EffectKey = effectKey ?? string.Empty;
            Magnitude = magnitude;
        }
    }
}
