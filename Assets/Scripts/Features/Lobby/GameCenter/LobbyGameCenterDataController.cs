using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Core;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerSession;
using UnityEngine;

namespace BlockBlast.Features.Lobby.GameCenter
{
    /// <summary>
    /// Coordinates data lifecycle for the Player Center panel.
    /// Responds to panel open events by triggering best-effort pending result flushes,
    /// loading scoped player cloud snapshots, and mapping domain models to passive presentation views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LobbyGameCenterDataController : MonoBehaviour
    {
        [SerializeField] private LobbyGameCenterPanelView _panelView;

        private IPlayerCloudService _cloudService;
        private IGameResultSubmissionService _submissionService;
        private CancellationTokenSource _cts;

        public LobbyGameCenterPanelView PanelView
        {
            get => _panelView;
            set => _panelView = value;
        }

        public void Initialize(
            IPlayerCloudService cloudService,
            LobbyGameCenterPanelView panelView,
            IGameResultSubmissionService submissionService = null)
        {
            _cloudService = cloudService;
            _panelView = panelView;
            _submissionService = submissionService;
        }

        private void Awake()
        {
            if (_panelView == null)
            {
                _panelView = GetComponent<LobbyGameCenterPanelView>();
            }
        }

        private void OnEnable()
        {
            if (_panelView != null)
            {
                _panelView.Opened -= HandlePanelOpened;
                _panelView.Opened += HandlePanelOpened;
            }
        }

        private void OnDisable()
        {
            if (_panelView != null)
            {
                _panelView.Opened -= HandlePanelOpened;
            }

            CancelInFlight();
        }

        private void OnDestroy()
        {
            CancelInFlight();
        }

        private void HandlePanelOpened()
        {
            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            EnsureDependencies();

            CancelInFlight();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            int pendingCount = _submissionService != null ? _submissionService.PendingCount : 0;

            if (_cloudService != null && _cloudService.CachedSnapshot != null &&
                (_cloudService.CachedSnapshot.Revision > 0 || _cloudService.CachedSnapshot.LifetimeStats.GamesPlayed > 0))
            {
                _panelView?.Render(GameCenterViewDataMapper.Map(
                    _cloudService.CachedSnapshot,
                    statusOverride: "Syncing...",
                    pendingCount: pendingCount));
            }
            else
            {
                _panelView?.Render(GameCenterViewDataMapper.CreateEmptyViewData(statusText: "Syncing..."));
            }

            if (_cloudService == null)
            {
                _panelView?.Render(GameCenterViewDataMapper.CreateEmptyViewData(statusText: "Offline"));
                return;
            }

            try
            {
                if (_submissionService != null && _submissionService.PendingCount > 0)
                {
                    try
                    {
                        await _submissionService.FlushPendingAsync(token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LobbyGameCenterDataController] Best-effort pending flush encountered non-fatal error: {ex.Message}");
                    }
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                pendingCount = _submissionService != null ? _submissionService.PendingCount : 0;

                var loadResult = await _cloudService.LoadSnapshotAsync(token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (loadResult.IsSuccess && loadResult.Value != null)
                {
                    _panelView?.Render(GameCenterViewDataMapper.Map(
                        loadResult.Value,
                        statusOverride: "Synced",
                        pendingCount: pendingCount));
                }
                else
                {
                    if (_cloudService.CachedSnapshot != null &&
                        (_cloudService.CachedSnapshot.Revision > 0 || _cloudService.CachedSnapshot.LifetimeStats.GamesPlayed > 0))
                    {
                        _panelView?.Render(GameCenterViewDataMapper.Map(
                            _cloudService.CachedSnapshot,
                            statusOverride: null,
                            pendingCount: pendingCount));
                    }
                    else
                    {
                        _panelView?.Render(GameCenterViewDataMapper.CreateEmptyViewData(statusText: "Offline"));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation cancelled gracefully
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LobbyGameCenterDataController] Failed to refresh GameCenter data: {ex.Message}");
                if (!token.IsCancellationRequested)
                {
                    _panelView?.Render(GameCenterViewDataMapper.CreateEmptyViewData(statusText: "Unavailable"));
                }
            }
        }

        private void EnsureDependencies()
        {
            if (_cloudService == null)
            {
                if (ServiceLocator.TryGet<IPlayerCloudService>(out var resolvedCloud))
                {
                    _cloudService = resolvedCloud;
                }
            }

            if (_submissionService == null)
            {
                if (ServiceLocator.TryGet<IGameResultSubmissionService>(out var resolvedSubmission))
                {
                    _submissionService = resolvedSubmission;
                }
            }
        }

        private void CancelInFlight()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
