using UnityEngine;
using UnityEngine.InputSystem;
using SpiderVer2.Player;
using SpiderVer2.CameraRig;
using SpiderVer2.Web;

namespace SpiderVer2.DebugTools
{
    /// <summary>
    /// 런타임 디버그 오버레이 + 인게임 도움말.
    ///
    /// 기준서 §10: "Free Web은 이게 없으면 튜닝이 불가능하다."
    /// 작업 규칙 7: 조작법이나 기능을 추가하면 인게임 도움말에도 반드시 같이 넣는다.
    /// 그래서 조작 목록은 여기 한 곳에만 있고, 새 조작이 생기면 여기를 고친다.
    ///
    /// 릴리즈 기본 OFF (기준서 §10). showOnStart를 끄면 F3로만 켜진다.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public PlayerMotor motor;
        public PlayerInputRouter input;
        public FirstPersonCamera fpCam;
        public WebController web;
        public TuningConfig tuning;

        [Tooltip("시작할 때 오버레이를 켜둘지. 릴리즈에서는 끈다")]
        public bool showOnStart = true;

        bool _showStats;
        bool _showHelp;
        GUIStyle _box, _label, _title, _help1, _help2, _help3;

        // 가상 해상도. 형님 Game 뷰가 QHD(2560x1440)라 고정 픽셀로 그리면
        // 조준점이 4~5픽셀짜리로 쪼그라든다. 1080 기준으로 그린 뒤 통째로 확대한다.
        const float RefHeight = 1080f;
        float _vw, _vh;

        void Awake()
        {
            _showStats = showOnStart;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) SetHelp(!_showHelp);
            if (kb.f3Key.wasPressedThisFrame) _showStats = !_showStats;
        }

        void EnsureStyles()
        {
            if (_box != null) return;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
            bg.Apply();

            _box = new GUIStyle(GUI.skin.box);
            _box.normal.background = bg;
            _box.padding = new RectOffset(12, 12, 10, 10);

            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = 14;
            _label.normal.textColor = Color.white;
            _label.richText = true;

            _title = new GUIStyle(_label);
            _title.fontSize = 17;
            _title.fontStyle = FontStyle.Bold;

            // 도움말은 전체화면이라 글씨도 그만큼 커야 한다.
            _help1 = new GUIStyle(_label); _help1.fontSize = 34; _help1.fontStyle = FontStyle.Bold;
            _help2 = new GUIStyle(_label); _help2.fontSize = 22; _help2.fontStyle = FontStyle.Bold;
            _help2.normal.textColor = new Color(0.55f, 0.85f, 1f);
            _help3 = new GUIStyle(_label); _help3.fontSize = 20;
        }

        /// <summary>도움말이 열려 있는 동안 게임을 멈춘다. 닫으면 되돌린다.</summary>
        void SetHelp(bool on)
        {
            _showHelp = on;
            Time.timeScale = on ? 0f : 1f;
            if (input != null)
            {
                input.suspended = on;
                input.LockCursor(!on);
            }
        }

        void OnDisable()
        {
            // Play를 끄거나 씬이 바뀔 때 시간이 멈춘 채로 남으면 안 된다.
            if (_showHelp) Time.timeScale = 1f;
            if (input != null) input.suspended = false;
        }

        void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / RefHeight);
            _vw = Screen.width / scale;
            _vh = Screen.height / scale;

            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                                       new Vector3(scale, scale, 1f));

            DrawReticle();
            if (_showStats) DrawStats();
            if (_showHelp) DrawHelp();
            DrawHint();

            GUI.matrix = oldMatrix;
        }

        /// <summary>
        /// 중앙 조준점. P2는 조준해서 쏘는 단계라 없으면 어디를 겨누는지 알 수 없다.
        /// P3에서 자동 앵커가 들어오면 이건 '의도 방향' 표시로 성격이 바뀐다.
        /// </summary>
        void DrawReticle()
        {
            float cx = _vw * 0.5f, cy = _vh * 0.5f;
            bool on = web != null && web.Connected;

            const float arm = 9f;    // 팔 길이
            const float gap = 4f;    // 가운데 빈 공간. 조준한 지점이 가려지지 않는다
            const float th = 2f;     // 두께

            var old = GUI.color;

            // 검은 테두리를 먼저 깔아야 밝은 하늘에서도 보인다
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            Cross(cx, cy, arm + 1f, gap - 1f, th + 2f);

            GUI.color = on ? new Color(0.45f, 1f, 0.55f, 0.95f)
                           : new Color(1f, 1f, 1f, 0.85f);
            Cross(cx, cy, arm, gap, th);

            GUI.color = old;
        }

        static void Cross(float cx, float cy, float arm, float gap, float th)
        {
            float h = th * 0.5f;
            GUI.DrawTexture(new Rect(cx - gap - arm, cy - h, arm, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + gap, cy - h, arm, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - h, cy - gap - arm, th, arm), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - h, cy + gap, th, arm), Texture2D.whiteTexture);
        }

        void DrawHint()
        {
            var r = new Rect(_vw - 230f, _vh - 32f, 220f, 24f);
            GUI.Label(r, "<color=#aaaaaa>F1 도움말 · F3 디버그</color>", _label);
        }

        void DrawStats()
        {
            var r = new Rect(12, 12, 340, 360);
            GUI.Box(r, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20));

            GUILayout.Label("SPIDER VER.2 — DEBUG", _title);
            GUILayout.Space(4);

            if (motor != null)
            {
                Vector3 v = motor.Velocity;
                GUILayout.Label(string.Format("Speed        <color=#7fdfff>{0,7:F1}</color> m/s", motor.Speed), _label);
                GUILayout.Label(string.Format("Velocity     {0,6:F1} {1,6:F1} {2,6:F1}", v.x, v.y, v.z), _label);
                GUILayout.Label(string.Format("Position     {0,6:F1} {1,6:F1} {2,6:F1}",
                    motor.transform.position.x, motor.transform.position.y, motor.transform.position.z), _label);
                GUILayout.Label("State        " + motor.State, _label);
                GUILayout.Label("Grounded     " + (motor.Grounded ? "<color=#8f8>YES</color>" : "no"), _label);
            }
            else
            {
                GUILayout.Label("<color=#f88>motor 미연결</color>", _label);
            }

            if (fpCam != null)
                GUILayout.Label(string.Format("Pitch/Lean   {0,6:F1} / {1,5:F1} deg",
                    fpCam.PitchDegrees, fpCam.LeanDegrees), _label);

            GUILayout.Space(6);
            if (web != null)
            {
                var c = web.Web;
                if (c.connected)
                {
                    GUILayout.Label("Web          <color=#8f8>" + c.hand + " CONNECTED</color>", _label);
                    GUILayout.Label(string.Format("Rope len     {0,7:F1} m  (실거리 {1:F1})",
                        c.length, web.AnchorDistance), _label);
                    GUILayout.Label(string.Format("Anchor       {0,6:F1} {1,6:F1} {2,6:F1}",
                        c.anchor.x, c.anchor.y, c.anchor.z), _label);
                    GUILayout.Label(string.Format("Age          {0,7:F2} s{1}", c.age,
                        web.Reeling ? "   <color=#ff0>REELING</color>" : ""), _label);
                }
                else
                {
                    GUILayout.Label("Web          <color=#888>none</color>   reject: " + web.LastReject, _label);
                }
            }
            GUILayout.Label("<color=#888>Auto Anchor  P3 · Candidates P3 · Swing Phase P6</color>", _label);

            GUILayout.Space(4);
            GUILayout.Label(string.Format("<color=#666>fixedDeltaTime {0:F5} ({1:F0} Hz)</color>",
                Time.fixedDeltaTime, 1f / Mathf.Max(Time.fixedDeltaTime, 0.0001f)), _label);

            GUILayout.EndArea();
        }

        /// <summary>
        /// 도움말은 화면 전체를 쓴다. 형님 모니터가 QHD라 작은 창은 읽히지 않는다.
        /// 열려 있는 동안 게임은 멈춘다 (Time.timeScale = 0).
        /// </summary>
        void DrawHelp()
        {
            var r = new Rect(0f, 0f, _vw, _vh);
            GUI.Box(r, GUIContent.none, _box);

            float pad = Mathf.Min(_vw, _vh) * 0.06f;
            GUILayout.BeginArea(new Rect(pad, pad, _vw - pad * 2f, _vh - pad * 2f));

            GUILayout.Label("조작법  <size=20><color=#888>— F1 을 다시 누르면 닫히고 게임이 계속됩니다</color></size>", _help1);
            GUILayout.Space(pad * 0.4f);

            GUILayout.Label("이동", _help2);
            HRow("W A S D", "이동");
            HRow("마우스", "시점");
            HRow("Space", "점프  (땅에 있고 웹이 없을 때)");
            HRow("왼쪽 Shift", "달리기  (땅에서)");
            HRow("Esc", "마우스 커서 풀기 (다시 화면을 클릭하면 잠김)");
            GUILayout.Space(pad * 0.35f);

            GUILayout.Label("웹  —  버튼을 누르고 있는 동안 붙어 있습니다", _help2);
            HRow("마우스 왼쪽", "오른손 웹 발사 / 떼면 놓기");
            HRow("마우스 오른쪽", "왼손 웹 발사 / 떼면 놓기");
            HRow("Space  (매달린 중)", "줄 감기 — 빨라지고 앵커 쪽으로 끌려갑니다");
            HRow("E  (매달린 중)", "펌프 — 진행 방향으로 가속");
            GUILayout.Space(pad * 0.35f);

            GUILayout.Label("속도를 버는 법", _help2);
            HRow("자동", "호 바닥에 가까워지면 줄이 저절로 감깁니다. 가만히 있어도 스윙이 삽니다");
            HRow("1", "옥상에서 앞으로 뛰어내립니다");
            HRow("2", "앞 건물 위쪽을 겨누고 마우스 왼쪽을 누르고 있습니다");
            HRow("3", "더 빠르게 하려면 Space 로 줄을 감습니다");
            HRow("4", "호의 바닥을 지날 때 E 로 펌프합니다");
            HRow("5", "올라가는 구간에서 버튼을 떼어 놓습니다");
            GUILayout.Space(pad * 0.35f);

            GUILayout.Label("디버그", _help2);
            HRow("F1", "이 도움말 (열려 있는 동안 게임 일시정지)");
            HRow("F3", "디버그 오버레이");
            GUILayout.Space(pad * 0.35f);

            GUILayout.Label("<color=#888>아직 없는 조작 — AUTO ANCHOR(조준 없이 자동) P3 · 양손 동시 P4 · " +
                            "벽 짚기/벽 타기 P5 · 적에게 웹 P9</color>", _help3);
            GUILayout.Label("<color=#888>지금은 P2 — 단일 웹입니다. 아직 조준한 곳에만 걸립니다.</color>", _help3);

            GUILayout.EndArea();
        }

        void HRow(string key, string desc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, _help3, GUILayout.Width(_vw * 0.22f));
            GUILayout.Label(desc, _help3);
            GUILayout.EndHorizontal();
        }

        void Row(string key, string desc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, _label, GUILayout.Width(130));
            GUILayout.Label(desc, _label);
            GUILayout.EndHorizontal();
        }
    }
}
