namespace CubePuzzle3DPlayerData;

public static class PlayerDataConstants
{
    public const string ModuleName = "CubePuzzle3DPlayerData";
    public const string SubmitFunctionName = "SubmitGameResult";
    public const string ProtectedDataKey = "game_center_v1";
    public const int CurrentSchemaVersion = 1;

    public const int MaxRecentGames = 7;
    public const int MaxInsights = 2;
    public const int MaxReceipts = 256;
    public const int MaxWriteConflictAttempts = 3;

    public const int ConflictRetryDelayMs1 = 25;
    public const int ConflictRetryDelayMs2 = 75;

    public const int MaxScore = 2_000_000_000;
    public const int MaxMetric = 10_000_000;
    public const int MinSessionIdLength = 8;
    public const int MaxSessionIdLength = 64;

    public const long AllowedPastSeconds = 365L * 24L * 60L * 60L; // 365 days
    public const long AllowedFutureSeconds = 300L; // 5 minutes

    public static class RejectionCodes
    {
        public const string Unauthenticated = "UNAUTHENTICATED";
        public const string InvalidPayload = "INVALID_PAYLOAD";
        public const string SchemaMismatch = "SCHEMA_MISMATCH";
        public const string InvalidSessionId = "INVALID_SESSION_ID";
        public const string MetricOutOfRange = "METRIC_OUT_OF_RANGE";
        public const string InvalidMetricRelation = "INVALID_METRIC_RELATION";
        public const string TimestampOutOfRange = "TIMESTAMP_OUT_OF_RANGE";
    }

    public static class FailureCodes
    {
        public const string WriteConflictRetryExhausted = "WRITE_CONFLICT_RETRY_EXHAUSTED";
        public const string ServerSchemaUnsupported = "SERVER_SCHEMA_UNSUPPORTED";
        public const string ServerDataCorrupt = "SERVER_DATA_CORRUPT";
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    }
}
