using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class TimeArchitectureDebugPanelPlayModeValidator
{
    private const string MenuPath = "Tools/Time Architecture/Validate Debug Panel Play Mode";
    private const string PointerDiagnosticsMenuPath =
        "Tools/Time Architecture/Enable Pointer Diagnostics (15 Seconds)";

    [MenuItem(MenuPath, true)]
    private static bool CanValidate()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem(MenuPath)]
    private static void Validate()
    {
        try
        {
            TimeArchitectureDebugPanel panel =
                UnityEngine.Object.FindFirstObjectByType<TimeArchitectureDebugPanel>();
            DummyTimeArchitectureDebugSource dummy =
                UnityEngine.Object.FindFirstObjectByType<DummyTimeArchitectureDebugSource>();

            Require(panel != null, "TimeArchitectureDebugPanel was not found.");
            Require(dummy != null, "DummyTimeArchitectureDebugSource was not found.");

            Dictionary<string, Button> buttons = GetButtons(panel);
            ValidateSnapshotDisplay(panel);
            ValidateTimeScale(buttons);
            ValidateTrade(buttons, dummy);
            ValidateManualTime(panel, buttons, dummy);

            Time.timeScale = 1f;
            Debug.Log("Time Architecture Debug Panel PlayMode validation: PASS", panel);
        }
        catch (Exception exception)
        {
            Time.timeScale = 1f;
            Debug.LogError(
                $"Time Architecture Debug Panel PlayMode validation: FAIL\n{exception}");
        }
    }

    [MenuItem(PointerDiagnosticsMenuPath, true)]
    private static bool CanEnablePointerDiagnostics()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem(PointerDiagnosticsMenuPath)]
    private static void EnablePointerDiagnostics()
    {
        TimeArchitectureDebugPanel panel =
            UnityEngine.Object.FindFirstObjectByType<TimeArchitectureDebugPanel>();
        if (panel == null)
        {
            Debug.LogError("Pointer diagnostics requires TimeArchitectureDebugPanel in PlayMode.");
            return;
        }

        TimeArchitectureDebugPanelPointerDiagnostics diagnostics =
            panel.GetComponent<TimeArchitectureDebugPanelPointerDiagnostics>();
        if (diagnostics == null)
        {
            diagnostics = panel.gameObject.AddComponent<TimeArchitectureDebugPanelPointerDiagnostics>();
        }

        diagnostics.Begin(15f);
    }

    private static Dictionary<string, Button> GetButtons(TimeArchitectureDebugPanel panel)
    {
        Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            buttons.Add(button.name, button);
        }

        return buttons;
    }

    private static void ValidateSnapshotDisplay(TimeArchitectureDebugPanel panel)
    {
        Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();
        foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            labels[label.name] = label;
        }

        RequireText(labels, "StrategyLabel", "Strategy : Debug Dummy");
        RequireText(labels, "TimeSourceLabel", "Time Source : Dummy Source");
        RequirePrefix(labels, "CurrentUtcLabel", "Current UTC : ");
        RequirePrefix(labels, "ElapsedEpochLabel", "Elapsed From Epoch : ");
        RequirePrefix(labels, "UnityTimeLabel", "Time.time : ");
        RequirePrefix(labels, "TimeScaleLabel", "Time.timeScale : ");
        RequireText(labels, "YearLabel", "Year : 1");
        RequireText(labels, "MonthLabel", "Month : 3");
        RequireText(labels, "DayLabel", "Day : 30");
        RequireText(labels, "DayOfYearLabel", "Day Of Year : 90");
        RequireText(labels, "GameDayIndexLabel", "Game Day Index : 89");
        RequireText(labels, "SeasonLabel", "Current Season : Spring");
        RequireText(labels, "SeasonDayLabel", "Season Day : 90 / 90");
        RequireText(labels, "TradeStateLabel", "State : Idle");
        RequireText(labels, "TradeStartUtcLabel", "Start UTC : -");
        RequireText(labels, "TradeEndUtcLabel", "End UTC : -");
        RequireText(labels, "TradeRemainingLabel", "Remaining : -");
        RequireText(labels, "CurrentModeLabel", "Mode : System Time");
    }

    private static void ValidateTimeScale(IReadOnlyDictionary<string, Button> buttons)
    {
        Click(buttons, "PauseButton");
        Require(Mathf.Approximately(Time.timeScale, 0f), "Pause did not set Time.timeScale to 0.");
        Click(buttons, "X1Button");
        Require(Mathf.Approximately(Time.timeScale, 1f), "1x did not set Time.timeScale to 1.");
        Click(buttons, "X2Button");
        Require(Mathf.Approximately(Time.timeScale, 2f), "2x did not set Time.timeScale to 2.");
        Click(buttons, "X5Button");
        Require(Mathf.Approximately(Time.timeScale, 5f), "5x did not set Time.timeScale to 5.");
    }

    private static void ValidateTrade(
        IReadOnlyDictionary<string, Button> buttons,
        DummyTimeArchitectureDebugSource dummy)
    {
        Click(buttons, "StartTradeButton");
        TimeArchitectureDebugSnapshot started = dummy.GetSnapshot();
        Require(started.TradeState == "Traveling", "Start Trade did not enter Traveling state.");
        Require(started.TradeStartUtc.HasValue, "Start Trade did not set Start UTC.");
        Require(started.TradeEndUtc.HasValue, "Start Trade did not set End UTC.");
        Require(started.TradeRemainingSeconds.HasValue, "Start Trade did not set Remaining.");

        Click(buttons, "ResetTradeButton");
        TimeArchitectureDebugSnapshot reset = dummy.GetSnapshot();
        Require(reset.TradeState == "Idle", "Reset Trade did not return to Idle state.");
        Require(!reset.TradeStartUtc.HasValue, "Reset Trade did not clear Start UTC.");
        Require(!reset.TradeEndUtc.HasValue, "Reset Trade did not clear End UTC.");
        Require(!reset.TradeRemainingSeconds.HasValue, "Reset Trade did not clear Remaining.");
    }

    private static void ValidateManualTime(
        TimeArchitectureDebugPanel panel,
        IReadOnlyDictionary<string, Button> buttons,
        DummyTimeArchitectureDebugSource dummy)
    {
        Transform manualSection = panel.transform.Find("Content/ManualTimeSection");
        Require(manualSection != null && manualSection.gameObject.activeSelf,
            "Manual Time section is not active.");

        Click(buttons, "UseManualTimeButton");
        Require(dummy.GetSnapshot().IsManualTime, "Use Manual Time did not enable manual mode.");

        int dayOfYear = dummy.GetSnapshot().DayOfYear;
        Click(buttons, "AddDayButton");
        Require(dummy.GetSnapshot().DayOfYear == dayOfYear + 1, "+1 Day was not delivered.");
        Click(buttons, "AddMonthButton");
        Require(dummy.GetSnapshot().DayOfYear == dayOfYear + 31, "+1 Month was not delivered.");
        Click(buttons, "AddSeasonButton");
        Require(dummy.GetSnapshot().DayOfYear == dayOfYear + 121, "+1 Season was not delivered.");
        Click(buttons, "AddYearButton");
        Require(dummy.GetSnapshot().DayOfYear == dayOfYear + 481, "+1 Year was not delivered.");

        Click(buttons, "ResetManualTimeButton");
        Require(dummy.GetSnapshot().DayOfYear == 90, "Reset Time did not restore the initial date.");
        Click(buttons, "UseSystemTimeButton");
        Require(!dummy.GetSnapshot().IsManualTime, "Use System Time did not disable manual mode.");
    }

    private static void Click(IReadOnlyDictionary<string, Button> buttons, string name)
    {
        Require(buttons.TryGetValue(name, out Button button), $"{name} was not found.");
        Require(button.interactable, $"{name} is not interactable.");
        button.onClick.Invoke();
    }

    private static void RequireText(
        IReadOnlyDictionary<string, TMP_Text> labels,
        string name,
        string expected)
    {
        Require(labels.TryGetValue(name, out TMP_Text label), $"{name} was not found.");
        Require(label.text == expected, $"{name} expected '{expected}' but was '{label.text}'.");
    }

    private static void RequirePrefix(
        IReadOnlyDictionary<string, TMP_Text> labels,
        string name,
        string expectedPrefix)
    {
        Require(labels.TryGetValue(name, out TMP_Text label), $"{name} was not found.");
        Require(label.text.StartsWith(expectedPrefix, StringComparison.Ordinal),
            $"{name} expected prefix '{expectedPrefix}' but was '{label.text}'.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

public sealed class TimeArchitectureDebugPanelPointerDiagnostics : MonoBehaviour
{
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private float finishTime;
    private int pressCount;
    private string lastTopTarget;
    private bool running;

    public void Begin(float durationSeconds)
    {
        finishTime = Time.realtimeSinceStartup + durationSeconds;
        pressCount = 0;
        lastTopTarget = null;
        running = true;

        Debug.Log(
            $"Pointer diagnostics started for {durationSeconds:F0} seconds. " +
            $"Move the pointer over a Debug Panel button and left-click it. " +
            $"Button screen centers: {DescribeButtonCenters()}",
            this);
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }

        if (Time.realtimeSinceStartup >= finishTime)
        {
            running = false;
            Debug.Log($"Pointer diagnostics finished. Left Click presses detected: {pressCount}.", this);
            return;
        }

        Mouse mouse = Mouse.current;
        EventSystem eventSystem = EventSystem.current;
        if (mouse == null || eventSystem == null)
        {
            running = false;
            Debug.LogError(
                $"Pointer diagnostics stopped. Mouse: {(mouse == null ? "missing" : "available")}, " +
                $"EventSystem: {(eventSystem == null ? "missing" : "available")}.",
                this);
            return;
        }

        Vector2 position = mouse.position.ReadValue();
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = position
        };

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);

        string topTarget = DescribeTopTarget();
        if (topTarget != lastTopTarget)
        {
            lastTopTarget = topTarget;
            Debug.Log($"Pointer hover at {position}: {topTarget}", this);
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            pressCount++;
            Debug.Log(
                $"Left Click detected at {position}. " +
                $"IsPointerOverGameObject: {eventSystem.IsPointerOverGameObject()}. " +
                $"Raycast hits: {DescribeRaycastHits()}",
                this);
        }
    }

    private string DescribeButtonCenters()
    {
        List<string> descriptions = new List<string>();
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            Image image = button.GetComponent<Image>();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                null,
                button.transform.position);
            descriptions.Add(
                $"{button.name}=" +
                $"center:{center}, " +
                $"size:{rectTransform.rect.size}, " +
                $"imageEnabled:{image != null && image.enabled}, " +
                $"raycastTarget:{image != null && image.raycastTarget}, " +
                $"culled:{image != null && image.canvasRenderer.cull}");
        }

        return string.Join(", ", descriptions);
    }

    private string DescribeTopTarget()
    {
        if (raycastResults.Count == 0)
        {
            return "no UI raycast hit";
        }

        GameObject target = raycastResults[0].gameObject;
        Button button = target.GetComponentInParent<Button>();
        return button == null
            ? $"top={GetPath(target.transform)}, button=none"
            : $"top={GetPath(target.transform)}, button={button.name}, interactable={button.interactable}";
    }

    private string DescribeRaycastHits()
    {
        if (raycastResults.Count == 0)
        {
            return "none";
        }

        List<string> descriptions = new List<string>(raycastResults.Count);
        foreach (RaycastResult result in raycastResults)
        {
            descriptions.Add(GetPath(result.gameObject.transform));
        }

        return string.Join(" -> ", descriptions);
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = $"{target.name}/{path}";
        }

        return path;
    }
}
