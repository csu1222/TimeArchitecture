using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class CentralManagerSceneBuilder
{
    public const string ScenePath = "Assets/00.Scenes/CentralManagerScene.unity";
    private const string DebugPanelPath = "Assets/02.Prefab/TimeArchitectureDebugPanel.prefab";

    [MenuItem("Tools/Time Architecture/Create Central Manager Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Create the scene outside PlayMode.");
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            throw new InvalidOperationException("CentralManagerScene already exists; it will not be overwritten.");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DebugPanelPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("The common Debug Panel prefab was not found.");
        }

        // 열린 Scene과 미저장 변경을 유지하면서 새 Scene에만 오브젝트를 생성합니다.
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject runtime = new GameObject("Runtime");
            CentralTimeManager manager = runtime.AddComponent<CentralTimeManager>();
            CentralManagerDebugSource source = runtime.AddComponent<CentralManagerDebugSource>();
            SetReference(source, "manager", manager);

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
            SetReference(panel, "manualTimeCommandBehaviour", null);

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            CreateBackgroundCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("CentralManagerScene could not be saved.");
            }

            Debug.Log($"Central Manager scene created: {ScenePath}");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previous);
        }
    }

    [MenuItem("Tools/Time Architecture/Add Central Manager Background Camera")]
    public static void AddBackgroundCamera()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
        {
            throw new InvalidOperationException("Open CentralManagerScene outside PlayMode first.");
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<Camera>(true) != null)
            {
                return;
            }
        }

        Camera camera = CreateBackgroundCamera();
        Undo.RegisterCreatedObjectUndo(camera.gameObject, "Add Central Manager Background Camera");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static Camera CreateBackgroundCamera()
    {
        Camera camera = new GameObject("BackgroundCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        return camera;
    }

    internal static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
