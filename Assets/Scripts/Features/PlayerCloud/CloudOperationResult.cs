using System;

namespace BlockBlast.Features.PlayerCloud
{
    public sealed class CloudOperationResult<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public CloudFailureKind FailureKind { get; }
        public bool IsRetryable { get; }
        public string ErrorCode { get; }
        public int? RetryAfterSeconds { get; }

        private CloudOperationResult(
            bool isSuccess,
            T value,
            CloudFailureKind failureKind,
            bool isRetryable,
            string errorCode,
            int? retryAfterSeconds)
        {
            IsSuccess = isSuccess;
            Value = value;
            FailureKind = failureKind;
            IsRetryable = isRetryable;
            ErrorCode = errorCode;
            RetryAfterSeconds = retryAfterSeconds;
        }

        public static CloudOperationResult<T> Success(T value)
        {
            return new CloudOperationResult<T>(
                true,
                value,
                CloudFailureKind.None,
                false,
                null,
                null);
        }

        public static CloudOperationResult<T> Failure(
            CloudFailureKind failureKind,
            bool isRetryable,
            string errorCode = null,
            int? retryAfterSeconds = null)
        {
            if (failureKind == CloudFailureKind.None)
            {
                throw new ArgumentException("FailureKind cannot be None for a failure result.", nameof(failureKind));
            }

            return new CloudOperationResult<T>(
                false,
                default,
                failureKind,
                isRetryable,
                errorCode,
                retryAfterSeconds);
        }
    }
}
