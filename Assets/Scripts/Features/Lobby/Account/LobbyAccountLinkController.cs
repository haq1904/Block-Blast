using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerIdentity;
using UnityEngine;

namespace BlockBlast.Features.Lobby.Account
{
    public class LobbyAccountLinkController : MonoBehaviour
    {
        [SerializeField] private LobbyAccountLinkView _view;

        private IPlayerIdentityService _identityService;
        private CancellationTokenSource _operationCts;

        private bool IsPlatformSupported =>
#if UNITY_ANDROID && !UNITY_EDITOR
            true;
#else
            false;
#endif

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<LobbyAccountLinkView>() ?? GetComponentInChildren<LobbyAccountLinkView>(true);
            }
        }

        private void OnEnable()
        {
            _operationCts = new CancellationTokenSource();

            if (_view != null)
            {
                _view.LinkRequested += OnLinkRequested;
                _view.KeepProgressConfirmed += OnKeepProgressConfirmed;
                _view.SwitchProgressRequested += OnSwitchProgressRequested;
                _view.ConflictCancelled += OnConflictCancelled;
            }

            if (ServiceLocator.TryGet<IPlayerIdentityService>(out var identityService))
            {
                _identityService = identityService;
                _identityService.StateChanged += OnIdentityStateChanged;
                _view?.Render(_identityService.Snapshot, IsPlatformSupported);

                _ = RefreshAsync(_operationCts.Token);
            }
            else
            {
                _view?.Render(PlayerIdentitySnapshot.Unavailable(), IsPlatformSupported);
            }
        }

        private void OnDisable()
        {
            _operationCts?.Cancel();
            _operationCts?.Dispose();
            _operationCts = null;

            if (_view != null)
            {
                _view.LinkRequested -= OnLinkRequested;
                _view.KeepProgressConfirmed -= OnKeepProgressConfirmed;
                _view.SwitchProgressRequested -= OnSwitchProgressRequested;
                _view.ConflictCancelled -= OnConflictCancelled;
            }

            if (_identityService != null)
            {
                _identityService.StateChanged -= OnIdentityStateChanged;
                _identityService = null;
            }
        }

        private void OnIdentityStateChanged(PlayerIdentitySnapshot snapshot)
        {
            _view?.Render(snapshot, IsPlatformSupported);
        }

        private async Task RefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_identityService != null)
                {
                    await _identityService.RefreshAsync(cancellationToken);
                }
            }
            catch (Exception)
            {
                // Suppress exception on refresh to avoid breaking UI lifecycle
            }
        }

        private async void OnLinkRequested()
        {
            if (_identityService == null || _operationCts == null)
            {
                return;
            }

            try
            {
                _view?.SetLoading(true);
                var result = await _identityService.LinkGooglePlayGamesAsync(_operationCts.Token);
                _view?.SetLoading(false);

                if (!result.IsSuccess && result.FailureKind == PlayerIdentityFailureKind.AccountAlreadyLinked)
                {
                    _view?.ShowConflictModal(true);
                }
            }
            catch (Exception)
            {
                _view?.SetLoading(false);
            }
        }

        private async void OnKeepProgressConfirmed()
        {
            if (_identityService == null || _operationCts == null)
            {
                return;
            }

            try
            {
                _view?.SetLoading(true);
                var result = await _identityService.ResolveConflictAsync(
                    AccountLinkConflictResolution.KeepCurrentProgress,
                    _operationCts.Token);
                _view?.SetLoading(false);

                if (result.IsSuccess)
                {
                    _view?.ShowConflictModal(false);
                }
            }
            catch (Exception)
            {
                _view?.SetLoading(false);
            }
        }

        private async void OnSwitchProgressRequested()
        {
            if (_identityService == null || _operationCts == null)
            {
                return;
            }

            try
            {
                _view?.SetLoading(true);
                var result = await _identityService.ResolveConflictAsync(
                    AccountLinkConflictResolution.SwitchToExistingAccount,
                    _operationCts.Token);
                _view?.SetLoading(false);

                if (result.IsSuccess)
                {
                    _view?.ShowConflictModal(false);
                }
            }
            catch (Exception)
            {
                _view?.SetLoading(false);
            }
        }

        private async void OnConflictCancelled()
        {
            if (_identityService == null || _operationCts == null)
            {
                _view?.ShowConflictModal(false);
                return;
            }

            try
            {
                await _identityService.ResolveConflictAsync(
                    AccountLinkConflictResolution.Cancel,
                    _operationCts.Token);
            }
            catch (Exception)
            {
                // Suppress exception on cancel
            }
            finally
            {
                _view?.ShowConflictModal(false);
            }
        }
    }
}
