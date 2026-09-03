using System;
using UnityEngine;

public sealed class ManualTimeProvider : MonoBehaviour, ITimeProvider
{
    [Tooltip("수동 시계 Reset 시 복사할 시스템 UTC 공급자")]
    [SerializeField] private SystemUtcTimeProvider systemProvider;

    private DateTime utc = ProviderConstants.EpochUtc;

    public DateTime UtcNow => utc;

    public void SetUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Manual time requires UTC.", nameof(value));
        }
        utc = value;
    }

    public void AddGameDays(int days)
    {
        utc = utc.AddSeconds(days * ProviderConstants.RealSecondsPerGameDay);
    }

    // Unity의 Reset 메시지와 충돌하지 않도록 런타임 Reset API를 명시합니다.
    public void ResetToSystemTime() => SetUtc(systemProvider.UtcNow);
}
