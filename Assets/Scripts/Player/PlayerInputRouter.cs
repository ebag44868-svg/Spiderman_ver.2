using UnityEngine;
using UnityEngine.InputSystem;

namespace SpiderVer2.Player
{
    /// <summary>
    /// 입력 액션맵. .inputactions 에셋 대신 코드로 만든다.
    ///
    /// 왜 코드인가:
    ///   .inputactions는 GUID가 잔뜩 박힌 JSON이라 Claude가 손으로 쓰면 깨지기 쉽다.
    ///   코드로 만들면 diff가 읽히고, 테스트가 되고, 바인딩 하나 바꾸는 데 에디터 조작이 필요 없다.
    ///   나중에 리바인딩 UI가 필요해지면 그때 에셋으로 뽑는다.
    ///
    /// 다른 스크립트는 이 컴포넌트의 프로퍼티만 읽는다. Input System을 직접 만지지 않는다.
    /// </summary>
    public class PlayerInputRouter : MonoBehaviour
    {
        public Vector2 Move { get; private set; }      // WASD, 정규화 안 함
        public Vector2 Look { get; private set; }      // 마우스 델타 (픽셀)
        public bool JumpPressed { get; private set; }  // 이번 프레임에 눌림
        public bool JumpHeld { get; private set; }
        public bool WebLeftHeld { get; private set; }  // 마우스 오른쪽 = 왼손
        public bool WebRightHeld { get; private set; } // 마우스 왼쪽  = 오른손
        public bool WebLeftPressed { get; private set; }
        public bool WebRightPressed { get; private set; }
        // Ver.1 배치 그대로. Space는 상황에 따라 두 가지다 —
        // 웹이 없고 접지면 점프, 매달려 있으면 줄 감기 (game3d.js:6257, 6429).
        public bool ReelHeld { get { return JumpHeld; } }
        public bool PumpHeld { get; private set; }      // E
        public bool SprintHeld { get; private set; }    // 지상 Shift 달리기
        public bool CursorLocked { get; private set; }

        [Tooltip("켜면 모든 입력을 0으로 만든다. 도움말이 열려 있을 때 쓴다")]
        public bool suspended;

        InputActionMap _map;
        InputAction _move, _look, _jump, _webL, _webR, _pump, _sprint, _unlock;

        void Awake()
        {
            _map = new InputActionMap("Player");

            _move = _map.AddAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _look = _map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            _jump = _map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");

            // 레퍼런스 기준: 왼손/오른손이 따로 논다. 처음부터 좌우를 분리해 둔다.
            _webR = _map.AddAction("WebRight", InputActionType.Button, "<Mouse>/leftButton");
            _webL = _map.AddAction("WebLeft", InputActionType.Button, "<Mouse>/rightButton");

            _pump = _map.AddAction("Pump", InputActionType.Button, "<Keyboard>/e");
            _sprint = _map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _unlock = _map.AddAction("Unlock", InputActionType.Button, "<Keyboard>/escape");
        }

        void OnEnable()
        {
            _map.Enable();
            LockCursor(true);
        }

        void OnDisable()
        {
            _map.Disable();
            LockCursor(false);
        }

        void Update()
        {
            if (suspended)
            {
                // 도움말을 보는 동안 웹이 발사되거나 시점이 돌아가면 안 된다.
                Move = Vector2.zero;
                Look = Vector2.zero;
                JumpPressed = JumpHeld = false;
                WebLeftHeld = WebRightHeld = false;
                WebLeftPressed = WebRightPressed = false;
                PumpHeld = false;
                SprintHeld = false;
                return;
            }

            Move = _move.ReadValue<Vector2>();
            Look = CursorLocked ? _look.ReadValue<Vector2>() : Vector2.zero;

            JumpPressed = _jump.WasPressedThisFrame();
            JumpHeld = _jump.IsPressed();

            WebLeftHeld = _webL.IsPressed();
            WebRightHeld = _webR.IsPressed();
            WebLeftPressed = _webL.WasPressedThisFrame();
            WebRightPressed = _webR.WasPressedThisFrame();

            PumpHeld = _pump.IsPressed();
            SprintHeld = _sprint.IsPressed();

            if (_unlock.WasPressedThisFrame()) LockCursor(false);

            // 화면을 클릭하면 다시 잡는다. 잠금 해제 상태의 클릭은 웹 발사로 세지 않는다.
            if (!CursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor(true);
                WebRightHeld = false;
                WebRightPressed = false;
            }
        }

        public void LockCursor(bool locked)
        {
            CursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
