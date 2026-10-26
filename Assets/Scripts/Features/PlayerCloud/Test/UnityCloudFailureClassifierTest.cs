using System;
using BlockBlast.Features.PlayerCloud.Gateway;
using NUnit.Framework;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class UnityCloudFailureClassifierTest
    {
        [Test]
        public void Classify_Null_ReturnsUnknown()
        {
            var result = UnityCloudFailureClassifier.Classify(null);
            Assert.AreEqual(CloudFailureKind.Unknown, result.Kind);
            Assert.IsFalse(result.IsRetryable);
            Assert.AreEqual(PlayerCloudConstants.ErrorCodes.Unknown, result.ErrorCode);
        }

        [Test]
        public void Classify_OperationCanceledException_ReturnsCancelled()
        {
            var result = UnityCloudFailureClassifier.Classify(new OperationCanceledException());
            Assert.AreEqual(CloudFailureKind.Cancelled, result.Kind);
            Assert.IsFalse(result.IsRetryable);
            Assert.AreEqual(PlayerCloudConstants.ErrorCodes.Cancelled, result.ErrorCode);
        }

        [Test]
        public void Classify_CloudCodeRateLimitedException_CapsRetryAfterAtFiveSeconds()
        {
            // Reflection or internal constructor test
            var ex = Activator.CreateInstance(
                typeof(CloudCodeRateLimitedException),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[] { CloudCodeExceptionReason.TooManyRequests, 429, "Rate limited", null, 60 },
                null) as CloudCodeRateLimitedException;

            var result = UnityCloudFailureClassifier.Classify(ex);

            Assert.AreEqual(CloudFailureKind.RateLimited, result.Kind);
            Assert.IsTrue(result.IsRetryable);
            Assert.AreEqual(5, result.RetryAfterSeconds);
        }

        [Test]
        public void Classify_CloudSaveRateLimitedException_CapsRetryAfterAtFiveSeconds()
        {
            var ex = Activator.CreateInstance(
                typeof(CloudSaveRateLimitedException),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[] { CloudSaveExceptionReason.TooManyRequests, 429, "Rate limited", 12.5f, null },
                null) as CloudSaveRateLimitedException;

            var result = UnityCloudFailureClassifier.Classify(ex);

            Assert.AreEqual(CloudFailureKind.RateLimited, result.Kind);
            Assert.IsTrue(result.IsRetryable);
            Assert.AreEqual(5, result.RetryAfterSeconds);
        }

        [Test]
        public void Classify_CloudCodeException_NoInternet_ReturnsOfflineRetryable()
        {
            var ex = Activator.CreateInstance(
                typeof(CloudCodeException),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[] { CloudCodeExceptionReason.NoInternetConnection, 0, "No internet" },
                null) as CloudCodeException;

            var result = UnityCloudFailureClassifier.Classify(ex);

            Assert.AreEqual(CloudFailureKind.Offline, result.Kind);
            Assert.IsTrue(result.IsRetryable);
        }

        [Test]
        public void Classify_CloudSaveException_Unauthorized_ReturnsAuthUnavailableNonRetryable()
        {
            var ex = Activator.CreateInstance(
                typeof(CloudSaveException),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[] { CloudSaveExceptionReason.Unauthorized, 401, "Unauthorized", null },
                null) as CloudSaveException;

            var result = UnityCloudFailureClassifier.Classify(ex);

            Assert.AreEqual(CloudFailureKind.AuthenticationUnavailable, result.Kind);
            Assert.IsFalse(result.IsRetryable);
        }

        [Test]
        public void Classify_FormatException_ReturnsInvalidResponseNonRetryable()
        {
            var result = UnityCloudFailureClassifier.Classify(new FormatException("Corrupt JSON"));

            Assert.AreEqual(CloudFailureKind.InvalidResponse, result.Kind);
            Assert.IsFalse(result.IsRetryable);
        }

        [Test]
        public void Classify_RequestFailedException_ReturnsServiceUnavailableRetryable()
        {
            var ex = new RequestFailedException(503, "Service unavailable");
            var result = UnityCloudFailureClassifier.Classify(ex);

            Assert.AreEqual(CloudFailureKind.ServiceUnavailable, result.Kind);
            Assert.IsTrue(result.IsRetryable);
        }

        [Test]
        public void Classify_TimeoutException_ReturnsServiceUnavailableRetryable()
        {
            var result = UnityCloudFailureClassifier.Classify(new TimeoutException("Request timed out"));
            Assert.AreEqual(CloudFailureKind.ServiceUnavailable, result.Kind);
            Assert.IsTrue(result.IsRetryable);
        }
    }
}
