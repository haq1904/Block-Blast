using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Build index of the target scene to load after bootstrap initialization.")]
    [SerializeField] private int _targetSceneBuildIndex = 1;

    [Tooltip("Scene load mode.")]
    [SerializeField] private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;

    [Header("Visual Clear Configuration")]
    [Tooltip("Clear color used by the bootstrap camera to prevent stale GPU framebuffer artifacts from previous play sessions.")]
    [SerializeField] private Color _clearColor = new Color(0.12f, 0.13f, 0.16f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeDOTweenCapacity()
    {
        DG.Tweening.DOTween.SetTweensCapacity(500, 150);
    }

    private void Awake()
    {
        EnsureBootstrapCamera();
    }

    private void Start()
    {
        LoadNextScene();
    }

    private void EnsureBootstrapCamera()
    {
        if (Camera.main == null && FindAnyObjectByType<Camera>() == null)
        {
            GameObject camObj = new GameObject("Bootstrap Camera");
            camObj.transform.SetParent(transform);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _clearColor;
            cam.cullingMask = 0; // Clear framebuffer without rendering any geometry
        }
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
