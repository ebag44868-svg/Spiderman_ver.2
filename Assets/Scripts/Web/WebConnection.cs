using UnityEngine;

namespace SpiderVer2.Web
{
    public enum Hand { Left, Right }

    /// <summary>
    /// 한 손의 웹 연결 상태. P2에서는 한 번에 하나만 팽팽하지만,
    /// 손 정체성(Hand)과 역할(Role)은 지금부터 들고 다닌다. P4에서 좌/우가 동시에 살아난다.
    /// </summary>
    public enum WebRole { None, Primary, Secondary }

    [System.Serializable]
    public struct WebConnection
    {
        public bool connected;
        public Hand hand;
        public WebRole role;
        public Vector3 anchor;
        public Vector3 anchorNormal;
        public float length;
        public float age;          // 붙어 있은 시간(초)

        public static WebConnection None
        {
            get { return new WebConnection { connected = false, role = WebRole.None }; }
        }

        public static WebConnection Attach(Hand hand, Vector3 anchor, Vector3 normal, float length)
        {
            return new WebConnection
            {
                connected = true,
                hand = hand,
                role = WebRole.Primary,
                anchor = anchor,
                anchorNormal = normal,
                length = length,
                age = 0f,
            };
        }
    }
}
