namespace BlockBlast.Core.Feedback
{
    public interface IFeedbackClock
    {
        double RealtimeSinceStartup { get; }
    }
}
