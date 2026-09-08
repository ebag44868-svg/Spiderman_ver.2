// 배치모드에서만 에디터를 종료한다.
// 형님이 유니티를 켜놓고 보는 중에 EditorApplication.Exit를 부르면 에디터가 그냥 꺼진다.
using UnityEditor;
using UnityEngine;

public static class EditorRun
{
    public static void Finish(int code, string message)
    {
        if (code == 0) Debug.Log(message);
        else Debug.LogError(message);

        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
