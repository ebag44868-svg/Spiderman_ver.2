namespace SpiderVer2.Core
{
    /// <summary>
    /// 전용 결정론적 난수. xorshift32.
    ///
    /// 왜 UnityEngine.Random을 안 쓰는가:
    ///   Ver.1에서 THREE의 객체 생성자가 전역 난수를 몰래 먹어서, 코드 한 줄 위치를 옮겼을 뿐인데
    ///   도시 배치가 통째로 달라졌다. 세 번 겪었다. 유니티에는 그 문제가 없지만,
    ///   전역 상태를 공유하는 난수는 여전히 같은 종류의 함정이다.
    ///   씬 생성은 시드만 같으면 항상 같은 결과가 나와야 한다.
    /// </summary>
    public struct Rng
    {
        uint _s;

        public Rng(uint seed)
        {
            _s = seed == 0u ? 0x9E3779B9u : seed;
        }

        public uint NextUInt()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s;
        }

        /// <summary>[0, 1)</summary>
        public float Next01()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        /// <summary>[min, max)</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * Next01();
        }

        /// <summary>[min, max) 정수</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min));
        }
    }
}
