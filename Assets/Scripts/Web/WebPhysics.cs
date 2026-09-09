using UnityEngine;

namespace SpiderVer2.Web
{
    /// <summary>
    /// 로프 수학. 전부 순수 함수다. MonoBehaviour도 씬도 필요 없다.
    ///
    /// 왜 순수 함수인가 (기준서 §12):
    ///   유니티에서는 Play Mode 확인이 형님 몫이 된다. Claude가 스스로 검증할 수 있는 건
    ///   EditMode 테스트뿐이다. 그래서 스윙의 핵심 수학을 여기 몰아넣고 전부 테스트한다.
    ///
    /// 절대 조건:
    ///   1. NaN을 내지 않는다.
    ///   2. 속도를 늘리지 않는다. 로프 구속은 에너지를 옮길 뿐 만들지 않는다.
    ///      (Ver.1에서 연출이 몰래 속도를 더한 사고가 있었다 — 기준서 부록 D)
    /// </summary>
    public static class WebPhysics
    {
        const float Eps = 1e-5f;

        /// <summary>
        /// 로프가 팽팽할 때 반경(줄 방향) 속도를 제거하고 그 일부를 접선으로 되돌린다.
        /// convert = SWING_CONVERT (§7.2, 0.995). 1.0이어도 속도는 늘지 않는다.
        /// 줄이 늘어져 있으면(dist &lt;= length) 속도를 건드리지 않는다.
        /// </summary>
        public static Vector3 SolveRope(Vector3 position, Vector3 velocity,
                                        Vector3 anchor, float length, float convert)
        {
            return SolveRope(position, velocity, anchor, length, convert, Vector3.zero);
        }

        /// <summary>
        /// intent = 플레이어가 가고 싶어 하는 방향(보통 카메라 앞).
        ///
        /// 이게 왜 필요한가:
        ///   속도가 줄과 완전히 나란하면(앵커 바로 아래에서 수직 낙하) 접선 성분이 0이다.
        ///   방향이 없으면 속도가 통째로 사라진다. 화면에서는 "줄에 걸리자마자 뚝 멈춤"으로 보인다.
        ///   그럴 때 의도 방향을 접평면에 투영해 그쪽으로 흘려보낸다.
        ///   헌법 §5.1 — 플레이어가 의도를 주면 게임이 알아서 잘 쓴다.
        /// </summary>
        public static Vector3 SolveRope(Vector3 position, Vector3 velocity,
                                        Vector3 anchor, float length, float convert,
                                        Vector3 intent)
        {
            Vector3 d = position - anchor;
            float dist = d.magnitude;

            // 딱 팽팽한 순간(dist == length)에도 구속을 건다.
            // <= 로 두면 그 프레임은 그냥 통과해서 줄이 한 스텝만큼 늘어난 뒤에야 잡힌다.
            if (dist < length || dist < Eps) return velocity;     // 늘어짐 = 자유낙하

            Vector3 n = d / dist;                                 // 앵커에서 바깥으로
            float radial = Vector3.Dot(velocity, n);
            if (radial <= 0f) return velocity;                    // 앵커 쪽으로 가는 중이면 줄이 막지 않는다

            Vector3 tangential = velocity - n * radial;
            float tanSq = tangential.sqrMagnitude;

            // 반경 속도의 에너지를 접선으로 옮긴다. convert <= 1 이므로 총 속력은 절대 늘지 않는다.
            float c = Mathf.Clamp01(convert);
            float newSpeed = Mathf.Sqrt(tanSq + c * radial * radial);

            Vector3 dir;
            if (tanSq > Eps * Eps)
            {
                dir = tangential / Mathf.Sqrt(tanSq);
            }
            else
            {
                Vector3 t = intent - n * Vector3.Dot(intent, n);   // 의도를 접평면에 투영
                if (t.sqrMagnitude < Eps * Eps) return Vector3.zero;
                dir = t.normalized;
            }

            return dir * newSpeed;
        }

        /// <summary>
        /// 앵커가 너무 아래면 그네가 아니라 추락이다 (§7.1 DROP_MAX).
        /// 벽에 처박히는 대부분의 상황이 여기서 걸러진다.
        /// </summary>
        public static bool IsAnchorTooLow(float anchorY, float playerY, float dropMax)
        {
            return anchorY < playerY - dropMax;
        }

        /// <summary>
        /// 위치가 로프 길이를 넘어갔으면 구(球) 위로 되돌린다.
        /// stiffness 1.0이면 즉시, 낮추면 부드럽게. 넘지 않았으면 그대로 둔다.
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

        /// <summary>
        /// 펌프 — 진행 중인 접선 방향으로 가속한다. 그네에서 다리를 굽혔다 펴는 것.
        /// 로프와 나란한 성분은 넣지 않는다. 넣으면 줄이 늘어나려 하고 구속이 다시 지운다.
        /// </summary>
        public static Vector3 Pump(Vector3 position, Vector3 velocity, Vector3 anchor,
                                   float accel, float dt)
        {
            Vector3 d = position - anchor;
            float dist = d.magnitude;
            if (dist < Eps) return velocity;

            Vector3 n = d / dist;
            Vector3 tangential = velocity - n * Vector3.Dot(velocity, n);
            float tSpeed = tangential.magnitude;
            if (tSpeed < Eps) return velocity;

            return velocity + (tangential / tSpeed) * (accel * dt);
        }

        /// <summary>줄 감기. 길이를 줄이되 ROPE_MIN 아래로는 내려가지 않는다.</summary>
        public static float Reel(float length, float rate, float dt, float ropeMin)
        {
            return Mathf.Max(ropeMin, length - rate * dt);
        }

        /// <summary>
        /// 줄을 감으면 각운동량이 보존되어 접선 속도가 빨라진다 (v_t · r = 일정).
        /// 피겨 스케이터가 팔을 오므리면 빨라지는 것과 같다.
        ///
        /// ★ 이게 스윙에서 속도를 버는 진짜 방법이다.
        ///   길이만 줄이고 속도를 안 건드리면 "감아도 안 빨라지는" 그네가 된다.
        ///   한 스텝에 붙는 배율은 막아둔다. 안 막으면 앵커에 가까울수록 발산한다.
        /// </summary>
        public static Vector3 ReelBoost(Vector3 position, Vector3 velocity, Vector3 anchor,
                                        float oldLength, float newLength, float maxStepGain)
        {
            if (newLength < Eps || oldLength < Eps || newLength >= oldLength) return velocity;

            Vector3 d = position - anchor;
            float dist = d.magnitude;
            if (dist < Eps) return velocity;

            // 줄이 늘어져 있으면 감아도 아무 일 없다. 팽팽해질 때까지는 길이만 준다.
            if (dist < newLength) return velocity;

            Vector3 n = d / dist;
            float radial = Vector3.Dot(velocity, n);
            Vector3 tangential = velocity - n * radial;

            float scale = Mathf.Clamp(oldLength / newLength, 1f, Mathf.Max(1f, maxStepGain));
            return n * radial + tangential * scale;
        }

        /// <summary>
        /// 앵커 쪽으로 당긴다. 줄을 감을 때 몸이 실제로 끌려가는 힘.
        /// 공중에서 바닥에 쏘고 급강하하거나, 벽으로 당겨 붙는 데 쓴다.
        /// </summary>
        public static Vector3 Pull(Vector3 position, Vector3 velocity, Vector3 anchor,
                                   float accel, float dt)
        {
            Vector3 d = anchor - position;
            float dist = d.magnitude;
            if (dist < Eps) return velocity;

            return velocity + (d / dist) * (accel * dt);
        }

        /// <summary>이 앵커가 스윙으로 쓸 만한가. 너무 짧으면 그네가 아니라 벽에 박는다 (§7.2).</summary>
        public static bool IsSwingable(float distance, float ropeMin, float ropeMax)
        {
            return distance >= ropeMin && distance <= ropeMax;
        }

        /// <summary>NaN/Infinity 방어. 물리 값이 오염되면 여기서 잡아 0으로 돌린다.</summary>
        public static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }
    }
}
