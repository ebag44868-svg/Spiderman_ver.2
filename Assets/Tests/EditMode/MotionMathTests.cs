using NUnit.Framework;
using UnityEngine;
using SpiderVer2.Core;

namespace SpiderVer2.Tests
{
    /// <summary>속도 상한. 기준서 P2 "속도 폭발 없음"의 최종 방어선이다.</summary>
    public class MotionMathTests
    {
        const float Soft = 82f, Max = 112f, Drag = 0.9f, Dt = 1f / 120f;

        [Test]
        public void SOFT_SPEED_아래는_건드리지_않는다()
        {
            var v = new Vector3(30f, -20f, 10f);
            Assert.AreEqual(v, MotionMath.ClampSpeed(v, Soft, Max, Drag, Dt));
        }

        [Test]
        public void MAX_SPEED를_절대_넘기지_않는다()
        {
            var v = new Vector3(0f, 0f, 5000f);
            var outv = MotionMath.ClampSpeed(v, Soft, Max, Drag, Dt);
            Assert.That(outv.magnitude, Is.EqualTo(Max).Within(0.001f));
        }

        [Test]
        public void 방향은_바뀌지_않는다()
        {
            var v = new Vector3(100f, -60f, 40f);
            var outv = MotionMath.ClampSpeed(v, Soft, Max, Drag, Dt);
            Assert.That(Vector3.Angle(v, outv), Is.LessThan(0.01f));
        }

        [Test]
        public void SOFT와_MAX_사이는_서서히_줄어든다()
        {
            var v = new Vector3(0f, 0f, 100f);
            var outv = MotionMath.ClampSpeed(v, Soft, Max, Drag, Dt);

            Assert.That(outv.magnitude, Is.LessThan(100f), "감쇠가 없다");
            Assert.That(outv.magnitude, Is.GreaterThan(99f), "한 스텝에 너무 많이 깎였다");
        }

        [Test]
        public void 정지_상태에서_NaN이_안_난다()
        {
            var outv = MotionMath.ClampSpeed(Vector3.zero, Soft, Max, Drag, Dt);
            Assert.IsTrue(WebPhysicsIsFinite(outv));
        }

        static bool WebPhysicsIsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
        }
    }
}
