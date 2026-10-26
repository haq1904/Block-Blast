using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerIdentity
{
    public interface IPlayerIdentityService
    {
        PlayerIdentitySnapshot Snapshot { get; }
        event Action<PlayerIdentitySnapshot> StateChanged;

        Task<PlayerIdentityResult> RefreshAsync(CancellationToken cancellationToken = default);
        Task<PlayerIdentityResult> LinkGooglePlayGamesAsync(CancellationToken cancellationToken = default);
        Task<PlayerIdentityResult> ResolveConflictAsync(
            AccountLinkConflictResolution resolution,
            CancellationToken cancellationToken = default);
    }
}
