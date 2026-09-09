using UnityEngine;

namespace SpiderVer2.Core
{
    /// <summary>
    /// 이동 공통 수학. 순수 함수만 둔다.
    /// Ver.1 game3d.js:6203-6224 의 이식이다.
    /// </summary>
    public static class MotionMath
    {
        /// <summary>
        /// 속도 상한. Ver.1 원문 주석:
        ///   "하드 클램프는 '속도가 쌓이는 맛'을 죽인다. 소프트캡 위로만 드래그가 붙음."
        ///
        /// 초과분의 제곱에 비례하는 지수 감쇠라, SOFT 바로 위에서는 거의 안 눌리고
        /// cap에 가까울수록 급격히 눌린다. 선형 드래그로는 이 느낌이 안 난다.
        /// </summary>
        public static Vector3 ClampSpeed(Vector3 velocity, float softSpeed, float cap, float dt)
        {
            float sp = velocity.magnitude;
            if (sp < 1e-5f) return velocity;

            if (sp > softSpeed)
            {
                float over = (sp - softSpeed) / Mathf.Max(1f, cap - softSpeed);
                velocity *= Mathf.Exp(-over * over * 3f * dt);
                sp = velocity.magnitude;
            }

            if (sp > cap) velocity *= cap / sp;
            return velocity;
        }

        /// <summary>
        /// 공기/지면 저항. Ver.1은 스윙 중 드래그를 0.003으로 거의 없앤다.
        /// 스윙이 속도를 모으는 구간이라 여기서 깎으면 절대 빨라지지 않기 때문이다.
        /// </summary>
        public static Vector3 ApplyDrag(Vector3 velocity, float horizontal, bool grounded, float dt)
        {
            float f = Mathf.Exp(-horizontal * dt);
            velocity.x *= f;
            velocity.z *= f;
            if (!grounded) velocity.y *= Mathf.Exp(-0.01f * dt);
            return velocity;
        }
    }
}
