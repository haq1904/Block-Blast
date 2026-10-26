using System;

namespace BlockBlast.Features.PlayerIdentity
{
    public enum PlayerIdentityStatus
    {
        Unavailable,
        Anonymous,
        Linked,
        Linking,
        Conflict,
        Switching,
        Error
    }

    public enum PlayerIdentityFailureKind
    {
        None,
        Cancelled,
        UnsupportedPlatform,
        ProviderUnavailable,
        Offline,
        AuthenticationUnavailable,
        AccountAlreadyLinked,
        PendingResults,
        InvalidResponse,
        Unknown
    }

    public enum AccountLinkConflictResolution
    {
        Cancel,
        KeepCurrentProgress,
        SwitchToExistingAccount
    }

    public sealed class PlayerIdentitySnapshot
    {
        public PlayerIdentityStatus Status { get; }
        public PlayerIdentityFailureKind FailureKind { get; }
        public string ErrorCode { get; }
        public bool IsLinked { get; }
        public string ProviderDisplayName { get; }

        public PlayerIdentitySnapshot(
            PlayerIdentityStatus status,
            bool isLinked,
            PlayerIdentityFailureKind failureKind = PlayerIdentityFailureKind.None,
            string errorCode = "",
            string providerDisplayName = "Google Play Games")
        {
            Status = status;
            IsLinked = isLinked;
            FailureKind = failureKind;
            ErrorCode = errorCode ?? string.Empty;
            ProviderDisplayName = providerDisplayName ?? "Google Play Games";
        }

        public static PlayerIdentitySnapshot Unavailable(string errorCode = "") =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Unavailable, false, PlayerIdentityFailureKind.AuthenticationUnavailable, errorCode);

        public static PlayerIdentitySnapshot Anonymous() =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Anonymous, false);

        public static PlayerIdentitySnapshot Linked(string providerName = "Google Play Games") =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Linked, true, PlayerIdentityFailureKind.None, string.Empty, providerName);

        public static PlayerIdentitySnapshot Linking(bool isLinked = false) =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Linking, isLinked);

        public static PlayerIdentitySnapshot Conflict() =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Conflict, false, PlayerIdentityFailureKind.AccountAlreadyLinked, "ACCOUNT_ALREADY_LINKED");

        public static PlayerIdentitySnapshot Switching() =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Switching, false);

        public static PlayerIdentitySnapshot Error(PlayerIdentityFailureKind kind, string errorCode, bool isLinked = false) =>
            new PlayerIdentitySnapshot(PlayerIdentityStatus.Error, isLinked, kind, errorCode);
    }

    public readonly struct PlayerIdentityResult
    {
        public bool IsSuccess { get; }
        public PlayerIdentitySnapshot Snapshot { get; }
        public PlayerIdentityFailureKind FailureKind { get; }
        public string ErrorCode { get; }

        private PlayerIdentityResult(
            bool isSuccess,
            PlayerIdentitySnapshot snapshot,
            PlayerIdentityFailureKind failureKind,
            string errorCode)
        {
            IsSuccess = isSuccess;
            Snapshot = snapshot;
            FailureKind = failureKind;
            ErrorCode = errorCode ?? string.Empty;
        }

        public static PlayerIdentityResult Success(PlayerIdentitySnapshot snapshot) =>
            new PlayerIdentityResult(true, snapshot, PlayerIdentityFailureKind.None, string.Empty);

        public static PlayerIdentityResult Failure(
            PlayerIdentitySnapshot snapshot,
            PlayerIdentityFailureKind failureKind,
            string errorCode) =>
            new PlayerIdentityResult(false, snapshot, failureKind, errorCode);
    }
}
