using System;
using UnityEngine;

public sealed class TradeService : MonoBehaviour
{
    [Tooltip("ITimeProvider를 구현한 시간 접근점")]
    [SerializeField] private MonoBehaviour timeProviderBehaviour;

    private ITimeProvider TimeProvider => (ITimeProvider)timeProviderBehaviour;

    public ProviderTradeState State { get; private set; }
    public DateTime? StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }

    private void Update() => Evaluate(TimeProvider.UtcNow);

    public void StartTrade()
    {
        if (State == ProviderTradeState.Traveling)
        {
            return;
        }
        DateTime utc = TimeProvider.UtcNow;
        DateTime end = utc.AddSeconds(ProviderConstants.TradeDurationSeconds);
        StartUtc = utc;
        EndUtc = end;
        State = ProviderTradeState.Traveling;
    }

    // 완료는 되돌리지 않습니다. 공급자가 과거 UTC로 전환되어도 저장된 무역은 유지합니다.
    public void Evaluate(DateTime utc)
    {
        if (State == ProviderTradeState.Traveling && utc >= EndUtc.Value)
        {
            State = ProviderTradeState.Completed;
        }
    }

    public double? GetRemainingSeconds() => GetRemainingSeconds(TimeProvider.UtcNow);

    public double? GetRemainingSeconds(DateTime utc)
    {
        Evaluate(utc);
        if (State == ProviderTradeState.Completed)
        {
            return 0d;
        }
        return EndUtc.HasValue ? Math.Max(0d, (EndUtc.Value - utc).TotalSeconds) : (double?)null;
    }

    public void ResetTrade()
    {
        State = ProviderTradeState.Idle;
        StartUtc = null;
        EndUtc = null;
    }
}
