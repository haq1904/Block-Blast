using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class RecentGameResult
    {
        public string GameSessionId { get; }
        public int Score { get; }
        public long CompletedAtUnixSeconds { get; }

        public RecentGameResult(string gameSessionId, int score, long completedAtUnixSeconds)
        {
            if (string.IsNullOrEmpty(gameSessionId))
            {
                throw new ArgumentException("GameSessionId cannot be null or empty.", nameof(gameSessionId));
            }
            GameSessionId = gameSessionId;
            Score = Math.Max(0, score);
            CompletedAtUnixSeconds = Math.Max(0L, completedAtUnixSeconds);
        }
    }
}
