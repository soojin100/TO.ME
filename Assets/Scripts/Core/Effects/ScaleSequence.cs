using System.Collections.Generic;
using UnityEngine;

namespace TOME.Core
{
    /// <summary>키프레임 스케일 보간. 기획서의 "0 → 1.3 → 0.9 → 1.08 → 1.0" 같은
    /// 펀치 등장 연출을 배열 데이터로만 표현하기 위한 순수 함수 모음.
    /// keys가 n개면 구간은 n-1개이며 durations[i]가 i번째 구간의 소요 시간이다.
    /// durations가 짧으면 마지막 값을 반복 사용한다.</summary>
    public static class ScaleSequence
    {
        /// <summary>구간 i의 소요 시간. durations가 비었으면 0.</summary>
        public static float SegmentDuration(IReadOnlyList<float> durations, int i)
        {
            if (durations == null || durations.Count == 0) return 0f;
            return durations[Mathf.Min(i, durations.Count - 1)];
        }

        /// <summary>전체 재생 시간. 키 사이 구간(n-1개)만 합산한다.</summary>
        public static float TotalDuration(IReadOnlyList<float> keys, IReadOnlyList<float> durations)
        {
            if (keys == null || keys.Count < 2) return 0f;
            float total = 0f;
            for (int i = 0; i < keys.Count - 1; i++) total += SegmentDuration(durations, i);
            return total;
        }

        /// <summary>시각 t에서의 스케일. keys가 비었으면 1(원본 크기)을 돌려준다.</summary>
        public static float Evaluate(IReadOnlyList<float> keys, IReadOnlyList<float> durations, float t)
        {
            if (keys == null || keys.Count == 0) return 1f;
            if (keys.Count == 1) return keys[0];
            if (t <= 0f) return keys[0];

            float elapsed = 0f;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                float dur = SegmentDuration(durations, i);
                if (dur <= 0f)
                {
                    // 시간 0 구간은 즉시 통과 — 다음 키로 점프.
                    elapsed += dur;
                    continue;
                }
                if (t < elapsed + dur)
                    return Mathf.Lerp(keys[i], keys[i + 1], (t - elapsed) / dur);
                elapsed += dur;
            }
            return keys[keys.Count - 1];
        }
    }
}
