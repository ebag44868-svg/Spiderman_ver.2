using UnityEngine;
using SpiderVer2.Player;

namespace SpiderVer2.Web
{
    /// <summary>
    /// P2 — 단일 웹. 실제 월드 표면에만 건다. 허공에 가짜 앵커를 만들지 않는다 (헌법 §5.1).
    ///
    /// P2에서는 조준한 곳에 건다. 조준 없이 알아서 걸리는 건 P3(AUTO ANCHOR V2)다.
    /// 여기서 하는 일은 "로프가 물리적으로 정직하게 도는가"를 세우는 것뿐이다.
    ///
    /// 속도의 주인은 PlayerMotor다. 이 스크립트는 PlayerMotor가 불러줄 때만 속도를 만진다.
    /// Unity Joint를 쓰지 않는다 (기준서 P2: "SpringJoint 붙이고 완료로 처리하지 않는다").
    /// </summary>
    public class WebController : MonoBehaviour
    {
        public TuningConfig tuning;
        public PlayerInputRouter input;
        public Transform eye;                  // 조준 기준 = 1인칭 카메라
        public LayerMask worldMask = ~0;


        public WebConnection Web { get { return _web; } }
        public bool Connected { get { return _web.connected; } }
        public float RopeLength { get { return _web.length; } }
        public float AnchorDistance { get; private set; }
        public string LastReject { get; private set; }
        public bool Reeling { get; private set; }

        WebConnection _web = WebConnection.None;
        Collider _self;

        void Awake()
        {
            _self = GetComponent<Collider>();
            LastReject = "-";
        }

        void Update()
        {
            if (input == null || tuning == null) return;

            // 누르면 쏘고, 떼면 놓는다. 홀드 방식이라 손을 뗐는지가 손에 남는다.
            if (input.WebRightPressed) TryShoot(Hand.Right);
            if (input.WebLeftPressed) TryShoot(Hand.Left);

            if (_web.connected)
            {
                bool stillHeld = _web.hand == Hand.Right ? input.WebRightHeld : input.WebLeftHeld;
                if (!stillHeld) Release();
            }
        }

        /// <summary>조준선 끝의 실제 표면을 찾는다. 못 찾으면 붙지 않는다.</summary>
        public void TryShoot(Hand hand)
        {
            if (eye == null) { LastReject = "no eye"; return; }

            RaycastHit hit;
            if (!Physics.Raycast(eye.position, eye.forward, out hit, tuning.ROPE_MAX,
                                 worldMask, QueryTriggerInteraction.Ignore))
            {
                LastReject = "no surface";
                return;
            }
            if (hit.collider == _self) { LastReject = "self"; return; }

            float dist = Vector3.Distance(transform.position, hit.point);
            if (!WebPhysics.IsSwingable(dist, tuning.ROPE_MIN, tuning.ROPE_MAX))
            {
                LastReject = dist < tuning.ROPE_MIN ? "too close" : "too far";
                return;
            }

            // 아래쪽 표면도 허용한다.
            // 기준서 §7.1의 DROP_MAX는 P3 '자동 앵커가 후보를 고를 때'의 점수 규칙이지,
            // 플레이어가 직접 조준한 곳을 막으라는 뜻이 아니다.
            // 공중에서 바닥에 쏘고 급강하하는 것도 이 게임이 하려는 것이다 (헌법 §5.3).

            _web = WebConnection.Attach(hand, hit.point, hit.normal, dist);
            LastReject = "-";
        }

        public void Release()
        {
            _web = WebConnection.None;
        }

        /// <summary>
        /// PlayerMotor가 중력·가속을 다 넣은 뒤에 부른다.
        /// 순서가 중요하다. 로프는 마지막에 진실을 강제하는 쪽이어야 한다.
        /// </summary>
        public Vector3 Constrain(Vector3 velocity, ref Vector3 position, float dt)
        {
            if (!_web.connected || tuning == null) return velocity;

            _web.age += dt;
            AnchorDistance = Vector3.Distance(position, _web.anchor);

            // 감기 — 왼쪽 Shift.
            // 길이만 줄이면 아무 일도 안 일어난다. 각운동량 보존으로 접선 속도가 붙고,
            // 동시에 앵커 쪽으로 끌려간다. 이 둘이 있어야 "감으면 빨라진다"가 된다.
            if (input != null && input.ClimbHeld)
            {
                float oldLen = _web.length;
                _web.length = WebPhysics.Reel(oldLen, tuning.REEL_RATE, dt, tuning.ROPE_MIN);
                velocity = WebPhysics.ReelBoost(position, velocity, _web.anchor,
                                                oldLen, _web.length, tuning.REEL_GAIN_MAX);
                velocity = WebPhysics.Pull(position, velocity, _web.anchor, tuning.ZIP_PULL, dt);
                Reeling = true;
            }
            else Reeling = false;

            // 펌프 — Space. 접선 방향으로만 민다.
            if (input != null && input.JumpHeld)
                velocity = WebPhysics.Pump(position, velocity, _web.anchor, tuning.PUMP_ACCEL, dt);

            Vector3 intent = eye != null ? eye.forward : transform.forward;
            velocity = WebPhysics.SolveRope(position, velocity, _web.anchor, _web.length,
                                            tuning.SWING_CONVERT, intent);
            position = WebPhysics.CorrectPosition(position, _web.anchor, _web.length, tuning.ROPE_STIFF);

            // 물리가 오염되면 여기서 끊는다. 조용히 NaN을 흘려보내지 않는다.
            if (!WebPhysics.IsFinite(velocity) || !WebPhysics.IsFinite(position))
            {
                Debug.LogError("WEB_NAN — 로프를 강제로 놓습니다. anchor=" + _web.anchor + " len=" + _web.length);
                Release();
                return Vector3.zero;
            }

            return velocity;
        }

        /// <summary>1인칭에서 줄이 나가는 지점. 어깨/손 근사다. 진짜 손 본은 P6.</summary>
        public Vector3 HandOrigin(Hand hand)
        {
            if (eye == null) return transform.position;
            float side = hand == Hand.Right ? 0.30f : -0.30f;
            return eye.position + eye.right * side - eye.up * 0.14f + eye.forward * 0.22f;
        }
    }
}
