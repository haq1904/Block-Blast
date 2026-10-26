using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerIdentity.Gateway
{
    public readonly struct AuthCodeFetchResult
    {
        public bool IsSuccess { get; }
        public string AuthCode { get; }
        public PlayerIdentityFailureKind FailureKind { get; }
        public string ErrorCode { get; }

        private AuthCodeFetchResult(
            bool isSuccess,
            string authCode,
            PlayerIdentityFailureKind failureKind,
            string errorCode)
        {
            IsSuccess = isSuccess;
            AuthCode = authCode ?? string.Empty;
            FailureKind = failureKind;
            ErrorCode = errorCode ?? string.Empty;
        }

        public static AuthCodeFetchResult Success(string authCode) =>
            new AuthCodeFetchResult(true, authCode, PlayerIdentityFailureKind.None, string.Empty);

        public static AuthCodeFetchResult Failure(PlayerIdentityFailureKind failureKind, string errorCode) =>
            new AuthCodeFetchResult(false, string.Empty, failureKind, errorCode);
    }

    public interface IPlatformAuthCodeProvider
    {
        bool IsSupported { get; }
        Task<AuthCodeFetchResult> RequestServerAuthCodeAsync(bool isManual, CancellationToken cancellationToken = default);
    }
}
