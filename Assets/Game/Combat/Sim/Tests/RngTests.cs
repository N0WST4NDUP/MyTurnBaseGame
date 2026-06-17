using NUnit.Framework;

namespace MyTurnBase.Combat.Sim.Tests
{
    // 결정론의 토대인 PRNG 자체 검증.
    public class RngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new XorShiftRng(42);
            var b = new XorShiftRng(42);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(a.NextInt(1000), b.NextInt(1000));
        }

        [Test]
        public void NextInt_StaysInRange()
        {
            var rng = new XorShiftRng(7);
            for (int i = 0; i < 1000; i++)
            {
                int v = rng.NextInt(6);
                Assert.GreaterOrEqual(v, 0);
                Assert.Less(v, 6);
            }
        }

        [Test]
        public void NextInt_NonPositiveMax_Throws()
        {
            var rng = new XorShiftRng(1);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => rng.NextInt(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => rng.NextInt(-3));
        }

        [Test]
        public void ZeroSeed_DoesNotLockToZero()
        {
            // seed 0 → 내부에서 비영(非零) 보정되어야 함(안 그러면 영원히 0).
            var rng = new XorShiftRng(0);
            bool anyNonZero = false;
            for (int i = 0; i < 10; i++)
                if (rng.NextInt(1000) != 0) { anyNonZero = true; break; }
            Assert.IsTrue(anyNonZero);
        }
    }
}
