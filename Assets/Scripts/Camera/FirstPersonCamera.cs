using UnityEngine;
using SpiderVer2.Player;

namespace SpiderVer2.CameraRig
{
    /// <summary>
    /// 1인칭 카메라. 레퍼런스 영상이 1인칭이라 이걸 먼저 완성한다 (3인칭은 P7).
    ///
    /// Ver.1 교훈 두 가지가 이 코드의 뼈대다:
    ///   1. 1인칭에서 화면을 기울이면 "세상이 도는" 걸로 읽혀 멀미가 난다.
    ///      그래서 몸을 기울이는 방식(FP_LEAN_BODY)을 기본으로 두고, 화면 롤은 설정으로 뺀다.
    ///   2. yaw는 몸에, pitch는 카메라에. 몸이 카메라를 따라 도는 게 아니라 그 반대다.
    ///
    /// 속도감은 숫자가 아니라 카메라에서 나온다 (§4.5). 여기서 광각 + 속도 FOV를 담당한다.
    /// </summary>
    public class FirstPersonCamera : MonoBehaviour
    {
        public TuningConfig tuning;
        public Transform body;          // 플레이어 캡슐. yaw를 여기에 준다.
        public PlayerInputRouter input;
        public PlayerMotor motor;

        [Tooltip("눈 높이 (캡슐 중심 기준)")] public float eyeHeight = 0.65f;

        float _pitch;
        float _roll;
        float _lean;
        Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (body == null && transform.parent != null) body = transform.parent;
        }

        void LateUpdate()
        {
            if (tuning == null || body == null) return;

            // ── 시점
            Vector2 look = input != null ? input.Look : Vector2.zero;
            body.Rotate(Vector3.up, look.x * tuning.LOOK_SENS, Space.World);

            _pitch -= look.y * tuning.LOOK_SENS;
            _pitch = Mathf.Clamp(_pitch, -tuning.PITCH_LIMIT, tuning.PITCH_LIMIT);

            // ── 뱅킹. 옆으로 가는 속도만큼 기운다.
            float bank = 0f;
            if (motor != null && motor.Speed > 1f)
            {
                Vector3 v = motor.Velocity;
                float lateral = Vector3.Dot(v, body.right);
                bank = Mathf.Clamp(-lateral / Mathf.Max(tuning.SP_REF, 1f), -1f, 1f);
            }

            if (tuning.FP_LEAN_BODY)
            {
                // 화면 기울기 0도, 몸 기울기 32도. Ver.1에서 이 방식으로 멀미를 없앴다.
                _lean = Mathf.Lerp(_lean, bank * tuning.FP_LEAN_MAX, 1f - Mathf.Exp(-8f * Time.deltaTime));
                _roll = Mathf.Lerp(_roll, 0f, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }
            else
            {
                _roll = Mathf.Lerp(_roll, bank * tuning.FP_ROLL * 45f, 1f - Mathf.Exp(-8f * Time.deltaTime));
                _lean = Mathf.Lerp(_lean, 0f, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, _roll);

            // ── 광각 + 속도 FOV (§4.5)
            if (_cam != null)
            {
                float t = motor != null ? Mathf.Clamp01(motor.Speed / tuning.MAX_SPEED) : 0f;
                float target = tuning.FP_FOV + tuning.FP_FOV_SPEED_ADD * t;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
            }
        }

        /// <summary>몸 기울기(도). P6에서 캐릭터 메시가 들어오면 이 값을 쓴다.</summary>
        public float LeanDegrees { get { return _lean; } }
        public float PitchDegrees { get { return _pitch; } }
    }
}
