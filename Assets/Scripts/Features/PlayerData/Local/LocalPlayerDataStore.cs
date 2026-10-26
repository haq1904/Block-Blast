using System;
using BlockBlast.Core.Persistence;

namespace BlockBlast.Features.PlayerData
{
    public sealed class LocalPlayerDataStore : ILocalPlayerDataStore
    {
        private readonly IKeyValueStore _store;
        private bool _isMigrated;

        public LocalPlayerDataStore(IKeyValueStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        private void EnsureMigrated()
        {
            if (_isMigrated) return;
            _isMigrated = true;

            int currentVersion = _store.GetInt(PlayerDataKeys.LocalSchemaVersion, 0);
            if (currentVersion > PlayerDataSchema.CurrentVersion)
            {
                // Future version detected; do not mutate existing data.
                return;
            }

            if (currentVersion < PlayerDataSchema.CurrentVersion)
            {
                int maxScore = 0;
                bool hadData = false;

                if (_store.HasKey(PlayerDataKeys.HighScoreV1))
                {
                    hadData = true;
                    maxScore = Math.Max(maxScore, _store.GetInt(PlayerDataKeys.HighScoreV1, 0));
                }

                if (_store.HasKey(PlayerDataKeys.LegacyScoreControllerHighScore))
                {
                    hadData = true;
                    maxScore = Math.Max(maxScore, _store.GetInt(PlayerDataKeys.LegacyScoreControllerHighScore, 0));
                    _store.DeleteKey(PlayerDataKeys.LegacyScoreControllerHighScore);
                }

                if (_store.HasKey(PlayerDataKeys.LegacyLobbyHighScore))
                {
                    hadData = true;
                    maxScore = Math.Max(maxScore, _store.GetInt(PlayerDataKeys.LegacyLobbyHighScore, 0));
                    _store.DeleteKey(PlayerDataKeys.LegacyLobbyHighScore);
                }

                _store.SetInt(PlayerDataKeys.HighScoreV1, Math.Max(0, maxScore));
                _store.SetInt(PlayerDataKeys.LocalSchemaVersion, PlayerDataSchema.CurrentVersion);

                if (hadData || currentVersion == 0)
                {
                    _store.Save();
                }
            }
        }

        public int LoadHighScore()
        {
            EnsureMigrated();
            return Math.Max(0, _store.GetInt(PlayerDataKeys.HighScoreV1, 0));
        }

        public bool SaveHighScore(int newScore)
        {
            EnsureMigrated();

            int version = _store.GetInt(PlayerDataKeys.LocalSchemaVersion, 0);
            if (version > PlayerDataSchema.CurrentVersion)
            {
                return false;
            }

            int current = LoadHighScore();
            if (newScore <= current)
            {
                return false;
            }

            _store.SetInt(PlayerDataKeys.HighScoreV1, newScore);
            _store.Save();
            return true;
        }

        public long LoadLastCloudSyncUnixSeconds()
        {
            EnsureMigrated();
            string val = _store.GetString(PlayerDataKeys.LastCloudSyncUnixSecondsV1, "0");
            return long.TryParse(val, out long result) ? Math.Max(0L, result) : 0L;
        }

        public void SaveLastCloudSyncUnixSeconds(long unixSeconds)
        {
            EnsureMigrated();
            _store.SetString(PlayerDataKeys.LastCloudSyncUnixSecondsV1, Math.Max(0L, unixSeconds).ToString());
            _store.Save();
        }

        public long LoadCachedSnapshotRevision()
        {
            EnsureMigrated();
            string val = _store.GetString(PlayerDataKeys.CachedSnapshotRevisionV1, "0");
            return long.TryParse(val, out long result) ? Math.Max(0L, result) : 0L;
        }

        public void SaveCachedSnapshotRevision(long revision)
        {
            EnsureMigrated();
            _store.SetString(PlayerDataKeys.CachedSnapshotRevisionV1, Math.Max(0L, revision).ToString());
            _store.Save();
        }
    }
}
