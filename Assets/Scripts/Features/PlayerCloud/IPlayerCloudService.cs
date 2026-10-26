using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerCloud
{
    /// <summary>
    /// Service boundary managing player cloud data synchronization with Unity Gaming Services.
    /// Exposes cached snapshot access, single-flight loading, and game result submissions.
    /// </summary>
    public interface IPlayerCloudService
    {
        CloudSyncState State { get; }
        GameCenterSnapshot CachedSnapshot { get; }

        Task<CloudOperationResult<GameCenterSnapshot>> LoadSnapshotAsync(
            CancellationToken cancellationToken = default);

        Task<CloudOperationResult<SubmitGameResultResponse>> SubmitGameResultAsync(
            GameResult result,
            CancellationToken cancellationToken = default);

        Task<CloudOperationResult<GameCenterSnapshot>> ResetForIdentityChangeAsync(
            CancellationToken cancellationToken = default);
    }
}
