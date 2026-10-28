using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerCloud.Persistence
{
    /// <summary>
    /// Contract for loading, saving, and clearing cached GameCenterSnapshots scoped to environment and player.
    /// </summary>
    public interface IPlayerCloudSnapshotCache
    {
        bool TryLoad(PlayerCloudCacheScope scope, out GameCenterSnapshot snapshot);
        bool Save(PlayerCloudCacheScope scope, GameCenterSnapshot snapshot);
        void Clear(PlayerCloudCacheScope scope);
    }
}
