// 켜져 있는 에디터 안에서 EditMode 테스트를 돌리고 결과를 파일로 남긴다.
//
// 왜 필요한가:
//   유니티가 켜져 있으면 Claude는 -runTests 배치모드로 프로젝트를 열 수 없다.
//   그렇다고 매번 "유니티를 닫아 주십시오"라고 할 수는 없다.
//   ClaudeBridge로 이걸 부르면 형님 화면에서 테스트가 돌고, 결과가 tools/claude_tests.txt에 떨어진다.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class TestRunnerBridge
{
    const string OutPath = "tools/claude_tests.txt";

    static TestRunnerApi _api;

    [MenuItem("Spider/테스트/EditMode 전체 실행")]
    public static void RunEditMode()
    {
        Directory.CreateDirectory("tools");
        File.WriteAllText(OutPath, "status=RUNNING\n");

        _api = ScriptableObject.CreateInstance<TestRunnerApi>();
        _api.RegisterCallbacks(new Callbacks());
        _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));

        Debug.Log("TESTS_STARTED EditMode");
    }

    class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            var failures = new List<string>();
            Collect(result, failures);

            var sb = new StringBuilder();
            sb.AppendLine("status=" + (result.FailCount == 0 ? "PASS" : "FAIL"));
            sb.AppendLine("passed=" + result.PassCount);
            sb.AppendLine("failed=" + result.FailCount);
            sb.AppendLine("skipped=" + result.SkipCount);
            sb.AppendLine("inconclusive=" + result.InconclusiveCount);
            sb.AppendLine("duration=" + result.Duration.ToString("F2") + "s");

            foreach (var f in failures)
                sb.AppendLine(f);

            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }

            if (result.FailCount == 0)
                Debug.Log("TESTS_PASS " + result.PassCount + "/" +
                          (result.PassCount + result.FailCount));
            else
                Debug.LogError("TESTS_FAIL " + result.FailCount + " failed");
        }

        static void Collect(ITestResultAdaptor node, List<string> outList)
        {
            if (!node.HasChildren)
            {
                if (node.TestStatus == TestStatus.Failed)
                {
                    outList.Add("FAILED " + node.FullName + "\n  " +
                                (node.Message ?? "").Replace("\n", "\n  "));
                }
                return;
            }

            foreach (var c in node.Children)
                Collect(c, outList);
        }
    }
}
