using UnityEngine;

/// <summary>
/// 기준서 §7 실측 숫자표. Ver.1에서 브라우저 실측으로 맞춘 값들이다.
/// 필드 이름은 기준서와 원본 JS 상수 이름을 그대로 쓴다. 대조가 쉬워야 하기 때문이다.
/// Inspector에서 바꾼 값은 플레이 중에도 즉시 반영된다 (매 프레임 읽는다).
/// </summary>
[CreateAssetMenu(fileName = "TuningConfig", menuName = "Spider/Tuning Config")]
public class TuningConfig : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────
    [Header("§7.1 자동 앵커 — 의도 벡터 가중치 (P3)")]
    [Tooltip("'빠름'의 기준 속도 (m/s)")] public float SP_REF = 55f;
    [Tooltip("느릴 때 카메라 비중")] public float W_CAM_SLOW = 1.60f;
    [Tooltip("빠를 때 카메라 비중. 낮춰야 90도 급snap이 안 난다")] public float W_CAM_FAST = 0.70f;
    [Tooltip("느릴 때 관성 비중")] public float W_MOM_SLOW = 0.25f;
    [Tooltip("빠를 때 관성 비중")] public float W_MOM_FAST = 1.55f;
    [Tooltip("WASD 방향")] public float W_MOVE = 0.55f;
    [Tooltip("A/D만의 옆 편향")] public float W_TURN = 0.90f;

    [Header("§7.1 자동 앵커 — 후보 점수식 (P3)")]
    [Tooltip("의도 방향 일치도")] public float W_FWD = 1.60f;
    [Tooltip("줄 길이의 질")] public float W_LEN = 1.20f;
    [Tooltip("머리 바로 위 회피")] public float W_STEEP = 0.80f;
    [Tooltip("높이")] public float W_HIGH = 1.10f;
    [Tooltip("손 쪽 편향. 기계적 교대가 안 될 만큼만")] public float W_HAND = 0.55f;
    [Tooltip("직전 손의 반대쪽 보너스")] public float W_ALT = 0.30f;
    [Tooltip("급선회 벌점 (속도 비례)")] public float W_SHARP = 1.30f;

    [Header("§7.1 자동 앵커 — 기하 제약 (P3)")]
    [Tooltip("최대 사거리의 이 비율이 최적 호")] public float LEN_BEST = 0.62f;
    public float LEN_SPAN = 0.45f;
    [Tooltip("이보다 아래는 그네가 아니라 추락")] public float DROP_MAX = 14f;
    [Tooltip("의도 기준 이보다 뒤면 기각")] public float BACK_MIN = -0.15f;

    [Tooltip("부채꼴 yaw (도). 손 쪽으로 최대 10도 편향된다")]
    public float[] FAN_YAW = { 0f, 15f, -15f, 32f, -32f, 52f, -52f, 74f, -74f, 96f, -96f };
    [Tooltip("부채꼴 pitch (도). 11 x 8 = 88발")]
    public float[] FAN_PITCH = { 34f, 44f, 24f, 56f, 14f, 68f, 4f, -6f };

    // ─────────────────────────────────────────────────────────────
    [Header("§7.2 로프 · 속도")]
    [Tooltip("최소 로프 길이")] public float ROPE_MIN = 12f;
    [Tooltip("이보다 짧으면 그네가 아니라 벽에 박는다")] public float SWING_MIN_LEN = 18f;
    [Tooltip("사거리 = 최대 로프 길이")] public float ROPE_MAX = 150f;
    [Tooltip("낙하(반경) 속도를 접선으로 되돌리는 비율")] public float SWING_CONVERT = 0.995f;
    public float MAX_SPEED = 112f;
    [Tooltip("이 위로는 하드 클램프 대신 드래그")] public float SOFT_SPEED = 82f;
    [Tooltip("지상 가속. 90은 출발이 무거웠다")] public float ACCEL = 118f;
    [Tooltip("공중 조작. 30은 스윙 중 전환이 굼떴다")] public float AIR_ACCEL = 42f;
    [Tooltip("홀드 속도 증강")] public float PUMP_ACCEL = 58f;
    [Tooltip("고정 타임스텝 (1/120)")] public float DT = 1f / 120f;

    // ─────────────────────────────────────────────────────────────
    [Header("§7.3 벽 짚기 (P5)")]
    [Tooltip("이 시간 안에 벽면을 넘어설 것 같으면 짚는다")] public float PLANT_LOOK = 0.20f;
    [Tooltip("이보다 느리면 짚지 않는다. 저속은 '벽 붙기'의 몫")] public float PLANT_MIN_V = 15f;
    [Tooltip("손이 벽에 닿아 있는 시간")] public float PLANT_TIME = 0.30f;
    [Tooltip("벽면을 따라 흐르는 속도 유지율")] public float PLANT_KEEP = 0.88f;
    [Tooltip("벽을 향하던 속도의 이만큼으로 되밀린다")] public float PLANT_BOUNCE = 0.45f;
    [Tooltip("최소 밀기")] public float PLANT_PUSH = 11f;
    [Tooltip("상한. 없으면 고속에서 튕겨 날아간다")] public float PLANT_PUSH_MAX = 28f;
    [Tooltip("살짝 위로. 없으면 짚을 때마다 고도가 깎인다")] public float PLANT_UP = 7f;
    [Tooltip("되짚기 방지")] public float PLANT_CD = 0.45f;
    [Tooltip("접촉점을 어깨 높이로")] public float PLANT_HAND_Y = 1.15f;

    // ─────────────────────────────────────────────────────────────
    [Header("§7.4 웹 수직 이동 (P5)")]
    [Tooltip("벽에서 이 안쪽이어야 '건물을 타는 중'")] public float VC_NEAR = 9f;
    [Tooltip("한 번에 타고 오르는 높이")] public float VC_STEP = 45f;
    [Tooltip("벽에서 앵커까지 이격. 기준서 §4.4에 따라 3.6 → 1.5로 낮췄다")] public float VC_OUT = 1.5f;
    [Tooltip("앵커에 이만큼 가까워지면 다음 자리")] public float VC_ARRIVE = 7f;
    [Tooltip("지붕을 이만큼 넘겨 잡아야 옥상으로 올라선다")] public float VC_TOP = 2.5f;
    [Tooltip("연속 발사 간격")] public float VC_CD = 0.30f;

    // ─────────────────────────────────────────────────────────────
    [Header("§7.5 보조 웹 · 슬링샷 (P4)")]
    [Tooltip("보조 웹이 당기는 가속 (m/s^2)")] public float WEB2_PULL = 62f;
    [Tooltip("이 거리부터 약해진다")] public float WEB2_FADE = 45f;
    [Tooltip("최대 충전 시간")] public float SLING_MAX = 0.50f;
    public float SLING_MIN = 0.10f;
    [Tooltip("최대 충전에서 더해지는 속도")] public float SLING_BOOST = 64f;
    [Tooltip("밀고 나가면서 줄을 감는 비율")] public float SLING_REEL = 0.22f;
    [Tooltip("물고 있는 동안 속도 감쇠")] public float SLING_DRAG = 5.2f;

    // ─────────────────────────────────────────────────────────────
    [Header("§7.5 카메라")]
    [Tooltip("뱅킹 롤 강도. 1인칭은 아래 FP_ROLL_MODE로 따로 고른다")] public float CAM_ROLL = 0.5f;
    [Tooltip("소프트 락온 원뿔 (대략 +-84도)")] public float SOFT_CONE = 0.10f;
    [Tooltip("선입력 유지 시간")] public float M_BUF_T = 0.42f;

    [Header("카메라 — 1인칭 (기준서 §4.5 · 멀미 대책)")]
    [Tooltip("기본 시야각. 레퍼런스는 광각이다")] public float FP_FOV = 78f;
    [Tooltip("최고 속도에서 더해지는 시야각")] public float FP_FOV_SPEED_ADD = 14f;
    [Tooltip("마우스 감도 (도/픽셀)")] public float LOOK_SENS = 0.12f;
    [Tooltip("위아래 시야 제한 (도)")] public float PITCH_LIMIT = 89f;
    [Tooltip("1인칭 롤 세기. 0=끔 / 0.35=약(기본) / 0.5=강")] public float FP_ROLL = 0.35f;
    [Tooltip("켜면 화면 대신 몸을 기울인다. Ver.1에서 멀미를 없앤 방식")] public bool FP_LEAN_BODY = true;
    [Tooltip("몸 기울기 최대 각도 (도)")] public float FP_LEAN_MAX = 32f;

    // ─────────────────────────────────────────────────────────────
    [Header("웹 조작 — Ver.1 game3d.js:4237-4241 이식. 스윙 감각의 핵심이다")]
    [Tooltip("로프가 완전히 물리기까지의 시간(초). 붙는 순간 덜컹거림을 없앤다")] public float GRIP_TIME = 0.13f;
    [Tooltip("실제 길이가 목표를 따라가는 속도 (m/s). 즉시 바꾸면 뚝뚝 끊긴다")] public float ROPE_FOLLOW_RATE = 40f;
    [Tooltip("Space 홀드로 줄을 감는 속도 (m/s)")] public float REEL_MANUAL = 26f;
    [Tooltip("자동 펌핑 강도. 호 바닥에서 로프가 이 비율만큼 줄어든다")] public float PUMP_DEPTH = 0.12f;
    [Tooltip("감기 한 스텝의 최대 배율. 앵커에 붙을수록 발산하는 걸 막는다 (Ver.1 1.01)")] public float REEL_GAIN_MAX = 1.01f;
    [Tooltip("줄 감을 때 앵커 쪽으로 끌려가는 가속 (m/s^2). Ver.2 신규 — 급강하·벽 붙기")] public float ZIP_PULL = 55f;
    [Tooltip("로프 위치 보정 강도. Ver.1은 즉시 스냅(1.0)한다")] [Range(0f, 1f)] public float ROPE_STIFF = 1f;

    [Header("드래그 (Ver.1 game3d.js:6203). 스윙 중에는 거의 걸지 않는다")]
    [Tooltip("지상 수평 감쇠 계수")] public float DRAG_GROUND = 0.06f;
    [Tooltip("공중 수평 감쇠 계수")] public float DRAG_AIR = 0.02f;
    [Tooltip("스윙 중 수평 감쇠 계수. 여기서 깎으면 절대 빨라지지 않는다")] public float DRAG_SWING = 0.003f;

    // ─────────────────────────────────────────────────────────────
    [Header("지상 이동 — Ver.1 game3d.js:4151-4157 이식")]
    [Tooltip("중력. Ver.1 G=72. 큰 스케일에서 붕 뜨지 않도록 실제 중력보다 크게 잡는다")] public float GRAVITY = 72f;
    [Tooltip("점프 초기 속도. Ver.1 JUMP_V=42. 중력을 올린 만큼 같이 올렸다")] public float JUMP_SPEED = 42f;
    [Tooltip("걷기 최고 속도. Ver.1 MOVE_SPEED=19")] public float MOVE_SPEED = 19f;
    [Tooltip("지상 Shift 달리기 배수. Ver.1 SPRINT_MULT=1.85")] public float SPRINT_MULT = 1.85f;
    [Tooltip("접지 판정 거리")] public float GROUND_CHECK = 0.25f;
}
