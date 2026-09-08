using NUnit.Framework;
using SpiderVer2.Core;

namespace SpiderVer2.Tests
{
    /// <summary>
    /// 씬 생성이 재현 가능해야 한다. 시드가 같으면 도시가 같아야 한다.
    /// Ver.1에서 난수열이 밀려 도시가 통째로 달라진 사고를 세 번 겪었다 (기준서 §6 [C]).
    /// </summary>
    public class RngTests
    {
        [Test]
        public void 같은_시드는_같은_수열을_낸다()
        {
            var a = new Rng(20260909u);
            var b = new Rng(20260909u);

            for (int i = 0; i < 200; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt(), "i=" + i);
        }

        [Test]
        public void 다른_시드는_다른_수열을_낸다()
        {
            var a = new Rng(1u);
            var b = new Rng(2u);

            int same = 0;
            for (int i = 0; i < 200; i++)
                if (a.NextUInt() == b.NextUInt()) same++;

            Assert.Less(same, 5, "서로 다른 시드인데 수열이 너무 비슷하다");
        }

        [Test]
        public void Next01은_0이상_1미만이다()
        {
            var r = new Rng(12345u);
            for (int i = 0; i < 20000; i++)
            {
                float v = r.Next01();
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }

        [Test]
        public void Range는_구간을_벗어나지_않는다()
        {
            var r = new Rng(777u);
            for (int i = 0; i < 20000; i++)
            {
                float v = r.Range(28f, 78f);
                Assert.GreaterOrEqual(v, 28f);
                Assert.Less(v, 78f);
            }
        }

        [Test]
        public void 시드_0도_멈추지_않는다()
        {
            // xorshift는 상태가 0이 되면 영원히 0을 뱉는다. 생성자에서 막아뒀다.
            var r = new Rng(0u);
            uint first = r.NextUInt();
            Assert.AreNotEqual(0u, first);

            int nonZero = 0;
            for (int i = 0; i < 100; i++)
                if (r.NextUInt() != 0u) nonZero++;
            Assert.AreEqual(100, nonZero);
        }

        [Test]
        public void 분포가_한쪽으로_쏠리지_않는다()
        {
            var r = new Rng(4242u);
            var buckets = new int[10];
            const int n = 100000;

            for (int i = 0; i < n; i++)
                buckets[(int)(r.Next01() * 10f)]++;

            foreach (var b in buckets)
                Assert.That(b, Is.InRange(n / 10 * 0.9, n / 10 * 1.1), "분포 편향");
        }
    }
}
