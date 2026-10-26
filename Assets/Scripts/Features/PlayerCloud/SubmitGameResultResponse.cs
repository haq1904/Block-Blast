using System;
using System.Collections.Generic;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerCloud
{
    public sealed class SubmitGameResultResponse
    {
        public GameCenterSnapshot UpdatedSnapshot { get; }
        public IReadOnlyList<string> NewlyUnlockedAchievementIds { get; }
        public bool IsNewPersonalBest { get; }
        public bool IsDuplicate { get; }

        public SubmitGameResultResponse(
            GameCenterSnapshot updatedSnapshot,
            IEnumerable<string> newlyUnlockedAchievementIds,
            bool isNewPersonalBest,
            bool isDuplicate)
        {
            UpdatedSnapshot = updatedSnapshot ?? throw new ArgumentNullException(nameof(updatedSnapshot));

            var unlockList = new List<string>();
            if (newlyUnlockedAchievementIds != null)
            {
                foreach (var id in newlyUnlockedAchievementIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        unlockList.Add(id);
                    }
                }
            }
            NewlyUnlockedAchievementIds = unlockList.AsReadOnly();

            IsNewPersonalBest = isNewPersonalBest;
            IsDuplicate = isDuplicate;
        }
    }
}
