using UnityEngine;
using SpiderVer2.Player;

namespace SpiderVer2.Web
{
    /// <summary>
    /// P2 — 단일 웹. 실제 월드 표면에만 건다. 허공에 가짜 앵커를 만들지 않는다 (헌법 §5.1).
    ///
    /// Ver.1 game3d.js:6244-6303 의 이식. 로프 길이가 base(목표) / len(실제) 두 개다.
    ///   base — 플레이어가 정한다. 걸 때의 거리, Space로 감으면 줄어든다.
    ///   len  — 자동 펌핑이 얹힌 목표를 초당 ROPE_FOLLOW_RATE로 따라간다.
    /// 이 분리가 없으면 길이가 뚝뚝 끊겨 스윙이 기계적으로 느껴진다.
    ///
    /// P2에서는 조준한 곳에 건다. 조준 없이 알아서 걸리는 건 P3(AUTO ANCHOR V2)다.
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
        public float RopeBase { get { return _base; } }
        public float AnchorDistance { get; private set; }
        public float Grip { get; private set; }
        public string LastReject { get; private set; }
        public bool Reeling { get; private set; }

        WebConnection _web = WebConnection.None;
        float _base;
        Collider _self;

        void Awake()
        {
            _self = GetComponent<Collider>();
            LastReject = "-";
        }

        void Update()
        {
            if (input == null || tuning == null) return;

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

            // Ver.1과 같다: 코앞 벽에 걸면 줄이 ROPE_MIN으로 잘려 벽에 박는다.
            float dist = Mathf.Max(Vector3.Distance(transform.position, hit.point), tuning.ROPE_MIN);
            if (dist > tuning.ROPE_MAX) { LastReject = "too far"; return; }

            // 아래쪽 표면도 허용한다. DROP_MAX는 P3 자동 앵커가 후보를 고를 때의 점수 규칙이지
            // 플레이어가 직접 조준한 곳을 막으라는 뜻이 아니다.

            _web = WebConnection.Attach(hand, hit.point, hit.normal, dist);
            _base = dist;
            Grip = 0f;
            LastReject = "-";
        }

        public void Release()
        {
            _web = WebConnection.None;
            Grip = 0f;
            Reeling = false;
        }

        /// <summary>
        /// PlayerMotor가 중력·가속·드래그를 다 넣은 뒤에 부른다.
        /// Ver.1은 위치를 먼저 적분하고 그 뒤에 구속을 건다. 그래서 여기서도
        /// 이번 스텝에 도달할 위치를 먼저 예측한 다음 그 자리 기준으로 푼다.
        /// 순서를 바꾸면 항상 한 스텝 늦게 잡혀 줄이 늘어나 보인다.
        /// </summary>
        public Vector3 Constrain(Vector3 velocity, ref Vector3 position, float dt)
        {
            if (!_web.connected || tuning == null) return velocity;

            _web.age += dt;
            Grip = Mathf.Min(1f, _web.age / Mathf.Max(tuning.GRIP_TIME, 0.0001f));

            Vector3 predicted = position + velocity * dt;
            AnchorDistance = Vector3.Distance(predicted, _web.anchor);

            // ── 수동 릴 인 (Space). 팽팽할 때 감으면 아래 각운동량 보존이 가속으로 이어진다.
            Reeling = input != null && input.ReelHeld;
            if (Reeling)
            {
                _base = WebPhysics.Reel(_base, tuning.REEL_MANUAL, dt, tuning.ROPE_MIN);
                velocity = WebPhysics.Pull(predicted, velocity, _web.anchor, tuning.ZIP_PULL, dt);
            }

            // ── 자동 펌핑 + 길이 추종. 이게 "가만히 있어도 스윙이 살아 있는" 느낌을 만든다.
            float desired = WebPhysics.AutoPumpTarget(predicted, _web.anchor, _base,
                                                      tuning.PUMP_DEPTH, tuning.ROPE_MIN);
            float oldLen = _web.length;
            _web.length = WebPhysics.FollowLength(oldLen, desired, tuning.ROPE_FOLLOW_RATE, dt);

            // ── 감기면 각운동량 보존으로 접선 속도가 붙는다 (채찍 가속)
            velocity = WebPhysics.ReelBoost(predicted, velocity, _web.anchor,
                                            oldLen, _web.length, tuning.REEL_GAIN_MAX);

            // ── 펌프 (E). 수평 진행 방향으로 민다.
            if (input != null && input.PumpHeld)
                velocity = WebPhysics.Pump(velocity, tuning.PUMP_ACCEL, dt);

            // ── 로프 구속
            Vector3 intent = eye != null ? eye.forward : transform.forward;
            velocity = WebPhysics.SolveRope(predicted, velocity, _web.anchor, _web.length,
                                            tuning.SWING_CONVERT, Grip, intent, tuning.MAX_SPEED);

            // 쌓인 늘어남은 현재 위치에서 걷어낸다.
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
