using System;
using CubePuzzle3DPlayerData.Contracts;

namespace CubePuzzle3DPlayerData.Domain;

public readonly struct ValidationResult
{
    public bool IsValid => ErrorCode == null;
    public string? ErrorCode { get; }

    private ValidationResult(string? errorCode)
    {
        ErrorCode = errorCode;
    }

    public static ValidationResult Success() => new(null);
    public static ValidationResult Fail(string errorCode) => new(errorCode);
}

public static class GameResultValidator
{
    public static ValidationResult Validate(
        string? playerId,
        GameResultRequest? request,
        long serverNowUnixSeconds)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.Unauthenticated);
        }

        if (request == null)
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.InvalidPayload);
        }

        if (request.SchemaVersion != PlayerDataConstants.CurrentSchemaVersion)
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.SchemaMismatch);
        }

        if (string.IsNullOrEmpty(request.GameSessionId) ||
            request.GameSessionId.Length < PlayerDataConstants.MinSessionIdLength ||
            request.GameSessionId.Length > PlayerDataConstants.MaxSessionIdLength)
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.InvalidSessionId);
        }

        for (int i = 0; i < request.GameSessionId.Length; i++)
        {
            char c = request.GameSessionId[i];
            bool isAllowed = (c >= 'a' && c <= 'z') ||
                             (c >= 'A' && c <= 'Z') ||
                             (c >= '0' && c <= '9') ||
                             c == '_' || c == '-';
            if (!isAllowed)
            {
                return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.InvalidSessionId);
            }
        }

        if (request.Score < 0 || request.Score > PlayerDataConstants.MaxScore ||
            request.LinesCleared < 0 || request.LinesCleared > PlayerDataConstants.MaxMetric ||
            request.BlocksPlaced < 0 || request.BlocksPlaced > PlayerDataConstants.MaxMetric ||
            request.BestCombo < 0 || request.BestCombo > PlayerDataConstants.MaxMetric)
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.MetricOutOfRange);
        }

        if (request.BestCombo > request.LinesCleared)
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.InvalidMetricRelation);
        }

        if (request.BlocksPlaced == 0 &&
            (request.Score != 0 || request.LinesCleared != 0 || request.BestCombo != 0))
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.InvalidMetricRelation);
        }

        long minAllowedTimestamp = serverNowUnixSeconds - PlayerDataConstants.AllowedPastSeconds;
        long maxAllowedTimestamp = serverNowUnixSeconds + PlayerDataConstants.AllowedFutureSeconds;
        if (request.CompletedAtUnixSeconds < minAllowedTimestamp ||
            request.CompletedAtUnixSeconds > maxAllowedTimestamp)
        {
            return ValidationResult.Fail(PlayerDataConstants.RejectionCodes.TimestampOutOfRange);
        }

        return ValidationResult.Success();
    }
}
