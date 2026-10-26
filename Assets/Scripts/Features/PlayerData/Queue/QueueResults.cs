namespace BlockBlast.Features.PlayerData
{
    public enum QueueEnqueueResult
    {
        Success,
        Duplicate,
        Full,
        InvalidPayload,
        StorageCorrupt
    }

    public enum QueueAcknowledgeResult
    {
        Success,
        NotFound,
        NotHead,
        StorageCorrupt
    }
}
