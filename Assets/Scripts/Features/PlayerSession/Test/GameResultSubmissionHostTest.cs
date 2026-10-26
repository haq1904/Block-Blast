using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Core;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.PlayerSession.Test
{
    [TestFixture]
    public sealed class GameResultSubmissionHostTest
    {
        private GameObject _go;
        private FakeCloudService _cloudService;

        [SetUp]
        public void SetUp()
        {
            if (ServiceLocator.TryGet<IGameResultSubmissionService>(out var existingSubmission))
            {
                ServiceLocator.Unregister<IGameResultSubmissionService>(existingSubmission);
            }

            if (ServiceLocator.TryGet<IPlayerCloudService>(out var existingCloud))
            {
                ServiceLocator.Unregister<IPlayerCloudService>(existingCloud);
            }

            _cloudService = new FakeCloudService();
            ServiceLocator.Register<IPlayerCloudService>(_cloudService);

            _go = new GameObject("GameResultSubmissionHostTest");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }

            if (ServiceLocator.TryGet<IGameResultSubmissionService>(out var existingSubmission))
            {
                ServiceLocator.Unregister<IGameResultSubmissionService>(existingSubmission);
            }

            if (ServiceLocator.TryGet<IPlayerCloudService>(out var existingCloud))
            {
                ServiceLocator.Unregister<IPlayerCloudService>(existingCloud);
            }
        }

        [Test]
        public void Start_RegistersServiceInServiceLocator()
        {
            var host = _go.AddComponent<GameResultSubmissionHost>();
            InvokePrivateMethod(host, "Start");

            Assert.IsTrue(host.OwnsRegistration);
            Assert.IsTrue(ServiceLocator.TryGet<IGameResultSubmissionService>(out var service));
            Assert.IsNotNull(service);
        }

        [Test]
        public void Start_DuplicateHost_DisablesWithoutOverwritingExistingOwner()
        {
            var host1 = _go.AddComponent<GameResultSubmissionHost>();
            InvokePrivateMethod(host1, "Start");

            var secondGo = new GameObject("DuplicateSubmissionHostObject");
            try
            {
                var host2 = secondGo.AddComponent<GameResultSubmissionHost>();
                InvokePrivateMethod(host2, "Start");

                Assert.IsFalse(host2.OwnsRegistration);
                Assert.IsFalse(host2.enabled);
                Assert.IsTrue(host1.OwnsRegistration);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondGo);
            }
        }

        [Test]
        public void OnDestroy_UnregistersService_WhenOwningRegistration()
        {
            var host = _go.AddComponent<GameResultSubmissionHost>();
            InvokePrivateMethod(host, "Start");

            Assert.IsTrue(ServiceLocator.TryGet<IGameResultSubmissionService>(out _));

            InvokePrivateMethod(host, "OnDestroy");

            Assert.IsFalse(ServiceLocator.TryGet<IGameResultSubmissionService>(out _));
            Assert.IsFalse(host.OwnsRegistration);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(instance, null);
        }

        private sealed class FakeCloudService : IPlayerCloudService
        {
            public CloudSyncState State => CloudSyncState.Ready;
            public GameCenterSnapshot CachedSnapshot => null;

            public Task<CloudOperationResult<GameCenterSnapshot>> LoadSnapshotAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(null));
            }

            public Task<CloudOperationResult<SubmitGameResultResponse>> SubmitGameResultAsync(GameResult result, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<SubmitGameResultResponse>.Success(null));
            }

            public Task<CloudOperationResult<GameCenterSnapshot>> ResetForIdentityChangeAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(null));
            }
        }
    }
}
