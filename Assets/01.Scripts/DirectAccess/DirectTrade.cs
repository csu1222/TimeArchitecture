using System;
using UnityEngine;

public enum DemoTradeState
{
    Idle,
    Traveling,
    Completed
}

public sealed class DirectTrade : MonoBehaviour
{
    private DemoTradeState state = DemoTradeState.Idle;
    private DateTime? startUtc;
    private DateTime? endUtc;

    public DemoTradeState State => state;
    public DateTime? StartUtc => startUtc;
    public DateTime? EndUtc => endUtc;

    private void Update()
    {
        if (state == DemoTradeState.Traveling && DateTime.UtcNow >= endUtc)
        {
            state = DemoTradeState.Completed;
        }
    }

    public void StartTrade()
    {
        if (state == DemoTradeState.Traveling)
        {
            return;
        }

        StartTradeAt(DateTime.UtcNow);
    }

    public void StartTradeAt(DateTime utc)
    {
        startUtc = utc;
        endUtc = utc.AddSeconds(DirectAccessConstants.TradeDurationSeconds);
        state = DemoTradeState.Traveling;
    }

    public void Evaluate(DateTime utc)
    {
        if (state == DemoTradeState.Traveling && utc >= endUtc)
        {
            state = DemoTradeState.Completed;
        }
    }

    public double? GetRemainingSeconds()
    {
        return GetRemainingSeconds(DateTime.UtcNow);
    }

    public double? GetRemainingSeconds(DateTime utc)
    {
        if (!endUtc.HasValue)
        {
            return null;
        }

        return Math.Max(0d, (endUtc.Value - utc).TotalSeconds);
    }

    public void ResetTrade()
    {
        state = DemoTradeState.Idle;
        startUtc = null;
        endUtc = null;
    }
}
