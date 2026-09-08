using UnityEngine;
using UnityEngine.InputSystem;
using SpiderVer2.Player;
using SpiderVer2.CameraRig;

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
        public TuningConfig tuning;

        [Tooltip("시작할 때 오버레이를 켜둘지. 릴리즈에서는 끈다")]
        public bool showOnStart = true;

        bool _showStats;
        bool _showHelp;
        GUIStyle _box, _label, _title;

        void Awake()
        {
            _showStats = showOnStart;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) _showHelp = !_showHelp;
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
            _label.fontSize = 13;
            _label.normal.textColor = Color.white;
            _label.richText = true;

            _title = new GUIStyle(_label);
            _title.fontSize = 15;
            _title.fontStyle = FontStyle.Bold;
        }

        void OnGUI()
        {
            EnsureStyles();
            if (_showStats) DrawStats();
            if (_showHelp) DrawHelp();
            DrawHint();
        }

        void DrawHint()
        {
            var r = new Rect(Screen.width - 210, Screen.height - 30, 200, 22);
            GUI.Label(r, "<color=#aaaaaa>F1 도움말 · F3 디버그</color>", _label);
        }

        void DrawStats()
        {
            var r = new Rect(12, 12, 330, 330);
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
            GUILayout.Label("<color=#888>── 아래는 다음 단계에서 채운다 ──</color>", _label);
            GUILayout.Label("<color=#888>Web L/R      P2</color>", _label);
            GUILayout.Label("<color=#888>Anchor       P3</color>", _label);
            GUILayout.Label("<color=#888>Candidates   P3</color>", _label);
            GUILayout.Label("<color=#888>Swing Phase  P6</color>", _label);

            GUILayout.Space(4);
            GUILayout.Label(string.Format("<color=#666>fixedDeltaTime {0:F5} ({1:F0} Hz)</color>",
                Time.fixedDeltaTime, 1f / Mathf.Max(Time.fixedDeltaTime, 0.0001f)), _label);

            GUILayout.EndArea();
        }

        void DrawHelp()
        {
            float w = 460f, h = 400f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));

            GUILayout.Label("조작법", _title);
            GUILayout.Space(6);

            Row("W A S D", "이동");
            Row("마우스", "시점");
            Row("Space", "점프");
            Row("Esc", "마우스 커서 풀기 (다시 클릭하면 잠김)");
            GUILayout.Space(8);

            GUILayout.Label("<color=#888>아직 없는 조작 (예정)</color>", _label);
            Row("<color=#888>마우스 왼쪽</color>", "<color=#888>오른손 웹 — P2</color>");
            Row("<color=#888>마우스 오른쪽</color>", "<color=#888>왼손 웹 — P2</color>");
            Row("<color=#888>왼쪽 Shift</color>", "<color=#888>벽 타기 — P5</color>");
            GUILayout.Space(8);

            GUILayout.Label("디버그", _label);
            Row("F1", "이 도움말");
            Row("F3", "디버그 오버레이");

            GUILayout.Space(10);
            GUILayout.Label("<color=#888>지금은 P1 — 빈 놀이터입니다. 캡슐이 움직이고 점프하고\n" +
                            "벽에 부딪히는지만 보시면 됩니다. 웹은 P2에서 들어갑니다.</color>", _label);

            GUILayout.EndArea();
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
