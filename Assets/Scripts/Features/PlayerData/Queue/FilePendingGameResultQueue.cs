using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BlockBlast.Features.PlayerData
{
    public sealed class FilePendingGameResultQueue : IPendingGameResultQueue
    {
        public const int DefaultCapacity = 100;

        private readonly object _lock = new object();
        private readonly string _filePath;
        private readonly string _tempPath;
        private readonly string _backupPath;
        private readonly int _capacity;
        private readonly List<GameResult> _items = new List<GameResult>();
        private bool _isStorageCorrupt;
        private bool _isLoaded;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    EnsureLoaded();
                    return _isStorageCorrupt ? 0 : _items.Count;
                }
            }
        }

        public FilePendingGameResultQueue()
            : this(Path.Combine(Application.persistentDataPath, "PlayerData", "pending_game_results_v1.json"), DefaultCapacity)
        {
        }

        public FilePendingGameResultQueue(string filePath, int capacity = DefaultCapacity)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("FilePath cannot be null or empty.", nameof(filePath));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

            _filePath = Path.GetFullPath(filePath);
            _tempPath = _filePath + ".tmp";
            _backupPath = _filePath + ".bak";
            _capacity = capacity;
        }

        private void EnsureLoaded()
        {
            if (_isLoaded) return;
            _isLoaded = true;

            string directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(_filePath))
            {
                if (TryLoadFromFile(_filePath, out var results))
                {
                    _items.Clear();
                    _items.AddRange(results);
                    return;
                }

                // Target is corrupted; try backup
                if (File.Exists(_backupPath) && TryLoadFromFile(_backupPath, out var backupResults))
                {
                    _items.Clear();
                    _items.AddRange(backupResults);
                    // Recover target file from backup
                    SaveToFile();
                    return;
                }

                _isStorageCorrupt = true;
                return;
            }

            if (File.Exists(_backupPath))
            {
                if (TryLoadFromFile(_backupPath, out var backupResults))
                {
                    _items.Clear();
                    _items.AddRange(backupResults);
                    SaveToFile();
                    return;
                }

                _isStorageCorrupt = true;
            }
        }

        private bool TryLoadFromFile(string path, out List<GameResult> results)
        {
            results = null;
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return false;

                var envelope = JsonUtility.FromJson<QueueEnvelopeDto>(json);
                if (envelope == null) return false;

                // Protect against future or unsupported schema versions
                if (envelope.schemaVersion > PlayerDataSchema.CurrentVersion || envelope.schemaVersion <= 0)
                {
                    return false;
                }

                var list = new List<GameResult>();
                if (envelope.results != null)
                {
                    foreach (var dto in envelope.results)
                    {
                        if (dto == null || string.IsNullOrEmpty(dto.gameSessionId))
                        {
                            return false;
                        }

                        list.Add(new GameResult(
                            dto.gameSessionId,
                            dto.score,
                            dto.linesCleared,
                            dto.blocksPlaced,
                            dto.bestCombo,
                            dto.completedAtUnixSeconds,
                            dto.schemaVersion));
                    }
                }

                results = list;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SaveToFile()
        {
            var envelope = new QueueEnvelopeDto
            {
                schemaVersion = PlayerDataSchema.CurrentVersion,
                results = new List<GameResultDto>(_items.Count)
            };

            foreach (var item in _items)
            {
                envelope.results.Add(new GameResultDto
                {
                    schemaVersion = item.SchemaVersion,
                    gameSessionId = item.GameSessionId,
                    score = item.Score,
                    linesCleared = item.LinesCleared,
                    blocksPlaced = item.BlocksPlaced,
                    bestCombo = item.BestCombo,
                    completedAtUnixSeconds = item.CompletedAtUnixSeconds
                });
            }

            string json = JsonUtility.ToJson(envelope);
            string directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to temporary file first with explicit flush
            using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            // Atomic replace / backup
            if (File.Exists(_filePath))
            {
                File.Copy(_filePath, _backupPath, true);
                File.Delete(_filePath);
            }
            File.Move(_tempPath, _filePath);
        }

        public QueueEnqueueResult Enqueue(GameResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.GameSessionId))
            {
                return QueueEnqueueResult.InvalidPayload;
            }

            lock (_lock)
            {
                EnsureLoaded();
                if (_isStorageCorrupt)
                {
                    return QueueEnqueueResult.StorageCorrupt;
                }

                if (_items.Count >= _capacity)
                {
                    return QueueEnqueueResult.Full;
                }

                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].GameSessionId == result.GameSessionId)
                    {
                        return QueueEnqueueResult.Duplicate;
                    }
                }

                _items.Add(result);
                try
                {
                    SaveToFile();
                }
                catch
                {
                    _items.RemoveAt(_items.Count - 1);
                    return QueueEnqueueResult.StorageCorrupt;
                }

                return QueueEnqueueResult.Success;
            }
        }

        public bool TryPeek(out GameResult result)
        {
            lock (_lock)
            {
                EnsureLoaded();
                if (_isStorageCorrupt || _items.Count == 0)
                {
                    result = null;
                    return false;
                }

                result = _items[0];
                return true;
            }
        }

        public QueueAcknowledgeResult Acknowledge(string gameSessionId)
        {
            if (string.IsNullOrEmpty(gameSessionId))
            {
                return QueueAcknowledgeResult.NotFound;
            }

            lock (_lock)
            {
                EnsureLoaded();
                if (_isStorageCorrupt)
                {
                    return QueueAcknowledgeResult.StorageCorrupt;
                }

                if (_items.Count == 0)
                {
                    return QueueAcknowledgeResult.NotFound;
                }

                if (_items[0].GameSessionId != gameSessionId)
                {
                    return QueueAcknowledgeResult.NotHead;
                }

                var removed = _items[0];
                _items.RemoveAt(0);
                try
                {
                    SaveToFile();
                }
                catch
                {
                    _items.Insert(0, removed);
                    return QueueAcknowledgeResult.StorageCorrupt;
                }

                return QueueAcknowledgeResult.Success;
            }
        }

        [Serializable]
        private class QueueEnvelopeDto
        {
            public int schemaVersion;
            public List<GameResultDto> results;
        }

        [Serializable]
        private class GameResultDto
        {
            public int schemaVersion;
            public string gameSessionId;
            public int score;
            public int linesCleared;
            public int blocksPlaced;
            public int bestCombo;
            public long completedAtUnixSeconds;
        }
    }
}
