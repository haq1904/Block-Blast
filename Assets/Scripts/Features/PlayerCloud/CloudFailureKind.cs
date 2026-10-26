namespace BlockBlast.Features.PlayerCloud
{
    public enum CloudFailureKind
    {
        None,
        Cancelled,
        Offline,
        AuthenticationUnavailable,
        RateLimited,
        ServiceUnavailable,
        InvalidPayload,
        SchemaMismatch,
        InvalidResponse,
        Unknown
    }
}
