using UnityEngine;

namespace SpiderVer2.Web
{
    /// <summary>
    /// 줄 그리기. 레퍼런스 기준으로 아주 가늘다 (§4.2).
    /// "줄은 대부분 화면 밖으로 빠지고, 방향은 줄이 아니라 팔이 저기를 향하는 걸로 읽힌다."
    /// 그래서 굵게 그리면 오히려 레퍼런스에서 멀어진다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class WebVisual : MonoBehaviour
    {
        public WebController web;
        [Tooltip("줄 두께(m). 레퍼런스는 실처럼 가늘다")] public float width = 0.035f;
        [Tooltip("늘어짐 표현. 0이면 직선")] public float sag = 0.6f;
        [Tooltip("줄을 나눌 점 개수")] public int segments = 12;

        LineRenderer _lr;

        void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.numCapVertices = 2;
            _lr.textureMode = LineTextureMode.Stretch;
            _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lr.receiveShadows = false;
            _lr.enabled = false;
        }

        void LateUpdate()
        {
            if (web == null || !web.Connected)
            {
                if (_lr.enabled) _lr.enabled = false;
                return;
            }

            var c = web.Web;
            Vector3 start = web.HandOrigin(c.hand);
            Vector3 end = c.anchor;

            _lr.enabled = true;
            _lr.startWidth = width;
            _lr.endWidth = width * 0.7f;   // 앵커 쪽이 더 가늘게. 거리감이 산다.

            int n = Mathf.Max(2, segments);
            if (_lr.positionCount != n) _lr.positionCount = n;

            // 팽팽할수록 곧게. 늘어져 있으면 아래로 처진다.
            float slack = Mathf.Max(0f, c.length - Vector3.Distance(start, end));
            float droop = Mathf.Min(sag, slack * 0.25f);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(start, end, t);
                p.y -= droop * Mathf.Sin(t * Mathf.PI);
                _lr.SetPosition(i, p);
            }
        }
    }
}
