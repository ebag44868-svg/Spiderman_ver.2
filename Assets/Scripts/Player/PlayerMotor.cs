using UnityEngine;
using SpiderVer2.Web;

namespace SpiderVer2.Player
{
    /// <summary>
    /// P1 이동. 지상 걷기 · 점프 · 공중 조작만 한다.
    ///
    /// 이건 임시가 아니라 '바닥'이다. P2에서 웹 물리가 들어오면 이 스크립트는
    /// 중력과 공중 가속만 남기고 궤적의 주인 자리를 웹에 넘긴다.
    /// 그래서 지금부터 속도(_rb.linearVelocity)를 단일 진실로 쓴다.
    /// 헌법 §5.2 MOTION FIRST — 물리가 이동을 결정한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        public TuningConfig tuning;
        [Tooltip("이 레이어만 바닥/벽으로 친다")] public LayerMask worldMask = ~0;
        [Tooltip("붙어 있으면 로프 구속을 받는다. 없어도 P1처럼 동작한다")]
        public WebController web;

        public bool Grounded { get; private set; }
        public float Speed { get; private set; }
        public Vector3 Velocity { get { return _rb != null ? _rb.linearVelocity : Vector3.zero; } }
        public string State { get; private set; }

        Rigidbody _rb;
        CapsuleCollider _col;
        PlayerInputRouter _input;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _input = GetComponent<PlayerInputRouter>();

            _rb.useGravity = false;              // 중력은 우리가 준다 (기준서 §7의 GRAVITY)
            _rb.freezeRotation = true;           // 캡슐이 굴러다니면 안 된다
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (tuning != null)
                Time.fixedDeltaTime = tuning.DT; // 고정 120Hz (§7.2)

            State = "GROUND";
        }

        void FixedUpdate()
        {
            if (tuning == null) return;

            float dt = Time.fixedDeltaTime;
            var v = _rb.linearVelocity;

            Grounded = CheckGrounded();
            bool swinging = web != null && web.Connected;

            // ── 입력을 월드 방향으로. 몸의 yaw를 기준으로 한다 (1인칭 카메라가 몸을 돌린다)
            Vector2 mv = _input != null ? _input.Move : Vector2.zero;
            Vector3 wish = transform.right * mv.x + transform.forward * mv.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            float accel = Grounded ? tuning.ACCEL : tuning.AIR_ACCEL;
            v += wish * (accel * dt);

            // ── 지상에서만 걷기 속도로 묶는다. 공중은 웹이 주인이라 묶지 않는다.
            // 스윙 중에는 바닥을 스쳐도 묶지 않는다. 묶으면 저고도 스윙이 매번 죽는다 (§4.1 골목 구간).
            if (Grounded && !swinging)
            {
                Vector3 flat = new Vector3(v.x, 0f, v.z);
                float flatSpeed = flat.magnitude;

                if (wish.sqrMagnitude < 0.01f)
                {
                    // 마찰
                    float drop = tuning.GROUND_DRAG * dt;
                    float scale = flatSpeed > 0f ? Mathf.Max(0f, flatSpeed - drop) / flatSpeed : 0f;
                    flat *= scale;
                }
                else if (flatSpeed > tuning.GROUND_MAX_SPEED)
                {
                    flat *= tuning.GROUND_MAX_SPEED / flatSpeed;
                }

                v.x = flat.x; v.z = flat.z;
            }

            // ── 중력
            if (!Grounded || swinging) v.y -= tuning.GRAVITY * dt;
            else if (v.y < 0f) v.y = 0f;

            // ── 점프. 웹에 매달린 상태에서 Space는 점프가 아니라 펌프다.
            if (_input != null && _input.JumpPressed && Grounded && !swinging)
            {
                v.y = tuning.JUMP_SPEED;
                Grounded = false;
            }

            // ── 로프 구속. 중력·가속을 다 넣은 뒤 마지막에 진실을 강제한다 (헌법 §5.2).
            if (swinging)
            {
                Vector3 p = _rb.position;
                v = web.Constrain(v, ref p, dt);
                if ((p - _rb.position).sqrMagnitude > 1e-10f) _rb.position = p;
            }

            // ── 속도 상한. SOFT_SPEED 위로는 하드 클램프가 아니라 드래그다 (§7.2)
            // 수학은 MotionMath에 있다. 씬 없이 테스트할 수 있어야 하기 때문이다.
            v = SpiderVer2.Core.MotionMath.ClampSpeed(v, tuning.SOFT_SPEED, tuning.MAX_SPEED,
                                                      tuning.SOFT_DRAG, dt);

            _rb.linearVelocity = v;

            Speed = v.magnitude;
            State = swinging ? "SWING"
                  : Grounded ? "GROUND"
                  : (v.y > 0f ? "AIR_UP" : "AIR_DOWN");
        }

        bool CheckGrounded()
        {
            // 캡슐 중심에서 아래로 쏜다. 시작 시점에 겹친 콜라이더(= 자기 자신)는 SphereCast가 무시한다.
            float r = _col.radius * 0.95f;
            Vector3 origin = transform.TransformPoint(_col.center);
            float dist = _col.height * 0.5f - r + tuning.GROUND_CHECK;

            RaycastHit hit;
            return Physics.SphereCast(origin, r, Vector3.down, out hit, dist,
                                      worldMask, QueryTriggerInteraction.Ignore);
        }
    }
}
