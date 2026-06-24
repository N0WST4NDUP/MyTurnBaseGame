namespace MyTurnBase.Combat.Sim
{
    // 데미지 계산 구조(#20, E3): 기본 → 가산 보정 → 곱연산 배율.
    //   최종 = (기본 + Σ가산) × Π곱연산 → 0 미만 클램프.
    //   수치는 밸런스(E3 추후)·효과 소스는 #17. 여기선 '한 곳에 모은' 프레임워크만
    //    = 추후 크로스플랫폼 결정론용 고정소수점 교체 단일 지점.
    internal static class DamagePipeline
    {
        // 가산=합산값, 곱연산=누적곱(기본 1.0). 여러 소스 집계는 호출부/#17.
        public static float Compute(float baseDamage, float additive, float multiplier)
        {
            float dmg = (baseDamage + additive) * multiplier;
            return dmg < 0f ? 0f : dmg;
        }

        // 가드: 완전 → 0, 기본 → max(0, dmg − 고정경감N). (N·완전조건 = 카드 #18·밸런스 E3)
        public static float ApplyGuard(float dmg, bool fullGuard, float flatReduction)
        {
            if (fullGuard) return 0f;
            float r = dmg - flatReduction;
            return r < 0f ? 0f : r;
        }
    }
}