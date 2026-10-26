using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class AchievementProgress
    {
        public string AchievementId { get; }
        public long CurrentValue { get; }
        public long TargetValue { get; }
        public long UnlockedAtUnixSeconds { get; }

        public bool IsUnlocked => UnlockedAtUnixSeconds > 0;

        public AchievementProgress(string achievementId, long currentValue, long targetValue, long unlockedAtUnixSeconds = 0)
        {
            if (string.IsNullOrEmpty(achievementId))
            {
                throw new ArgumentException("AchievementId cannot be null or empty.", nameof(achievementId));
            }
            AchievementId = achievementId;
            CurrentValue = Math.Max(0L, currentValue);
            TargetValue = Math.Max(0L, targetValue);
            UnlockedAtUnixSeconds = Math.Max(0L, unlockedAtUnixSeconds);
        }
    }
}
