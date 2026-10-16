using System;
using BlockBlast.Features.GameFlow.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Logical route destination to load after bootstrap initialization.")]
    [SerializeField] private GameSceneRoute _targetRoute = GameSceneRoute.Intro;

    [Tooltip("Presentation mode for initial scene transition.")]
    [SerializeField] private SceneTransitionPresentation _presentation = SceneTransitionPresentation.None;

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
            if (ServiceLocator.TryGet<IGameSceneNavigator>(out var navigator))
            {
                await navigator.LoadAsync(_targetRoute, _presentation, _minimumVisibleDuration);
            }
            else if (ServiceLocator.TryGet<ISceneService>(out var sceneService))
            {
                var request = new SceneLoadRequest(1, LoadSceneMode.Single, _minimumVisibleDuration, _presentation);
                await sceneService.LoadSceneAsync(request);
            }
            else
            {
                Debug.LogError("[Bootstrapper] Neither IGameSceneNavigator nor ISceneService found in ServiceLocator. Ensure SceneController and GameSceneNavigator are configured in the bootstrap scene.", this);
            }
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
