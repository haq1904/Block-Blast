using System;

namespace BlockBlast.Features.PlayerCloud.Time
{
    public sealed class SystemPlayerCloudClock : IPlayerCloudClock
    {
        public long NowUnixSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
