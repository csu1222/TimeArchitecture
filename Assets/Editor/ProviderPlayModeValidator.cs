using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProviderPlayModeValidator
{
    private const string RunningKey = "Provider.FullValidation";
    private const string ResultPath = "Library/ProviderValidation.log";
    private static readonly float[] Scales = { 0f, 1f, 2f, 5f };
    private static readonly string[] ScaleButtons = { "PauseButton", "X1Button", "X2Button", "X5Button" };
    private static ProviderRuntimeController runtime;
    private static ManualTimeProvider manual;
    private static CalendarService calendar;
    private static TradeService trade;
    private static ProviderDebugSource source;
    private static TimeArchitectureDebugPanel panel;
    private static Dictionary<string, Button> buttons;
    private static TimeArchitectureDebugSnapshot sample;
    private static bool initialized;
    private static float previousScale;
    private static bool previousBackground;
    private static double startedAt;
    private static double sampleAt;
    private static int phase;
    private static int scaleIndex;
    private static int lastFrame;
    private static int completedFrame;

    static ProviderPlayModeValidator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Tools/Time Architecture/Run Provider Full Validation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Start validation outside PlayMode.");
        }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                throw new InvalidOperationException("Save open scenes before validation.");
            }
        }
        ProviderValidator.Validate();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProviderSceneBuilder.ScenePath) == null)
        {
            ProviderSceneBuilder.CreateScene();
        }
        EditorSceneManager.OpenScene(ProviderSceneBuilder.ScenePath, OpenSceneMode.Single);
        File.WriteAllText(ResultPath, "Provider full validation started\n");
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying ||
            EditorApplication.isPaused || Time.frameCount < 3)
        {
            return;
        }
        if (initialized && lastFrame == Time.frameCount)
        {
            return;
        }
        lastFrame = Time.frameCount;
        try
        {
            if (!initialized)
            {
                Initialize();
                return;
            }
            Require(EditorApplication.timeSinceStartup - startedAt < 110d, "Validation timeout.");
            if (phase == 0)
            {
                ValidateFrozenClock();
            }
            else if (phase == 1)
            {
                ValidateProviderSwitch();
            }
            else
            {
                ValidateSystemClock();
            }
        }
        catch (Exception exception)
        {
            Finish(false, exception.Message);
            Debug.LogException(exception);
        }
    }

    private static void Initialize()
    {
        previousScale = Time.timeScale;
        previousBackground = Application.runInBackground;
        initialized = true;
        Application.runInBackground = true;
        startedAt = EditorApplication.timeSinceStartup;
        phase = 0;
        scaleIndex = 0;
        completedFrame = -1;
        runtime = UnityEngine.Object.FindFirstObjectByType<ProviderRuntimeController>();
        manual = UnityEngine.Object.FindFirstObjectByType<ManualTimeProvider>();
        calendar = UnityEngine.Object.FindFirstObjectByType<CalendarService>();
        trade = UnityEngine.Object.FindFirstObjectByType<TradeService>();
        source = UnityEngine.Object.FindFirstObjectByType<ProviderDebugSource>();
        panel = UnityEngine.Object.FindFirstObjectByType<TimeArchitectureDebugPanel>();
        Require(runtime != null && manual != null && calendar != null && trade != null &&
            source != null && panel != null && panel.enabled, "Scene components.");
        SerializedObject bindings = new SerializedObject(panel);
        foreach (string field in new[] { "debugSourceBehaviour", "debugCommandBehaviour", "manualTimeCommandBehaviour" })
        {
            Require(bindings.FindProperty(field).objectReferenceValue == source, "Panel binding: " + field);
        }
        GameObject section = (GameObject)bindings.FindProperty("manualTimeSection").objectReferenceValue;
        Require(section != null && section.activeInHierarchy, "Manual UI active.");
        RequireLabel("StrategyLabel", "Strategy : Time Provider + Domain Services");
        buttons = new Dictionary<string, Button>();
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            buttons.Add(button.name, button);
        }
        TimeArchitectureDebugSnapshot initial = source.GetSnapshot();
        Require(initial.SupportsManualTime && !initial.IsManualTime &&
            initial.TimeSourceName == nameof(SystemUtcTimeProvider), "Initial system mode.");
        Click("UseManualTimeButton");
        Require(runtime.IsManualTime && (manual.UtcNow - initial.CurrentUtc).TotalSeconds < 1d &&
            manual.UtcNow >= initial.CurrentUtc, "Manual entry copies system UTC.");

        ValidateManualBoundaries();
        manual.SetUtc(ProviderConstants.EpochUtc);
        foreach (var step in new[] { ("AddDayButton", 1), ("AddMonthButton", 30),
            ("AddSeasonButton", 90), ("AddYearButton", 360) })
        {
            DateTime before = manual.UtcNow;
            Click(step.Item1);
            Require(manual.UtcNow == before.AddSeconds(step.Item2 * 2d), step.Item1);
        }
        Click("ResetManualTimeButton");
        Require(runtime.IsManualTime && Math.Abs((manual.UtcNow - DateTime.UtcNow).TotalSeconds) < 1d,
            "Reset resynchronizes and stays manual.");
        Click("StartTradeButton");
        DateTime start = trade.StartUtc.Value;
        Click("StartTradeButton");
        Require(trade.StartUtc == start && trade.EndUtc == start.AddSeconds(60) &&
            source.GetSnapshot().TradeRemainingSeconds == 60d, "Manual trade / repeated start.");
        Click("AddMonthButton");
        Require(source.GetSnapshot().TradeState == "Completed" && trade.GetRemainingSeconds() == 0d,
            "Manual +1 Month trade completion.");
        Click("ResetTradeButton");
        RequireIdle();
        Click("StartTradeButton");
        Click("PauseButton");
        sample = source.GetSnapshot();
        sampleAt = EditorApplication.timeSinceStartup;
        Record("PASS: Manual UI, entry/reset, day/month/season/year buttons, runtime boundaries, manual trade completion/reset.");
    }

    private static void ValidateManualBoundaries()
    {
        foreach (var boundary in new[] { (29, 1, 2, "Spring"), (89, 1, 4, "Summer"),
            (179, 1, 7, "Autumn"), (269, 1, 10, "Winter"), (359, 2, 1, "Spring") })
        {
            manual.SetUtc(ProviderConstants.EpochUtc.AddSeconds(boundary.Item1 * 2d));
            Require(calendar.GetCurrentCalendar().Day == 30, "Boundary precondition.");
            Click("AddDayButton");
            TimeArchitectureDebugSnapshot actual = source.GetSnapshot();
            ProviderCalendarData date = calendar.GetCurrentCalendar();
            Require(actual.Year == boundary.Item2 && actual.Month == boundary.Item3 && actual.Day == 1 &&
                actual.Season == boundary.Item4 && date.GameDayIndex == boundary.Item1 + 1,
                "Runtime boundary after day " + boundary.Item1);
        }
    }

    private static void ValidateFrozenClock()
    {
        TimeArchitectureDebugSnapshot current = source.GetSnapshot();
        Require(current.CurrentUtc == sample.CurrentUtc && current.GameDayIndex == sample.GameDayIndex &&
            current.TradeRemainingSeconds == sample.TradeRemainingSeconds && current.IsManualTime &&
            current.TimeSourceName == nameof(ManualTimeProvider), "Manual clock freeze.");
        if (EditorApplication.timeSinceStartup - sampleAt < 1.5d)
        {
            return;
        }
        RequireLabel("CurrentModeLabel", "Mode : Manual Time");
        scaleIndex++;
        if (scaleIndex < Scales.Length)
        {
            Click(ScaleButtons[scaleIndex]);
            sampleAt = EditorApplication.timeSinceStartup;
            return;
        }
        Record("PASS: Manual clock frozen for at least 6 real seconds across 0/1/2/5x.");
        DateTime start = trade.StartUtc.Value;
        DateTime end = trade.EndUtc.Value;
        Click("UseSystemTimeButton");
        Require(!runtime.IsManualTime && Math.Abs((runtime.UtcNow - DateTime.UtcNow).TotalSeconds) < 1d &&
            trade.StartUtc == start && trade.EndUtc == end, "Return to system preserves trade timestamps.");
        Click("ResetTradeButton");
        Click("StartTradeButton");
        Click("X1Button");
        sample = source.GetSnapshot();
        phase = 1;
    }

    private static void ValidateProviderSwitch()
    {
        TimeArchitectureDebugSnapshot before = source.GetSnapshot();
        if ((before.CurrentUtc - sample.CurrentUtc).TotalSeconds < 3d)
        {
            return;
        }
        Click("UseManualTimeButton");
        TimeArchitectureDebugSnapshot after = source.GetSnapshot();
        Require(after.TradeStartUtc == before.TradeStartUtc && after.TradeEndUtc == before.TradeEndUtc &&
            Math.Abs(after.TradeRemainingSeconds.Value - before.TradeRemainingSeconds.Value) < 1d,
            "System to manual preserves running trade without remaining jump.");
        Click("AddMonthButton");
        Require(source.GetSnapshot().TradeState == "Completed", "Switched trade completes via manual time.");
        Click("UseSystemTimeButton");
        Require(source.GetSnapshot().TradeState == "Completed" && trade.GetRemainingSeconds() == 0d,
            "Completed state survives return to earlier system time.");
        Record("PASS: System/manual/system switching preserves trade data and completed state.");
        Click("ResetTradeButton");
        Click("StartTradeButton");
        Click("PauseButton");
        scaleIndex = 0;
        sample = source.GetSnapshot();
        phase = 2;
        Record("Started actual 60-second system trade.");
    }

    private static void ValidateSystemClock()
    {
        // 이번 Snapshot 조회 이전에 이미 완료 상태가 반영되었는지도 확인합니다.
        bool updatedToCompleted = trade.State == ProviderTradeState.Completed;
        TimeArchitectureDebugSnapshot current = source.GetSnapshot();
        double elapsed = (current.CurrentUtc - sample.CurrentUtc).TotalSeconds;
        if (scaleIndex < Scales.Length && elapsed >= 3d)
        {
            double unityElapsed = current.UnityTime - sample.UnityTime;
            Require(Math.Abs(unityElapsed - elapsed * Scales[scaleIndex]) < Math.Max(0.5d, elapsed * 0.2d),
                "Unity scaled time at " + Scales[scaleIndex]);
            int days = current.GameDayIndex - sample.GameDayIndex;
            Require(days >= Math.Floor(elapsed / 2d) && days <= Math.Ceiling(elapsed / 2d),
                "One game day per 2 real seconds.");
            Require(Math.Abs(sample.TradeRemainingSeconds.Value - current.TradeRemainingSeconds.Value - elapsed) < 0.01d,
                "Trade elapsed UTC independent of scale.");
            Record($"PASS: System {Scales[scaleIndex]}x, real={elapsed:F3}s, Unity={unityElapsed:F3}s, days=+{days}.");
            scaleIndex++;
            Click(scaleIndex < Scales.Length ? ScaleButtons[scaleIndex] : "PauseButton");
            sample = source.GetSnapshot();
        }
        if (scaleIndex == Scales.Length && current.CurrentUtc >= trade.EndUtc.Value.AddSeconds(0.1d))
        {
            if (completedFrame < 0)
            {
                completedFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount < completedFrame + 2)
            {
                return;
            }
            Require(updatedToCompleted && current.TradeState == "Completed" &&
                current.TradeRemainingSeconds == 0d && Time.timeScale == 0f, "60s trade completes while paused.");
            RequireLabel("TradeStateLabel", "State : Completed");
            Click("ResetTradeButton");
            RequireIdle();
            Finish(true, "Actual 60s trade, paused completion, all provider/runtime/UI checks.");
        }
    }

    private static void Click(string name)
    {
        Require(buttons.TryGetValue(name, out Button button) && button.isActiveAndEnabled &&
            button.interactable, "Button active: " + name);
        button.onClick.Invoke();
    }

    private static void RequireIdle()
    {
        TimeArchitectureDebugSnapshot current = source.GetSnapshot();
        Require(current.TradeState == "Idle" && !current.TradeStartUtc.HasValue &&
            !current.TradeEndUtc.HasValue && !current.TradeRemainingSeconds.HasValue, "Idle reset.");
    }

    private static void RequireLabel(string name, string expected)
    {
        foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name == name)
            {
                Require(label.text == expected, "Label " + name + ": " + label.text);
                return;
            }
        }
        throw new InvalidOperationException("Missing label " + name);
    }

    private static void Require(bool condition, string message) => ProviderValidator.Require(condition, message);

    private static void Record(string message)
    {
        File.AppendAllText(ResultPath, message + "\n");
        Debug.Log("Provider PlayMode: " + message);
    }

    private static void OnLog(string message, string trace, LogType type)
    {
        if (SessionState.GetBool(RunningKey, false) &&
            (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            Finish(false, "Unity error: " + message);
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(RunningKey, false))
        {
            Finish(false, "Validation interrupted.");
        }
    }

    private static void Finish(bool passed, string detail)
    {
        SessionState.SetBool(RunningKey, false);
        if (initialized)
        {
            Time.timeScale = previousScale;
            Application.runInBackground = previousBackground;
        }
        initialized = false;
        Record((passed ? "PASS" : "FAIL") + ": " + detail);
        EditorApplication.isPlaying = false;
    }
}
