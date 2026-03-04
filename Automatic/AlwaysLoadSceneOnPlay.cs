#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AlwaysLoadSceneOnPlay
{
    private const string PreviousScenePathKey = "AlwaysLoadSceneOnPlay.PreviousScenePath";
    private const string EnabledKey = "AlwaysLoadSceneOnPlay.Enabled";
    private const string ScenePathKey = "AlwaysLoadSceneOnPlay.ScenePath";

    static AlwaysLoadSceneOnPlay()
    {
        if (IsEnabled())
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
    }

    // Menu items
    [MenuItem("Tools/Always Load Scene On Play/Enable", true)]
    private static bool ShowEnable() => !IsEnabled();

    [MenuItem("Tools/Always Load Scene On Play/Enable")]
    private static void Enable()
    {
        SetEnabled(true);
    }

    [MenuItem("Tools/Always Load Scene On Play/Disable", true)]
    private static bool ShowDisable() => IsEnabled();

    [MenuItem("Tools/Always Load Scene On Play/Disable")]
    private static void Disable()
    {
        SetEnabled(false);
    }

    [MenuItem("Tools/Always Load Scene On Play/Set Scene Path")]
    private static void SetScenePath()
    {
        string currentPath = EditorPrefs.GetString(ScenePathKey, "");
        string newPath = EditorUtility.OpenFilePanel("Select Scene to Load on Play", "Assets", "unity");

        if (!string.IsNullOrEmpty(newPath))
        {
            if (newPath.StartsWith(Application.dataPath))
            {
                newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                EditorPrefs.SetString(ScenePathKey, newPath);
                Debug.Log("Always Load Scene Path set to: " + newPath);
            }
            else
            {
                Debug.LogError("Scene must be inside the Assets folder.");
            }
        }
    }

    private static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(EnabledKey, enabled);
        if (enabled)
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Debug.Log("Always Load Scene On Play: Enabled");
        }
        else
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Debug.Log("Always Load Scene On Play: Disabled");
        }
    }

    private static bool IsEnabled()
    {
        return EditorPrefs.GetBool(EnabledKey, false);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!IsEnabled()) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            string previousScenePath = SceneManager.GetActiveScene().path;
            EditorPrefs.SetString(PreviousScenePathKey, previousScenePath);

            string scenePath = EditorPrefs.GetString(ScenePathKey, "");
            if (!string.IsNullOrEmpty(scenePath))
            {
                if (SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0)
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scenePath);
                }
                else
                {
                    Debug.LogError("Scene is not in the build settings: " + scenePath);
                }
            }
            else
            {
                Debug.LogWarning("No Always Load Scene path set. Use Tools → Always Load Scene On Play → Set Scene Path.");
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (EditorPrefs.HasKey(PreviousScenePathKey))
            {
                string previousScenePath = EditorPrefs.GetString(PreviousScenePathKey);
                if (!string.IsNullOrEmpty(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath);
                }
                EditorPrefs.DeleteKey(PreviousScenePathKey);
            }
        }
    }
}
#endif