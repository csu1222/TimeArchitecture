using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Serialized YAML을 직접 편집하지 않고 공통 Debug Panel Prefab을 안전하게 생성합니다.
/// </summary>
public static class TimeArchitectureDebugPanelPrefabBuilder
{
    private const string PrefabPath = "Assets/02.Prefab/TimeArchitectureDebugPanel.prefab";
    private static readonly Color BackgroundColor = new Color(0.045f, 0.055f, 0.075f, 0.94f);
    private static readonly Color SectionColor = new Color(0.09f, 0.11f, 0.15f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.18f, 0.28f, 0.42f, 1f);

    [InitializeOnLoadMethod]
    private static void CreateMissingPrefabAfterReload()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            EditorApplication.delayCall += CreatePrefab;
        }
    }

    [MenuItem("Tools/Time Architecture/Create Debug Panel Prefab")]
    public static void CreatePrefab()
    {
        GameObject root = CreateUiObject("TimeArchitectureDebugPanel", null);
        ConfigureRoot(root);

        DummyTimeArchitectureDebugSource dummy = root.AddComponent<DummyTimeArchitectureDebugSource>();
        TimeArchitectureDebugPanel panel = root.AddComponent<TimeArchitectureDebugPanel>();
        Dictionary<string, Object> references = new Dictionary<string, Object>();

        GameObject content = CreateUiObject("Content", root.transform);
        ConfigureContent(content);

        CreateHeader(content.transform, references);
        CreateClockSection(content.transform, references);
        CreateUnityTimeSection(content.transform, references);
        CreateCalendarSection(content.transform, references);
        CreateSeasonSection(content.transform, references);
        CreateTradeSection(content.transform, references);
        CreateManualTimeSection(content.transform, references);

        SerializedObject serializedPanel = new SerializedObject(panel);
        SetReference(serializedPanel, "debugSourceBehaviour", dummy);
        SetReference(serializedPanel, "debugCommandBehaviour", dummy);
        SetReference(serializedPanel, "manualTimeCommandBehaviour", dummy);
        foreach (KeyValuePair<string, Object> reference in references)
        {
            SetReference(serializedPanel, reference.Key, reference.Value);
        }

        serializedPanel.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Time Architecture Debug Panel prefab created: {PrefabPath}");
    }

    private static void ConfigureRoot(GameObject root)
    {
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
    }

    private static void ConfigureContent(GameObject content)
    {
        RectTransform rect = content.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(24f, 0f);
        rect.sizeDelta = new Vector2(600f, -48f);

        Image background = content.AddComponent<Image>();
        background.color = BackgroundColor;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
    }

    private static void CreateHeader(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("Header", parent);
        CreateText("Title", "TIME ARCHITECTURE", section.transform, 30f, FontStyles.Bold, null);
        CreateLabel("StrategyLabel", "Strategy : -", section.transform, references, "strategyLabel");
        CreateLabel("TimeSourceLabel", "Time Source : -", section.transform, references, "timeSourceLabel");
    }

    private static void CreateClockSection(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("ClockSection", parent);
        CreateSectionTitle("ABSOLUTE TIME", section.transform);
        CreateLabel("CurrentUtcLabel", "Current UTC : -", section.transform, references, "currentUtcLabel");
        CreateLabel("ElapsedEpochLabel", "Elapsed From Epoch : -", section.transform, references, "elapsedEpochLabel");
    }

    private static void CreateUnityTimeSection(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("UnityTimeSection", parent);
        CreateSectionTitle("UNITY TIME", section.transform);
        CreateLabel("UnityTimeLabel", "Time.time : -", section.transform, references, "unityTimeLabel");
        CreateLabel("TimeScaleLabel", "Time.timeScale : -", section.transform, references, "timeScaleLabel");
        GameObject row = CreateButtonRow(section.transform);
        CreateButton("PauseButton", "Pause", row.transform, references, "pauseButton");
        CreateButton("X1Button", "1x", row.transform, references, "x1Button");
        CreateButton("X2Button", "2x", row.transform, references, "x2Button");
        CreateButton("X5Button", "5x", row.transform, references, "x5Button");
    }

    private static void CreateCalendarSection(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("CalendarSection", parent);
        CreateSectionTitle("CALENDAR", section.transform);
        CreateLabel("YearLabel", "Year : -", section.transform, references, "yearLabel");
        CreateLabel("MonthLabel", "Month : -", section.transform, references, "monthLabel");
        CreateLabel("DayLabel", "Day : -", section.transform, references, "dayLabel");
        CreateLabel("DayOfYearLabel", "Day Of Year : -", section.transform, references, "dayOfYearLabel");
        CreateLabel("GameDayIndexLabel", "Game Day Index : -", section.transform, references, "gameDayIndexLabel");
    }

    private static void CreateSeasonSection(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("SeasonSection", parent);
        CreateSectionTitle("SEASON", section.transform);
        CreateLabel("SeasonLabel", "Current Season : -", section.transform, references, "seasonLabel");
        CreateLabel("SeasonDayLabel", "Season Day : -", section.transform, references, "seasonDayLabel");
    }

    private static void CreateTradeSection(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("TradeSection", parent);
        CreateSectionTitle("TRADE", section.transform);
        CreateLabel("TradeStateLabel", "State : -", section.transform, references, "tradeStateLabel");
        CreateLabel("TradeStartUtcLabel", "Start UTC : -", section.transform, references, "tradeStartUtcLabel");
        CreateLabel("TradeEndUtcLabel", "End UTC : -", section.transform, references, "tradeEndUtcLabel");
        CreateLabel("TradeRemainingLabel", "Remaining : -", section.transform, references, "tradeRemainingLabel");
        GameObject row = CreateButtonRow(section.transform);
        CreateButton("StartTradeButton", "Start Trade", row.transform, references, "startTradeButton");
        CreateButton("ResetTradeButton", "Reset Trade", row.transform, references, "resetTradeButton");
    }

    private static void CreateManualTimeSection(Transform parent, IDictionary<string, Object> references)
    {
        GameObject section = CreateSection("ManualTimeSection", parent);
        references.Add("manualTimeSection", section);
        CreateSectionTitle("MANUAL TIME", section.transform);
        CreateLabel("CurrentModeLabel", "Mode : -", section.transform, references, "currentModeLabel");
        GameObject modeRow = CreateButtonRow(section.transform);
        CreateButton("UseSystemTimeButton", "Use System Time", modeRow.transform, references, "useSystemTimeButton");
        CreateButton("UseManualTimeButton", "Use Manual Time", modeRow.transform, references, "useManualTimeButton");
        GameObject addRow = CreateButtonRow(section.transform);
        CreateButton("AddDayButton", "+1 Day", addRow.transform, references, "addDayButton");
        CreateButton("AddMonthButton", "+1 Month", addRow.transform, references, "addMonthButton");
        CreateButton("AddSeasonButton", "+1 Season", addRow.transform, references, "addSeasonButton");
        CreateButton("AddYearButton", "+1 Year", addRow.transform, references, "addYearButton");
        GameObject resetRow = CreateButtonRow(section.transform);
        CreateButton("ResetManualTimeButton", "Reset Time", resetRow.transform, references, "resetManualTimeButton");
    }

    private static GameObject CreateSection(string name, Transform parent)
    {
        GameObject section = CreateUiObject(name, parent);
        section.AddComponent<Image>().color = SectionColor;
        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 6, 6);
        layout.spacing = 3f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        return section;
    }

    private static GameObject CreateButtonRow(Transform parent)
    {
        GameObject row = CreateUiObject("Buttons", parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        row.AddComponent<LayoutElement>().preferredHeight = 34f;
        return row;
    }

    private static void CreateSectionTitle(string text, Transform parent)
    {
        CreateText("Title", text, parent, 22f, FontStyles.Bold, null);
    }

    private static void CreateLabel(
        string name,
        string text,
        Transform parent,
        IDictionary<string, Object> references,
        string propertyName)
    {
        TMP_Text label = CreateText(name, text, parent, 18f, FontStyles.Normal, null);
        references.Add(propertyName, label);
    }

    private static TMP_Text CreateText(
        string name,
        string value,
        Transform parent,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions? alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = alignment ?? TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = fontSize + 10f;
        return text;
    }

    private static void CreateButton(
        string name,
        string caption,
        Transform parent,
        IDictionary<string, Object> references,
        string propertyName)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = ButtonColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        TMP_Text label = CreateText("Label", caption, buttonObject.transform, 16f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        references.Add(propertyName, button);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject instance = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            instance.transform.SetParent(parent, false);
        }

        return instance;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }
}
