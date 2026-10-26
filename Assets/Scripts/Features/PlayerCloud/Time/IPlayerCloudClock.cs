namespace BlockBlast.Features.PlayerCloud.Time
{
    public interface IPlayerCloudClock
    {
        long NowUnixSeconds { get; }
    }
}
