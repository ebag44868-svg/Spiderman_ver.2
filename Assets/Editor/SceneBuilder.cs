// P1 놀이터 씬을 코드로 만든다.
// .unity YAML을 텍스트로 쓰지 않는다. 전부 에디터 API를 통한다 (기준서 §3).
// 시드가 같으면 결과가 항상 같다 (SpiderVer2.Core.Rng).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpiderVer2.Core;
using SpiderVer2.Player;
using SpiderVer2.CameraRig;
using SpiderVer2.DebugTools;

public static class SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Playground.unity";
    const string TuningPath = "Assets/Settings/TuningConfig.asset";
    const string MatDir = "Assets/Settings/Materials";

    static Material _mGround, _mBuilding, _mAccent, _mRoof;

    [MenuItem("Spider/씬/놀이터 만들기 (Playground)")]
    public static void BuildPlayground()
    {
        var tuning = EnsureTuningConfig();
        EnsureMaterials();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        BuildLighting();
        BuildGround();

        var rng = new Rng(20260909u);
        BuildStartTower();
        BuildWideStreet(ref rng);
        BuildNarrowAlley(ref rng);
        BuildTallWall();
        BuildPlatforms(ref rng);

        var player = BuildPlayer(tuning);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();

        EditorRun.Finish(0, "SCENE_OK path=" + ScenePath +
                            " roots=" + scene.rootCount +
                            " playerY=" + player.transform.position.y.ToString("F1"));
    }

    // ─────────────────────────────────────────── 지형

    static void BuildLighting()
    {
        var lightGo = GameObject.Find("Directional Light");
        if (lightGo != null)
        {
            lightGo.transform.rotation = Quaternion.Euler(48f, 145f, 0f);
            var l = lightGo.GetComponent<Light>();
            if (l != null) { l.intensity = 1.15f; l.shadows = LightShadows.Soft; }
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0016f;   // 거리감. 속도감의 참조물이 된다 (§4.5)
        RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.76f);
    }

    static void BuildGround()
    {
        var root = new GameObject("Ground").transform;
        Box(root, "GroundSlab", new Vector3(0f, -1f, 60f), new Vector3(600f, 2f, 600f), _mGround);

        // 도로 차선. 저고도로 날 때 속도가 읽히려면 바닥에 무늬가 있어야 한다 (§4.5 근접).
        for (int i = -6; i <= 30; i++)
            Box(root, "Lane" + i, new Vector3(0f, 0.02f, i * 12f), new Vector3(1.2f, 0.05f, 6f), _mAccent);
    }

    /// <summary>레퍼런스는 옥상에서 시작한다 (§4.1). 플레이어 스폰 지점.</summary>
    static void BuildStartTower()
    {
        var root = new GameObject("StartTower").transform;
        Box(root, "Tower", new Vector3(0f, 20f, -60f), new Vector3(18f, 40f, 18f), _mBuilding);
        Box(root, "Roof", new Vector3(0f, 40.2f, -60f), new Vector3(18.4f, 0.4f, 18.4f), _mRoof);
        // 물탱크. 레퍼런스 옥상에 있던 것. 눈으로 방향을 잡는 표식이 된다.
        Box(root, "WaterTank", new Vector3(5f, 42.5f, -55f), new Vector3(4f, 4.5f, 4f), _mAccent);
    }

    /// <summary>넓은 거리 — 시나리오 A/B/C (고속 직진, 좌우 웹).</summary>
    static void BuildWideStreet(ref Rng rng)
    {
        var root = new GameObject("WideStreet").transform;
        const float gap = 26f;   // 중심에서 건물 벽까지

        for (int i = 0; i < 6; i++)
        {
            float z = -20f + i * 42f;

            float hL = rng.Range(28f, 78f);
            float wL = rng.Range(16f, 24f);
            Box(root, "L" + i, new Vector3(-gap - wL * 0.5f, hL * 0.5f, z), new Vector3(wL, hL, 26f),
                i % 3 == 0 ? _mAccent : _mBuilding);
            Box(root, "L" + i + "_roof", new Vector3(-gap - wL * 0.5f, hL + 0.2f, z), new Vector3(wL + 0.6f, 0.4f, 26.6f), _mRoof);

            float hR = rng.Range(24f, 90f);
            float wR = rng.Range(16f, 24f);
            Box(root, "R" + i, new Vector3(gap + wR * 0.5f, hR * 0.5f, z), new Vector3(wR, hR, 26f),
                i % 4 == 0 ? _mAccent : _mBuilding);
            Box(root, "R" + i + "_roof", new Vector3(gap + wR * 0.5f, hR + 0.2f, z), new Vector3(wR + 0.6f, 0.4f, 26.6f), _mRoof);
        }
    }

    /// <summary>좁은 골목 — 레퍼런스 16~24초 구간. 벽면에 걸고 좌우로 꺾는 곳 (§4.1).</summary>
    static void BuildNarrowAlley(ref Rng rng)
    {
        var root = new GameObject("NarrowAlley").transform;
        const float half = 5f;   // 골목 폭 10m

        for (int i = 0; i < 7; i++)
        {
            float x = 150f + i * 30f;
            float hA = rng.Range(34f, 60f);
            float hB = rng.Range(34f, 60f);

            Box(root, "A" + i, new Vector3(x, hA * 0.5f, -half - 11f), new Vector3(26f, hA, 22f), _mBuilding);
            Box(root, "B" + i, new Vector3(x, hB * 0.5f, half + 11f), new Vector3(26f, hB, 22f), _mBuilding);
        }
        // 넓은 거리가 Z축을 따라 뻗으므로, 이 골목은 X축을 따라 놓여 이미 직교한다.
    }

    /// <summary>높은 벽 — 시나리오 G/H (고속 벽 짚기, 웹 수직 이동). P5용이지만 지금 세워둔다.</summary>
    static void BuildTallWall()
    {
        var root = new GameObject("TallWall").transform;
        Box(root, "Wall", new Vector3(0f, 45f, 240f), new Vector3(160f, 90f, 6f), _mBuilding);
        Box(root, "WallTop", new Vector3(0f, 90.3f, 240f), new Vector3(160.6f, 0.6f, 8f), _mRoof);
    }

    /// <summary>높이가 다른 플랫폼 — 점프·착지 확인용.</summary>
    static void BuildPlatforms(ref Rng rng)
    {
        var root = new GameObject("Platforms").transform;
        for (int i = 0; i < 9; i++)
        {
            float h = 3f + i * 3.5f;
            float x = rng.Range(-18f, 18f);
            float z = -44f + i * 9f;
            Box(root, "P" + i, new Vector3(x, h * 0.5f, z), new Vector3(9f, h, 9f), _mRoof);
        }
    }

    // ─────────────────────────────────────────── 플레이어

    static GameObject BuildPlayer(TuningConfig tuning)
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 42f, -60f);   // 시작 옥상 위

        var col = player.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.4f;
        col.center = Vector3.zero;

        var rb = player.AddComponent<Rigidbody>();
        rb.mass = 80f;

        var input = player.AddComponent<PlayerInputRouter>();

        var motor = player.AddComponent<PlayerMotor>();
        motor.tuning = tuning;

        // 기존 Main Camera를 플레이어 눈으로 쓴다. 새로 만들지 않는다.
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null)
        {
            camGo = new GameObject("Main Camera");
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
        }
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        camGo.transform.localRotation = Quaternion.identity;

        var cam = camGo.GetComponent<Camera>();
        cam.fieldOfView = tuning.FP_FOV;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 1200f;

        var fp = camGo.AddComponent<FirstPersonCamera>();
        fp.tuning = tuning;
        fp.body = player.transform;
        fp.input = input;
        fp.motor = motor;
        fp.eyeHeight = 0.65f;

        var overlay = player.AddComponent<DebugOverlay>();
        overlay.motor = motor;
        overlay.input = input;
        overlay.fpCam = fp;
        overlay.tuning = tuning;

        return player;
    }

    // ─────────────────────────────────────────── 에셋

    static TuningConfig EnsureTuningConfig()
    {
        Directory.CreateDirectory("Assets/Settings");
        AssetDatabase.Refresh();

        var existing = AssetDatabase.LoadAssetAtPath<TuningConfig>(TuningPath);
        if (existing != null) return existing;

        var cfg = ScriptableObject.CreateInstance<TuningConfig>();  // 기본값이 곧 §7 실측치다
        AssetDatabase.CreateAsset(cfg, TuningPath);
        AssetDatabase.SaveAssets();
        return cfg;
    }

    static void EnsureMaterials()
    {
        Directory.CreateDirectory(MatDir);
        AssetDatabase.Refresh();

        _mGround = Mat("M_Ground", new Color(0.20f, 0.21f, 0.23f), 0.9f);
        _mBuilding = Mat("M_Building", new Color(0.46f, 0.47f, 0.50f), 0.75f);
        _mAccent = Mat("M_Accent", new Color(0.25f, 0.45f, 0.62f), 0.55f);
        _mRoof = Mat("M_Roof", new Color(0.32f, 0.30f, 0.28f), 0.95f);
    }

    static Material Mat(string name, Color color, float smoothnessInv)
    {
        var path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(shader);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 1f - smoothnessInv);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        g.transform.localScale = size;
        g.isStatic = true;
        if (mat != null) g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void AddSceneToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list)
            if (s.path == ScenePath) return;

        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
