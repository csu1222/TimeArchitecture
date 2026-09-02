using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시간 계산 없이 Snapshot을 표시하고 사용자 입력을 Command로 전달하는 공통 View입니다.
/// </summary>
public sealed class TimeArchitectureDebugPanel : MonoBehaviour
{
    private const string UtcFormat = "yyyy-MM-dd HH:mm:ss 'UTC'";

    [Header("Debug Bindings")]
    [SerializeField] private MonoBehaviour debugSourceBehaviour;
    [SerializeField] private MonoBehaviour debugCommandBehaviour;
    [SerializeField] private MonoBehaviour manualTimeCommandBehaviour;

    [Header("Sections")]
    [SerializeField] private GameObject manualTimeSection;

    [Header("Labels")]
    [SerializeField] private TMP_Text strategyLabel;
    [SerializeField] private TMP_Text timeSourceLabel;
    [SerializeField] private TMP_Text currentUtcLabel;
    [SerializeField] private TMP_Text elapsedEpochLabel;
    [SerializeField] private TMP_Text unityTimeLabel;
    [SerializeField] private TMP_Text timeScaleLabel;
    [SerializeField] private TMP_Text yearLabel;
    [SerializeField] private TMP_Text monthLabel;
    [SerializeField] private TMP_Text dayLabel;
    [SerializeField] private TMP_Text dayOfYearLabel;
    [SerializeField] private TMP_Text gameDayIndexLabel;
    [SerializeField] private TMP_Text seasonLabel;
    [SerializeField] private TMP_Text seasonDayLabel;
    [SerializeField] private TMP_Text tradeStateLabel;
    [SerializeField] private TMP_Text tradeStartUtcLabel;
    [SerializeField] private TMP_Text tradeEndUtcLabel;
    [SerializeField] private TMP_Text tradeRemainingLabel;
    [SerializeField] private TMP_Text currentModeLabel;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button x1Button;
    [SerializeField] private Button x2Button;
    [SerializeField] private Button x5Button;
    [SerializeField] private Button startTradeButton;
    [SerializeField] private Button resetTradeButton;
    [SerializeField] private Button useSystemTimeButton;
    [SerializeField] private Button useManualTimeButton;
    [SerializeField] private Button addDayButton;
    [SerializeField] private Button addMonthButton;
    [SerializeField] private Button addSeasonButton;
    [SerializeField] private Button addYearButton;
    [SerializeField] private Button resetManualTimeButton;

    private ITimeArchitectureDebugSource debugSource;
    private ITimeArchitectureDebugCommand debugCommand;
    private IManualTimeDebugCommand manualTimeCommand;

    private void Awake()
    {
        debugSource = debugSourceBehaviour as ITimeArchitectureDebugSource;
        debugCommand = debugCommandBehaviour as ITimeArchitectureDebugCommand;
        manualTimeCommand = manualTimeCommandBehaviour as IManualTimeDebugCommand;

        if (debugSource == null)
        {
            Debug.LogError(
                $"{nameof(TimeArchitectureDebugPanel)} requires Debug Source Behaviour to implement " +
                $"{nameof(ITimeArchitectureDebugSource)}.",
                this);
            enabled = false;
            return;
        }

        bool hasDebugCommand = debugCommand != null;
        pauseButton.interactable = hasDebugCommand;
        x1Button.interactable = hasDebugCommand;
        x2Button.interactable = hasDebugCommand;
        x5Button.interactable = hasDebugCommand;
        startTradeButton.interactable = hasDebugCommand;
        resetTradeButton.interactable = hasDebugCommand;
        manualTimeSection.SetActive(manualTimeCommand != null);
    }

    private void OnEnable()
    {
        pauseButton.onClick.AddListener(Pause);
        x1Button.onClick.AddListener(SetNormalSpeed);
        x2Button.onClick.AddListener(SetDoubleSpeed);
        x5Button.onClick.AddListener(SetFiveTimesSpeed);
        startTradeButton.onClick.AddListener(StartTrade);
        resetTradeButton.onClick.AddListener(ResetTrade);
        useSystemTimeButton.onClick.AddListener(UseSystemTime);
        useManualTimeButton.onClick.AddListener(UseManualTime);
        addDayButton.onClick.AddListener(AddDay);
        addMonthButton.onClick.AddListener(AddMonth);
        addSeasonButton.onClick.AddListener(AddSeason);
        addYearButton.onClick.AddListener(AddYear);
        resetManualTimeButton.onClick.AddListener(ResetManualTime);
    }

    private void OnDisable()
    {
        pauseButton.onClick.RemoveListener(Pause);
        x1Button.onClick.RemoveListener(SetNormalSpeed);
        x2Button.onClick.RemoveListener(SetDoubleSpeed);
        x5Button.onClick.RemoveListener(SetFiveTimesSpeed);
        startTradeButton.onClick.RemoveListener(StartTrade);
        resetTradeButton.onClick.RemoveListener(ResetTrade);
        useSystemTimeButton.onClick.RemoveListener(UseSystemTime);
        useManualTimeButton.onClick.RemoveListener(UseManualTime);
        addDayButton.onClick.RemoveListener(AddDay);
        addMonthButton.onClick.RemoveListener(AddMonth);
        addSeasonButton.onClick.RemoveListener(AddSeason);
        addYearButton.onClick.RemoveListener(AddYear);
        resetManualTimeButton.onClick.RemoveListener(ResetManualTime);
    }

    private void Update()
    {
        if (debugSource != null)
        {
            Refresh(debugSource.GetSnapshot());
        }
    }

    private void Refresh(TimeArchitectureDebugSnapshot snapshot)
    {
        strategyLabel.text = $"Strategy : {snapshot.StrategyName}";
        timeSourceLabel.text = $"Time Source : {snapshot.TimeSourceName}";
        currentUtcLabel.text = $"Current UTC : {snapshot.CurrentUtc:yyyy-MM-dd HH:mm:ss} UTC";
        elapsedEpochLabel.text = $"Elapsed From Epoch : {FormatElapsed(snapshot.ElapsedFromEpochSeconds)}";
        unityTimeLabel.text = $"Time.time : {snapshot.UnityTime:F2}";
        timeScaleLabel.text = $"Time.timeScale : {snapshot.TimeScale:F1}";
        yearLabel.text = $"Year : {snapshot.Year}";
        monthLabel.text = $"Month : {snapshot.Month}";
        dayLabel.text = $"Day : {snapshot.Day}";
        dayOfYearLabel.text = $"Day Of Year : {snapshot.DayOfYear}";
        gameDayIndexLabel.text = $"Game Day Index : {snapshot.GameDayIndex}";
        seasonLabel.text = $"Current Season : {snapshot.Season}";
        seasonDayLabel.text = $"Season Day : {snapshot.SeasonDay} / 90";
        tradeStateLabel.text = $"State : {snapshot.TradeState}";
        tradeStartUtcLabel.text = $"Start UTC : {FormatOptionalUtc(snapshot.TradeStartUtc)}";
        tradeEndUtcLabel.text = $"End UTC : {FormatOptionalUtc(snapshot.TradeEndUtc)}";
        tradeRemainingLabel.text = $"Remaining : {FormatRemaining(snapshot.TradeRemainingSeconds)}";
        currentModeLabel.text = $"Mode : {(snapshot.IsManualTime ? "Manual Time" : "System Time")}";
    }

    private static string FormatElapsed(double seconds)
    {
        TimeSpan elapsed = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        return $"{(int)elapsed.TotalDays}d {elapsed:hh\\:mm\\:ss}";
    }

    private static string FormatOptionalUtc(DateTime? utc)
    {
        return utc.HasValue ? utc.Value.ToString(UtcFormat) : "-";
    }

    private static string FormatRemaining(double? seconds)
    {
        return seconds.HasValue ? $"{Math.Max(0d, seconds.Value):F1}s" : "-";
    }

    private void Pause() => debugCommand?.SetTimeScale(0f);
    private void SetNormalSpeed() => debugCommand?.SetTimeScale(1f);
    private void SetDoubleSpeed() => debugCommand?.SetTimeScale(2f);
    private void SetFiveTimesSpeed() => debugCommand?.SetTimeScale(5f);
    private void StartTrade() => debugCommand?.StartTrade();
    private void ResetTrade() => debugCommand?.ResetTrade();
    private void UseSystemTime() => manualTimeCommand?.UseSystemTime();
    private void UseManualTime() => manualTimeCommand?.UseManualTime();
    private void AddDay() => manualTimeCommand?.AddGameDays(1);
    private void AddMonth() => manualTimeCommand?.AddGameDays(30);
    private void AddSeason() => manualTimeCommand?.AddGameDays(90);
    private void AddYear() => manualTimeCommand?.AddGameDays(360);
    private void ResetManualTime() => manualTimeCommand?.ResetManualTime();
}
