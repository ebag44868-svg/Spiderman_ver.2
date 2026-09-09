using UnityEngine;

namespace SpiderVer2.Core
{
    /// <summary>
    /// 이동 공통 수학. 순수 함수만 둔다.
    ///
    /// 속도 상한이 여기 있는 이유:
    ///   펌프도 감기도 속도를 늘리는 쪽이다. 늘리는 건 웹의 몫이고,
    ///   "그래도 폭발하지 않는다"를 보증하는 건 한 곳이어야 한다.
    ///   PlayerMotor 안에 묻혀 있으면 씬 없이 테스트할 수 없다 (기준서 §12).
    /// </summary>
    public static class MotionMath
    {
        /// <summary>
        /// SOFT_SPEED 위로는 하드 클램프 대신 드래그를 걸고, MAX_SPEED는 절대 넘기지 않는다 (§7.2).
        /// </summary>
        public static Vector3 ClampSpeed(Vector3 velocity, float softSpeed, float maxSpeed,
                                         float softDrag, float dt)
        {
            float sp = velocity.magnitude;
            if (sp <= softSpeed || sp < 1e-5f) return velocity;

            float over = sp - softSpeed;
            float damped = sp - over * softDrag * dt;
            if (damped > maxSpeed) damped = maxSpeed;
            if (damped < 0f) damped = 0f;

            return velocity * (damped / sp);
        }
    }
}
