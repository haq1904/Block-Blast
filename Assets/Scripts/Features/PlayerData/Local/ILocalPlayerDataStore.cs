namespace BlockBlast.Features.PlayerData
{
    public interface ILocalPlayerDataStore
    {
        int LoadHighScore();
        bool SaveHighScore(int newScore);
        long LoadLastCloudSyncUnixSeconds();
        void SaveLastCloudSyncUnixSeconds(long unixSeconds);
        long LoadCachedSnapshotRevision();
        void SaveCachedSnapshotRevision(long revision);
    }
}
