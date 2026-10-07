using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Build index of the target scene to load after bootstrap initialization.")]
    [SerializeField] private int _targetSceneBuildIndex = 1;

    [Tooltip("Scene load mode.")]
    [SerializeField] private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;

    [Tooltip("Minimum visible duration in seconds for the initial transition.")]
    [SerializeField] private float _minimumVisibleDuration = 0f;

    [Header("Visual Clear Configuration")]
    [Tooltip("Clear color used by the bootstrap camera to prevent stale GPU framebuffer artifacts from previous play sessions.")]
    [SerializeField] private Color _clearColor = new Color(0.12f, 0.13f, 0.16f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeDOTweenCapacity()
    {
        DG.Tweening.DOTween.SetTweensCapacity(1000, 400);
    }

    private void Awake()
    {
        EnsureBootstrapCamera();
    }

    private async void Start()
    {
        try
        {
            if (!ServiceLocator.TryGet<ISceneService>(out var sceneService))
            {
                Debug.LogError("[Bootstrapper] ISceneService not found in ServiceLocator. Ensure SceneController is configured in the bootstrap scene.", this);
                return;
            }

            var request = new SceneLoadRequest(_targetSceneBuildIndex, _loadSceneMode, _minimumVisibleDuration);
            await sceneService.LoadSceneAsync(request);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
        }
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
}
