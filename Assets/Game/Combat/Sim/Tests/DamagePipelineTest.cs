using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // #20 데미지 파이프라인: 기본 → 가산 → 곱연산, 가드 적용. (수치는 밸런스/후속)
    public class DamagePipelineTests
    {
        const float Eps = 1e-4f;

        [Test] // (기본+가산)×곱연산 — 가산이 곱연산보다 먼저
        public void Compute_AdditiveBeforeMultiplicative()
            => Assert.AreEqual(30f, DamagePipeline.Compute(10f, 5f, 2f), Eps); // (10+5)*2

        [Test] // 스텁(가산0·곱1) → 기본 그대로
        public void Compute_DefaultStub_ReturnsBase()
            => Assert.AreEqual(7f, DamagePipeline.Compute(7f, 0f, 1f), Eps);

        [Test] // 곱연산 배율 반영(DoD)
        public void Compute_MultiplierReflected()
            => Assert.AreEqual(5f, DamagePipeline.Compute(10f, 0f, 0.5f), Eps);

        [Test] // 음수 → 0 클램프
        public void Compute_ClampsNegativeToZero()
            => Assert.AreEqual(0f, DamagePipeline.Compute(1f, -5f, 1f), Eps);

        [Test] // 완전 가드 → 0
        public void ApplyGuard_Full_ZeroesDamage()
            => Assert.AreEqual(0f, DamagePipeline.ApplyGuard(100f, true, 0f), Eps);

        [Test] // 기본 가드 → 고정 경감
        public void ApplyGuard_Basic_FlatReduction()
            => Assert.AreEqual(7f, DamagePipeline.ApplyGuard(10f, false, 3f), Eps);

        [Test] // 기본 가드가 데미지보다 크면 0
        public void ApplyGuard_Basic_ClampsToZero()
            => Assert.AreEqual(0f, DamagePipeline.ApplyGuard(2f, false, 5f), Eps);
    }
}