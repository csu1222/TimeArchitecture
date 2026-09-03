using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class ProviderSceneBuilder
{
    public const string ScenePath = "Assets/00.Scenes/ProviderScene.unity";
    private const string PanelPath = "Assets/02.Prefab/TimeArchitectureDebugPanel.prefab";

    [MenuItem("Tools/Time Architecture/Create Provider Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Create ProviderScene outside PlayMode.");
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            throw new InvalidOperationException("ProviderScene already exists; it will not be overwritten.");
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("Common Debug Panel prefab is missing.");
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject time = new GameObject("Time");
            SystemUtcTimeProvider system = time.AddComponent<SystemUtcTimeProvider>();
            ManualTimeProvider manual = time.AddComponent<ManualTimeProvider>();
            ProviderRuntimeController runtime = time.AddComponent<ProviderRuntimeController>();
            SetReference(manual, "systemProvider", system);
            SetReference(runtime, "systemProvider", system);
            SetReference(runtime, "manualProvider", manual);

            GameObject domain = new GameObject("Domain");
            CalendarService calendar = domain.AddComponent<CalendarService>();
            SeasonResolver season = domain.AddComponent<SeasonResolver>();
            TradeService trade = domain.AddComponent<TradeService>();
            SetReference(calendar, "timeProviderBehaviour", runtime);
            SetReference(trade, "timeProviderBehaviour", runtime);

            ProviderDebugSource source = new GameObject("Debug").AddComponent<ProviderDebugSource>();
            SetReference(source, "providerRuntime", runtime);
            SetReference(source, "calendar", calendar);
            SetReference(source, "season", season);
            SetReference(source, "trade", trade);

            GameObject panelObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            panelObject.name = "TimeArchitectureDebugPanel";
            DummyTimeArchitectureDebugSource dummy = panelObject.GetComponent<DummyTimeArchitectureDebugSource>();
            if (dummy != null)
            {
                UnityEngine.Object.DestroyImmediate(dummy);
            }
            TimeArchitectureDebugPanel panel = panelObject.GetComponent<TimeArchitectureDebugPanel>();
            SetReference(panel, "debugSourceBehaviour", source);
            SetReference(panel, "debugCommandBehaviour", source);
            SetReference(panel, "manualTimeCommandBehaviour", source);

            GameObject events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Camera camera = new GameObject("BackgroundCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("ProviderScene could not be saved.");
            }
            Debug.Log("Provider scene created: " + ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previous);
        }
    }

    internal static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
