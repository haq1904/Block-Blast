using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    public sealed class UnityRealtimeFeedbackClock : IFeedbackClock
    {
        public static UnityRealtimeFeedbackClock Instance { get; } = new UnityRealtimeFeedbackClock();

        private UnityRealtimeFeedbackClock()
        {
        }

        public double RealtimeSinceStartup => Time.realtimeSinceStartupAsDouble;
    }
}
