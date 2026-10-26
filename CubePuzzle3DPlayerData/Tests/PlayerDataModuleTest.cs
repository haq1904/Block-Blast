using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CubePuzzle3DPlayerData.Contracts;
using CubePuzzle3DPlayerData.Domain;
using CubePuzzle3DPlayerData.Endpoints;
using CubePuzzle3DPlayerData.Persistence;
using NUnit.Framework;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace CubePuzzle3DPlayerData.Tests;

public class FakeExecutionContext : IExecutionContext
{
    public string ProjectId { get; set; } = "4d2723eb-7755-47ae-a041-9b17ce79fc83";
    public string PlayerId { get; set; } = "test_player_01";
    public string EnvironmentId { get; set; } = "development";
    public string EnvironmentName { get; set; } = "development";
    public string ServiceToken { get; set; } = "mock_service_token";
    public string? AccessToken { get; set; } = "mock_access_token";
    public string? AnalyticsUserId { get; set; } = "analytics_user_01";
    public string? UnityInstallationId { get; set; } = "inst_01";
    public string? CorrelationId { get; set; } = "corr_01";
    public string? UserId { get; set; } = "user_01";
    public string? Issuer { get; set; } = "issuer_01";
    public string? ScopeId { get; set; } = "scope_01";
    public int CallDepth { get; set; } = 0;
    public ISession? Session { get; set; }
}

public class FakePlayerSnapshotStore : IPlayerSnapshotStore
{
    public StoredPlayerSnapshot? StoredSnapshot { get; set; }
    public string? CurrentWriteLock { get; set; }
    public bool SimulateMissing { get; set; } = true;
    public string? SimulateLoadErrorCode { get; set; }

    public int LoadCallCount { get; private set; }
    public int SaveCallCount { get; private set; }
    public List<string?> RecordedSaveWriteLocks { get; } = new();

    public int ConflictsRemainingToThrow { get; set; }

    public Task<PlayerSnapshotLoadResult> LoadAsync(IExecutionContext context, IGameApiClient gameApiClient, CancellationToken ct = default)
    {
        LoadCallCount++;
        if (SimulateLoadErrorCode != null)
        {
            return Task.FromResult(PlayerSnapshotLoadResult.Fail(SimulateLoadErrorCode));
        }

        if (SimulateMissing)
        {
            return Task.FromResult(PlayerSnapshotLoadResult.Missing());
        }

        return Task.FromResult(PlayerSnapshotLoadResult.Success(StoredSnapshot!, CurrentWriteLock));
    }

    public Task<PlayerSnapshotSaveResult> SaveAsync(IExecutionContext context, IGameApiClient gameApiClient, StoredPlayerSnapshot snapshot, string? writeLock, CancellationToken ct = default)
    {
        SaveCallCount++;
        RecordedSaveWriteLocks.Add(writeLock);

        if (ConflictsRemainingToThrow > 0)
        {
            ConflictsRemainingToThrow--;
            return Task.FromResult(PlayerSnapshotSaveResult.Conflict());
        }

        StoredSnapshot = snapshot;
        SimulateMissing = false;
        CurrentWriteLock = "lock_" + SaveCallCount;
        return Task.FromResult(PlayerSnapshotSaveResult.Success());
    }
}

[TestFixture]
public class PlayerDataModuleTest
{
    private const long ServerNow = 1761465600L;

    private static GameResultRequest CreateValidRequest(string sessionId = "session_test_01", int score = 1000)
    {
        return new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = sessionId,
            Score = score,
            LinesCleared = 5,
            BlocksPlaced = 10,
            BestCombo = 1,
            CompletedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 5
        };
    }

    [Test]
    public async Task SubmitGameResult_InvalidPayload_ReturnsRejectedWithoutStoreCalls()
    {
        var store = new FakePlayerSnapshotStore();
        var module = new PlayerDataModule(store);
        var context = new FakeExecutionContext();
        var request = CreateValidRequest();
        request.Score = -10; // invalid

        var response = await module.SubmitGameResult(context, null!, request);

        Assert.That(response.Status, Is.EqualTo(SubmitGameResultStatus.Rejected));
        Assert.That(response.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.MetricOutOfRange));
        Assert.That(store.LoadCallCount, Is.EqualTo(0));
        Assert.That(store.SaveCallCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SubmitGameResult_FirstGameMissingStore_CreatesDefaultAndSavesWithNullLock()
    {
        var store = new FakePlayerSnapshotStore { SimulateMissing = true };
        var module = new PlayerDataModule(store);
        var context = new FakeExecutionContext();
        var request = CreateValidRequest();

        var response = await module.SubmitGameResult(context, null!, request);

        Assert.That(response.Status, Is.EqualTo(SubmitGameResultStatus.Accepted));
        Assert.That(response.UpdatedSnapshot, Is.Not.Null);
        Assert.That(response.UpdatedSnapshot!.Revision, Is.EqualTo(1L));
        Assert.That(store.LoadCallCount, Is.EqualTo(1));
        Assert.That(store.SaveCallCount, Is.EqualTo(1));
        Assert.That(store.RecordedSaveWriteLocks[0], Is.Null);
    }

    [Test]
    public async Task SubmitGameResult_DuplicateSession_ReturnsDuplicateWithoutSaving()
    {
        var store = new FakePlayerSnapshotStore { SimulateMissing = true };
        var module = new PlayerDataModule(store);
        var context = new FakeExecutionContext();
        var request = CreateValidRequest("session_unique_1", 2000);

        // First call
        var resp1 = await module.SubmitGameResult(context, null!, request);
        Assert.That(resp1.Status, Is.EqualTo(SubmitGameResultStatus.Accepted));
        Assert.That(store.SaveCallCount, Is.EqualTo(1));

        // Duplicate call with same sessionId
        var resp2 = await module.SubmitGameResult(context, null!, request);
        Assert.That(resp2.Status, Is.EqualTo(SubmitGameResultStatus.Duplicate));
        Assert.That(resp2.IsDuplicate, Is.True);
        Assert.That(resp2.NewlyUnlockedAchievementIds, Is.Empty);
        Assert.That(resp2.UpdatedSnapshot!.Revision, Is.EqualTo(1L)); // not incremented
        Assert.That(store.SaveCallCount, Is.EqualTo(1)); // zero additional save calls!
    }

    [Test]
    public async Task SubmitGameResult_Conflict409_RetriesAndSucceeds()
    {
        var store = new FakePlayerSnapshotStore
        {
            SimulateMissing = true,
            ConflictsRemainingToThrow = 1 // will fail once with 409 then succeed
        };
        var module = new PlayerDataModule(store);
        var context = new FakeExecutionContext();
        var request = CreateValidRequest();

        var response = await module.SubmitGameResult(context, null!, request);

        Assert.That(response.Status, Is.EqualTo(SubmitGameResultStatus.Accepted));
        Assert.That(store.LoadCallCount, Is.EqualTo(2));
        Assert.That(store.SaveCallCount, Is.EqualTo(2));
    }

    [Test]
    public async Task SubmitGameResult_ExhaustedConflicts_ReturnsRetryableFailure()
    {
        var store = new FakePlayerSnapshotStore
        {
            SimulateMissing = true,
            ConflictsRemainingToThrow = 5 // exceeds max 3 attempts
        };
        var module = new PlayerDataModule(store);
        var context = new FakeExecutionContext();
        var request = CreateValidRequest();

        var response = await module.SubmitGameResult(context, null!, request);

        Assert.That(response.Status, Is.EqualTo(SubmitGameResultStatus.RetryableFailure));
        Assert.That(response.ErrorCode, Is.EqualTo(PlayerDataConstants.FailureCodes.WriteConflictRetryExhausted));
        Assert.That(store.SaveCallCount, Is.EqualTo(3));
    }

    [Test]
    public async Task SubmitGameResult_CorruptServerData_ReturnsRetryableFailureWithoutOverwriting()
    {
        var store = new FakePlayerSnapshotStore
        {
            SimulateLoadErrorCode = PlayerDataConstants.FailureCodes.ServerDataCorrupt
        };
        var module = new PlayerDataModule(store);
        var context = new FakeExecutionContext();
        var request = CreateValidRequest();

        var response = await module.SubmitGameResult(context, null!, request);

        Assert.That(response.Status, Is.EqualTo(SubmitGameResultStatus.RetryableFailure));
        Assert.That(response.ErrorCode, Is.EqualTo(PlayerDataConstants.FailureCodes.ServerDataCorrupt));
        Assert.That(store.SaveCallCount, Is.EqualTo(0)); // no overwrite!
    }
}
