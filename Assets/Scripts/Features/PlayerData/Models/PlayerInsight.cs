using System;

namespace BlockBlast.Features.PlayerData
{
    public sealed class PlayerInsight
    {
        public string InsightId { get; }
        public double CurrentValue { get; }
        public double PreviousValue { get; }

        public PlayerInsight(string insightId, double currentValue, double previousValue)
        {
            if (string.IsNullOrEmpty(insightId))
            {
                throw new ArgumentException("InsightId cannot be null or empty.", nameof(insightId));
            }
            InsightId = insightId;
            CurrentValue = currentValue;
            PreviousValue = previousValue;
        }
    }
}
