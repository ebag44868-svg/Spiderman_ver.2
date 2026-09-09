using NUnit.Framework;
using UnityEngine;
using SpiderVer2.Web;

namespace SpiderVer2.Tests
{
    /// <summary>
    /// 기준서 P2 완료 기준의 자동 검증분:
    ///   NaN 없음 · 속도 폭발 없음 · 5초 안정.
    /// 씬 없이 순수 함수만 돌린다. Claude가 형님 없이도 계속 확인할 수 있는 부분이다.
    /// </summary>
    public class WebPhysicsTests
    {
        const float Convert = 0.995f;   // §7.2 SWING_CONVERT

        // ─────────────────────────────── 기본 성질

        [Test]
        public void 줄이_늘어져_있으면_속도를_건드리지_않는다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -10f, 0f);   // 길이 20짜리 줄에 거리 10
            Vector3 vel = new Vector3(3f, -14f, 2f);

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert);

            Assert.AreEqual(vel, outv);
        }

        [Test]
        public void 팽팽하면_반경_속도가_사라진다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(10f, -30f, 0f);  // 아래로 = 앵커에서 멀어지는 방향

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert);

            Vector3 n = (pos - anchor).normalized;
            float radial = Vector3.Dot(outv, n);
            Assert.That(Mathf.Abs(radial), Is.LessThan(0.001f), "반경 성분이 남아 있다: " + radial);
        }

        [Test]
        public void 앵커_쪽으로_가는_속도는_막지_않는다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(4f, 12f, 0f);    // 위로 = 앵커 쪽

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert);

            Assert.AreEqual(vel, outv);
        }

        [Test]
        public void 로프_구속은_속도를_늘리지_않는다()
        {
            var rng = new SpiderVer2.Core.Rng(99u);
            Vector3 anchor = new Vector3(3f, 40f, -7f);

            for (int i = 0; i < 5000; i++)
            {
                Vector3 pos = anchor + new Vector3(
                    rng.Range(-60f, 60f), rng.Range(-60f, 60f), rng.Range(-60f, 60f));
                Vector3 vel = new Vector3(
                    rng.Range(-110f, 110f), rng.Range(-110f, 110f), rng.Range(-110f, 110f));
                float len = rng.Range(12f, 150f);

                var outv = WebPhysics.SolveRope(pos, vel, anchor, len, Convert);

                Assert.IsTrue(WebPhysics.IsFinite(outv), "NaN i=" + i);
                Assert.That(outv.magnitude, Is.LessThanOrEqualTo(vel.magnitude + 0.001f),
                    "속도가 늘었다 i=" + i + " " + vel.magnitude + " -> " + outv.magnitude);
            }
        }

        [Test]
        public void convert가_1이어도_속도가_늘지_않는다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(7f, -50f, 3f);

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, 1f);

            Assert.That(outv.magnitude, Is.LessThanOrEqualTo(vel.magnitude + 0.001f));
        }

        // ─────────────────────────────── 방어

        [Test]
        public void 앵커와_같은_자리여도_NaN이_안_난다()
        {
            var outv = WebPhysics.SolveRope(Vector3.zero, new Vector3(1f, 2f, 3f),
                                            Vector3.zero, 10f, Convert);
            Assert.IsTrue(WebPhysics.IsFinite(outv));
        }

        [Test]
        public void 길이가_0이어도_NaN이_안_난다()
        {
            var pos = new Vector3(0f, -5f, 0f);
            var outv = WebPhysics.SolveRope(pos, new Vector3(0f, -20f, 0f), Vector3.zero, 0f, Convert);
            Assert.IsTrue(WebPhysics.IsFinite(outv));
        }

        [Test]
        public void 줄과_완전히_나란한_속도는_0이_된다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(0f, -30f, 0f);   // 접선 성분이 전혀 없다

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert);

            Assert.IsTrue(WebPhysics.IsFinite(outv));
            Assert.That(outv.magnitude, Is.LessThan(0.001f));
        }

        [Test]
        public void 위치_보정은_구_위로_되돌린다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -40f, 0f);

            var outp = WebPhysics.CorrectPosition(pos, anchor, 20f, 1f);

            Assert.That(Vector3.Distance(outp, anchor), Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void 안쪽에_있으면_위치를_건드리지_않는다()
        {
            Vector3 pos = new Vector3(0f, -10f, 0f);
            var outp = WebPhysics.CorrectPosition(pos, Vector3.zero, 20f, 1f);
            Assert.AreEqual(pos, outp);
        }

        [Test]
        public void 감기는_ROPE_MIN_아래로_안_내려간다()
        {
            float len = 14f;
            for (int i = 0; i < 500; i++)
                len = WebPhysics.Reel(len, 26f, 1f / 120f, 12f);

            Assert.That(len, Is.EqualTo(12f).Within(0.0001f));
        }

        // ─────────────────────────────── 펌프 (Ver.1 E키)

        [Test]
        public void 펌프는_수평_진행_방향으로_민다()
        {
            Vector3 vel = new Vector3(20f, 0f, 0f);
            var outv = WebPhysics.Pump(vel, 58f, 1f / 120f);

            Assert.That(outv.x, Is.GreaterThan(20f), "앞으로 안 밀었다");
            Assert.That(Mathf.Abs(outv.z), Is.LessThan(0.001f), "옆으로 새면 안 된다");
        }

        [Test]
        public void 펌프는_하강_속도를_조금_되돌린다()
        {
            Vector3 vel = new Vector3(20f, -30f, 0f);
            var outv = WebPhysics.Pump(vel, 58f, 1f / 120f);

            Assert.That(outv.y, Is.GreaterThan(vel.y), "떨어지는 속도를 전혀 안 잡았다");
        }

        [Test]
        public void 거의_멈춰_있으면_펌프가_안_먹는다()
        {
            // 방향이 없는데 밀면 아무 쪽으로나 튄다. Ver.1도 0.5 m/s 미만이면 건너뛴다.
            Vector3 vel = new Vector3(0.1f, -5f, 0.1f);
            Assert.AreEqual(vel, WebPhysics.Pump(vel, 58f, 1f / 120f));
        }

        // ─────────────────────────────── 자동 펌핑 · 길이 추종

        [Test]
        public void 호_바닥에서_줄이_가장_많이_감긴다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 bottom = new Vector3(0f, -40f, 0f);    // 앵커 바로 아래
            Vector3 side = new Vector3(40f, 0f, 0f);       // 옆

            float atBottom = WebPhysics.AutoPumpTarget(bottom, anchor, 40f, 0.12f, 12f);
            float atSide = WebPhysics.AutoPumpTarget(side, anchor, 40f, 0.12f, 12f);

            Assert.That(atBottom, Is.EqualTo(40f * (1f - 0.12f)).Within(0.01f));
            Assert.That(atSide, Is.EqualTo(40f).Within(0.01f), "옆에서는 안 감겨야 한다");
        }

        [Test]
        public void 자동_펌핑도_ROPE_MIN_아래로_안_간다()
        {
            var v = WebPhysics.AutoPumpTarget(new Vector3(0f, -12f, 0f), Vector3.zero, 12f, 0.9f, 12f);
            Assert.That(v, Is.GreaterThanOrEqualTo(12f));
        }

        [Test]
        public void 길이는_한_번에_안_바뀌고_따라간다()
        {
            float len = WebPhysics.FollowLength(40f, 20f, 40f, 1f / 120f);
            Assert.That(len, Is.EqualTo(40f - 40f / 120f).Within(0.001f), "줄일 때는 제 속도로");

            float len2 = WebPhysics.FollowLength(20f, 40f, 40f, 1f / 120f);
            Assert.That(len2, Is.EqualTo(20f + 40f * 0.7f / 120f).Within(0.001f), "늘일 때는 70%로");
        }

        [Test]
        public void 목표에_도달하면_넘어가지_않는다()
        {
            float len = WebPhysics.FollowLength(40f, 39.99f, 40f, 1f / 120f);
            Assert.That(len, Is.EqualTo(39.99f).Within(0.001f));
        }

        // ─────────────────────────────── GRIP (붙는 순간 덜컹 제거)

        [Test]
        public void grip이_0이면_속도를_거의_안_되돌린다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(10f, -30f, 0f);

            var atStart = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert, 0f, Vector3.forward, 112f);
            var atFull = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert, 1f, Vector3.forward, 112f);

            Assert.That(atStart.magnitude, Is.LessThan(atFull.magnitude), "grip이 안 듣는다");
            Assert.That(atStart.magnitude, Is.EqualTo(10f).Within(0.01f), "grip 0이면 접선만 남아야 한다");
        }

        [Test]
        public void 구속은_MAX_SPEED를_넘겨_되돌리지_않는다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(30f, -200f, 0f);

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, 1f, 1f, Vector3.forward, 112f);
            Assert.That(outv.magnitude, Is.LessThanOrEqualTo(112f + 0.001f));
        }

        // ─────────────────────────────── 5초 안정 (P2 완료 기준)

        [Test]
        public void 스윙_5초_동안_폭발하지_않는다()
        {
            const float dt = 1f / 120f;
            const float gravity = 72f;      // Ver.1 G
            const float ropeLen = 40f;

            Vector3 anchor = new Vector3(0f, 60f, 0f);
            Vector3 pos = anchor + new Vector3(28f, -28f, 0f);   // 45도에서 놓는다
            Vector3 vel = Vector3.zero;
            float maxSpeed = 0f;

            for (int step = 0; step < 600; step++)   // 5초 * 120Hz
            {
                vel.y -= gravity * dt;
                pos += vel * dt;

                vel = WebPhysics.SolveRope(pos, vel, anchor, ropeLen, Convert);
                pos = WebPhysics.CorrectPosition(pos, anchor, ropeLen, 1f);

                Assert.IsTrue(WebPhysics.IsFinite(vel), "속도 NaN, step=" + step);
                Assert.IsTrue(WebPhysics.IsFinite(pos), "위치 NaN, step=" + step);

                float s = vel.magnitude;
                if (s > maxSpeed) maxSpeed = s;

                // 진자는 에너지 보존이라 최고 속도가 sqrt(2*g*h) 근처를 넘지 않아야 한다.
                // h = 28 (놓은 높이차). sqrt(2*72*28) ≈ 63.5. 여유를 두고 75로 막는다.
                Assert.That(s, Is.LessThan(75f), "속도 폭발, step=" + step + " speed=" + s);

                float d = Vector3.Distance(pos, anchor);
                Assert.That(d, Is.LessThanOrEqualTo(ropeLen + 0.01f), "줄이 늘어났다, step=" + step);
            }

            Assert.That(maxSpeed, Is.GreaterThan(20f), "5초 동안 거의 안 움직였다. 스윙이 아니다.");
        }

        [Test]
        public void 펌프를_계속_넣어도_MAX_SPEED를_넘지_않는다()
        {
            // 펌프는 순수 가속기다. 10초 넣으면 580 m/s까지 간다. 그게 정상이다.
            // 폭발을 막는 건 MotionMath.ClampSpeed 쪽 책임이라, 그것까지 같이 돌려야
            // 기준서 P2의 "속도 폭발 없음"을 실제로 검증하는 것이 된다.
            const float dt = 1f / 120f;
            const float ropeLen = 40f;
            Vector3 anchor = new Vector3(0f, 60f, 0f);
            Vector3 pos = anchor + new Vector3(28f, -28f, 0f);
            Vector3 vel = Vector3.zero;

            for (int step = 0; step < 1200; step++)   // 10초
            {
                vel.y -= 72f * dt;
                vel = WebPhysics.Pump(vel, 58f, dt);
                pos += vel * dt;
                vel = WebPhysics.SolveRope(pos, vel, anchor, ropeLen, Convert);
                pos = WebPhysics.CorrectPosition(pos, anchor, ropeLen, 1f);

                // PlayerMotor가 매 스텝 마지막에 하는 일. 이게 상한을 책임진다.
                vel = SpiderVer2.Core.MotionMath.ClampSpeed(vel, 82f, 112f, dt);

                Assert.IsTrue(WebPhysics.IsFinite(vel), "step=" + step);
                Assert.IsTrue(WebPhysics.IsFinite(pos), "step=" + step);
                Assert.That(vel.magnitude, Is.LessThanOrEqualTo(112f + 0.001f),
                    "MAX_SPEED를 넘었다 step=" + step + " v=" + vel.magnitude);
            }
        }

        // ─────────────────────────────── 의도 방향 (속도가 줄과 나란할 때)

        [Test]
        public void 줄과_나란해도_의도가_있으면_속도가_살아남는다()
        {
            // 앵커 바로 아래에서 수직 낙하. 접선 성분이 0이라
            // 의도가 없으면 속도가 통째로 사라진다 ("걸리자마자 뚝 멈춤").
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(0f, -30f, 0f);
            Vector3 intent = Vector3.forward;

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert, intent);

            Assert.IsTrue(WebPhysics.IsFinite(outv));
            Assert.That(outv.magnitude, Is.GreaterThan(25f), "의도가 있는데 속도가 죽었다");
            Assert.That(outv.magnitude, Is.LessThanOrEqualTo(vel.magnitude + 0.001f), "속도가 늘었다");
        }

        [Test]
        public void 의도로_살린_속도도_반경_성분이_없다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(0f, -30f, 0f);

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert, Vector3.forward);

            Vector3 n = (pos - anchor).normalized;
            Assert.That(Mathf.Abs(Vector3.Dot(outv, n)), Is.LessThan(0.001f));
        }

        [Test]
        public void 의도가_줄과_나란하면_어쩔_수_없이_0이다()
        {
            // 아래를 보며 아래로 떨어지는 중. 접평면에 투영할 성분이 없다.
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(0f, -30f, 0f);

            var outv = WebPhysics.SolveRope(pos, vel, anchor, 20f, Convert, Vector3.down);

            Assert.IsTrue(WebPhysics.IsFinite(outv));
            Assert.That(outv.magnitude, Is.LessThan(0.001f));
        }

        [Test]
        public void 의도가_있어도_속도는_늘지_않는다()
        {
            var rng = new SpiderVer2.Core.Rng(31337u);
            Vector3 anchor = new Vector3(-5f, 30f, 12f);

            for (int i = 0; i < 3000; i++)
            {
                Vector3 pos = anchor + new Vector3(
                    rng.Range(-60f, 60f), rng.Range(-60f, 60f), rng.Range(-60f, 60f));
                Vector3 vel = new Vector3(
                    rng.Range(-110f, 110f), rng.Range(-110f, 110f), rng.Range(-110f, 110f));
                Vector3 intent = new Vector3(
                    rng.Range(-1f, 1f), rng.Range(-1f, 1f), rng.Range(-1f, 1f));
                float len = rng.Range(12f, 150f);

                var outv = WebPhysics.SolveRope(pos, vel, anchor, len, Convert, intent);

                Assert.IsTrue(WebPhysics.IsFinite(outv), "NaN i=" + i);
                Assert.That(outv.magnitude, Is.LessThanOrEqualTo(vel.magnitude + 0.001f), "i=" + i);
            }
        }

        // ─────────────────────────────── 줄 감기 = 가속

        [Test]
        public void 줄을_감으면_접선_속도가_빨라진다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(15f, 0f, 0f);   // 완전 접선

            var outv = WebPhysics.ReelBoost(pos, vel, anchor, 20f, 19.6f, 2f);

            Assert.That(outv.magnitude, Is.GreaterThan(vel.magnitude), "감았는데 안 빨라졌다");
            Assert.That(outv.magnitude, Is.EqualTo(15f * 20f / 19.6f).Within(0.01f), "각운동량이 안 맞다");
        }

        [Test]
        public void 감기는_반경_속도를_건드리지_않는다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -20f, 0f);
            Vector3 vel = new Vector3(10f, -10f, 0f);

            var outv = WebPhysics.ReelBoost(pos, vel, anchor, 20f, 19f, 2f);

            Vector3 n = (pos - anchor).normalized;
            Assert.That(Vector3.Dot(outv, n), Is.EqualTo(Vector3.Dot(vel, n)).Within(0.001f));
        }

        [Test]
        public void 줄이_늘어져_있으면_감아도_안_빨라진다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -10f, 0f);   // 거리 10, 줄은 20 -> 늘어짐
            Vector3 vel = new Vector3(15f, 0f, 0f);

            var outv = WebPhysics.ReelBoost(pos, vel, anchor, 20f, 19f, 2f);

            Assert.AreEqual(vel, outv);
        }

        [Test]
        public void 감기_배율_상한이_발산을_막는다()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 pos = new Vector3(0f, -13f, 0f);
            Vector3 vel = new Vector3(20f, 0f, 0f);

            // 길이를 절반으로 줄여도 상한 1.02배까지만 붙어야 한다
            var outv = WebPhysics.ReelBoost(pos, vel, anchor, 13f, 6.5f, 1.02f);

            Assert.That(outv.magnitude, Is.LessThanOrEqualTo(20f * 1.02f + 0.001f));
        }

        [Test]
        public void 감기를_1200스텝_돌려도_발산하지_않는다()
        {
            const float dt = 1f / 120f;
            Vector3 anchor = new Vector3(0f, 60f, 0f);
            Vector3 pos = anchor + new Vector3(30f, -30f, 0f);
            Vector3 vel = Vector3.zero;
            float len = 42.4f;

            for (int step = 0; step < 1200; step++)
            {
                vel.y -= 72f * dt;

                float oldLen = len;
                len = WebPhysics.Reel(len, 26f, dt, 12f);
                vel = WebPhysics.ReelBoost(pos, vel, anchor, oldLen, len, 1.01f);
                vel = WebPhysics.Pull(pos, vel, anchor, 55f, dt);

                pos += vel * dt;
                vel = WebPhysics.SolveRope(pos, vel, anchor, len, Convert, Vector3.forward);
                pos = WebPhysics.CorrectPosition(pos, anchor, len, 1f);
                vel = SpiderVer2.Core.MotionMath.ClampSpeed(vel, 82f, 112f, dt);

                Assert.IsTrue(WebPhysics.IsFinite(vel), "step=" + step);
                Assert.IsTrue(WebPhysics.IsFinite(pos), "step=" + step);
                Assert.That(vel.magnitude, Is.LessThan(500f), "발산 step=" + step + " v=" + vel.magnitude);
            }
        }

        [Test]
        public void Pull은_앵커_쪽으로_가속한다()
        {
            Vector3 anchor = new Vector3(0f, 50f, 0f);
            Vector3 pos = Vector3.zero;

            var outv = WebPhysics.Pull(pos, Vector3.zero, anchor, 55f, 1f / 120f);

            Assert.That(outv.y, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(outv.x), Is.LessThan(0.001f));
            Assert.IsTrue(WebPhysics.IsFinite(outv));
        }

        // ─────────────────────────────── 앵커 높이

        [Test]
        public void DROP_MAX보다_아래_앵커는_거른다()
        {
            // 플레이어 y=100. DROP_MAX=14.
            Assert.IsTrue(WebPhysics.IsAnchorTooLow(85f, 100f, 14f), "15m 아래는 걸러야 한다");
            Assert.IsFalse(WebPhysics.IsAnchorTooLow(90f, 100f, 14f), "10m 아래는 허용");
            Assert.IsFalse(WebPhysics.IsAnchorTooLow(140f, 100f, 14f), "위쪽은 당연히 허용");
        }

        [Test]
        public void IsSwingable은_사거리_밖을_거른다()
        {
            Assert.IsFalse(WebPhysics.IsSwingable(5f, 12f, 150f));
            Assert.IsFalse(WebPhysics.IsSwingable(200f, 12f, 150f));
            Assert.IsTrue(WebPhysics.IsSwingable(60f, 12f, 150f));
        }
    }
}
