namespace BlockBlast.Features.PlayerData
{
    public static class PlayerDataKeys
    {
        public const string LocalSchemaVersion = "BlockBlast.PlayerData.LocalSchemaVersion";
        public const string HighScoreV1 = "BlockBlast.PlayerData.HighScore.v1";
        public const string LastCloudSyncUnixSecondsV1 = "BlockBlast.PlayerData.LastCloudSyncUnixSeconds.v1";
        public const string CachedSnapshotRevisionV1 = "BlockBlast.PlayerData.CachedSnapshotRevision.v1";

        public const string LegacyScoreControllerHighScore = "BLOCK_BLAST_HIGH_SCORE";
        public const string LegacyLobbyHighScore = "HighScore";
    }
}
