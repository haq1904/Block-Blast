using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Persistent MonoBehaviour managing scene transitions and publishing ISceneService to ServiceLocator.
/// </summary>
public sealed class SceneController : MonoBehaviour, ISceneService
{
    [Tooltip("Optional presentation component for transition animations. If unassigned, transitions operate without visual overlay.")]
    [SerializeField] private BaseLoadingScreen _loadingScreen;

    private SceneTransitionRunner _runner;
    private bool _isRegistered;
    private readonly CancellationTokenSource _destroyCts = new CancellationTokenSource();

    public bool IsTransitioning => _runner != null && _runner.IsTransitioning;

    private void Awake()
    {
        if (ServiceLocator.TryGet<ISceneService>(out _))
        {
            Debug.LogWarning("[SceneController] An ISceneService is already registered. Destroying duplicate instance.", this);
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        var backend = new UnitySceneLoadingBackend();
        ILoadingScreen screen = _loadingScreen != null ? (ILoadingScreen)_loadingScreen : NullLoadingScreen.Instance;
        _runner = new SceneTransitionRunner(backend, screen, backend);

        ServiceLocator.Register<ISceneService>(this);
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

    private void OnDestroy()
    {
        _destroyCts.Cancel();
        _destroyCts.Dispose();

        if (_isRegistered)
        {
            ServiceLocator.Unregister<ISceneService>();
            _isRegistered = false;
        }

        if (_loadingScreen != null)
        {
            _loadingScreen.ResetImmediate();
        }
    }
}
