using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerSession
{
    public sealed class GameResultSubmissionService : IGameResultSubmissionService, IDisposable
    {
        public const int MaxBatchCount = 10;

        private readonly IPendingGameResultQueue _queue;
        private readonly IPlayerCloudService _cloudService;
        private readonly SemaphoreSlim _flushGate = new SemaphoreSlim(1, 1);
        private readonly object _flushLock = new object();

        private Task _currentFlushTask;

        public event Action<GameResultSubmissionOutcome> OnResultProcessed;

        public int PendingCount => _queue.Count;

        public GameResultSubmissionService(
            IPendingGameResultQueue queue,
            IPlayerCloudService cloudService)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
        }

        public QueueEnqueueResult Enqueue(GameResult result)
        {
            if (result == null)
            {
                OnResultProcessed?.Invoke(GameResultSubmissionOutcome.Rejected(
                    null,
                    CloudFailureKind.InvalidPayload,
                    "NULL_RESULT"));
                return QueueEnqueueResult.InvalidPayload;
            }

            var enqueueResult = _queue.Enqueue(result);
            if (enqueueResult == QueueEnqueueResult.Full ||
                enqueueResult == QueueEnqueueResult.InvalidPayload ||
                enqueueResult == QueueEnqueueResult.StorageCorrupt)
            {
                OnResultProcessed?.Invoke(GameResultSubmissionOutcome.Rejected(
                    result.GameSessionId,
                    CloudFailureKind.InvalidPayload,
                    enqueueResult.ToString()));
            }

            return enqueueResult;
        }

        public async Task FlushPendingAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Task currentTask;
            lock (_flushLock)
            {
                if (_currentFlushTask == null || _currentFlushTask.IsCompleted)
                {
                    _currentFlushTask = ExecuteFlushPendingAsync();
                }

                currentTask = _currentFlushTask;
            }

            if (cancellationToken.CanBeCanceled)
            {
                var cancelTcs = new TaskCompletionSource<bool>();
                using (cancellationToken.Register(() => cancelTcs.TrySetResult(true)))
                {
                    var completedTask = await Task.WhenAny(currentTask, cancelTcs.Task);
                    if (completedTask == cancelTcs.Task)
                    {
                        return;
                    }
                }
            }

            await currentTask;
        }

        private async Task ExecuteFlushPendingAsync()
        {
            await _flushGate.WaitAsync();
            try
            {
                for (int i = 0; i < MaxBatchCount; i++)
                {
                    if (!_queue.TryPeek(out var head) || head == null)
                    {
                        break;
                    }

                    var cloudResult = await _cloudService.SubmitGameResultAsync(head, CancellationToken.None);

                    if (cloudResult.IsSuccess)
                    {
                        var ackResult = _queue.Acknowledge(head.GameSessionId);
                        if (ackResult == QueueAcknowledgeResult.Success)
                        {
                            OnResultProcessed?.Invoke(GameResultSubmissionOutcome.Submitted(head.GameSessionId, cloudResult.Value));
                            continue;
                        }

                        OnResultProcessed?.Invoke(GameResultSubmissionOutcome.Deferred(head.GameSessionId, CloudFailureKind.Unknown, "ACK_FAILED"));
                        break;
                    }

                    if (cloudResult.FailureKind == CloudFailureKind.InvalidPayload ||
                        cloudResult.FailureKind == CloudFailureKind.SchemaMismatch)
                    {
                        _queue.Acknowledge(head.GameSessionId);
                        OnResultProcessed?.Invoke(GameResultSubmissionOutcome.Rejected(head.GameSessionId, cloudResult.FailureKind, cloudResult.ErrorCode));
                        continue;
                    }

                    OnResultProcessed?.Invoke(GameResultSubmissionOutcome.Deferred(head.GameSessionId, cloudResult.FailureKind, cloudResult.ErrorCode));
                    break;
                }
            }
            finally
            {
                _flushGate.Release();
            }
        }

        public void Dispose()
        {
            _flushGate?.Dispose();
        }
    }
}
