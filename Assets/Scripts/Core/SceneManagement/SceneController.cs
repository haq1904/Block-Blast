using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Persistent MonoBehaviour managing scene transitions and publishing ISceneService to ServiceLocator.
/// </summary>
public sealed class SceneController : MonoBehaviour, ISceneService, ILoadingScreenService
{
    [Tooltip("Optional presentation component for transition animations. If unassigned, transitions operate without visual overlay.")]
    [SerializeField] private BaseLoadingScreen _loadingScreen;

    private SceneTransitionRunner _runner;
    private bool _isRegistered;
    private readonly CancellationTokenSource _destroyCts = new CancellationTokenSource();

    public bool IsTransitioning => _runner != null && _runner.IsTransitioning;

    private void Awake()
    {
        if (ServiceLocator.TryGet<ISceneService>(out var existing) && existing != null && !ReferenceEquals(existing, this))
        {
            Debug.LogWarning("[SceneController] An active ISceneService is already registered. Destroying duplicate component.", this);
            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
            return;
        }

        DontDestroyOnLoad(gameObject);

        var backend = new UnitySceneLoadingBackend();
        ILoadingScreen screen = _loadingScreen != null ? (ILoadingScreen)_loadingScreen : NullLoadingScreen.Instance;
        _runner = new SceneTransitionRunner(backend, screen, backend);

        ServiceLocator.Register<ISceneService>(this);
        ServiceLocator.Register<ILoadingScreenService>(this);
        _isRegistered = true;
    }

    public async Task LoadSceneAsync(SceneLoadRequest request, CancellationToken cancellationToken = default)
    {
        if (_runner == null)
        {
            throw new InvalidOperationException("[SceneController] Runner is not initialized.");
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyCts.Token);
        await _runner.RunAsync(request, linkedCts.Token);
    }

    public async Task TransitionAdditiveAsync(AdditiveSceneTransitionRequest request, CancellationToken cancellationToken = default)
    {
        if (_runner == null)
        {
            throw new InvalidOperationException("[SceneController] Runner is not initialized.");
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyCts.Token);
        await _runner.RunAdditiveAsync(request, linkedCts.Token);
    }

    public async Task RunCoveredOperationAsync(Func<CancellationToken, Task> coveredOperation, CancellationToken cancellationToken = default)
    {
        if (_runner == null)
        {
            throw new InvalidOperationException("[SceneController] Runner is not initialized.");
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyCts.Token);
        await _runner.RunCoveredOperationAsync(coveredOperation, linkedCts.Token);
    }

    private void OnDestroy()
    {
        _destroyCts.Cancel();
        _destroyCts.Dispose();

        if (_isRegistered)
        {
            ServiceLocator.Unregister<ISceneService>(this);
            ServiceLocator.Unregister<ILoadingScreenService>(this);
            _isRegistered = false;
        }

        if (_loadingScreen != null)
        {
            _loadingScreen.ResetImmediate();
        }
    }
}
