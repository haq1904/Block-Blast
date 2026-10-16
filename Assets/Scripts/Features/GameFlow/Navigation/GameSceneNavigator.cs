using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockBlast.Features.GameFlow.Navigation
{
    /// <summary>
    /// Persistent component implementing IGameSceneNavigator, resolving routes via catalog and forwarding to ISceneService.
    /// </summary>
    public sealed class GameSceneNavigator : MonoBehaviour, IGameSceneNavigator
    {
        [Tooltip("Route catalog mapping GameSceneRoute enums to build indexes.")]
        [SerializeField] private GameSceneRouteCatalogSO _routeCatalog;

        private bool _isRegistered;

        public bool IsTransitioning => ServiceLocator.TryGet<ISceneService>(out var sceneService) && sceneService.IsTransitioning;

        public GameSceneRouteCatalogSO RouteCatalog
        {
            get => _routeCatalog;
            set => _routeCatalog = value;
        }

        private void Awake()
        {
            if (ServiceLocator.TryGet<IGameSceneNavigator>(out _))
            {
                Debug.LogWarning("[GameSceneNavigator] An IGameSceneNavigator is already registered. Destroying duplicate instance.", this);
                Destroy(gameObject);
                return;
            }

            ServiceLocator.Register<IGameSceneNavigator>(this);
            _isRegistered = true;
        }

        public async Task LoadAsync(
            GameSceneRoute route,
            SceneTransitionPresentation presentation,
            float minimumVisibleDuration = 0f,
            CancellationToken cancellationToken = default)
        {
            if (_routeCatalog == null)
            {
                throw new InvalidOperationException("[GameSceneNavigator] RouteCatalog is not assigned.");
            }

            if (!ServiceLocator.TryGet<ISceneService>(out var sceneService))
            {
                throw new InvalidOperationException("[GameSceneNavigator] ISceneService not found in ServiceLocator.");
            }

            int buildIndex = _routeCatalog.GetBuildIndex(route);
            var request = new SceneLoadRequest(buildIndex, LoadSceneMode.Single, minimumVisibleDuration, presentation);

            await sceneService.LoadSceneAsync(request, cancellationToken);
        }

        private void OnDestroy()
        {
            if (_isRegistered)
            {
                ServiceLocator.Unregister<IGameSceneNavigator>();
                _isRegistered = false;
            }
        }
    }
}
