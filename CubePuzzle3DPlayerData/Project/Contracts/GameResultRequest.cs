using System.Text.Json.Serialization;

namespace CubePuzzle3DPlayerData.Contracts;

public sealed class GameResultRequest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("gameSessionId")]
    public string GameSessionId { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("linesCleared")]
    public int LinesCleared { get; set; }

    [JsonPropertyName("blocksPlaced")]
    public int BlocksPlaced { get; set; }

    [JsonPropertyName("bestCombo")]
    public int BestCombo { get; set; }

    [JsonPropertyName("completedAtUnixSeconds")]
    public long CompletedAtUnixSeconds { get; set; }
}
