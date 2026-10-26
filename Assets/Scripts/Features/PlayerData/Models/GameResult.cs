using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class GameResult
    {
        public int SchemaVersion { get; }
        public string GameSessionId { get; }
        public int Score { get; }
        public int LinesCleared { get; }
        public int BlocksPlaced { get; }
        public int BestCombo { get; }
        public long CompletedAtUnixSeconds { get; }

        public GameResult(
            string gameSessionId,
            int score,
            int linesCleared,
            int blocksPlaced,
            int bestCombo,
            long completedAtUnixSeconds,
            int schemaVersion = PlayerDataSchema.CurrentVersion)
        {
            if (string.IsNullOrEmpty(gameSessionId))
            {
                throw new ArgumentException("GameSessionId cannot be null or empty.", nameof(gameSessionId));
            }

            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative.");
            if (linesCleared < 0) throw new ArgumentOutOfRangeException(nameof(linesCleared), "LinesCleared cannot be negative.");
            if (blocksPlaced < 0) throw new ArgumentOutOfRangeException(nameof(blocksPlaced), "BlocksPlaced cannot be negative.");
            if (bestCombo < 0) throw new ArgumentOutOfRangeException(nameof(bestCombo), "BestCombo cannot be negative.");
            if (completedAtUnixSeconds < 0) throw new ArgumentOutOfRangeException(nameof(completedAtUnixSeconds), "CompletedAtUnixSeconds cannot be negative.");

            SchemaVersion = schemaVersion;
            GameSessionId = gameSessionId;
            Score = score;
            LinesCleared = linesCleared;
            BlocksPlaced = blocksPlaced;
            BestCombo = bestCombo;
            CompletedAtUnixSeconds = completedAtUnixSeconds;
        }
    }
}
