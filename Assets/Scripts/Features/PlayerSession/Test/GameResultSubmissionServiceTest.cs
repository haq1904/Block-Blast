using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerCloud.Mapping;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerSession.Test
{
    [TestFixture]
    public sealed class GameResultSubmissionServiceTest
    {
        private FakePendingQueue _queue;
        private FakePlayerCloudService _cloudService;
        private GameResultSubmissionService _service;
        private List<GameResultSubmissionOutcome> _outcomes;

        [SetUp]
        public void SetUp()
        {
            _queue = new FakePendingQueue();
            _cloudService = new FakePlayerCloudService();
            _service = new GameResultSubmissionService(_queue, _cloudService);
            _outcomes = new List<GameResultSubmissionOutcome>();

            _service.OnResultProcessed += outcome => _outcomes.Add(outcome);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        [Test]
        public void Enqueue_NullResult_ReturnsInvalidPayload_AndPublishesRejected()
        {
            var result = _service.Enqueue(null);

            Assert.AreEqual(QueueEnqueueResult.InvalidPayload, result);
            Assert.AreEqual(1, _outcomes.Count);
            Assert.AreEqual(GameResultSubmissionStatus.Rejected, _outcomes[0].Status);
        }

        [Test]
        public void PendingCount_ReflectsQueueItemCount()
        {
            Assert.AreEqual(0, _service.PendingCount);

            var gameResult1 = new GameResult("session_1", 100, 1, 5, 1, 1000L);
            _queue.Enqueue(gameResult1);
            Assert.AreEqual(1, _service.PendingCount);

            var gameResult2 = new GameResult("session_2", 200, 2, 8, 2, 1000L);
            _queue.Enqueue(gameResult2);
            Assert.AreEqual(2, _service.PendingCount);
        }

        [Test]
        public async Task FlushPendingAsync_CloudSuccess_AcknowledgesHead_AndPublishesSubmitted()
        {
            var gameResult = new GameResult("session_success", 500, 4, 12, 2, 1000L);
            _queue.Enqueue(gameResult);

            var domainResponse = new SubmitGameResultResponse(
                PlayerCloudContractMapper.CreateEmptySnapshot(),
                Array.Empty<string>(),
                true,
                false);

            _cloudService.SubmitResultToReturn = CloudOperationResult<SubmitGameResultResponse>.Success(domainResponse);

            await _service.FlushPendingAsync();

            Assert.AreEqual(0, _queue.Count);
            Assert.AreEqual(1, _cloudService.SubmittedResults.Count);
            Assert.AreEqual("session_success", _cloudService.SubmittedResults[0].GameSessionId);

            Assert.AreEqual(1, _outcomes.Count);
            Assert.AreEqual(GameResultSubmissionStatus.Submitted, _outcomes[0].Status);
            Assert.AreEqual("session_success", _outcomes[0].GameSessionId);
            Assert.IsNotNull(_outcomes[0].Response);
        }

        [Test]
        public async Task FlushPendingAsync_PermanentFailure_TerminalDropsHead_AndPublishesRejected()
        {
            var gameResult = new GameResult("session_rejected", 500, 4, 12, 2, 1000L);
            _queue.Enqueue(gameResult);

            _cloudService.SubmitResultToReturn = CloudOperationResult<SubmitGameResultResponse>.Failure(
                CloudFailureKind.InvalidPayload,
                false,
                "INVALID_SESSION_ID");

            await _service.FlushPendingAsync();

            // Terminal drop: acknowledged and removed from queue
            Assert.AreEqual(0, _queue.Count);
            Assert.AreEqual(1, _outcomes.Count);
            Assert.AreEqual(GameResultSubmissionStatus.Rejected, _outcomes[0].Status);
            Assert.AreEqual("session_rejected", _outcomes[0].GameSessionId);
            Assert.AreEqual("INVALID_SESSION_ID", _outcomes[0].ErrorCode);
        }

        [Test]
        public async Task FlushPendingAsync_TransientFailure_KeepsHead_AndStopsFlush()
        {
            var r1 = new GameResult("session_transient_1", 100, 1, 3, 1, 1000L);
            var r2 = new GameResult("session_transient_2", 200, 2, 6, 1, 1001L);
            _queue.Enqueue(r1);
            _queue.Enqueue(r2);

            _cloudService.SubmitResultToReturn = CloudOperationResult<SubmitGameResultResponse>.Failure(
                CloudFailureKind.Offline,
                true,
                "OFFLINE");

            await _service.FlushPendingAsync();

            // Head item is preserved for future retry
            Assert.AreEqual(2, _queue.Count);
            Assert.AreEqual(1, _cloudService.SubmittedResults.Count);
            Assert.AreEqual(1, _outcomes.Count);
            Assert.AreEqual(GameResultSubmissionStatus.Deferred, _outcomes[0].Status);
            Assert.AreEqual("session_transient_1", _outcomes[0].GameSessionId);
        }

        [Test]
        public async Task FlushPendingAsync_BatchProcessesUpToTenItems()
        {
            for (int i = 0; i < 15; i++)
            {
                _queue.Enqueue(new GameResult($"session_{i}", 100, 1, 3, 1, 1000L + i));
            }

            var domainResponse = new SubmitGameResultResponse(
                PlayerCloudContractMapper.CreateEmptySnapshot(),
                Array.Empty<string>(),
                false,
                false);
            _cloudService.SubmitResultToReturn = CloudOperationResult<SubmitGameResultResponse>.Success(domainResponse);

            await _service.FlushPendingAsync();

            Assert.AreEqual(10, _cloudService.SubmittedResults.Count);
            Assert.AreEqual(5, _queue.Count);
            Assert.AreEqual(10, _outcomes.Count);
        }

        private sealed class FakePendingQueue : IPendingGameResultQueue
        {
            private readonly List<GameResult> _list = new List<GameResult>();

            public int Count => _list.Count;

            public QueueEnqueueResult Enqueue(GameResult result)
            {
                if (result == null) return QueueEnqueueResult.InvalidPayload;
                _list.Add(result);
                return QueueEnqueueResult.Success;
            }

            public bool TryPeek(out GameResult result)
            {
                if (_list.Count > 0)
                {
                    result = _list[0];
                    return true;
                }

                result = null;
                return false;
            }

            public QueueAcknowledgeResult Acknowledge(string gameSessionId)
            {
                if (_list.Count == 0) return QueueAcknowledgeResult.NotFound;
                if (_list[0].GameSessionId != gameSessionId) return QueueAcknowledgeResult.NotHead;

                _list.RemoveAt(0);
                return QueueAcknowledgeResult.Success;
            }
        }

        private sealed class FakePlayerCloudService : IPlayerCloudService
        {
            public CloudSyncState State => CloudSyncState.Ready;
            public GameCenterSnapshot CachedSnapshot => PlayerCloudContractMapper.CreateEmptySnapshot();
            public List<GameResult> SubmittedResults { get; } = new List<GameResult>();
            public CloudOperationResult<SubmitGameResultResponse> SubmitResultToReturn { get; set; }

            public Task<CloudOperationResult<GameCenterSnapshot>> LoadSnapshotAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(CachedSnapshot));
            }

            public Task<CloudOperationResult<SubmitGameResultResponse>> SubmitGameResultAsync(GameResult result, CancellationToken cancellationToken = default)
            {
                SubmittedResults.Add(result);
                return Task.FromResult(SubmitResultToReturn);
            }

            public Task<CloudOperationResult<GameCenterSnapshot>> ResetForIdentityChangeAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(CachedSnapshot));
            }
        }
    }
}
