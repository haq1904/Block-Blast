using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerSession
{
    public interface IGameResultSubmissionService
    {
        event Action<GameResultSubmissionOutcome> OnResultProcessed;

        int PendingCount { get; }

        QueueEnqueueResult Enqueue(GameResult result);
        Task FlushPendingAsync(CancellationToken cancellationToken = default);
    }
}
