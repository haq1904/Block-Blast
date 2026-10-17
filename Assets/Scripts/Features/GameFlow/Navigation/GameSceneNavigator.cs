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
            if (ServiceLocator.TryGet<IGameSceneNavigator>(out var existing) && existing != null && !ReferenceEquals(existing, this))
            {
                Debug.LogWarning("[GameSceneNavigator] An active IGameSceneNavigator is already registered. Destroying duplicate component.", this);
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

            if (route == GameSceneRoute.Intro)
            {
                int introIndex = _routeCatalog.GetBuildIndex(GameSceneRoute.Intro);
                var request = new SceneLoadRequest(introIndex, LoadSceneMode.Single, minimumVisibleDuration, presentation);
                await sceneService.LoadSceneAsync(request, cancellationToken);
                return;
            }

            int worldIndex = _routeCatalog.WorldBuildIndex;
            int targetIndex = _routeCatalog.GetBuildIndex(route);

            int[] loadIndexes;
            int[] unloadIndexes;

            if (route == GameSceneRoute.Lobby)
            {
                int introIndex = _routeCatalog.GetBuildIndex(GameSceneRoute.Intro);
                int playIndex = _routeCatalog.GetBuildIndex(GameSceneRoute.Play);
                loadIndexes = new[] { worldIndex, targetIndex };
                unloadIndexes = new[] { introIndex, playIndex };
            }
            else if (route == GameSceneRoute.Play)
            {
                int introIndex = _routeCatalog.GetBuildIndex(GameSceneRoute.Intro);
                int lobbyIndex = _routeCatalog.GetBuildIndex(GameSceneRoute.Lobby);
                loadIndexes = new[] { worldIndex, targetIndex };
                unloadIndexes = new[] { lobbyIndex, introIndex };
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(route), route, "Unsupported GameSceneRoute value.");
            }

            var additiveRequest = new AdditiveSceneTransitionRequest(
                targetSceneBuildIndex: targetIndex,
                loadBuildIndexes: loadIndexes,
                unloadBuildIndexes: unloadIndexes,
                activeSceneBuildIndex: worldIndex,
                minimumVisibleDuration: minimumVisibleDuration,
                presentation: presentation);

            await sceneService.TransitionAdditiveAsync(additiveRequest, cancellationToken);
        }

        private void OnDestroy()
        {
            if (_isRegistered)
            {
                ServiceLocator.Unregister<IGameSceneNavigator>(this);
                _isRegistered = false;
            }
        }
    }
}
