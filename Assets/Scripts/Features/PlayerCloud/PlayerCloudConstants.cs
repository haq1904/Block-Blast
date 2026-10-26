namespace BlockBlast.Features.PlayerCloud
{
    public static class PlayerCloudConstants
    {
        public const string ProtectedPlayerDataKey = "game_center_v1";
        public const string SnapshotCacheFileName = "game_center_snapshot_v1.json";
        public const string PlayerDataDirectoryName = "PlayerData";

        public const int MaxRetryAttempts = 3;
        public const int DefaultRetryDelay1Ms = 250;
        public const int DefaultRetryDelay2Ms = 1000;
        public const int MaxRetryAfterCapSeconds = 5;

        public static class ErrorCodes
        {
            public const string Offline = "OFFLINE";
            public const string RateLimited = "RATE_LIMITED";
            public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
            public const string Unauthenticated = "UNAUTHENTICATED";
            public const string SchemaMismatch = "SCHEMA_MISMATCH";
            public const string InvalidPayload = "INVALID_PAYLOAD";
            public const string InvalidResponse = "INVALID_RESPONSE";
            public const string Cancelled = "CANCELLED";
            public const string Unknown = "UNKNOWN";
        }
    }
}
