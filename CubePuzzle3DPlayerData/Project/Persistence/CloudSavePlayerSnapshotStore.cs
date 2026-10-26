using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CubePuzzle3DPlayerData.Domain;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace CubePuzzle3DPlayerData.Persistence;

public sealed class CloudSavePlayerSnapshotStore : IPlayerSnapshotStore
{
    private readonly ILogger? _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CloudSavePlayerSnapshotStore(ILogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<PlayerSnapshotLoadResult> LoadAsync(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        CancellationToken ct = default)
    {
        try
        {
            var response = await gameApiClient.CloudSaveData.GetProtectedItemsAsync(
                context,
                context.ServiceToken,
                context.ProjectId,
                context.PlayerId!,
                new List<string> { PlayerDataConstants.ProtectedDataKey },
                string.Empty,
                ct);

            var items = response.Data?.Results;
            if (items == null || items.Count == 0)
            {
                return PlayerSnapshotLoadResult.Missing();
            }

            Item? targetItem = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Key, PlayerDataConstants.ProtectedDataKey, StringComparison.Ordinal))
                {
                    targetItem = items[i];
                    break;
                }
            }

            if (targetItem == null || targetItem.Value == null)
            {
                return PlayerSnapshotLoadResult.Missing();
            }

            string? jsonStr = targetItem.Value switch
            {
                string s => s,
                _ => JsonSerializer.Serialize(targetItem.Value, s_jsonOptions)
            };

            if (string.IsNullOrWhiteSpace(jsonStr))
            {
                return PlayerSnapshotLoadResult.Missing();
            }

            StoredPlayerSnapshot? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<StoredPlayerSnapshot>(jsonStr, s_jsonOptions);
            }
            catch (Exception)
            {
                _logger?.LogWarning("Failed to deserialize stored player snapshot: corrupt JSON");
                return PlayerSnapshotLoadResult.Fail(PlayerDataConstants.FailureCodes.ServerDataCorrupt);
            }

            if (snapshot == null)
            {
                return PlayerSnapshotLoadResult.Fail(PlayerDataConstants.FailureCodes.ServerDataCorrupt);
            }

            if (snapshot.SchemaVersion != PlayerDataConstants.CurrentSchemaVersion)
            {
                _logger?.LogWarning("Unsupported stored schema version {Version}", snapshot.SchemaVersion);
                return PlayerSnapshotLoadResult.Fail(PlayerDataConstants.FailureCodes.ServerSchemaUnsupported);
            }

            if (snapshot.LifetimeStats == null)
            {
                return PlayerSnapshotLoadResult.Fail(PlayerDataConstants.FailureCodes.ServerDataCorrupt);
            }

            return PlayerSnapshotLoadResult.Success(snapshot, targetItem.WriteLock);
        }
        catch (ApiException ex)
        {
            _logger?.LogError("CloudSave load failed with status code {StatusCode}", ex.Response?.StatusCode);
            throw;
        }
    }

    public async Task<PlayerSnapshotSaveResult> SaveAsync(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        StoredPlayerSnapshot snapshot,
        string? writeLock,
        CancellationToken ct = default)
    {
        try
        {
            string json = JsonSerializer.Serialize(snapshot, s_jsonOptions);
            var setBody = new SetItemBody(PlayerDataConstants.ProtectedDataKey, json, writeLock ?? string.Empty);

            await gameApiClient.CloudSaveData.SetProtectedItemAsync(
                context,
                context.ServiceToken,
                context.ProjectId,
                context.PlayerId!,
                setBody,
                ct);

            return PlayerSnapshotSaveResult.Success();
        }
        catch (ApiException ex) when (ex.Response != null && (int)ex.Response.StatusCode == 409)
        {
            _logger?.LogInformation("CloudSave write conflict (409) encountered");
            return PlayerSnapshotSaveResult.Conflict();
        }
        catch (ApiException ex)
        {
            _logger?.LogError("CloudSave save failed with status code {StatusCode}", ex.Response?.StatusCode);
            throw;
        }
    }
}
