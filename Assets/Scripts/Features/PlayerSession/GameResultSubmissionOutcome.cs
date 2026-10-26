using BlockBlast.Features.PlayerCloud;

namespace BlockBlast.Features.PlayerSession
{
    public enum GameResultSubmissionStatus
    {
        Submitted,
        Deferred,
        Rejected
    }

    public sealed class GameResultSubmissionOutcome
    {
        public string GameSessionId { get; }
        public GameResultSubmissionStatus Status { get; }
        public SubmitGameResultResponse Response { get; }
        public CloudFailureKind FailureKind { get; }
        public string ErrorCode { get; }

        public GameResultSubmissionOutcome(
            string gameSessionId,
            GameResultSubmissionStatus status,
            SubmitGameResultResponse response = null,
            CloudFailureKind failureKind = CloudFailureKind.None,
            string errorCode = null)
        {
            GameSessionId = gameSessionId;
            Status = status;
            Response = response;
            FailureKind = failureKind;
            ErrorCode = errorCode;
        }

        public static GameResultSubmissionOutcome Submitted(string sessionId, SubmitGameResultResponse response)
        {
            return new GameResultSubmissionOutcome(
                sessionId,
                GameResultSubmissionStatus.Submitted,
                response,
                CloudFailureKind.None,
                null);
        }

        public static GameResultSubmissionOutcome Deferred(string sessionId, CloudFailureKind failureKind, string errorCode)
        {
            return new GameResultSubmissionOutcome(
                sessionId,
                GameResultSubmissionStatus.Deferred,
                null,
                failureKind,
                errorCode);
        }

        public static GameResultSubmissionOutcome Rejected(string sessionId, CloudFailureKind failureKind, string errorCode)
        {
            return new GameResultSubmissionOutcome(
                sessionId,
                GameResultSubmissionStatus.Rejected,
                null,
                failureKind,
                errorCode);
        }
    }
}
