# SPIDER GAME VER.2 — UNITY 개발 기준서

> 이 문서는 **새 세션에서 이것만 읽고 바로 작업을 시작할 수 있도록** 쓰였다.
> 이전 대화 내용, 레퍼런스 분석 결과, 앞으로의 전체 계획이 전부 여기 있다.
>
> 작성 2026-09-08 · Ver.1(Three.js) 동결 직후
> 이 문서와 실제 코드가 어긋나면 **코드가 아니라 이 문서를 먼저 고친다.**

---

## 0. 지금 당장 알아야 할 것 (새 세션용 30초 요약)

- 스파이더맨류 3D 액션 게임을 만든다. **AAA 복제가 아니다.**
- Ver.1을 Three.js로 이미 만들었고 **동결**했다. 연구 프로토타입이었다.
- Ver.2는 **Unity**로 새로 만든다. 이유는 남은 작업의 3분의 2가 애니메이션·리그·카메라인데 그게 Three.js에서 가장 비싼 구간이기 때문이다.
- 최우선 목표는 **자유로운 웹 사용(FREE WEB)**. 정확히 조준하지 않아도 가고 싶은 방향에 거미줄이 걸리는 것.
- **1인칭을 먼저 완성한다.** 레퍼런스 영상이 1인칭이고, 캐릭터 모델 없이 캡슐로 끝까지 갈 수 있다.
- 사용자는 유니티가 처음이다. **씬·프리팹·컴포넌트 연결까지 전부 Claude가 코드로 만든다.** (실제로 검증했다 — §3)

---

## 1. 확정된 환경

```
Unity            6.6 (6000.6.0f1)
경로             C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe
모듈             WebGLSupport · windowsstandalonesupport
Hub              C:\Program Files\Unity Hub\Unity Hub.exe

Ver.2 폴더       C:\Users\SAMSUNG\OneDrive\Desktop\Spiderman_Ver2
Ver.2 저장소     https://github.com/ebag44868-svg/Spiderman_ver.2   (빈 상태)

Ver.1 폴더       C:\Users\SAMSUNG\OneDrive\Desktop\spider man       (동결 · 읽기 참조용)
Ver.1 저장소     https://github.com/ebag44868-svg/spider-man-game
Ver.1 동결 지점  태그 v1.0-final = 커밋 fe4a5cb (main)

레퍼런스 영상    C:\Users\SAMSUNG\OneDrive\Desktop\문서\카카오톡 받은 파일\스파이더맨 래퍼런스.mp4
                 46.8초 · 960x720 · 24fps
ffmpeg           설치되어 있고 PATH에 있다
플랫폼           Windows 11 · PowerShell 주 · Bash 병용 · iOS 빌드 불가
```

**주 타깃은 Windows 데스크톱 빌드.** WebGL은 Vertical Slice 완성 후 검토한다 (빌드가 수십 MB라 Ver.1의 "링크만 주면 친구가 바로 함"은 재현되지 않는다).

---

## 2. 작업 규칙 (사용자 선호 — 반드시 지킬 것)

1. **토큰을 아낀다.** 사용자는 만성적인 주간 한도 부족에 시달린다. 중요한 코드·설계 작업이 아니면 최대한 아낀다. 불필요한 재확인, 장황한 설명, 반복 읽기 금지.
2. **한 번에 하나씩.** 큰 변경 전 백업(브랜치/커밋). 단계마다 검증하고 넘어간다.
3. **보고는 하나의 연속된 복붙 가능한 코드블록으로.** 여러 블록으로 쪼개지 않는다.
4. **백업 브랜치는 절대 삭제하지 않는다.**
5. 사용자가 할 일을 요청할 때는 짧고 명확하게:
   ```
   USER ACTION
   1. ...
   2. ...
   DONE CRITERIA
   ...
   ```
6. 사용자 조작이 필요 없으면 굳이 에디터 작업을 요청하지 않는다.
7. 조작법이나 기능을 추가하면 **인게임 도움말에도 반드시 같이 넣는다.**
8. 모르는 것을 아는 척하지 않는다. 확인할 수 있으면 확인하고 말한다.

### 단계 보고 형식
```
[PHASE REPORT]
1. 목표          2. 작업 전 상태     3. Backup
4. 변경 파일     5. 실제 변경        6. 기존 기능 영향
7. 자동 테스트   8. Unity Play 확인  9. 레퍼런스 비교
10. 문제         11. 남은 위험       12. 사용자가 할 것
13. 완료 기준    14. Commit          15. 다음 단계
```

---

## 3. Claude가 유니티를 어디까지 직접 할 수 있는가 (실측 검증됨)

2026-09-08에 실제로 테스트해서 **둘 다 성공**했다. 추측이 아니다.

### 검증 1 — 헤드리스 프로젝트 생성
```bash
Unity.exe -batchmode -quit -createProject <경로> -logFile <로그>
# 종료코드 0. Assets/ Library/ Packages/ ProjectSettings/ 전부 생성됨
```

### 검증 2 — 코드로 씬 생성 (`-executeMethod`)
```bash
Unity.exe -batchmode -quit -projectPath <경로> -executeMethod Probe.Build -logFile <로그>
# 종료코드 0. 로그에 "PROBE_OK objects=4"
# Assets/Scenes/Probe.unity 파일 실제 생성 확인
# 씬 안에 큐브 건물 + Rigidbody 붙은 캡슐이 코드로 배치됨
```

검증에 쓴 스크립트 (이 패턴을 그대로 확장한다):
```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class Probe {
    public static void Build() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "TestBuilding";
        g.transform.position = new Vector3(0, 5, 20);
        g.transform.localScale = new Vector3(10, 40, 10);
        var p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        p.name = "Player";
        p.AddComponent<Rigidbody>();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Probe.unity");
        Debug.Log("PROBE_OK objects=" + scene.rootCount);
    }
}
```

### 따라서 Claude가 직접 하는 것
- C# 파일 작성/수정 (런타임 + 에디터 툴)
- **씬 생성, 오브젝트 배치, 컴포넌트 추가, 참조 연결** — 전부 C# 에디터 스크립트로
- 프리팹 생성 (`PrefabUtility.SaveAsPrefabAsset`)
- 패키지 설치 (`Packages/manifest.json`은 그냥 JSON 텍스트다)
- 프로젝트 설정 변경 (에디터 스크립트의 `PlayerSettings` API)
- 배치모드로 컴파일 확인 · 로그 읽기 · 테스트 실행
- Git 전부

### 사용자가 하는 것 (이것만)
- **Play 버튼 누르고 직접 조작해보기**
- 조작감·움직임·카메라가 좋은지 나쁜지 **판단**
- 유니티가 패키지 설치 승인을 물으면 승인
- 에셋(모델/사운드) 고르기
- 커밋/푸시 승인

### 절대 하지 않는 것
- `.unity` / `.prefab` / `.asset` YAML을 **텍스트로 직접 편집**하지 않는다. 파일이 깨진다.
  → 반드시 에디터 API를 통한다. 위 검증 2가 그 방법이다.

---

## 4. 레퍼런스 영상 — 141프레임 분석 결과 (이것이 시각적 기준)

3fps로 141프레임을 전부 확인한 결과다. **추측이 아니라 실제로 본 것이다.**

### 4.1 타임라인

| 시간 | 내용 |
|---|---|
| 0.0–2.5초 | 옥상(자갈 바닥, 물탱크). 스카이라인을 본다. **양손이 동시에 올라가고 웹 두 가닥이 좌우로 동시 발사된다.** 시작이 한 손이 아니다. |
| 2.5–5.0초 | 발사 직후 곧바로 뒤집힌다. 지면이 화면 위로 온다. 다리·몸통이 보인다. |
| 5.0–16초 | 고속 스윙. 화면이 계속 20~90도 기울고 완전 역전 구간도 있다. |
| 16–24초 | **저고도 골목.** 도로 차선이 또렷할 만큼 낮다. 건물 **벽면**에 걸고 왼팔↔오른팔이 번갈아 화면에 들어온다. 시선은 거의 항상 위. |
| 24–28초 | **3인칭 구간** (§4.3) |
| 28–37초 | 마천루 구역. 유리 타워를 올려다보며 통과. |
| 37–44초 | **건물 타기** (§4.4) |
| 44–47초 | 처음 그 옥상으로 복귀. 착지. |

### 4.2 1인칭에서 관찰된 것

- **거의 전부 1인칭이다.** 3인칭은 4초 남짓.
- **카메라 롤이 극단적이다.** 거의 모든 프레임에서 수평선이 20~60도 기울어 있고 완전히 뒤집힌 컷도 있다. 방향을 틀면 화면 자체가 눕는다.
- **양손이 동시에 보이는 게 기본.** 팔뚝이 화면 모서리를 크게 차지하고 앵커 쪽으로 뻗는다.
- **고개를 숙이면 몸통과 다리가 보인다.** 날아다니는 카메라가 아니라 몸을 가진 사람이다.
- **줄은 아주 가늘다.** 대부분 화면 밖으로 빠진다. 방향은 줄이 아니라 "팔이 저기를 향하고 있다"로 읽힌다.
- **저고도 골목 스윙이 지배적.** 멀리 옥상이 아니라 바로 옆 건물 벽면에 건다.
- 광각이라 화면 가장자리가 눈에 띄게 휘어 있다.

### 4.3 ★ 3인칭 구간 — 기준 문서를 뒤집는 발견

캐릭터가 **화면 높이의 3~5%**다. 점이다. 카메라는 따라붙지 않고, 멀리 떨어져 거의 고정된 채로 캐릭터가 그 앞을 지나간다. 리플레이/관전 카메라에 가깝다.

지피티가 쓴 기준 문서(03번 §32)는 이렇게 썼다:

> "캐릭터가 화면에서 자세가 읽히는 최소 크기를 유지한다. 속도가 올라갈수록 카메라를 뒤로 보내 작은 점처럼 만들지 말 것."

**영상은 정확히 반대이고, 그게 실수가 아니라 의도다.** 그 컷들이 존재하는 이유는 도시 대비 내가 얼마나 작은지를 보여주는 것이다. 유리 타워를 배경으로 실루엣 하나가 줄에 매달려 지나가는 그림.

**→ 결론: 3인칭 카메라는 하나가 아니라 둘이다.**

1. **플레이용 추적 카메라** — 자세가 읽혀야 한다 (기준 문서 §32가 맞는 경우)
2. **연출용 원경 카메라** — 캐릭터가 작아야 의미가 있다 (레퍼런스의 그 컷)

둘을 하나로 합치려 하면 어느 쪽도 안 된다. Ver.2에서 분리한다.

### 4.4 ★ 건물 타기의 실제 순서

```
위를 본다 (60~85도)
→ 타워 '벽면' 높은 곳에 가는 줄을 쏜다        ← 옥상이 아니라 벽면이다
→ 유리면이 화면을 가득 채울 만큼 끌려 붙는다   ← 거의 밀착
→ 손바닥이 유리에 닿는다 (짚기)
→ 반대 손이 더 높이 쏜다
→ 반복
→ 옥상 진입
```

### 4.5 속도감의 정체

사용자 요구: "저기서 속도감 좀 높이고." 영상의 속도감은 실제 m/s보다 **카메라**에서 나온다. 넷이다.

1. **근접** — 건물이 화면 가장자리를 스치듯 지나간다. 저고도라 참조물이 많다.
2. **롤** — 방향을 틀 때 화면 자체가 눕는다.
3. **광각** — 가장자리 왜곡이 눈에 보일 정도.
4. **시선** — 위를 보고 달린다. 건물이 위로 빨려 들어가며 흐른다.

Ver.1은 이미 85 m/s가 나온다. **숫자를 더 올릴 게 아니라 위 넷을 살려야 한다.**

⚠️ 단, **2번 롤은 어지럼증을 유발한다.** Ver.1에서 사용자가 직접 겪었다. Ver.2에서는 **1인칭 롤 세기를 설정으로 뺀다** (0 / 약 / 강). 기본값은 "약". 화면을 기울이는 대신 **몸을 기울이는** 방식도 옵션으로 둔다 (Ver.1에서 이 방식으로 멀미를 없앤 실적이 있다 — 화면 기울기 0도, 몸 기울기 32도).

---

## 5. Ver.2의 헌법 (세 가지)

### 5.1 FREE WEB — 이 게임의 정체성

플레이어가 건물을 하나하나 정확히 조준하지 않아도 된다.

플레이어가 **카메라 + 방향 입력 + 웹 입력**으로 의도를 주면, 게임이 **실제 world surface · 적절한 anchor · 사용할 손 · primary/secondary 역할**을 고른다.

> "내가 건물을 찍는 게임"이 아니라
> **"내가 어디로 가고 싶은지 정하면 캐릭터가 웹을 알아서 잘 쓰는 게임."**

허공에 가짜 앵커를 만들지 않는다. **실제 월드 표면에만 건다.**

### 5.2 MOTION FIRST — 물리가 이동을 결정한다

```
Gameplay Physics → Traversal State → Base Animation
→ Procedural Pose → IK/Contact → Camera → Audio/VFX
```

애니메이션이 궤적을 지배하지 않는다. **연출 때문에 게임플레이 물리를 바꾸지 않는다.**

### 5.3 ONE WEB SYSTEM — 웹은 게임의 중심 인터페이스

웹은 Environment / Ground / Enemy / Enemy Body Part 에 연결 가능하다. 같은 시스템이 장기적으로 Swing · Turn · Pull · Zip · Climb · Wall movement · Enemy Pull · Leg Trip · Ground Web Kick 으로 이어진다. **모드 전환 없이.**

---

## 6. Ver.1에서 무엇을 가져오는가

Ver.1은 폐기물이 아니라 **연구 프로토타입**이었다. 배운 것:

1. 좋은 모델을 넣는다고 게임이 좋아지지 않는다.
2. 3인칭 액션에서 Animation / Pose / IK / Camera가 매우 중요하다.
3. 웹 물리 자체보다 **Auto Anchor와 캐릭터 표현의 완성도**가 체감을 좌우한다.
4. 이동용 웹과 공격용 웹이 분리되면 "자유로운 웹 사용"이 약해진다.
5. **God File**로 계속 추가하면 유지보수가 급격히 어려워진다. (game3d.js 7,949줄 · 최상위 `let` 192개 · 함수 209개가 그걸 전부 공유. 실제로 1인칭 손 포즈 블록을 모듈로 빼지 못했다.)
6. 전체를 동시에 키우는 것보다 **핵심 Vertical Slice를 먼저** 완성해야 한다.

### [A] PORT — 알고리즘을 C#으로 옮긴다 ★

전부 순수 함수다. THREE를 쓰지 않는다. 그래서 이식이 기계적이다.

| Ver.1 파일 | 줄수 | 내용 |
|---|---|---|
| `src/anchor.js` | ~180 | **자동 앵커 V2** — 의도 벡터 · 점수식 7요소 · 손 선택 |
| `src/wallplant.js` | 96 | 벽 짚기 — 접촉 예측 · 속도 보존 임펄스 |
| `src/vclimb.js` | 64 | 웹 수직 이동 — 앵커 선정 · 발사 조건 |
| `src/reach.js` | 155 | 손이 향할 곳 — 각도 제한 · tanh 부드러움 |
| `src/mathx.js` | 81 | 보조 수학 |

**이 5개가 Ver.1의 진짜 자산이다.** 합쳐 약 580줄.

### [A2] PORT — 알고리즘만 (코드는 다시 쓴다)

- 로프 구속 수학 (반경 속도 제거 + 접선 보존)
- 고정 120Hz 타임스텝 + 누산기 + 렌더 보간
- 소프트 락온 가중치
- 슬링샷 충전/부스트 곡선

### [B] REDESIGN — 아이디어만

미션 / 보스 / 세이브 / 적 AI / 튜토리얼 / HUD. 돌아가지만 God File에 얽혀 있어 옮길 가치가 없다.

### [C] DISCARD

- `game3d.js` 7,949줄 통짜 파일
- **난수 제약** — Ver.1에서는 THREE의 Object3D/Material/Geometry/Texture 생성자가 uuid를 만들며 `Math.random()`을 먹어서, 선언 자리에서 객체를 만들면 도시 생성 난수열이 통째로 밀렸다. 상완·보조 웹·디버그 마커에서 세 번 겪었다. **유니티에는 이 문제가 없다.** Ver.2에서는 처음부터 전용 PRNG를 쓴다.
- Tab 모드 전환 체계
- 1인칭 팔을 원통으로 조립한 것 (유니티는 진짜 모델을 쓴다)

### [D] ASSET

- Mixamo 클립 14종(`.glb`) → 유니티 Humanoid로 바로 들어간다. 위치: `Ver.1/assets/models/player/anims/`
- 도시/텍스처 → 버린다. 새 테스트 씬은 큐브로 시작한다.

---

## 7. 실측 숫자표 ★ 가장 중요한 인수인계물

Ver.1에서 **브라우저 실측으로 맞춘** 값들이다. 유니티에서 처음부터 다시 맞추려면 몇 주가 걸린다.
Ver.2 **P1에서 이걸 ScriptableObject 하나(`TuningConfig.asset`)로 만들어 넣는다.** 그래야 Inspector에서 바로 만지며 테스트할 수 있다.

### 7.1 자동 앵커 (src/anchor.js)

```
SP_REF       55      '빠름'의 기준 속도 (m/s)
W_CAM_SLOW   1.60    느릴 때 카메라 비중
W_CAM_FAST   0.70    빠를 때 카메라 비중 (낮춰야 90도 급snap이 안 난다)
W_MOM_SLOW   0.25    느릴 때 관성 비중
W_MOM_FAST   1.55    빠를 때 관성 비중
W_MOVE       0.55    WASD 방향
W_TURN       0.90    A/D만의 옆 편향

W_FWD        1.60    의도 방향 일치도
W_LEN        1.20    줄 길이의 질
W_STEEP      0.80    머리 바로 위 회피
W_HIGH       1.10    높이
W_HAND       0.55    손 쪽 편향 (기계적 교대가 안 될 만큼만)
W_ALT        0.30    직전 손의 반대쪽 보너스
W_SHARP      1.30    급선회 벌점 (속도 비례)

LEN_BEST     0.62    최대 사거리의 62%가 최적 호
LEN_SPAN     0.45
DROP_MAX     14      이보다 아래는 그네가 아니라 추락
BACK_MIN    -0.15    의도 기준 이보다 뒤면 기각

부채꼴 yaw   0, ±15, ±32, ±52, ±74, ±96  (손 쪽으로 최대 10도 편향)
부채꼴 pitch 34, 44, 24, 56, 14, 68, 4, -6
총 88발 · 0.156ms/회 (Ver.1 기준. legacy 56발 0.093ms)
```

### 7.2 로프 · 속도

```
ROPE_MIN        12      최소 로프 길이
SWING_MIN_LEN   18      이보다 짧으면 그네가 아니라 벽에 박는다
ROPE_MAX        150     사거리 = 최대 로프 길이
SWING_CONVERT   0.995   낙하(반경) 속도를 접선으로 되돌리는 비율
MAX_SPEED       112
SOFT_SPEED      82      이 위로는 하드 클램프 대신 드래그
ACCEL           118     지상 가속 (90은 출발이 무거웠다)
AIR_ACCEL       42      공중 조작 (30은 스윙 중 전환이 굼떴다)
PUMP_ACCEL      58      홀드 속도 증강
DT              1/120   고정 타임스텝
```

### 7.3 벽 짚기 (src/wallplant.js)

```
PLANT_LOOK      0.20   이 시간 안에 벽면을 넘어설 것 같으면 짚는다
PLANT_MIN_V     15     이보다 느리면 짚지 않는다 (저속은 '벽 붙기'의 몫)
PLANT_TIME      0.30   손이 벽에 닿아 있는 시간
PLANT_KEEP      0.88   벽면을 따라 흐르는 속도 유지율
PLANT_BOUNCE    0.45   벽을 향하던 속도의 이만큼으로 되밀린다
PLANT_PUSH      11     최소 밀기
PLANT_PUSH_MAX  28     상한 (없으면 고속에서 튕겨 날아간다)
PLANT_UP        7      살짝 위로 (없으면 짚을 때마다 고도가 깎인다)
PLANT_CD        0.45   되짚기 방지
PLANT_HAND_Y    1.15   접촉점을 어깨 높이로
```

**절대 조건: 벽 짚기에서 속도를 0으로 만들지 않는다.** 고정값 15로 밀었더니 46 m/s로 정면 접근한 것이 15.9 m/s로 나갔다. 그래서 속도 비례 반발로 바꿨다.

### 7.4 웹 수직 이동 (src/vclimb.js)

```
VC_NEAR    9      벽에서 이 안쪽이어야 '건물을 타는 중'
VC_STEP    45     한 번에 타고 오르는 높이
VC_OUT     3.6    벽에서 앵커까지 이격  ← 레퍼런스 기준 1.5로 낮출 것 (§4.4)
VC_ARRIVE  7      앵커에 이만큼 가까워지면 다음 자리
VC_TOP     2.5    지붕을 이만큼 넘겨 잡아야 옥상으로 올라선다
VC_CD      0.30   연속 발사 간격
```

### 7.5 보조 웹 · 슬링샷 · 카메라

```
WEB2_PULL    62     보조 웹이 당기는 가속 (m/s²)
WEB2_FADE    45     이 거리부터 약해진다
SLING_MAX    0.50   최대 충전 시간
SLING_MIN    0.10
SLING_BOOST  64     최대 충전에서 더해지는 속도
SLING_REEL   0.22   밀고 나가면서 줄을 감는 비율
SLING_DRAG   5.2    물고 있는 동안 속도 감쇠
CAM_ROLL     0.5    뱅킹 롤 강도 (1인칭은 설정으로 0 / 0.35 / 0.5)
SOFT_CONE    0.10   소프트 락온 원뿔 (대략 ±84도)
M_BUF_T      0.42   선입력 유지 시간
```

### 7.6 실측 성능 기준 (Ver.1)

```
자동 앵커 V2 vs legacy — 조준 없이 연속 스윙, 올라가는 구간에서 놓기
  V2      6스윙   376m / 6.3초   평균 85 m/s   손 L R L L L L
  legacy 14스윙   157m / 13.6초  평균 33 m/s   손 R R R R R R ...
→ 2.4배 멀리, 절반 시간에, 2.6배 속도. Ver.2는 이 이상을 목표로 한다.
```

---

## 8. 개발 단계 P0 → P10

각 단계는 **완료 기준을 만족해야** 다음으로 넘어간다. 한 커밋에 리팩터 + 새 기능 + 튜닝 + 에셋 교체를 섞지 않는다.

### P0 — 프로젝트 부트스트랩

- [ ] Unity 6.6 URP 3D 프로젝트를 `Spiderman_Ver2/`에 CLI로 생성
- [ ] Git 초기화 · 유니티용 `.gitignore` · 원격 연결 · 첫 커밋
- [ ] 폴더 구조 생성
- [ ] 패키지: Input System · Cinemachine · Test Framework (`manifest.json` 직접 편집)
- [ ] Claude가 쓸 에디터 툴 뼈대 (`Assets/Editor/BuildTools.cs`)
- [ ] 배치모드 컴파일 확인 스크립트

**완료 기준:** `Unity.exe -batchmode -quit -projectPath . -executeMethod BuildTools.Verify` 가 종료코드 0을 낸다.
**사용자:** 없음.

```
Assets/
  Scripts/
    Core/        고정 타임스텝 · 난수 · 수학
    Player/      PlayerMotor · PlayerInput
    Web/         WebController · WebConnection · WebAnchorResolver · WebPhysics · WebVisual
    Traversal/   SwingController · WallPlantController · VerticalClimb · AirMovement
    Camera/      PlayerCameraController · CinematicCamera
    Animation/   (P6부터)
    Combat/      (P9부터)
    Debug/       DebugOverlay · AnchorVisualizer
    Config/      TuningConfig (ScriptableObject)
  Editor/        BuildTools · SceneBuilder
  Scenes/
  Settings/
  Tests/EditMode/
```

> 초기부터 클래스 40개를 만들지 않는다. **기능이 생길 때 책임별로 분리한다.**

---

### P1 — 빈 놀이터 + 튜닝 설정

- [ ] `SceneBuilder`로 테스트 씬 코드 생성: 큐브 건물 8~10개, 높이 다른 플랫폼, 좁은 골목, 넓은 거리, 높은 벽, 옥상
- [ ] 캡슐 플레이어 + Rigidbody + Collider + CameraTarget
- [ ] Input System 액션맵 (이동 / 시점 / 웹 좌우 / 점프 / 벽타기)
- [ ] **1인칭 카메라** (레퍼런스 기준. 3인칭은 P7)
- [ ] `TuningConfig` ScriptableObject에 §7 숫자 전부 입력
- [ ] 화면 디버그 오버레이 (속도 · 위치 · 상태)

**완료 기준:** 캡슐이 움직이고 점프하고 벽에 부딪힌다. Inspector에서 숫자를 바꾸면 즉시 반영된다.
**사용자:** Play 해보고 이동이 이상하지 않은지 확인.

---

### P2 — 단일 웹 물리 ★ 첫 번째 진짜 관문

- [ ] 실제 월드 표면에 레이캐스트로 앵커
- [ ] 로프 구속 (반경 속도 제거 + 접선 보존, §7.2)
- [ ] 줄 그리기 (LineRenderer, **가늘게** — 레퍼런스 기준)
- [ ] 붙이기 / 놓기 / 감기(reel) / 펌프
- [ ] 고정 타임스텝 + 렌더 보간
- [ ] EditMode 테스트: NaN 없음 · 속도 폭발 없음 · 5초 안정

**완료 기준: 캡슐 상태로도 웹스윙이 재미있다.**

> 이게 아니면 그 뒤가 전부 의미 없다. 여기서 반드시 한 번 멈추고 사용자 판정을 받는다.
> Unity Joint 하나에 스윙 수학을 전부 맡기지 않는다. SpringJoint 붙이고 "완료"로 처리하지 않는다.

**사용자:** 실제로 스윙해보고 재미있는지 판정. 재미없으면 여기서 숫자를 만진다.

---

### P3 — AUTO ANCHOR V2 ★ 이 게임의 정체성

- [ ] **먼저 디버그 시각화부터 만든다.** 후보 앵커 구슬(초록 통과 / 빨강 기각 / 노랑 채택) · 점수 · 기각 사유 · 의도 벡터 · 속도 벡터
- [ ] `anchor.js` → `WebAnchorResolver.cs` 이식 (§7.1 숫자 그대로)
- [ ] 의도 벡터 = 카메라 + 관성 + WASD + 선회
- [ ] 부채꼴 ±96도 · 낮은 피치 포함 · 화면 밖 앵커 허용
- [ ] 손 선택 (레이는 한 번, 후보마다 좌/우 두 점수)
- [ ] 성능: broad phase → cheap score → 최종 몇 개만 정밀 검증

> **왜 시각화가 먼저인가:** Ver.1에서 이걸 만들자마자 "좌우 점수가 소수점 셋째 자리까지 같다(4.114 / 4.114)"는 걸 첫 화면에서 발견했다. 없었으면 추측으로 고쳤을 것이다.

**완료 기준:** 정확히 조준하지 않고 A/D만으로 좌회전·우회전·직진이 의도대로 나온다.
**사용자:** 골목에서 좌우로 꺾어보며 "내가 원한 쪽에 걸리는지" 판정.

---

### P4 — 좌/우 웹 · 보조 웹

- [ ] 손별 독립 논리 상태 (`Left` / `Right`: connected · anchor · role · length · age)
- [ ] 손 선택 규칙 (한 손만 비면 그 손 / 둘 다 비면 target side / 교대 고려)
- [ ] Primary = 실제 로프 구속, **Secondary = 조향력** (true dual constraint 아님)
- [ ] 전환(transfer) · 릴리즈
- [ ] 양손 동시 발사 (레퍼런스 시작 장면 §4.1)
- [ ] 테스트: 속도 폭발 없음 · 두 앵커 사이 jitter 없음 · single-web fallback

> **true dual hard constraint는 하지 않는다.** Ver.1에서 Primary + Secondary 조향만으로 급선회 느낌이 나왔다. 기준 문서도 "Phase B를 최종 방식으로 써도 된다"고 허용한다.

**완료 기준:** 고속에서 좌우 방향 전환이 자유롭고, 손이 실제로 교대한다.

---

### P5 — 벽 짚기 · 웹 수직 이동

- [ ] `wallplant.js` → `WallPlantController.cs` (§7.3)
- [ ] 벽 접근 예측 · 접촉점 · 손 선택 · 접선 모멘텀 보존 · 밀어내기
- [ ] **Wall Plant와 Wall Cling은 완전히 다른 상태다.** 고속 = Plant, 저속/의도적 = Cling
- [ ] `vclimb.js` → `VerticalClimb.cs` (§7.4, `VC_OUT`을 1.5로)
- [ ] 옥상 진입 전환
- [ ] 테스트: 고속 접촉에서 속도 0 금지 · 벽 관통 금지

**완료 기준:** 고속으로 벽에 닿을 때 멈추지 않고 손을 탁 짚고 계속 간다. 웹으로 건물을 올라가고, **화면에서 웹이 상승의 원인처럼 보인다.**

---

### P6 — 휴머노이드 · 애니메이션 ★ 주말 모델링이 여기 들어간다

- [ ] Fab/Blender 모델 임포트 · Humanoid Avatar 설정
- [ ] Animator + Mixamo 기본 클립 (Ver.1 `assets/models/player/anims/`)
- [ ] Animation Rigging 패키지
- [ ] **실제 손 본에서 웹이 나간다** (몸통 근사 금지)
- [ ] `reach.js` → 손/팔이 앵커 방향을 향한다 (Two Bone IK 검토)
- [ ] 손별 상태기: `IDLE → SHOOT → CATCH → HOLD → RELEASE`
- [ ] 스윙 페이즈: `ATTACH / DESCEND / BOTTOM / ASCEND / RELEASE / FREE_AIR`
- [ ] 1인칭 몸 — 고개를 숙이면 가슴·다리가 보인다 (§4.2)

> **P1~P5는 캡슐로 한다.** 모델 교체와 이동 리팩터를 동시에 하면 버그 원인을 구분할 수 없다.
> Ver.1 교훈: 좋은 모델일수록 손/웹 불일치와 단일 스윙 포즈가 **더 잘 보인다.**
> 절차적 포즈는 Animator가 클립을 적용한 **뒤**, 렌더 **전**에 얹는다. 순서를 틀리면 다음 프레임에 사라진다.

**완료 기준:** 3인칭에서 캐릭터가 실제로 웹을 잡는 것처럼 보인다.

---

### P7 — 카메라 ★ 두 개로 나눈다

- [ ] **추적 카메라** (Cinemachine): 거리 곡선 · 속도 look-ahead · FOV · 충돌 · 스윙 구도
- [ ] **연출용 원경 카메라**: 캐릭터가 작게 보이는 관전 컷 (§4.3)
- [ ] 1인칭 롤 설정 (0 / 약 / 강, 기본 "약") + "화면 대신 몸을 기울이기" 옵션
- [ ] 광각 + 근접감 (§4.5의 속도감 4요소)
- [ ] 3인칭 중앙 조준(center reticle) 기본
- [ ] 카메라가 게임플레이를 납치하지 않는다

**완료 기준:** 레퍼런스 영상과 나란히 놓고 비교 가능하다.
**사용자:** 어지럽지 않은지 + 빨라 보이는지 판정.

---

### P8 — 레퍼런스 Vertical Slice ★ 최종 검증

```
옥상 점프 → 자동 오른손 웹 → 캐치 → 스윙
→ 왼손 보조 웹 → 급선회 → 전환 → 릴리즈
→ 자동 곡예 → 벽 짚기 → 높은 웹 → 릴
→ 벽 밀기 → 옥상 진입
```

- [ ] 자동 곡예 (**물리 궤적을 바꾸지 않는다** — Ver.1에서 이걸 어겨서 속도가 몰래 붙었다)
- [ ] 이 시퀀스를 녹화해서 레퍼런스와 항목별 비교
  (캐릭터 화면 크기 · 웹 원점 · 어느 손인지 · 캐치가 보이는지 · 몸 기울기 · 스윙 중 포즈 변화 · 화면 밖 웹 · 선회 예리함 · 모멘텀 유지 · 벽 접촉 · 수직 이동 · 릴리즈/트릭 · 카메라 구도)

**⛔ 이 단계에 도달하기 전에 게임을 확장하지 않는다.**
> "느낌이 다르다"로 끝내지 않는다. 어느 항목이 레퍼런스보다 부족한지 분해해서 수정한다.

---

### P9 — 컨텍스트 웹 전투

- [ ] 더미 적 1~3명
- [ ] 적 부위 타겟: Chest · LeftArm · RightArm · LeftLeg · RightLeg (본이 있으면 본, 없으면 virtual offset + 캡슐 판정)
- [ ] 네 가지만: **Web Pull · Web Zip · Leg Trip · Ground Web Kick**
- [ ] 모드 전환 없이 같은 웹 입력이 대상에 따라 다르게 동작

**완료 기준:** 스윙하다가 모드 전환 없이 적에게 웹을 걸고 붙어서 때린다.
> 이 넷이 자연스럽게 연결되기 전에는 적↔적 연계, 고급 Bind, 환경물체 연계, 공중 콤보를 늘리지 않는다.

---

### P10 — 게임 확장

적 AI · 전투 깊이 · 튜토리얼 · 미션 · 보스 · UI · 스토리 · 추가 캐릭터 · WebGL 빌드 검토.

> 첫 캐릭터가 완성되기 전 여러 캐릭터 금지. 캐릭터 하나 추가는 모델만 추가하는 게 아니라 animation / combat / traversal / VFX / tutorial / balance까지 늘어난다.

---

## 9. 처음부터 만들지 않을 것

AAA 도시 · HDRP · 수십 캐릭터 · 멀티플레이 · MMO · 대규모 NPC 시뮬레이션 · 거대 스킬트리 · 20시간 스토리 · 배틀패스 · 상점 · 인벤토리/제작 · DOTS/ECS · 복잡한 Addressables · 프로토타입 전 true dual constraint · 완전 절차적 바디 애니메이션 · 거대 커스텀 물리 엔진.

**멀티플레이는 현재 범위가 아니다.** 다만 미래 2~4인 PvE 가능성을 위해: Player를 전역 싱글턴에 과도하게 결합하지 않고, Character State를 분리하고, Mission State를 Player 스크립트에 박지 않고, Combat action을 명확한 event/state로 표현한다. **단, 이 준비 때문에 현재 개발이 느려지면 안 된다.**

---

## 10. 테스트 전략

```
순수 C# 로직 테스트   앵커 점수식 · 의도 벡터 · 로프 수학 · 벽 짚기 임펄스
                      → Claude가 배치모드로 직접 돌린다 (Ver.1의 705개에 해당)
EditMode 테스트       씬 없이 도는 것 전부
PlayMode 테스트       필요할 때만
런타임 디버그 오버레이 Free Web은 이게 없으면 튜닝이 불가능하다
```

**디버그 오버레이 표시 항목:**
Aim Mode · Web Mode · Left/Right Web State · Primary/Secondary Hand · Current Anchor · Candidate Count · Selected Score · Velocity · Speed · Intent Direction · Swing Phase · Wall State · Camera Distance

**월드 디버그:** 후보 앵커 · 채택 앵커 · 속도 벡터 · 의도 방향 · 벽 접촉점/법선 · 로프 두 가닥

**릴리즈 기본 OFF.**

### 수동 플레이 테스트 시나리오 (각 단계마다)

| | 상황 | 확인 |
|---|---|---|
| A | 넓은 거리 고속 직진 | 속도 유지 |
| B | 고속 → A → 웹 | 왼쪽 실제 앵커 · 과도한 snap 없음 |
| C | 고속 → D → 웹 | 오른쪽 |
| D | 교대 웹 R → L → R | 손이 실제로 갈리는가 |
| E | 지면 근처 저고도 | 엉뚱한 ground anchor · 충돌 |
| F | 고층 스카이라인 | |
| G | 고속 벽 접근 | 멈추지 않고 짚는가 |
| H | 벽 → 높은 앵커 → 옥상 | 웹이 상승의 원인처럼 보이는가 |
| I | 1인칭 ↔ 3인칭 전환 | 논리 상태 유지 |
| J | 스윙 → 적 → 웹 집 → 타격 | (P9) |

---

## 11. 성능 원칙

- 매 프레임 도시 전체에 full raycast 하지 않는다. **Broad Phase → Cheap Score → Expensive Validation**
- 스크래치 Vector3/Quaternion 재사용 · 본 참조 캐시 · 이펙트 풀링
- 프레임당 불필요한 할당 감소
- **Profiler 근거 없이 최적화하지 않는다**
- 초기부터 WebGL 최적화에 매몰되지 않는다

---

## 12. 위험 요소

| 위험 | 대응 |
|---|---|
| **검증 루프가 바뀐다.** Ver.1에서는 Claude가 705개 테스트를 돌리고 브라우저를 직접 몰아 스크린샷까지 찍었다. 유니티에서는 Play Mode 확인이 사용자 몫이 된다. | 순수 C# 로직은 EditMode 테스트로 계속 Claude가 검증한다. P2·P3를 로직 위주로 짜면 손실이 작다. |
| Auto Anchor 튜닝이 코드보다 어렵다 | P3에서 **시각화를 먼저** 만든다. 감이 아니라 화면으로 판단한다. |
| Secondary Web 과구속 | true dual constraint를 하지 않는다. Primary + 조향력. |
| 절차적 포즈가 Animator에 덮인다 | mixer 적용 **뒤**, 렌더 **전**에 얹는다. 매 프레임 누적시키지 않는다. |
| 모델 교체와 이동 리팩터가 겹침 | P1~P5는 캡슐. 모델은 P6. |
| WebGL 빌드가 무겁다 | Ver.1을 웹 프로토타입으로 남긴다. Ver.2 주 타깃은 Windows. |
| YAML 에셋 손상 | `.unity`/`.prefab`을 텍스트로 직접 편집하지 않는다. 반드시 에디터 API. |
| 1인칭 롤 멀미 | 설정으로 뺀다 (0/약/강). 화면 대신 몸을 기울이는 옵션도 둔다. |

---

## 13. 평가 순서 (그래픽부터 보지 않는다)

1. 캡슐로 스윙이 재미있는가?
2. 정확한 조준 없이 웹이 의도대로 걸리는가?
3. 좌/우 웹으로 방향 전환이 자유로운가?
4. 벽 짚기가 자연스러운가?
5. 웹으로 건물을 올라가는가?
6. 휴머노이드가 실제 웹을 잡는 것처럼 보이는가?
7. 스윙 중 몸이 살아 움직이는가?
8. 카메라가 이를 잘 보여주는가?
9. 같은 웹 시스템이 전투로 연결되는가?
10. **그 뒤에야** City / Model / UI를 확장한다.

---

## 14. IP · 에셋 원칙

개발 구조를 특정 상업 IP에 하드코딩하지 않는다. 내부적으로 `Spider` / `Agile` / `Brute` / `Tech` 같은 archetype을 쓴다. 외부 에셋은 라이선스를 확인한다. 공개 배포 가능성이 생기면 저작권 있는 캐릭터/모델 사용 여부를 다시 검토한다.

---

## FINAL SOURCE OF TRUTH

> Ver.2의 목표는 **레퍼런스보다 자유롭게 웹을 사용하고, 플레이어의 의도를 읽어 실제 환경에 웹을 연결하며, 좌/우 웹과 모멘텀으로 빠르게 방향을 바꾸고, 벽을 손으로 짚고 이동을 이어가며, 웹으로 건물을 올라가고, 그 동일한 웹 시스템으로 전투까지 이어지는 Motion-First 액션 게임**을 만드는 것이다.

유니티를 선택한 이유는 그래픽을 자동으로 좋게 만들기 위해서가 아니다.
**Animation · Rigging · Camera · Physics · Asset Pipeline 같은 일반적인 기반은 유니티에 맡기고, 우리 개발 시간을 Free Web과 Motion에 집중하기 위해서다.**

새 기능을 만들 때마다 묻는다:

> "이 기능은 플레이어가 웹을 더 자유롭고 자연스럽게 쓰게 만드는가?"

구현할 때마다 묻는다:

> "지금 정상 동작하는 것을 깨지 않고 이걸 추가할 수 있는 가장 작은 단계는 무엇인가?"

---

## 부록 A — 새 세션 시작 시 첫 명령

```
C:\Users\SAMSUNG\OneDrive\Desktop\Spiderman_Ver2\00_MASTER_PLAN.md 를 읽고
P0부터 시작해라. 구현 전에 현재 폴더 상태를 먼저 확인하고,
P0 계획을 보고한 뒤 승인을 기다려라.
```

## 부록 B — Ver.1 참조 방법

```bash
# 알고리즘 원본 (읽기 전용, 수정하지 말 것)
C:\Users\SAMSUNG\OneDrive\Desktop\spider man\src\anchor.js
C:\Users\SAMSUNG\OneDrive\Desktop\spider man\src\wallplant.js
C:\Users\SAMSUNG\OneDrive\Desktop\spider man\src\vclimb.js
C:\Users\SAMSUNG\OneDrive\Desktop\spider man\src\reach.js
C:\Users\SAMSUNG\OneDrive\Desktop\spider man\src\mathx.js

# 실행해서 감각을 비교하고 싶을 때
cd "C:\Users\SAMSUNG\OneDrive\Desktop\spider man" && node serve.js
# http://localhost:8173/game3d.html
# F1 도움말 · F3 앵커 디버그 · F4 앵커 V2/legacy 전환
```

## 부록 C — 레퍼런스 프레임 다시 뽑기

```bash
ffmpeg -i "스파이더맨 래퍼런스.mp4" \
  -vf "fps=3,crop=in_h*0.62:in_h*0.80:in_w*0.19:0,scale=200:-1" -q:v 5 h%04d.jpg
# 세로 영상이라 crop으로 가운데만 잘라야 한다 (좌우는 SNS UI다)
# 3인칭 구간 = 23.5~28초 · 건물 타기 = 37.5~44초
# 대조표로 볼 때: ffmpeg -i h%04d.jpg -vf tile=8x6 -q:v 4 sheet.jpg
```

## 부록 D — Ver.1에서 실제로 겪은 함정들 (같은 실수를 반복하지 않기 위해)

| 함정 | 증상 | 원인 / 교훈 |
|---|---|---|
| 음수 스케일로 좌우 반전 | 왼팔이 기괴하게 꺾임 | `scale.x = -1`은 좌표계 손잡이를 뒤집는다. 회전을 얹을 때마다 반대로 돈다. 기하로 미러링해야 한다. |
| 1인칭 몸이 카메라 뒤 | 아래를 봐도 몸이 안 보임 | z 부호를 반대로 넣었다. 앞은 −Z. |
| 시점이 몸을 따라 회전 | 멀미 | 1인칭에서 화면을 기울이면 "세상이 도는" 걸로 읽힌다. 몸만 돌려야 한다. |
| 포즈 적용 순서 | 회전이 사라짐 | 포즈 함수가 뒤에서 rotation을 통째로 다시 쓴다. 적용은 그 **뒤**에 해야 한다. |
| 벽 짚기 고정 임펄스 | 46 m/s가 15.9 m/s로 죽음 | 속도 비례 반발로 바꿔야 한다. |
| 앵커 좌우 동점 | 어느 손이 잡을지가 우연 | 점수식에 손 편향이 없었다. 디버그 시각화가 이걸 첫 화면에서 잡아냈다. |
| 테스트가 실패했는데 코드가 맞음 | | 한쪽에 건물이 없는 지점이었다. 테스트 조건을 먼저 의심할 것. |
| 자동 곡예가 속도를 더함 | 물리 궤적 오염 | 연출은 궤적을 건드리면 안 된다. |
