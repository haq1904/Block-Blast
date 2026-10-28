using System;
using System.IO;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerCloud.Persistence
{
    /// <summary>
    /// Scoped file-based cache for GameCenterSnapshot, isolated per environment and player.
    /// Provides atomic writes, backup recovery, and monotonic per-scope revision guards.
    /// </summary>
    public sealed class FilePlayerCloudSnapshotCache : IPlayerCloudSnapshotCache
    {
        public const string SnapshotFileName = "game_center_snapshot_v1.json";

        private readonly string _rootDirectory;
        private readonly IPlayerDataSerializer _serializer;

        public FilePlayerCloudSnapshotCache(
            string rootDirectory,
            IPlayerDataSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory cannot be null or empty.", nameof(rootDirectory));
            }

            _rootDirectory = rootDirectory;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            CleanUpLegacyUnscopedFiles();
        }

        private void CleanUpLegacyUnscopedFiles()
        {
            try
            {
                if (Directory.Exists(_rootDirectory))
                {
                    string legacyJson = Path.Combine(_rootDirectory, SnapshotFileName);
                    string legacyTmp = Path.Combine(_rootDirectory, SnapshotFileName + ".tmp");
                    string legacyBak = Path.Combine(_rootDirectory, SnapshotFileName + ".bak");

                    if (File.Exists(legacyJson)) File.Delete(legacyJson);
                    if (File.Exists(legacyTmp)) File.Delete(legacyTmp);
                    if (File.Exists(legacyBak)) File.Delete(legacyBak);
                }
            }
            catch
            {
                // Suppress legacy cleanup errors to prevent startup blocking
            }
        }

        public string GetScopedFilePath(PlayerCloudCacheScope scope)
        {
            string scopeDir = Path.Combine(_rootDirectory, scope.GetRelativeDirectoryPath());
            return Path.Combine(scopeDir, SnapshotFileName);
        }

        public bool TryLoad(PlayerCloudCacheScope scope, out GameCenterSnapshot snapshot)
        {
            snapshot = null;

            try
            {
                string filePath = GetScopedFilePath(scope);
                string bakPath = filePath + ".bak";

                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        var loaded = _serializer.DeserializeSnapshot(json);
                        if (loaded != null && loaded.SchemaVersion == PlayerDataSchema.CurrentVersion)
                        {
                            snapshot = loaded;
                            return true;
                        }
                    }
                    catch
                    {
                        // File corrupted or bad schema, fall back to backup.
                    }
                }

                if (File.Exists(bakPath))
                {
                    try
                    {
                        string bakJson = File.ReadAllText(bakPath);
                        var loadedBak = _serializer.DeserializeSnapshot(bakJson);
                        if (loadedBak != null && loadedBak.SchemaVersion == PlayerDataSchema.CurrentVersion)
                        {
                            snapshot = loadedBak;
                            try
                            {
                                File.Copy(bakPath, filePath, true);
                            }
                            catch
                            {
                                // Recovery copy failed, but in-memory snapshot is valid.
                            }

                            return true;
                        }
                    }
                    catch
                    {
                        // Backup file also corrupted.
                    }
                }
            }
            catch
            {
                // Suppress storage I/O failures to prevent unhandled crashes.
            }

            return false;
        }

        public bool Save(PlayerCloudCacheScope scope, GameCenterSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SchemaVersion != PlayerDataSchema.CurrentVersion)
            {
                return false;
            }

            string filePath = GetScopedFilePath(scope);
            string tmpPath = filePath + ".tmp";
            string bakPath = filePath + ".bak";

            try
            {
                // Monotonic revision check within this isolated scope
                if (TryLoad(scope, out var currentSnapshot) && currentSnapshot != null)
                {
                    if (snapshot.Revision < currentSnapshot.Revision)
                    {
                        return false;
                    }

                    if (snapshot.Revision == currentSnapshot.Revision && File.Exists(filePath))
                    {
                        return true;
                    }
                }

                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = _serializer.SerializeSnapshot(snapshot);
                File.WriteAllText(tmpPath, json);

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, bakPath, true);
                    File.Delete(filePath);
                }

                File.Move(tmpPath, filePath);

                if (File.Exists(tmpPath))
                {
                    try
                    {
                        File.Delete(tmpPath);
                    }
                    catch
                    {
                    }
                }

                return true;
            }
            catch
            {
                if (File.Exists(tmpPath))
                {
                    try
                    {
                        File.Delete(tmpPath);
                    }
                    catch
                    {
                    }
                }

                return false;
            }
        }

        public void Clear(PlayerCloudCacheScope scope)
        {
            try
            {
                string filePath = GetScopedFilePath(scope);
                string tmpPath = filePath + ".tmp";
                string bakPath = filePath + ".bak";

                if (File.Exists(filePath)) File.Delete(filePath);
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                if (File.Exists(bakPath)) File.Delete(bakPath);
            }
            catch
            {
                // Suppress storage I/O errors on clear to maintain exception safety
            }
        }
    }
}
