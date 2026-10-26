using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlockBlast.Features.CloudServices.Test
{
    [TestFixture]
    public class CloudInitializationServiceTest
    {
        private class FakeUnityServicesGateway : IUnityServicesGateway
        {
            public bool IsServicesInitialized { get; set; }
            public bool IsSignedIn { get; set; }
            public string PlayerId { get; set; } = string.Empty;

            public int InitializeCallCount { get; private set; }
            public int SignInCallCount { get; private set; }
            public string LastEnvironment { get; private set; }

            public Exception InitializeException { get; set; }
            public Exception SignInException { get; set; }
            public TaskCompletionSource<bool> InitializeSignal { get; set; }

            public async Task InitializeServicesAsync(string environmentName)
            {
                InitializeCallCount++;
                LastEnvironment = environmentName;

                if (InitializeSignal != null)
                {
                    await InitializeSignal.Task;
                }

                if (InitializeException != null)
                {
                    throw InitializeException;
                }

                IsServicesInitialized = true;
            }

            public async Task SignInAnonymouslyAsync()
            {
                SignInCallCount++;

                if (SignInException != null)
                {
                    throw SignInException;
                }

                IsSignedIn = true;
                if (string.IsNullOrEmpty(PlayerId))
                {
                    PlayerId = "fake-player-123";
                }

                await Task.Yield();
            }
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.ClearAll();
        }

        [Test]
        public void EnvironmentResolver_ResolvesCorrectEnvironment()
        {
            Assert.AreEqual("development", CloudEnvironmentResolver.Resolve(isEditor: true, isDebugBuild: false));
            Assert.AreEqual("development", CloudEnvironmentResolver.Resolve(isEditor: false, isDebugBuild: true));
            Assert.AreEqual("development", CloudEnvironmentResolver.Resolve(isEditor: true, isDebugBuild: true));
            Assert.AreEqual("production", CloudEnvironmentResolver.Resolve(isEditor: false, isDebugBuild: false));
        }

        [Test]
        public async Task InitializeAsync_WhenSuccessful_InitializesCoreAndSignsIn()
        {
            var gateway = new FakeUnityServicesGateway();
            var service = new CloudInitializationService(gateway, "development");

            Assert.AreEqual(CloudInitializationState.NotStarted, service.State);
            Assert.IsFalse(service.IsReady);

            bool success = await service.InitializeAsync();

            Assert.IsTrue(success);
            Assert.AreEqual(CloudInitializationState.Ready, service.State);
            Assert.IsTrue(service.IsReady);
            Assert.AreEqual("development", service.EnvironmentName);
            Assert.AreEqual("fake-player-123", service.PlayerId);
            Assert.AreEqual(1, gateway.InitializeCallCount);
            Assert.AreEqual(1, gateway.SignInCallCount);
        }

        [Test]
        public async Task InitializeAsync_WhenAlreadySignedIn_ReusesCachedSessionWithoutCallingSignIn()
        {
            var gateway = new FakeUnityServicesGateway
            {
                IsServicesInitialized = true,
                IsSignedIn = true,
                PlayerId = "cached-session-789"
            };

            var service = new CloudInitializationService(gateway, "development");
            bool success = await service.InitializeAsync();

            Assert.IsTrue(success);
            Assert.AreEqual(CloudInitializationState.Ready, service.State);
            Assert.AreEqual("cached-session-789", service.PlayerId);
            Assert.AreEqual(0, gateway.InitializeCallCount);
            Assert.AreEqual(0, gateway.SignInCallCount);
        }

        [Test]
        public async Task InitializeAsync_ConcurrentCallers_ExecuteSingleFlight()
        {
            var gateway = new FakeUnityServicesGateway
            {
                InitializeSignal = new TaskCompletionSource<bool>()
            };

            var service = new CloudInitializationService(gateway, "development");

            Task<bool> task1 = service.InitializeAsync();
            Task<bool> task2 = service.InitializeAsync();

            Assert.AreSame(task1, task2, "Concurrent callers must receive the exact same initialization task.");
            Assert.AreEqual(CloudInitializationState.Initializing, service.State);

            gateway.InitializeSignal.SetResult(true);

            bool[] results = await Task.WhenAll(task1, task2);

            Assert.IsTrue(results[0]);
            Assert.IsTrue(results[1]);
            Assert.AreEqual(CloudInitializationState.Ready, service.State);
            Assert.AreEqual(1, gateway.InitializeCallCount);
            Assert.AreEqual(1, gateway.SignInCallCount);
        }

        [Test]
        public async Task InitializeAsync_WhenCoreInitFails_CatchesExceptionAndSetsUnavailable()
        {
            var gateway = new FakeUnityServicesGateway
            {
                InitializeException = new InvalidOperationException("Core service unreachable")
            };

            var service = new CloudInitializationService(gateway, "development");

            bool success = await service.InitializeAsync();

            Assert.IsFalse(success);
            Assert.AreEqual(CloudInitializationState.Unavailable, service.State);
            Assert.IsFalse(service.IsReady);
            Assert.IsEmpty(service.PlayerId);
            Assert.AreEqual(1, gateway.InitializeCallCount);
            Assert.AreEqual(0, gateway.SignInCallCount);
        }

        [Test]
        public async Task InitializeAsync_WhenSignInFails_CatchesExceptionAndSetsUnavailable()
        {
            var gateway = new FakeUnityServicesGateway
            {
                SignInException = new InvalidOperationException("Authentication server error")
            };

            var service = new CloudInitializationService(gateway, "development");

            bool success = await service.InitializeAsync();

            Assert.IsFalse(success);
            Assert.AreEqual(CloudInitializationState.Unavailable, service.State);
            Assert.IsFalse(service.IsReady);
            Assert.IsEmpty(service.PlayerId);
            Assert.AreEqual(1, gateway.InitializeCallCount);
            Assert.AreEqual(1, gateway.SignInCallCount);
        }

        [Test]
        public async Task InitializeAsync_WhenFailed_CanRetryAndSucceed()
        {
            var gateway = new FakeUnityServicesGateway
            {
                InitializeException = new Exception("Temporary network glitch")
            };

            var service = new CloudInitializationService(gateway, "development");

            bool firstAttempt = await service.InitializeAsync();
            Assert.IsFalse(firstAttempt);
            Assert.AreEqual(CloudInitializationState.Unavailable, service.State);

            // Clear the failure condition
            gateway.InitializeException = null;

            bool secondAttempt = await service.InitializeAsync();
            Assert.IsTrue(secondAttempt);
            Assert.AreEqual(CloudInitializationState.Ready, service.State);
            Assert.AreEqual("fake-player-123", service.PlayerId);
            Assert.AreEqual(2, gateway.InitializeCallCount);
            Assert.AreEqual(1, gateway.SignInCallCount);
        }

        [Test]
        public void UGSBootstrap_DuplicateHost_DoesNotOverwriteExistingService()
        {
            var originalService = new CloudInitializationService(new FakeUnityServicesGateway(), "development");
            ServiceLocator.Register<ICloudInitializationService>(originalService);

            var hostGo = new GameObject("TestUGSBootstrap");
            try
            {
                var host = hostGo.AddComponent<UGSBootstrap>();

                Assert.IsFalse(host.OwnsRegistration, "Duplicate UGSBootstrap must not own registration when another service is already registered.");
                Assert.IsTrue(ServiceLocator.TryGet<ICloudInitializationService>(out var currentService));
                Assert.AreSame(originalService, currentService, "Original service must not be overwritten by duplicate host.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public async Task PlayerId_ReflectsLiveGatewayIdentity_WhenGatewayIdentityChanges()
        {
            var gateway = new FakeUnityServicesGateway { PlayerId = "initial-player" };
            var service = new CloudInitializationService(gateway, "development");

            await service.InitializeAsync();
            Assert.AreEqual("initial-player", service.PlayerId);

            gateway.PlayerId = "switched-player";
            Assert.AreEqual("switched-player", service.PlayerId);
        }

        [Test]
        public async Task IsReady_ReturnsFalse_WhenGatewayIsSignedOut()
        {
            var gateway = new FakeUnityServicesGateway { PlayerId = "player-1" };
            var service = new CloudInitializationService(gateway, "development");

            await service.InitializeAsync();
            Assert.IsTrue(service.IsReady);

            gateway.IsSignedIn = false;
            Assert.IsFalse(service.IsReady);

            gateway.IsSignedIn = true;
            Assert.IsTrue(service.IsReady);
        }

        [Test]
        public async Task InitializeAsync_Reinitializes_WhenPreviouslyReadyButSignedOut()
        {
            var gateway = new FakeUnityServicesGateway { PlayerId = "player-1" };
            var service = new CloudInitializationService(gateway, "development");

            await service.InitializeAsync();
            Assert.AreEqual(1, gateway.SignInCallCount);

            gateway.IsSignedIn = false;
            gateway.PlayerId = "player-2";

            bool reinitSuccess = await service.InitializeAsync();
            Assert.IsTrue(reinitSuccess);
            Assert.AreEqual(2, gateway.SignInCallCount);
            Assert.AreEqual("player-2", service.PlayerId);
            Assert.IsTrue(service.IsReady);
        }
    }
}
