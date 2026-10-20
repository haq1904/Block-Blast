using System;
using BlockBlast.Features.GameFlow.Navigation;
#if UNITY_EDITOR
using BlockBlast.Features.GameFlow.EditorSupport;
#endif
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

    private Camera _bootstrapCamera;
    private AudioListener _bootstrapAudioListener;
    private bool _bootstrapCameraWasEnabled;
    private bool _bootstrapAudioListenerWasEnabled;
    private bool _bootstrapPresentationSuppressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeDOTweenCapacity()
    {
        DG.Tweening.DOTween.SetTweensCapacity(1000, 400);
    }

    private void Awake()
    {
        EnsureBootstrapCamera();
        _bootstrapCamera = GetComponentInChildren<Camera>(true);
        _bootstrapAudioListener = GetComponentInChildren<AudioListener>(true);
    }

    private async void Start()
    {
        try
        {
            GameSceneRoute targetRoute = ResolveTargetRoute();

            if (ServiceLocator.TryGet<IGameSceneNavigator>(out var navigator))
            {
                if (UsesAdditiveComposition(targetRoute))
                {
                    SuppressBootstrapPresentation();
                }

                await navigator.LoadAsync(targetRoute, _presentation, _minimumVisibleDuration);
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
            RestoreBootstrapPresentation();
            Debug.LogException(ex, this);
        }
    }

    private static bool UsesAdditiveComposition(GameSceneRoute route)
    {
        return route == GameSceneRoute.Lobby || route == GameSceneRoute.Play;
    }

    private void SuppressBootstrapPresentation()
    {
        if (_bootstrapPresentationSuppressed)
        {
            return;
        }

        if (_bootstrapCamera != null)
        {
            _bootstrapCameraWasEnabled = _bootstrapCamera.enabled;
            _bootstrapCamera.enabled = false;
        }

        if (_bootstrapAudioListener != null)
        {
            _bootstrapAudioListenerWasEnabled = _bootstrapAudioListener.enabled;
            _bootstrapAudioListener.enabled = false;
        }

        _bootstrapPresentationSuppressed = true;
    }

    private void RestoreBootstrapPresentation()
    {
        if (!_bootstrapPresentationSuppressed)
        {
            return;
        }

        if (_bootstrapCamera != null)
        {
            _bootstrapCamera.enabled = _bootstrapCameraWasEnabled;
        }

        if (_bootstrapAudioListener != null)
        {
            _bootstrapAudioListener.enabled = _bootstrapAudioListenerWasEnabled;
        }

        _bootstrapPresentationSuppressed = false;
    }

    private GameSceneRoute ResolveTargetRoute()
    {
#if UNITY_EDITOR
        int rawProfile = UnityEditor.EditorPrefs.GetInt(
            EditorSceneLaunchOverride.ProfilePreferenceKey,
            (int)SceneQuickAccessProfile.FullFlow);
        SceneQuickAccessProfile profile = EditorSceneLaunchOverride.SanitizeProfile(rawProfile);
        return EditorSceneLaunchOverride.ResolveRoute(_targetRoute, profile);
#else
        return _targetRoute;
#endif
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
