using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Build index of the target scene to load after bootstrap initialization.")]
    [SerializeField] private int _targetSceneBuildIndex = 1;

    [Tooltip("Scene load mode.")]
    [SerializeField] private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;

    private void Start()
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (_targetSceneBuildIndex < 0 || _targetSceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[Bootstrapper] Cannot load scene at index {_targetSceneBuildIndex}. Total scenes in Build Settings: {SceneManager.sceneCountInBuildSettings}. Please verify Build Settings.");
            return;
        }

        SceneManager.LoadSceneAsync(_targetSceneBuildIndex, _loadSceneMode);
    }
}
