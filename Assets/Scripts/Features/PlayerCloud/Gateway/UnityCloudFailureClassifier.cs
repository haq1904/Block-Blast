using System;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace BlockBlast.Features.PlayerCloud.Gateway
{
    public readonly struct ClassifiedCloudFailure
    {
        public CloudFailureKind Kind { get; }
        public bool IsRetryable { get; }
        public string ErrorCode { get; }
        public int? RetryAfterSeconds { get; }

        public ClassifiedCloudFailure(CloudFailureKind kind, bool isRetryable, string errorCode, int? retryAfterSeconds = null)
        {
            Kind = kind;
            IsRetryable = isRetryable;
            ErrorCode = errorCode;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }

    public static class UnityCloudFailureClassifier
    {
        public static ClassifiedCloudFailure Classify(Exception exception)
        {
            if (exception == null)
            {
                return new ClassifiedCloudFailure(CloudFailureKind.Unknown, false, PlayerCloudConstants.ErrorCodes.Unknown);
            }

            if (exception is AggregateException agg && agg.InnerExceptions.Count > 0)
            {
                return Classify(agg.Flatten().InnerExceptions[0]);
            }

            if (exception is OperationCanceledException)
            {
                return new ClassifiedCloudFailure(CloudFailureKind.Cancelled, false, PlayerCloudConstants.ErrorCodes.Cancelled);
            }

            if (exception is TimeoutException)
            {
                return new ClassifiedCloudFailure(CloudFailureKind.ServiceUnavailable, true, PlayerCloudConstants.ErrorCodes.ServiceUnavailable);
            }

            if (exception is CloudCodeRateLimitedException codeRateLimited)
            {
                int retryAfter = CapRetryAfter(codeRateLimited.RetryAfter);
                return new ClassifiedCloudFailure(CloudFailureKind.RateLimited, true, PlayerCloudConstants.ErrorCodes.RateLimited, retryAfter);
            }

            if (exception is CloudSaveRateLimitedException saveRateLimited)
            {
                int retryAfter = CapRetryAfter((int)Math.Ceiling(saveRateLimited.RetryAfter));
                return new ClassifiedCloudFailure(CloudFailureKind.RateLimited, true, PlayerCloudConstants.ErrorCodes.RateLimited, retryAfter);
            }

            if (exception is CloudCodeException codeEx)
            {
                return ClassifyCloudCodeReason(codeEx.Reason, codeEx.ErrorCode);
            }

            if (exception is CloudSaveException saveEx)
            {
                return ClassifyCloudSaveReason(saveEx.Reason, saveEx.ErrorCode);
            }

            if (exception is FormatException)
            {
                return new ClassifiedCloudFailure(CloudFailureKind.InvalidResponse, false, PlayerCloudConstants.ErrorCodes.InvalidResponse);
            }

            if (exception is RequestFailedException reqEx)
            {
                return new ClassifiedCloudFailure(CloudFailureKind.ServiceUnavailable, true, PlayerCloudConstants.ErrorCodes.ServiceUnavailable);
            }

            return new ClassifiedCloudFailure(CloudFailureKind.Unknown, false, PlayerCloudConstants.ErrorCodes.Unknown);
        }

        private static ClassifiedCloudFailure ClassifyCloudCodeReason(CloudCodeExceptionReason reason, int rawErrorCode)
        {
            switch (reason)
            {
                case CloudCodeExceptionReason.NoInternetConnection:
                    return new ClassifiedCloudFailure(CloudFailureKind.Offline, true, PlayerCloudConstants.ErrorCodes.Offline);

                case CloudCodeExceptionReason.TooManyRequests:
                    return new ClassifiedCloudFailure(CloudFailureKind.RateLimited, true, PlayerCloudConstants.ErrorCodes.RateLimited, PlayerCloudConstants.MaxRetryAfterCapSeconds);

                case CloudCodeExceptionReason.ServiceUnavailable:
                    return new ClassifiedCloudFailure(CloudFailureKind.ServiceUnavailable, true, PlayerCloudConstants.ErrorCodes.ServiceUnavailable);

                case CloudCodeExceptionReason.Unauthorized:
                case CloudCodeExceptionReason.AccessTokenMissing:
                case CloudCodeExceptionReason.PlayerIdMissing:
                case CloudCodeExceptionReason.ProjectIdMissing:
                    return new ClassifiedCloudFailure(CloudFailureKind.AuthenticationUnavailable, false, PlayerCloudConstants.ErrorCodes.Unauthenticated);

                case CloudCodeExceptionReason.InvalidArgument:
                case CloudCodeExceptionReason.NotFound:
                case CloudCodeExceptionReason.ScriptError:
                case CloudCodeExceptionReason.SubscriptionError:
                    return new ClassifiedCloudFailure(CloudFailureKind.InvalidResponse, false, PlayerCloudConstants.ErrorCodes.InvalidResponse);

                default:
                    return new ClassifiedCloudFailure(CloudFailureKind.Unknown, false, PlayerCloudConstants.ErrorCodes.Unknown);
            }
        }

        private static ClassifiedCloudFailure ClassifyCloudSaveReason(CloudSaveExceptionReason reason, int rawErrorCode)
        {
            switch (reason)
            {
                case CloudSaveExceptionReason.NoInternetConnection:
                    return new ClassifiedCloudFailure(CloudFailureKind.Offline, true, PlayerCloudConstants.ErrorCodes.Offline);

                case CloudSaveExceptionReason.TooManyRequests:
                    return new ClassifiedCloudFailure(CloudFailureKind.RateLimited, true, PlayerCloudConstants.ErrorCodes.RateLimited, PlayerCloudConstants.MaxRetryAfterCapSeconds);

                case CloudSaveExceptionReason.ServiceUnavailable:
                    return new ClassifiedCloudFailure(CloudFailureKind.ServiceUnavailable, true, PlayerCloudConstants.ErrorCodes.ServiceUnavailable);

                case CloudSaveExceptionReason.Unauthorized:
                case CloudSaveExceptionReason.AccessTokenMissing:
                case CloudSaveExceptionReason.PlayerIdMissing:
                case CloudSaveExceptionReason.ProjectIdMissing:
                    return new ClassifiedCloudFailure(CloudFailureKind.AuthenticationUnavailable, false, PlayerCloudConstants.ErrorCodes.Unauthenticated);

                case CloudSaveExceptionReason.InvalidArgument:
                case CloudSaveExceptionReason.KeyLimitExceeded:
                case CloudSaveExceptionReason.NotFound:
                case CloudSaveExceptionReason.Conflict:
                    return new ClassifiedCloudFailure(CloudFailureKind.InvalidResponse, false, PlayerCloudConstants.ErrorCodes.InvalidResponse);

                default:
                    return new ClassifiedCloudFailure(CloudFailureKind.Unknown, false, PlayerCloudConstants.ErrorCodes.Unknown);
            }
        }

        private static int CapRetryAfter(int seconds)
        {
            if (seconds <= 0)
            {
                return 1;
            }

            return Math.Min(seconds, PlayerCloudConstants.MaxRetryAfterCapSeconds);
        }
    }
}
