using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class PlayerStats
    {
        public long GamesPlayed { get; }
        public long TotalScore { get; }
        public int BestScore { get; }
        public long TotalLinesCleared { get; }
        public long TotalBlocksPlaced { get; }
        public int BestCombo { get; }
        public long PersonalBestBreakCount { get; }

        public PlayerStats(
            long gamesPlayed,
            long totalScore,
            int bestScore,
            long totalLinesCleared,
            long totalBlocksPlaced,
            int bestCombo,
            long personalBestBreakCount)
        {
            GamesPlayed = Math.Max(0L, gamesPlayed);
            TotalScore = Math.Max(0L, totalScore);
            BestScore = Math.Max(0, bestScore);
            TotalLinesCleared = Math.Max(0L, totalLinesCleared);
            TotalBlocksPlaced = Math.Max(0L, totalBlocksPlaced);
            BestCombo = Math.Max(0, bestCombo);
            PersonalBestBreakCount = Math.Max(0L, personalBestBreakCount);
        }
    }
}
