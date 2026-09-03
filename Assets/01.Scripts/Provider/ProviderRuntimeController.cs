using System;
using UnityEngine;

// Service는 이 접근점을 ITimeProvider로 사용하여 공급자 선택 정책을 알 필요가 없습니다.
public sealed class ProviderRuntimeController : MonoBehaviour, ITimeProvider
{
    [Tooltip("실제 UTC 공급자")]
    [SerializeField] private SystemUtcTimeProvider systemProvider;
    [Tooltip("명령으로만 이동하는 UTC 공급자")]
    [SerializeField] private ManualTimeProvider manualProvider;

    public bool IsManualTime { get; private set; }
    public ITimeProvider CurrentProvider => IsManualTime ? (ITimeProvider)manualProvider : systemProvider;
    public DateTime UtcNow => CurrentProvider.UtcNow;

    public void UseSystemTime() => IsManualTime = false;

    public void UseManualTime()
    {
        if (IsManualTime)
        {
            return;
        }
        manualProvider.SetUtc(systemProvider.UtcNow);
        IsManualTime = true;
    }

    public void AddGameDays(int days)
    {
        if (IsManualTime)
        {
            manualProvider.AddGameDays(days);
        }
    }

    public void ResetManualTime() => manualProvider.ResetToSystemTime();
}
