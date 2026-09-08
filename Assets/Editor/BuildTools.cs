// Claude가 배치모드로 프로젝트 상태를 검증하는 진입점.
//   Unity.exe -batchmode -quit -projectPath . -executeMethod BuildTools.Verify
// 종료코드 0 = P0 완료 기준 충족.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildTools
{
    // 이 스크립트가 도는 시점에 이미 컴파일은 끝나 있다.
    // 그래서 Verify는 "컴파일됐다"가 아니라 "프로젝트가 기준대로 서 있다"를 본다.
    static readonly string[] RequiredFolders =
    {
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

    static readonly string[] RequiredPackages =
    {
        "com.unity.render-pipelines.universal",
        "com.unity.inputsystem",
        "com.unity.cinemachine",
        "com.unity.test-framework",
    };

    public static void Verify()
    {
        var fail = new List<string>();

        foreach (var f in RequiredFolders)
            if (!Directory.Exists(f)) fail.Add("missing folder: " + f);

        var manifest = File.Exists("Packages/manifest.json")
            ? File.ReadAllText("Packages/manifest.json")
            : "";
        foreach (var p in RequiredPackages)
            if (!manifest.Contains(p)) fail.Add("missing package: " + p);

        // URP가 말만 설치된 게 아니라 실제로 활성인지 본다.
        var rp = GraphicsSettings.defaultRenderPipeline;
        if (rp == null) fail.Add("render pipeline not assigned (still Built-in)");
        else if (!rp.GetType().Name.Contains("Universal"))
            fail.Add("render pipeline is not URP: " + rp.GetType().Name);

        // 컴파일 에러가 남아 있으면 에디터가 여기까지 오지 못하지만,
        // 어셈블리 목록을 한 번 훑어 스크립트 어셈블리가 실제로 생겼는지 확인한다.
        var asm = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
        if (asm == null || asm.Length == 0) fail.Add("no editor assemblies");

        if (fail.Count > 0)
        {
            foreach (var f in fail) Debug.LogError("VERIFY_FAIL " + f);
            EditorRun.Finish(1, "VERIFY_FAIL count=" + fail.Count);
            return;
        }

        EditorRun.Finish(0, "VERIFY_OK unity=" + Application.unityVersion +
                            " rp=" + rp.GetType().Name +
                            " folders=" + RequiredFolders.Length);
    }
}
