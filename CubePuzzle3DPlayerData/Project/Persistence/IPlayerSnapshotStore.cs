using System.Threading;
using System.Threading.Tasks;
using CubePuzzle3DPlayerData.Domain;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace CubePuzzle3DPlayerData.Persistence;

public sealed class PlayerSnapshotLoadResult
{
    public StoredPlayerSnapshot? Snapshot { get; }
    public string? WriteLock { get; }
    public bool IsMissing { get; }
    public string? ErrorCode { get; }

    public bool IsSuccess => ErrorCode == null;

    private PlayerSnapshotLoadResult(StoredPlayerSnapshot? snapshot, string? writeLock, bool isMissing, string? errorCode)
    {
        Snapshot = snapshot;
        WriteLock = writeLock;
        IsMissing = isMissing;
        ErrorCode = errorCode;
    }

    public static PlayerSnapshotLoadResult Success(StoredPlayerSnapshot snapshot, string? writeLock) =>
        new(snapshot, writeLock, false, null);

    public static PlayerSnapshotLoadResult Missing() =>
        new(null, null, true, null);

    public static PlayerSnapshotLoadResult Fail(string errorCode) =>
        new(null, null, false, errorCode);
}

public sealed class PlayerSnapshotSaveResult
{
    public bool IsSuccess { get; }
    public bool IsConflict { get; }
    public string? ErrorCode { get; }

    private PlayerSnapshotSaveResult(bool isSuccess, bool isConflict, string? errorCode)
    {
        IsSuccess = isSuccess;
        IsConflict = isConflict;
        ErrorCode = errorCode;
    }

    public static PlayerSnapshotSaveResult Success() => new(true, false, null);
    public static PlayerSnapshotSaveResult Conflict() => new(false, true, null);
    public static PlayerSnapshotSaveResult Fail(string errorCode) => new(false, false, errorCode);
}

public interface IPlayerSnapshotStore
{
    Task<PlayerSnapshotLoadResult> LoadAsync(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        CancellationToken ct = default);

    Task<PlayerSnapshotSaveResult> SaveAsync(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        StoredPlayerSnapshot snapshot,
        string? writeLock,
        CancellationToken ct = default);
}
