using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CubePuzzle3DPlayerData.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubmitGameResultStatus
{
    Accepted,
    Duplicate,
    Rejected,
    RetryableFailure
}

public sealed class SubmitGameResultResponse
{
    [JsonPropertyName("status")]
    public SubmitGameResultStatus Status { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("updatedSnapshot")]
    public GameCenterSnapshotContract? UpdatedSnapshot { get; set; }

    [JsonPropertyName("newlyUnlockedAchievementIds")]
    public List<string> NewlyUnlockedAchievementIds { get; set; } = new();

    [JsonPropertyName("isNewPersonalBest")]
    public bool IsNewPersonalBest { get; set; }

    [JsonPropertyName("isDuplicate")]
    public bool IsDuplicate { get; set; }

    public static SubmitGameResultResponse Accepted(
        GameCenterSnapshotContract snapshot,
        List<string> newlyUnlockedAchievementIds,
        bool isNewPersonalBest)
    {
        return new SubmitGameResultResponse
        {
            Status = SubmitGameResultStatus.Accepted,
            ErrorCode = null,
            UpdatedSnapshot = snapshot,
            NewlyUnlockedAchievementIds = newlyUnlockedAchievementIds,
            IsNewPersonalBest = isNewPersonalBest,
            IsDuplicate = false
        };
    }

    public static SubmitGameResultResponse Duplicate(GameCenterSnapshotContract snapshot)
    {
        return new SubmitGameResultResponse
        {
            Status = SubmitGameResultStatus.Duplicate,
            ErrorCode = null,
            UpdatedSnapshot = snapshot,
            NewlyUnlockedAchievementIds = new List<string>(),
            IsNewPersonalBest = false,
            IsDuplicate = true
        };
    }

    public static SubmitGameResultResponse Rejected(string errorCode)
    {
        return new SubmitGameResultResponse
        {
            Status = SubmitGameResultStatus.Rejected,
            ErrorCode = errorCode,
            UpdatedSnapshot = null,
            NewlyUnlockedAchievementIds = new List<string>(),
            IsNewPersonalBest = false,
            IsDuplicate = false
        };
    }

    public static SubmitGameResultResponse RetryableFailure(string errorCode)
    {
        return new SubmitGameResultResponse
        {
            Status = SubmitGameResultStatus.RetryableFailure,
            ErrorCode = errorCode,
            UpdatedSnapshot = null,
            NewlyUnlockedAchievementIds = new List<string>(),
            IsNewPersonalBest = false,
            IsDuplicate = false
        };
    }
}
