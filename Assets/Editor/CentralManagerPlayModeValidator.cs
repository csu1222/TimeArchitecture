using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CentralManagerPlayModeValidator
{
    private const string RunningKey = "CentralManager.PlayModeValidation";
    private static readonly float[] Scales = { 0f, 1f, 2f, 5f };
    private static readonly string[] ScaleButtons = { "PauseButton", "X1Button", "X2Button", "X5Button" };
    private static CentralTimeManager manager;
    private static CentralManagerDebugSource source;
    private static TimeArchitectureDebugPanel panel;
    private static Dictionary<string, Button> buttons;
    private static TimeArchitectureDebugSnapshot sample;
    private static int phase;
    private static float previousScale;
    private static bool initialized;
    private static int lastFrame;
    private static int completionFrame = -1;
    private static bool previousRunInBackground;

    static CentralManagerPlayModeValidator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Tools/Time Architecture/Run Central Manager Full Validation")]
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
                throw new InvalidOperationException("Save your open scenes before running validation.");
            }
        }

        CentralManagerValidator.Validate();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CentralManagerSceneBuilder.ScenePath) == null)
        {
            CentralManagerSceneBuilder.CreateScene();
        }

        EditorSceneManager.OpenScene(CentralManagerSceneBuilder.ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Debug.LogWarning("Central Manager PlayMode validation interrupted before completion.");
            }
            SessionState.SetBool(RunningKey, false);
            if (initialized)
            {
                Time.timeScale = previousScale;
                Application.runInBackground = previousRunInBackground;
            }
            initialized = false;
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying ||
            EditorApplication.isPaused || Time.frameCount < 3)
        {
            return;
        }

        // Editor update는 게임 프레임이 멈춰도 호출되므로 실제 PlayMode 프레임만 검사합니다.
        if (initialized && Time.frameCount == lastFrame)
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

            TimeArchitectureDebugSnapshot current = source.GetSnapshot();
            double elapsed = (current.CurrentUtc - sample.CurrentUtc).TotalSeconds;
            if (phase < Scales.Length && elapsed >= 3d)
            {
                double unityElapsed = current.UnityTime - sample.UnityTime;
                CentralManagerValidator.Require(
                    Math.Abs(unityElapsed - elapsed * Scales[phase]) < Math.Max(0.5d, elapsed * 0.2d),
                    $"Unity time at scale {Scales[phase]}: {unityElapsed} / real {elapsed}.");
                int expectedDays = manager.CalculateCalendar(current.CurrentUtc).GameDayIndex -
                    manager.CalculateCalendar(sample.CurrentUtc).GameDayIndex;
                CentralManagerValidator.Require(current.GameDayIndex - sample.GameDayIndex == expectedDays &&
                    expectedDays >= 1, "Calendar real-time progression.");
                CentralManagerValidator.Require(
                    Math.Abs(sample.TradeRemainingSeconds.Value - current.TradeRemainingSeconds.Value - elapsed) < 0.01d,
                    "Trade must follow UTC at every scale.");
                Debug.Log($"Central Manager PlayMode scale {Scales[phase]}: PASS; real={elapsed:F3}s, Unity={unityElapsed:F3}s, days=+{expectedDays}");
                phase++;
                Click(phase < Scales.Length ? ScaleButtons[phase] : "PauseButton");
                sample = source.GetSnapshot();
            }

            if (phase == Scales.Length && current.CurrentUtc >= manager.EndUtc.Value.AddSeconds(0.1d))
            {
                if (completionFrame < 0)
                {
                    completionFrame = Time.frameCount;
                    return;
                }
                if (Time.frameCount < completionFrame + 2)
                {
                    return;
                }
                CentralManagerValidator.Require(current.TradeState == "Completed" &&
                    current.TradeRemainingSeconds == 0d && Time.timeScale == 0f, "60s completion while paused.");
                CentralManagerValidator.Require(current.CurrentUtc >= manager.StartUtc.Value.AddSeconds(60),
                    "Trade completed before 60 real seconds.");
                RequireLabel("TradeStateLabel", "State : Completed");
                Click("ResetTradeButton");
                RequireIdle();
                Debug.Log("Central Manager PlayMode validation: PASS (60s actual trade, paused completion, reset, 0/1/2/5x, calendar progression, panel bindings, manual section inactive)");
                Finish();
            }
        }
        catch (Exception exception)
        {
            Finish();
            Debug.LogException(exception);
        }
    }

    private static void Initialize()
    {
        previousScale = Time.timeScale;
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        initialized = true;
        completionFrame = -1;
        phase = 0;
        manager = UnityEngine.Object.FindFirstObjectByType<CentralTimeManager>();
        source = UnityEngine.Object.FindFirstObjectByType<CentralManagerDebugSource>();
        panel = UnityEngine.Object.FindFirstObjectByType<TimeArchitectureDebugPanel>();
        CentralManagerValidator.Require(manager != null && source != null && panel != null && panel.enabled,
            "Scene runtime/panel references.");
        SerializedObject bindings = new SerializedObject(panel);
        CentralManagerValidator.Require(bindings.FindProperty("debugSourceBehaviour").objectReferenceValue == source &&
            bindings.FindProperty("debugCommandBehaviour").objectReferenceValue == source &&
            bindings.FindProperty("manualTimeCommandBehaviour").objectReferenceValue == null, "Panel bindings.");
        GameObject manualSection = (GameObject)bindings.FindProperty("manualTimeSection").objectReferenceValue;
        CentralManagerValidator.Require(manualSection != null && !manualSection.activeSelf, "Manual UI inactive.");
        RequireLabel("StrategyLabel", "Strategy : Central Time Manager");
        buttons = new Dictionary<string, Button>();
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            buttons.Add(button.name, button);
        }

        Click("StartTradeButton");
        CentralManagerValidator.Require(manager.State == CentralTradeState.Traveling, "Start button.");
        Click("ResetTradeButton");
        RequireIdle();
        Click("StartTradeButton");
        DateTime start = manager.StartUtc.Value;
        Click("StartTradeButton");
        CentralManagerValidator.Require(manager.StartUtc == start &&
            manager.EndUtc == start.AddSeconds(60), "Repeated start / 60-second duration.");
        Click("PauseButton");
        sample = source.GetSnapshot();
        Debug.Log("Central Manager PlayMode validation started: actual 60-second trade.");
    }

    private static void Click(string name)
    {
        CentralManagerValidator.Require(buttons.TryGetValue(name, out Button button) &&
            button.isActiveAndEnabled && button.interactable, $"Button {name}.");
        button.onClick.Invoke();
    }

    private static void RequireIdle()
    {
        TimeArchitectureDebugSnapshot current = source.GetSnapshot();
        CentralManagerValidator.Require(current.TradeState == "Idle" &&
            !current.TradeStartUtc.HasValue && !current.TradeEndUtc.HasValue &&
            !current.TradeRemainingSeconds.HasValue, "Reset button / Idle display values.");
    }

    private static void RequireLabel(string name, string expected)
    {
        foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name == name)
            {
                CentralManagerValidator.Require(label.text == expected, $"Label {name}: {label.text}.");
                return;
            }
        }
        throw new InvalidOperationException("Label missing: " + name);
    }

    private static void OnLog(string condition, string trace, LogType type)
    {
        if (SessionState.GetBool(RunningKey, false) &&
            (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            Finish();
        }
    }

    private static void Finish()
    {
        SessionState.SetBool(RunningKey, false);
        if (initialized)
        {
            Time.timeScale = previousScale;
            Application.runInBackground = previousRunInBackground;
        }
        initialized = false;
        EditorApplication.isPlaying = false;
    }
}
