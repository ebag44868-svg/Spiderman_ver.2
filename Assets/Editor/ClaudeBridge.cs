// CLAUDE BRIDGE
// 형님이 유니티 에디터를 켜놓은 채로, Claude가 시킨 작업이 그 에디터 안에서 실행되게 한다.
//
// 왜 필요한가:
//   유니티는 한 프로젝트를 두 인스턴스가 동시에 열지 못한다. 에디터가 켜져 있으면
//   Claude의 -batchmode 실행이 "project is already open"으로 실패한다.
//   그래서 파일 한 장을 우편함으로 쓴다. Claude가 명령을 적으면, 켜져 있는 에디터가 읽고 실행한다.
//   형님은 씬이 만들어지는 것을 화면에서 그대로 보게 된다.
//
// 우편함:  tools/claude_cmd.txt     1줄 = ID, 2줄 = 정적 메서드 이름 (예: SceneBuilder.BuildPlayground)
// 답장:    tools/claude_result.txt
// 상태:    tools/claude_state.txt   (도메인 리로드를 건너 살아남아야 해서 파일에 둔다)
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class ClaudeBridge
{
    const string CmdPath = "tools/claude_cmd.txt";
    const string ResultPath = "tools/claude_result.txt";
    const string StatePath = "tools/claude_state.txt";
    const string CompilePath = "tools/claude_compile.txt";
    const double PollSeconds = 0.5;

    static double _nextPoll;

    static ClaudeBridge()
    {
        EditorApplication.update += Tick;

        // 컴파일 에러를 파일로 흘려보낸다.
        // 에디터가 켜져 있으면 Claude는 배치모드로 컴파일을 못 돌린다.
        // 이게 없으면 "결과가 안 온다"와 "코드가 안 컴파일된다"를 구분할 수 없다.
        CompilationPipeline.compilationStarted -= OnCompileStart;
        CompilationPipeline.compilationStarted += OnCompileStart;
        CompilationPipeline.assemblyCompilationFinished -= OnAssemblyDone;
        CompilationPipeline.assemblyCompilationFinished += OnAssemblyDone;
    }

    static void OnCompileStart(object _)
    {
        try
        {
            Directory.CreateDirectory("tools");
            File.WriteAllText(CompilePath, "status=COMPILING\ntime=" +
                DateTime.Now.ToString("HH:mm:ss") + "\n");
        }
        catch { }
    }

    static void OnAssemblyDone(string assemblyPath, CompilerMessage[] messages)
    {
        try
        {
            var errors = new List<string>();
            foreach (var m in messages)
                if (m.type == CompilerMessageType.Error)
                    errors.Add(m.file + "(" + m.line + "): " + m.message);

            if (errors.Count == 0) return;

            Directory.CreateDirectory("tools");
            using (var w = new StreamWriter(CompilePath, true))
            {
                w.WriteLine("status=ERROR assembly=" + Path.GetFileName(assemblyPath));
                foreach (var e in errors) w.WriteLine(e);
            }
        }
        catch { }
    }

    static void Tick()
    {
        // 컴파일/임포트 중에 끼어들면 도메인 리로드에 씹힌다.
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.timeSinceStartup < _nextPoll) return;
        _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

        if (!File.Exists(CmdPath)) return;

        string id, method;
        if (!ReadCommand(out id, out method)) return;

        var state = ReadState();

        // 1단계 — 새 명령. 먼저 에셋을 새로고침한다. 새 .cs 파일이 있으면 여기서 컴파일된다.
        if (state.Id != id)
        {
            WriteState(id, "refresh");
            WriteResult(id, "RUNNING", "refreshing assets for " + method);
            AssetDatabase.Refresh();
            return; // 도메인 리로드가 일어나면 다음 틱은 리로드 뒤에 온다
        }

        // 2단계 — 새로고침이 끝났다. 이제 실행한다.
        if (state.Phase == "refresh")
        {
            WriteState(id, "running");
            Execute(id, method);
        }
    }

    static void Execute(string id, string method)
    {
        try
        {
            var mi = FindMethod(method);
            if (mi == null)
            {
                WriteState(id, "done");
                WriteResult(id, "FAIL", "method not found: " + method);
                Debug.LogError("BRIDGE_FAIL method not found: " + method);
                return;
            }

            Debug.Log("BRIDGE_RUN " + method);
            mi.Invoke(null, null);

            WriteState(id, "done");
            WriteResult(id, "OK", method + " finished");
            Debug.Log("BRIDGE_OK " + method);
        }
        catch (Exception e)
        {
            var inner = e is TargetInvocationException && e.InnerException != null
                ? e.InnerException
                : e;
            WriteState(id, "done");
            WriteResult(id, "FAIL", inner.GetType().Name + ": " + inner.Message + "\n" + inner.StackTrace);
            Debug.LogError("BRIDGE_FAIL " + method + " :: " + inner.Message);
        }
    }

    static MethodInfo FindMethod(string qualified)
    {
        int dot = qualified.LastIndexOf('.');
        if (dot <= 0) return null;
        var typeName = qualified.Substring(0, dot);
        var methodName = qualified.Substring(dot + 1);

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = null;
            try { t = asm.GetType(typeName, false); } catch { }
            if (t == null) continue;
            var mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (mi != null && mi.GetParameters().Length == 0) return mi;
        }
        return null;
    }

    static bool ReadCommand(out string id, out string method)
    {
        id = null; method = null;
        try
        {
            var lines = File.ReadAllLines(CmdPath);
            if (lines.Length < 2) return false;
            id = lines[0].Trim();
            method = lines[1].Trim();
            return id.Length > 0 && method.Length > 0;
        }
        catch { return false; } // Claude가 쓰는 중일 수 있다. 다음 틱에 다시 본다.
    }

    struct State { public string Id; public string Phase; }

    static State ReadState()
    {
        var s = new State { Id = "", Phase = "" };
        try
        {
            if (!File.Exists(StatePath)) return s;
            var parts = File.ReadAllText(StatePath).Trim().Split('|');
            if (parts.Length >= 2) { s.Id = parts[0]; s.Phase = parts[1]; }
        }
        catch { }
        return s;
    }

    static void WriteState(string id, string phase)
    {
        try
        {
            Directory.CreateDirectory("tools");
            File.WriteAllText(StatePath, id + "|" + phase);
        }
        catch { }
    }

    static void WriteResult(string id, string status, string message)
    {
        try
        {
            Directory.CreateDirectory("tools");
            File.WriteAllText(ResultPath,
                "id=" + id + "\nstatus=" + status + "\ntime=" +
                DateTime.Now.ToString("HH:mm:ss") + "\n" + message + "\n");
        }
        catch { }
    }

    [MenuItem("Spider/Claude Bridge/상태 보기")]
    static void ShowStatus()
    {
        var st = ReadState();
        var res = File.Exists(ResultPath) ? File.ReadAllText(ResultPath) : "(없음)";
        Debug.Log("BRIDGE state id=" + st.Id + " phase=" + st.Phase +
                  "\n--- last result ---\n" + res);
    }
}
