using System;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerSession
{
    public sealed class GameSessionStats : IGameSessionStats
    {
        public string GameSessionId { get; }
        public int LinesCleared { get; private set; }
        public int BlocksPlaced { get; private set; }
        public int BestCombo { get; private set; }
        public bool IsCompleted { get; private set; }

        public GameSessionStats(string gameSessionId = null)
        {
            GameSessionId = string.IsNullOrEmpty(gameSessionId)
                ? Guid.NewGuid().ToString("N")
                : gameSessionId;
        }

        public void RecordPlacement(int tilesPlaced, int totalLinesCleared)
        {
            if (IsCompleted)
            {
                return;
            }

            if (tilesPlaced < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tilesPlaced), "Tiles placed cannot be negative.");
            }

            if (totalLinesCleared < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalLinesCleared), "Lines cleared cannot be negative.");
            }

            BlocksPlaced += tilesPlaced;
            LinesCleared += totalLinesCleared;
        }

        public GameResult Complete(int finalScore, int bestCombo, long completedAtUnixSeconds)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException("Game session is already completed.");
            }

            if (finalScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finalScore), "Final score cannot be negative.");
            }

            if (bestCombo < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bestCombo), "Best combo cannot be negative.");
            }

            if (completedAtUnixSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completedAtUnixSeconds), "Completion timestamp cannot be negative.");
            }

            IsCompleted = true;
            BestCombo = bestCombo;

            return new GameResult(
                GameSessionId,
                finalScore,
                LinesCleared,
                BlocksPlaced,
                bestCombo,
                completedAtUnixSeconds,
                PlayerDataSchema.CurrentVersion);
        }
    }
}
