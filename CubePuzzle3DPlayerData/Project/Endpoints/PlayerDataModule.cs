using System;
using System.Threading.Tasks;
using CubePuzzle3DPlayerData.Contracts;
using CubePuzzle3DPlayerData.Domain;
using CubePuzzle3DPlayerData.Persistence;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace CubePuzzle3DPlayerData.Endpoints;

public class PlayerDataModule
{
    private readonly IPlayerSnapshotStore _snapshotStore;
    private readonly ILogger<PlayerDataModule>? _logger;

    internal PlayerDataModule(ILogger<PlayerDataModule>? logger = null)
        : this(new CloudSavePlayerSnapshotStore(logger), logger)
    {
    }

    public PlayerDataModule(IPlayerSnapshotStore snapshotStore, ILogger<PlayerDataModule>? logger = null)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _logger = logger;
    }

    [CloudCodeFunction(PlayerDataConstants.SubmitFunctionName)]
    public async Task<SubmitGameResultResponse> SubmitGameResult(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        GameResultRequest request)
    {
        long serverNowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var validation = GameResultValidator.Validate(
            context?.PlayerId,
            request,
            serverNowUnixSeconds);

        if (!validation.IsValid)
        {
            _logger?.LogWarning("GameResult validation rejected with code {ErrorCode}", validation.ErrorCode);
            return SubmitGameResultResponse.Rejected(validation.ErrorCode!);
        }

        for (int attempt = 1; attempt <= PlayerDataConstants.MaxWriteConflictAttempts; attempt++)
        {
            if (attempt > 1)
            {
                int delayMs = attempt == 2
                    ? PlayerDataConstants.ConflictRetryDelayMs1
                    : PlayerDataConstants.ConflictRetryDelayMs2;
                await Task.Delay(delayMs);
            }

            var loadResult = await _snapshotStore.LoadAsync(context!, gameApiClient);
            if (!loadResult.IsSuccess)
            {
                _logger?.LogWarning("Failed to load snapshot for player: {ErrorCode}", loadResult.ErrorCode);
                return SubmitGameResultResponse.RetryableFailure(loadResult.ErrorCode!);
            }

            StoredPlayerSnapshot currentSnapshot;
            string? writeLock;

            if (loadResult.IsMissing)
            {
                currentSnapshot = StoredPlayerSnapshot.CreateDefault(serverNowUnixSeconds);
                writeLock = null;
            }
            else
            {
                currentSnapshot = loadResult.Snapshot!;
                writeLock = loadResult.WriteLock;
            }

            if (currentSnapshot.HasReceipt(request.GameSessionId))
            {
                _logger?.LogInformation("Duplicate gameSessionId detected; returning current snapshot without write");
                return SubmitGameResultResponse.Duplicate(currentSnapshot.ToContract());
            }

            ReducerResult reduceResult;
            try
            {
                reduceResult = PlayerSnapshotReducer.Reduce(currentSnapshot, request, serverNowUnixSeconds);
            }
            catch (OverflowException)
            {
                _logger?.LogError("Arithmetic overflow during snapshot reduction");
                return SubmitGameResultResponse.RetryableFailure(PlayerDataConstants.FailureCodes.ServerDataCorrupt);
            }

            var saveResult = await _snapshotStore.SaveAsync(
                context!,
                gameApiClient,
                reduceResult.Snapshot,
                writeLock);

            if (saveResult.IsSuccess)
            {
                return SubmitGameResultResponse.Accepted(
                    reduceResult.Snapshot.ToContract(),
                    reduceResult.NewlyUnlockedAchievementIds,
                    reduceResult.IsNewPersonalBest);
            }

            if (saveResult.IsConflict)
            {
                _logger?.LogInformation("Write conflict on attempt {Attempt}; will reload and retry", attempt);
                continue;
            }

            return SubmitGameResultResponse.RetryableFailure(
                saveResult.ErrorCode ?? PlayerDataConstants.FailureCodes.ServiceUnavailable);
        }

        _logger?.LogWarning("Exhausted all {MaxAttempts} write attempts due to conflict", PlayerDataConstants.MaxWriteConflictAttempts);
        return SubmitGameResultResponse.RetryableFailure(PlayerDataConstants.FailureCodes.WriteConflictRetryExhausted);
    }
}
