namespace BlockBlast.Features.PlayerData
{
    public interface IPendingGameResultQueue
    {
        int Count { get; }
        QueueEnqueueResult Enqueue(GameResult result);
        bool TryPeek(out GameResult result);
        QueueAcknowledgeResult Acknowledge(string gameSessionId);
    }
}
