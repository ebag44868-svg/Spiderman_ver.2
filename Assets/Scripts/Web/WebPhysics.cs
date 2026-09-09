using UnityEngine;

namespace SpiderVer2.Web
{
    /// <summary>
    /// 로프 수학. 전부 순수 함수다. MonoBehaviour도 씬도 필요 없다.
    ///
    /// ★ Ver.1(Three.js) game3d.js:6244-6303 의 이식이다.
    ///   형님이 "괜찮았다"고 한 그 스윙의 실제 코드다. 숫자와 조건을 그대로 옮겼다.
    ///   Ver.1이 좋았던 이유는 로프 구속 자체가 아니라 그 주변 장치 셋이었다:
    ///     GRIP_TIME   붙는 순간 0.13초에 걸쳐 서서히 물린다 (덜컹 제거)
    ///     자동 펌핑   호 바닥에 갈수록 줄이 저절로 감긴다 (그네에서 무릎 굽혔다 펴기)
    ///     base/len    목표 길이와 실제 길이를 분리하고 초당 40m로 따라가게 한다
    ///
    /// 왜 순수 함수인가 (기준서 §12):
    ///   유니티에서는 Play Mode 확인이 형님 몫이 된다. Claude가 스스로 검증할 수 있는 건
    ///   EditMode 테스트뿐이다. 그래서 스윙의 핵심 수학을 여기 몰아넣고 전부 테스트한다.
    ///
    /// 절대 조건:
    ///   1. NaN을 내지 않는다.
    ///   2. 속도를 늘리지 않는다. 로프 구속은 에너지를 옮길 뿐 만들지 않는다.
    /// </summary>
    public static class WebPhysics
    {
        const float Eps = 1e-5f;

        // ───────────────────────────────────────── 로프 길이

        /// <summary>
        /// 자동 펌핑 목표 길이. Ver.1 원문:
        ///   "호 바닥에 가까울수록 살짝 감고 올라가면서 푼다. 그네에서 무릎 굽혔다 펴는 것."
        ///
        /// phase = 앵커 바로 아래일 때 1, 옆으로 갈수록 0.
        /// 이게 없으면 플레이어가 계속 감기 버튼을 눌러야만 속도가 붙는다.
        /// </summary>
        public static float AutoPumpTarget(Vector3 position, Vector3 anchor,
                                           float baseLength, float pumpDepth, float ropeMin)
        {
            Vector3 d = position - anchor;
            float dist = d.magnitude;
            if (dist < Eps) return Mathf.Max(ropeMin, baseLength);

            float uy = d.y / dist;
            float phase = Mathf.Max(0f, -uy);
            float desired = baseLength * (1f - pumpDepth * phase * phase);
            return Mathf.Max(ropeMin, desired);
        }

        /// <summary>
        /// 실제 길이가 목표를 따라간다. 줄일 때는 제 속도로, 늘일 때는 70%로.
        /// 즉시 바꾸면 뚝뚝 끊긴다. Ver.1 REEL_RATE = 40 m/s.
        /// </summary>
        public static float FollowLength(float current, float desired, float rate, float dt)
        {
            float r = rate * (desired < current ? 1f : 0.7f);
            float delta = Mathf.Clamp(desired - current, -r * dt, r * dt);
            return current + delta;
        }

        /// <summary>줄 감기. 길이를 줄이되 ROPE_MIN 아래로는 내려가지 않는다.</summary>
        public static float Reel(float length, float rate, float dt, float ropeMin)
        {
            return Mathf.Max(ropeMin, length - rate * dt);
        }

        /// <summary>
        /// 팽팽한 상태에서 줄이 감기면 각운동량 보존으로 접선 속도가 붙는다 (채찍 가속).
        /// Ver.1 조건 그대로: 실제 거리가 이전 길이에 0.35m 이내로 붙어 있어야 한다.
        /// 늘어진 줄을 감는 건 아무 일도 아니기 때문이다.
        /// 배율 상한(Ver.1 1.01)이 없으면 앵커에 가까워질수록 발산한다.
        /// </summary>
        public static Vector3 ReelBoost(Vector3 position, Vector3 velocity, Vector3 anchor,
                                        float oldLength, float newLength, float maxStepGain)
        {
            if (newLength >= oldLength || newLength < Eps) return velocity;

            Vector3 d = position - anchor;
            float dist = d.magnitude;
            if (dist < Eps) return velocity;
            if (dist < oldLength - 0.35f) return velocity;   // 늘어져 있으면 감아도 안 붙는다

            Vector3 n = d / dist;
            float radial = Vector3.Dot(velocity, n);
            Vector3 tangential = velocity - n * radial;

            float k = Mathf.Clamp(oldLength / Mathf.Max(newLength, 1f), 1f, Mathf.Max(1f, maxStepGain));
            return n * radial + tangential * k;
        }

        // ───────────────────────────────────────── 구속

        public static Vector3 SolveRope(Vector3 position, Vector3 velocity,
                                        Vector3 anchor, float length, float convert)
        {
            return SolveRope(position, velocity, anchor, length, convert, 1f, Vector3.zero, float.MaxValue);
        }

        public static Vector3 SolveRope(Vector3 position, Vector3 velocity,
                                        Vector3 anchor, float length, float convert, Vector3 intent)
        {
            return SolveRope(position, velocity, anchor, length, convert, 1f, intent, float.MaxValue);
        }

        /// <summary>
        /// 로프 구속. Ver.1 원문 주석:
        ///   "잘려나간 반경 속도를 접선 속도로 되돌린다.
        ///    이게 없으면 매 스윙 '덜컹'하고 속도가 깎여 절대 빨라지지 않음."
        ///
        /// grip = 붙은 뒤 경과시간 / GRIP_TIME, 0..1.
        ///   붙자마자 100%로 물리면 덜컹한다. 0.13초에 걸쳐 서서히 문다.
        ///
        /// intent = 플레이어가 가고 싶어 하는 방향(보통 카메라 앞).
        ///   속도가 줄과 완전히 나란하면(앵커 바로 아래 수직 낙하) 접선 성분이 0이라
        ///   방향이 없어 속도가 통째로 사라진다. Ver.1도 이 경우 속도를 잃는다.
        ///   Ver.2는 의도를 접평면에 투영해 그쪽으로 흘려보낸다 (헌법 §5.1).
        /// </summary>
        public static Vector3 SolveRope(Vector3 position, Vector3 velocity,
                                        Vector3 anchor, float length, float convert,
                                        float grip, Vector3 intent, float speedCap)
        {
            Vector3 d = position - anchor;
            float dist = d.magnitude;

            // 딱 팽팽한 순간(dist == length)에도 구속을 건다.
            // <= 로 두면 그 프레임은 통과해서 줄이 한 스텝만큼 늘어난 뒤에야 잡힌다.
            if (dist < length || dist < Eps) return velocity;    // 늘어짐 = 자유낙하

            Vector3 n = d / dist;                                 // 앵커에서 바깥으로
            float radial = Vector3.Dot(velocity, n);
            if (radial <= 0f) return velocity;                    // 앵커 쪽이면 줄이 막지 않는다

            float spBefore = velocity.magnitude;
            Vector3 tangential = velocity - n * radial;
            float spAfter = tangential.magnitude;

            float c = Mathf.Clamp01(convert) * Mathf.Clamp01(grip);
            float target = Mathf.Min(spAfter + (spBefore - spAfter) * c, speedCap);

            if (spAfter > 0.01f) return tangential * (target / spAfter);

            Vector3 t = intent - n * Vector3.Dot(intent, n);       // 의도를 접평면에 투영
            if (t.sqrMagnitude < Eps * Eps) return Vector3.zero;
            return t.normalized * target;
        }

        /// <summary>
        /// 위치가 로프 길이를 넘어갔으면 구(球) 위로 되돌린다.
        /// Ver.1은 즉시 스냅한다(stiffness 1). 낮추면 줄이 고무줄처럼 늘어난다.
        /// </summary>
        public static Vector3 CorrectPosition(Vector3 position, Vector3 anchor,
                                              float length, float stiffness)
        {
            Vector3 d = position - anchor;
            float dist = d.magnitude;
            if (dist <= length || dist < Eps) return position;

            Vector3 target = anchor + d * (length / dist);
            return Vector3.Lerp(position, target, Mathf.Clamp01(stiffness));
        }

        // ───────────────────────────────────────── 가속

        /// <summary>
        /// 펌프 (Ver.1 E키). 수평 진행 방향으로 민다.
        /// Ver.1은 접선이 아니라 수평 속도 방향으로 밀고, 하강 성분의 일부를 되돌린다.
        /// 그래야 "앞으로 쭉 뻗는" 느낌이 난다.
        /// </summary>
        public static Vector3 Pump(Vector3 velocity, float accel, float dt)
        {
            float hs = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            if (hs < 0.5f) return velocity;

            velocity.x += (velocity.x / hs) * accel * dt;
            velocity.z += (velocity.z / hs) * accel * dt;
            velocity.y += Mathf.Max(0f, -velocity.y) * 0.15f * dt * (accel / 10f);
            return velocity;
        }

        /// <summary>앵커 쪽으로 당긴다. 급강하 · 벽으로 끌려 붙기.</summary>
        public static Vector3 Pull(Vector3 position, Vector3 velocity, Vector3 anchor,
                                   float accel, float dt)
        {
            Vector3 d = anchor - position;
            float dist = d.magnitude;
            if (dist < Eps) return velocity;

            return velocity + (d / dist) * (accel * dt);
        }

        // ───────────────────────────────────────── 판정

        /// <summary>이 앵커가 스윙으로 쓸 만한가.</summary>
        public static bool IsSwingable(float distance, float ropeMin, float ropeMax)
        {
            return distance >= ropeMin && distance <= ropeMax;
        }

        /// <summary>
        /// 앵커가 너무 아래인가 (§7.1 DROP_MAX).
        /// P3 자동 앵커가 후보를 고를 때 쓴다. 플레이어가 직접 겨눈 곳은 막지 않는다.
        /// </summary>
        public static bool IsAnchorTooLow(float anchorY, float playerY, float dropMax)
        {
            return anchorY < playerY - dropMax;
        }

        /// <summary>NaN/Infinity 방어. 물리 값이 오염되면 여기서 잡아 0으로 돌린다.</summary>
        public static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }
    }
}
