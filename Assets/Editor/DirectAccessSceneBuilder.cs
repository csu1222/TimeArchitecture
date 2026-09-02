using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class DirectAccessSceneBuilder
{
    private const string ScenePath = "Assets/00.Scenes/DirectAccessScene.unity";
    private const string DebugPanelPath = "Assets/02.Prefab/TimeArchitectureDebugPanel.prefab";

    [MenuItem("Tools/Time Architecture/Create Direct Access Scene")]
    public static void CreateScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject runtime = new GameObject("Runtime");
        DirectCalendar calendar = runtime.AddComponent<DirectCalendar>();
        DirectSeason season = runtime.AddComponent<DirectSeason>();
        DirectTrade trade = runtime.AddComponent<DirectTrade>();
        DirectAccessDebugSource debugSource = runtime.AddComponent<DirectAccessDebugSource>();

        SetReference(season, "calendar", calendar);
        SetReference(debugSource, "calendar", calendar);
        SetReference(debugSource, "season", season);
        SetReference(debugSource, "trade", trade);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DebugPanelPath);
        GameObject panelObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        panelObject.name = "TimeArchitectureDebugPanel";
        DummyTimeArchitectureDebugSource dummy = panelObject.GetComponent<DummyTimeArchitectureDebugSource>();
        if (dummy != null)
        {
            Object.DestroyImmediate(dummy, true);
        }

        TimeArchitectureDebugPanel panel = panelObject.GetComponent<TimeArchitectureDebugPanel>();
        SetReference(panel, "debugSourceBehaviour", debugSource);
        SetReference(panel, "debugCommandBehaviour", debugSource);
        SetReference(panel, "manualTimeCommandBehaviour", null);

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Direct Access scene created: {ScenePath}");
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
