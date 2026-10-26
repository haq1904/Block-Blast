using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class SnapshotCacheMetadata
    {
        public int SchemaVersion { get; }
        public long CachedRevision { get; }
        public long LastCloudSyncUnixSeconds { get; }

        public SnapshotCacheMetadata(
            long cachedRevision,
            long lastCloudSyncUnixSeconds,
            int schemaVersion = PlayerDataSchema.CurrentVersion)
        {
            SchemaVersion = schemaVersion;
            CachedRevision = Math.Max(0L, cachedRevision);
            LastCloudSyncUnixSeconds = Math.Max(0L, lastCloudSyncUnixSeconds);
        }
    }
}
