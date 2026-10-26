#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace BlockBlast.Features.PlayerIdentity.Gateway
{
    public sealed class GooglePlayGamesAuthCodeProvider : IPlatformAuthCodeProvider
    {
        private static bool _activated;

        public bool IsSupported => true;

        public async Task<AuthCodeFetchResult> RequestServerAuthCodeAsync(
            bool isManual,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.Cancelled, "CANCELLED");
            }

            if (!_activated)
            {
                PlayGamesPlatform.Activate();
                _activated = true;
            }

            var authTcs = new TaskCompletionSource<SignInStatus>();
            Action<SignInStatus> callback = status => authTcs.TrySetResult(status);

            if (isManual)
            {
                PlayGamesPlatform.Instance.ManuallyAuthenticate(callback);
            }
            else
            {
                PlayGamesPlatform.Instance.Authenticate(callback);
            }

            using (cancellationToken.Register(() => authTcs.TrySetCanceled()))
            {
                SignInStatus status;
                try
                {
                    status = await authTcs.Task;
                }
                catch (OperationCanceledException)
                {
                    return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.Cancelled, "CANCELLED");
                }

                if (status == SignInStatus.Canceled)
                {
                    return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.Cancelled, "AUTH_CANCELLED");
                }

                if (status != SignInStatus.Success)
                {
                    return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.ProviderUnavailable, "AUTH_FAILED_" + status);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.Cancelled, "CANCELLED");
            }

            var codeTcs = new TaskCompletionSource<string>();
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, code => codeTcs.TrySetResult(code));

            using (cancellationToken.Register(() => codeTcs.TrySetCanceled()))
            {
                try
                {
                    string authCode = await codeTcs.Task;
                    if (string.IsNullOrEmpty(authCode))
                    {
                        return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.InvalidResponse, "EMPTY_AUTH_CODE");
                    }

                    return AuthCodeFetchResult.Success(authCode);
                }
                catch (OperationCanceledException)
                {
                    return AuthCodeFetchResult.Failure(PlayerIdentityFailureKind.Cancelled, "CANCELLED");
                }
            }
        }
    }
}
#else
using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerIdentity.Gateway
{
    public sealed class GooglePlayGamesAuthCodeProvider : IPlatformAuthCodeProvider
    {
        public bool IsSupported => false;

        public Task<AuthCodeFetchResult> RequestServerAuthCodeAsync(
            bool isManual,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AuthCodeFetchResult.Failure(
                PlayerIdentityFailureKind.UnsupportedPlatform,
                "UNSUPPORTED_PLATFORM"));
        }
    }
}
#endif
