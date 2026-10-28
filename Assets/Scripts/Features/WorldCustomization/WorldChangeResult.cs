namespace BlockBlast.Features.WorldCustomization
{
    public enum WorldChangeStatus
    {
        Success,
        Cancelled,
        Failed,
        Busy
    }

    public readonly struct WorldChangeResult
    {
        public WorldChangeStatus Status { get; }
        public string ErrorMessage { get; }

        public bool IsSuccess => Status == WorldChangeStatus.Success;

        public WorldChangeResult(WorldChangeStatus status, string errorMessage = null)
        {
            Status = status;
            ErrorMessage = errorMessage;
        }

        public static WorldChangeResult Success() => new WorldChangeResult(WorldChangeStatus.Success);
        public static WorldChangeResult Cancelled(string reason = null) => new WorldChangeResult(WorldChangeStatus.Cancelled, reason);
        public static WorldChangeResult Failed(string error) => new WorldChangeResult(WorldChangeStatus.Failed, error);
        public static WorldChangeResult Busy(string reason = "A world swap is already in progress.") => new WorldChangeResult(WorldChangeStatus.Busy, reason);
    }
}
