#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Enforces that entering Play Mode in the Unity Editor always starts from BoostrapScene,
/// regardless of which scene is currently open or being edited.
/// </summary>
[InitializeOnLoad]
public static class BootstrapSceneAutoLoader
{
    private const string BootstrapScenePath = "Assets/Scenes/BoostrapScene.unity";

    static BootstrapSceneAutoLoader()
    {
        SetPlayModeStartScene();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void SetPlayModeStartScene()
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        if (sceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
        else
        {
            Debug.LogWarning($"[BootstrapSceneAutoLoader] Scene not found at path: {BootstrapScenePath}");
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            SetPlayModeStartScene();
        }
    }
}
#endif
