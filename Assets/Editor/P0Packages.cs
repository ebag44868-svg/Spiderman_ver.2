// P0 부트스트랩 전용. 패키지 설치만 담당한다.
// 버전을 추측해서 박지 않고 Package Manager가 이 에디터에 맞는 버전을 고르게 한다.
using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class P0Packages
{
    static readonly string[] Wanted =
    {
        "com.unity.render-pipelines.universal",
        "com.unity.inputsystem",
        "com.unity.cinemachine",
        "com.unity.test-framework",
        "com.unity.ugui",
    };

    public static void Add()
    {
        var listed = ListInstalled();
        var missing = Wanted.Where(id => !listed.Contains(id)).ToArray();
        if (missing.Length == 0)
        {
            Debug.Log("P0_PKG_OK already=" + string.Join(",", Wanted));
            EditorApplication.Exit(0);
            return;
        }

        Debug.Log("P0_PKG_ADD " + string.Join(",", missing));
        var req = Client.AddAndRemove(missing, null);
        if (!Wait(req, 900))
        {
            Debug.LogError("P0_PKG_FAIL timeout");
            EditorApplication.Exit(2);
            return;
        }
        if (req.Status != StatusCode.Success)
        {
            Debug.LogError("P0_PKG_FAIL " + (req.Error != null ? req.Error.message : "unknown"));
            EditorApplication.Exit(3);
            return;
        }

        foreach (var p in req.Result)
            Debug.Log("P0_PKG " + p.name + "@" + p.version);
        Debug.Log("P0_PKG_OK count=" + req.Result.Count());
        EditorApplication.Exit(0);
    }

    static string[] ListInstalled()
    {
        var req = Client.List(true, false);
        if (!Wait(req, 300) || req.Status != StatusCode.Success)
            return Array.Empty<string>();
        return req.Result.Select(p => p.name).ToArray();
    }

    // 배치모드에는 에디터 업데이트 루프가 돌지 않는다. 직접 펌프한다.
    static bool Wait(Request req, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!req.IsCompleted)
        {
            if (DateTime.UtcNow > deadline) return false;
            Thread.Sleep(100);
        }
        return true;
    }
}
