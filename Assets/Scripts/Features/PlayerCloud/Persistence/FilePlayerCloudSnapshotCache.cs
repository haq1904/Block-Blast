using System;
using System.IO;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Time;

namespace BlockBlast.Features.PlayerCloud.Persistence
{
    public sealed class FilePlayerCloudSnapshotCache : IPlayerCloudSnapshotCache
    {
        private readonly string _filePath;
        private readonly string _tmpPath;
        private readonly string _bakPath;
        private readonly IPlayerDataSerializer _serializer;
        private readonly ILocalPlayerDataStore _localStore;
        private readonly IPlayerCloudClock _clock;

        public FilePlayerCloudSnapshotCache(
            string filePath,
            IPlayerDataSerializer serializer,
            ILocalPlayerDataStore localStore = null,
            IPlayerCloudClock clock = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Cache file path cannot be null or empty.", nameof(filePath));
            }

            _filePath = filePath;
            _tmpPath = filePath + ".tmp";
            _bakPath = filePath + ".bak";
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _localStore = localStore;
            _clock = clock;
        }

        public bool TryLoad(out GameCenterSnapshot snapshot)
        {
            snapshot = null;

            try
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(_filePath);
                        var loaded = _serializer.DeserializeSnapshot(json);
                        if (loaded != null && loaded.SchemaVersion == PlayerDataSchema.CurrentVersion)
                        {
                            snapshot = loaded;
                            return true;
                        }
                    }
                    catch
                    {
                        // File corrupted or bad schema, attempt recovery from backup.
                    }
                }

                if (File.Exists(_bakPath))
                {
                    try
                    {
                        string bakJson = File.ReadAllText(_bakPath);
                        var loadedBak = _serializer.DeserializeSnapshot(bakJson);
                        if (loadedBak != null && loadedBak.SchemaVersion == PlayerDataSchema.CurrentVersion)
                        {
                            snapshot = loadedBak;
                            try
                            {
                                File.Copy(_bakPath, _filePath, true);
                            }
                            catch
                            {
                                // Recovery copy failed, but memory snapshot is valid.
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

        public bool Save(GameCenterSnapshot snapshot)
        {
            if (snapshot == null || snapshot.SchemaVersion != PlayerDataSchema.CurrentVersion)
            {
                return false;
            }

            try
            {
                if (_localStore != null)
                {
                    long currentRevision = _localStore.LoadCachedSnapshotRevision();
                    if (snapshot.Revision < currentRevision)
                    {
                        return false;
                    }

                    if (snapshot.Revision == currentRevision && File.Exists(_filePath))
                    {
                        return true;
                    }
                }

                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = _serializer.SerializeSnapshot(snapshot);
                File.WriteAllText(_tmpPath, json);

                if (File.Exists(_filePath))
                {
                    File.Copy(_filePath, _bakPath, true);
                    File.Delete(_filePath);
                }

                File.Move(_tmpPath, _filePath);

                if (File.Exists(_tmpPath))
                {
                    try
                    {
                        File.Delete(_tmpPath);
                    }
                    catch
                    {
                    }
                }

                if (_localStore != null)
                {
                    _localStore.SaveCachedSnapshotRevision(snapshot.Revision);
                    if (_clock != null)
                    {
                        _localStore.SaveLastCloudSyncUnixSeconds(_clock.NowUnixSeconds);
                    }
                }

                return true;
            }
            catch
            {
                if (File.Exists(_tmpPath))
                {
                    try
                    {
                        File.Delete(_tmpPath);
                    }
                    catch
                    {
                    }
                }

                return false;
            }
        }

        public void Clear()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                if (File.Exists(_tmpPath))
                {
                    File.Delete(_tmpPath);
                }

                if (File.Exists(_bakPath))
                {
                    File.Delete(_bakPath);
                }

                if (_localStore != null)
                {
                    _localStore.SaveCachedSnapshotRevision(0);
                    if (_clock != null)
                    {
                        _localStore.SaveLastCloudSyncUnixSeconds(0);
                    }
                }
            }
            catch
            {
                // Suppress storage I/O errors on clear to maintain exception safety
            }
        }
    }
}
