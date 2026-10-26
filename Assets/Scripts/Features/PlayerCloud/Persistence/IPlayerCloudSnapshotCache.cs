using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerCloud.Persistence
{
    public interface IPlayerCloudSnapshotCache
    {
        bool TryLoad(out GameCenterSnapshot snapshot);
        bool Save(GameCenterSnapshot snapshot);
        void Clear();
    }
}
