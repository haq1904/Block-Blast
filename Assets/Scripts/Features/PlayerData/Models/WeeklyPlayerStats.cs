using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class WeeklyPlayerStats
    {
        public long WeekStartUnixSeconds { get; }
        public long GamesPlayed { get; }
        public long TotalScore { get; }
        public int BestScore { get; }
        public long TotalLinesCleared { get; }
        public long TotalBlocksPlaced { get; }
        public long PersonalBestBreakCount { get; }

        public WeeklyPlayerStats(
            long weekStartUnixSeconds,
            long gamesPlayed,
            long totalScore,
            int bestScore,
            long totalLinesCleared,
            long totalBlocksPlaced,
            long personalBestBreakCount)
        {
            WeekStartUnixSeconds = weekStartUnixSeconds;
            GamesPlayed = Math.Max(0L, gamesPlayed);
            TotalScore = Math.Max(0L, totalScore);
            BestScore = Math.Max(0, bestScore);
            TotalLinesCleared = Math.Max(0L, totalLinesCleared);
            TotalBlocksPlaced = Math.Max(0L, totalBlocksPlaced);
            PersonalBestBreakCount = Math.Max(0L, personalBestBreakCount);
        }
    }
}
