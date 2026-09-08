// P0 부트스트랩. URP 활성화 · 폴더 구조 · 프로젝트 설정.
// 한 번 돌면 끝이지만 멱등하게 짰다. 다시 돌려도 안전하다.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SetupProject
{
    const string SettingsDir = "Assets/Settings";
    const string RendererPath = SettingsDir + "/SpiderUniversalRenderer.asset";
    const string PipelinePath = SettingsDir + "/SpiderURP.asset";

    static readonly string[] Folders =
    {
        // 기준서 §8 구조. 지금은 빈 폴더고, 기능이 생길 때 책임별로 채운다.
        "Assets/Scripts/Core",
        "Assets/Scripts/Player",
        "Assets/Scripts/Web",
        "Assets/Scripts/Traversal",
        "Assets/Scripts/Camera",
        "Assets/Scripts/Animation",
        "Assets/Scripts/Combat",
        "Assets/Scripts/Debug",
        "Assets/Scripts/Config",
        "Assets/Editor",
        "Assets/Scenes",
        "Assets/Settings",
        "Assets/Tests/EditMode",
    };

    public static void Run()
    {
        CreateFolders();
        var urp = CreateOrLoadPipeline();
        AssignPipeline(urp);
        ConfigurePlayerSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var active = GraphicsSettings.defaultRenderPipeline;
        EditorRun.Finish(active == null ? 1 : 0,
            "SETUP_OK rp=" + (active == null ? "NULL" : active.GetType().Name) +
            " colorSpace=" + PlayerSettings.colorSpace);
    }

    static void CreateFolders()
    {
        foreach (var f in Folders)
        {
            Directory.CreateDirectory(f);
            // 유니티는 '.'로 시작하는 파일을 무시한다. git에는 빈 폴더가 남는다.
            var keep = Path.Combine(f, ".gitkeep");
            if (!File.Exists(keep)) File.WriteAllText(keep, "");
        }
        AssetDatabase.Refresh();
    }

    static UniversalRenderPipelineAsset CreateOrLoadPipeline()
    {
        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (existing != null) return existing;

        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
        }

        var urp = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(urp, PipelinePath);
        AssetDatabase.SaveAssets();
        return urp;
    }

    static void AssignPipeline(UniversalRenderPipelineAsset urp)
    {
        GraphicsSettings.defaultRenderPipeline = urp;

        // 품질 레벨마다 따로 물려 있다. 하나만 바꾸면 다른 레벨에서 Built-in으로 돌아간다.
        int original = QualitySettings.GetQualityLevel();
        var names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        QualitySettings.SetQualityLevel(original, false);
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Spider Ver2";
        PlayerSettings.productName = "Spiderman_Ver2";
        PlayerSettings.colorSpace = ColorSpace.Linear;   // URP 기준
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.runInBackground = true;

        SetActiveInputHandler(1); // 0=Old, 1=Input System(New), 2=Both
    }

    // activeInputHandler에는 공개 C# API가 없다. ProjectSettings.asset을
    // SerializedObject로 직접 만진다. (YAML 텍스트 편집이 아니라 에디터 API다)
    static void SetActiveInputHandler(int value)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning("SETUP_WARN ProjectSettings.asset not loadable; input handler unchanged");
            return;
        }

        var so = new SerializedObject(assets[0]);
        var prop = so.FindProperty("activeInputHandler");
        if (prop == null)
        {
            Debug.LogWarning("SETUP_WARN activeInputHandler property not found");
            return;
        }
        if (prop.intValue == value) return;

        prop.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log("SETUP_INPUT activeInputHandler=" + value);
    }
}
