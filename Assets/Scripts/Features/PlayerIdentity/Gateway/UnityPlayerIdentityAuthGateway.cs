using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace BlockBlast.Features.PlayerIdentity.Gateway
{
    public sealed class UnityPlayerIdentityAuthGateway : IPlayerIdentityAuthGateway
    {
        private IAuthenticationService Service => AuthenticationService.Instance;

        public bool IsSignedIn => Service != null && Service.IsSignedIn;

        public string PlayerId => Service != null && Service.IsSignedIn ? Service.PlayerId : string.Empty;

        public async Task<PlayerIdentityInfoResult> GetPlayerInfoAsync(CancellationToken cancellationToken = default)
        {
            if (!IsSignedIn)
            {
                return PlayerIdentityInfoResult.Failure(
                    PlayerIdentityFailureKind.AuthenticationUnavailable,
                    "NOT_SIGNED_IN");
            }

            try
            {
                var info = await Service.GetPlayerInfoAsync();
                bool hasGooglePlay = info?.Identities != null && info.Identities.Any(i =>
                    string.Equals(i.TypeId, "google.play.games", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.TypeId, "googleplaygames", StringComparison.OrdinalIgnoreCase));

                return PlayerIdentityInfoResult.Success(hasGooglePlay, Service.PlayerId);
            }
            catch (Exception ex)
            {
                var classified = ClassifyException(ex);
                return PlayerIdentityInfoResult.Failure(classified.kind, classified.code);
            }
        }

        public async Task<AuthGatewayResult> LinkWithGooglePlayGamesAsync(
            string authCode,
            bool forceLink,
            CancellationToken cancellationToken = default)
        {
            if (!IsSignedIn)
            {
                return AuthGatewayResult.Failure(
                    PlayerIdentityFailureKind.AuthenticationUnavailable,
                    "NOT_SIGNED_IN");
            }

            try
            {
                var options = new LinkOptions { ForceLink = forceLink };
                await Service.LinkWithGooglePlayGamesAsync(authCode, options);
                return AuthGatewayResult.Success(Service.PlayerId);
            }
            catch (Exception ex)
            {
                var classified = ClassifyException(ex);
                return AuthGatewayResult.Failure(classified.kind, classified.code);
            }
        }

        public async Task<AuthGatewayResult> SignInWithGooglePlayGamesAsync(
            string authCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await Service.SignInWithGooglePlayGamesAsync(authCode);
                return AuthGatewayResult.Success(Service.PlayerId);
            }
            catch (Exception ex)
            {
                var classified = ClassifyException(ex);
                return AuthGatewayResult.Failure(classified.kind, classified.code);
            }
        }

        public async Task<AuthGatewayResult> SignInAnonymouslyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Service.SignInAnonymouslyAsync();
                return AuthGatewayResult.Success(Service.PlayerId);
            }
            catch (Exception ex)
            {
                var classified = ClassifyException(ex);
                return AuthGatewayResult.Failure(classified.kind, classified.code);
            }
        }

        public void SignOut(bool clearCredentials)
        {
            try
            {
                if (Service != null && Service.IsSignedIn)
                {
                    Service.SignOut(clearCredentials);
                }
            }
            catch
            {
                // Sign out must be safe and not throw
            }
        }

        private static (PlayerIdentityFailureKind kind, string code) ClassifyException(Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return (PlayerIdentityFailureKind.Cancelled, "CANCELLED");
            }

            if (ex is AuthenticationException authEx)
            {
                if (authEx.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                {
                    return (PlayerIdentityFailureKind.AccountAlreadyLinked, "ACCOUNT_ALREADY_LINKED");
                }

                if (authEx.ErrorCode == AuthenticationErrorCodes.ClientNoActiveSession ||
                    authEx.ErrorCode == AuthenticationErrorCodes.ClientInvalidUserState)
                {
                    return (PlayerIdentityFailureKind.AuthenticationUnavailable, "AUTH_UNAVAILABLE");
                }

                return (PlayerIdentityFailureKind.AuthenticationUnavailable, $"AUTH_ERROR_{authEx.ErrorCode}");
            }

            if (ex is RequestFailedException reqEx)
            {
                return (PlayerIdentityFailureKind.Offline, $"OFFLINE_{reqEx.ErrorCode}");
            }

            return (PlayerIdentityFailureKind.Unknown, "UNKNOWN_ERROR");
        }
    }
}
