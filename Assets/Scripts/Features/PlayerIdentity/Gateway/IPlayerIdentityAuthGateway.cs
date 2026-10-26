using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerIdentity.Gateway
{
    public readonly struct PlayerIdentityInfoResult
    {
        public bool IsSuccess { get; }
        public bool HasGooglePlayGamesIdentity { get; }
        public string PlayerId { get; }
        public PlayerIdentityFailureKind FailureKind { get; }
        public string ErrorCode { get; }

        private PlayerIdentityInfoResult(
            bool isSuccess,
            bool hasGooglePlayGamesIdentity,
            string playerId,
            PlayerIdentityFailureKind failureKind,
            string errorCode)
        {
            IsSuccess = isSuccess;
            HasGooglePlayGamesIdentity = hasGooglePlayGamesIdentity;
            PlayerId = playerId ?? string.Empty;
            FailureKind = failureKind;
            ErrorCode = errorCode ?? string.Empty;
        }

        public static PlayerIdentityInfoResult Success(bool hasGooglePlayGamesIdentity, string playerId) =>
            new PlayerIdentityInfoResult(true, hasGooglePlayGamesIdentity, playerId, PlayerIdentityFailureKind.None, string.Empty);

        public static PlayerIdentityInfoResult Failure(PlayerIdentityFailureKind failureKind, string errorCode) =>
            new PlayerIdentityInfoResult(false, false, string.Empty, failureKind, errorCode);
    }

    public readonly struct AuthGatewayResult
    {
        public bool IsSuccess { get; }
        public string PlayerId { get; }
        public PlayerIdentityFailureKind FailureKind { get; }
        public string ErrorCode { get; }

        private AuthGatewayResult(
            bool isSuccess,
            string playerId,
            PlayerIdentityFailureKind failureKind,
            string errorCode)
        {
            IsSuccess = isSuccess;
            PlayerId = playerId ?? string.Empty;
            FailureKind = failureKind;
            ErrorCode = errorCode ?? string.Empty;
        }

        public static AuthGatewayResult Success(string playerId) =>
            new AuthGatewayResult(true, playerId, PlayerIdentityFailureKind.None, string.Empty);

        public static AuthGatewayResult Failure(PlayerIdentityFailureKind failureKind, string errorCode) =>
            new AuthGatewayResult(false, string.Empty, failureKind, errorCode);
    }

    public interface IPlayerIdentityAuthGateway
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }

        Task<PlayerIdentityInfoResult> GetPlayerInfoAsync(CancellationToken cancellationToken = default);
        Task<AuthGatewayResult> LinkWithGooglePlayGamesAsync(string authCode, bool forceLink, CancellationToken cancellationToken = default);
        Task<AuthGatewayResult> SignInWithGooglePlayGamesAsync(string authCode, CancellationToken cancellationToken = default);
        Task<AuthGatewayResult> SignInAnonymouslyAsync(CancellationToken cancellationToken = default);
        void SignOut(bool clearCredentials);
    }
}
