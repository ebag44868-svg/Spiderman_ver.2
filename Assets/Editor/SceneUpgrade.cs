// 이미 만들어진 Playground 씬에 P2 웹 부품을 붙인다.
// 씬을 다시 만들지 않는다. 형님이 씬에서 뭔가 만져놨을 수 있고, 그걸 날리면 안 된다.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpiderVer2.Player;
using SpiderVer2.CameraRig;
using SpiderVer2.DebugTools;
using SpiderVer2.Web;

public static class SceneUpgrade
{
    const string ScenePath = "Assets/Scenes/Playground.unity";
    const string WebMatPath = "Assets/Settings/Materials/M_Web.mat";

    /// <summary>P2 한 번에 적용 — 씬 배선 + 튜닝값 갱신 + 테스트 실행.</summary>
    [MenuItem("Spider/씬/P2 전체 적용 + 테스트")]
    public static void ApplyP2()
    {
        AttachWebP2();
        ApplyP2Tuning();
        TestRunnerBridge.RunEditMode();
    }

    /// <summary>
    /// 이미 저장된 TuningConfig.asset은 C# 기본값이 바뀌어도 따라오지 않는다.
    /// P2에서 바뀐 값만 골라서 덮어쓴다. 형님이 Inspector에서 만진 다른 값은 건드리지 않는다.
    /// </summary>
    [MenuItem("Spider/설정/P2 튜닝값 적용")]
    public static void ApplyP2Tuning()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TuningConfig>("Assets/Settings/TuningConfig.asset");
        if (cfg == null) { Debug.LogError("TUNING_FAIL TuningConfig.asset 없음"); return; }

        float before = cfg.GRAVITY;
        cfg.GRAVITY = 28f;      // 22는 진자가 느렸다. v ∝ sqrt(g) 라 속도에 가장 크게 듣는 값이다.

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        Debug.Log("TUNING_OK GRAVITY " + before + " -> " + cfg.GRAVITY +
                  " · REEL_RATE=" + cfg.REEL_RATE + " ZIP_PULL=" + cfg.ZIP_PULL);
    }

    [MenuItem("Spider/씬/P2 웹 붙이기")]
    public static void AttachWebP2()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var player = GameObject.Find("Player");
        if (player == null) { EditorRun.Finish(1, "UPGRADE_FAIL Player 없음"); return; }

        var motor = player.GetComponent<PlayerMotor>();
        var input = player.GetComponent<PlayerInputRouter>();
        var overlay = player.GetComponent<DebugOverlay>();
        var fp = player.GetComponentInChildren<FirstPersonCamera>();
        if (motor == null || input == null || fp == null)
        {
            EditorRun.Finish(1, "UPGRADE_FAIL 부품 없음 motor=" + (motor != null) +
                                " input=" + (input != null) + " fpCam=" + (fp != null));
            return;
        }

        // ── WebController
        var web = player.GetComponent<WebController>();
        if (web == null) web = player.AddComponent<WebController>();
        web.tuning = motor.tuning;
        web.input = input;
        web.eye = fp.transform;
        web.worldMask = motor.worldMask;

        motor.web = web;
        if (overlay != null) overlay.web = web;

        // ── 줄 그리기. 별도 오브젝트로 둔다. 플레이어가 회전해도 줄은 월드 좌표라 상관없다.
        var visualGo = GameObject.Find("WebLine");
        if (visualGo == null)
        {
            visualGo = new GameObject("WebLine");
            visualGo.transform.SetParent(player.transform, false);
        }

        var lr = visualGo.GetComponent<LineRenderer>();
        if (lr == null) lr = visualGo.AddComponent<LineRenderer>();
        lr.sharedMaterial = EnsureWebMaterial();

        var visual = visualGo.GetComponent<WebVisual>();
        if (visual == null) visual = visualGo.AddComponent<WebVisual>();
        visual.web = web;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorRun.Finish(0, "UPGRADE_OK web=" + (motor.web != null) +
                            " line=" + (visual.web != null) +
                            " overlay=" + (overlay != null && overlay.web != null));
    }

    static Material EnsureWebMaterial()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(WebMatPath);
        if (m != null) return m;

        Directory.CreateDirectory("Assets/Settings/Materials");

        // 줄은 조명을 받지 않는 편이 낫다. 가늘어서 Lit로는 거의 안 보인다.
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        m = new Material(shader);
        m.SetColor("_BaseColor", new Color(0.95f, 0.96f, 1f, 1f));
        AssetDatabase.CreateAsset(m, WebMatPath);
        AssetDatabase.SaveAssets();
        return m;
    }
}
