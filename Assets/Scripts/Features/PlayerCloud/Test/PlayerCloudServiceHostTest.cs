using System.Reflection;
using System.Threading.Tasks;
using BlockBlast.Core;
using BlockBlast.Features.CloudServices;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class PlayerCloudServiceHostTest
    {
        private GameObject _hostObject;
        private FakeInitService _initService;

        [SetUp]
        public void SetUp()
        {
            if (ServiceLocator.TryGet<IPlayerCloudService>(out var existingService))
            {
                ServiceLocator.Unregister<IPlayerCloudService>(existingService);
            }

            if (ServiceLocator.TryGet<ICloudInitializationService>(out var existingInit))
            {
                ServiceLocator.Unregister<ICloudInitializationService>(existingInit);
            }

            _initService = new FakeInitService();
            ServiceLocator.Register<ICloudInitializationService>(_initService);

            _hostObject = new GameObject("PlayerCloudServiceHostTest");
        }

        [TearDown]
        public void TearDown()
        {
            if (_hostObject != null)
            {
                Object.DestroyImmediate(_hostObject);
            }

            if (ServiceLocator.TryGet<IPlayerCloudService>(out var existingService))
            {
                ServiceLocator.Unregister<IPlayerCloudService>(existingService);
            }

            if (ServiceLocator.TryGet<ICloudInitializationService>(out var existingInit))
            {
                ServiceLocator.Unregister<ICloudInitializationService>(existingInit);
            }
        }

        [Test]
        public void Start_RegistersServiceInServiceLocator()
        {
            var host = _hostObject.AddComponent<PlayerCloudServiceHost>();
            InvokePrivateMethod(host, "Start");

            Assert.IsTrue(host.OwnsRegistration);
            Assert.IsTrue(ServiceLocator.TryGet<IPlayerCloudService>(out var service));
            Assert.IsNotNull(service);
        }

        [Test]
        public void Start_WhenAlreadyRegistered_DisablesDuplicateWithoutOverwriting()
        {
            var host1 = _hostObject.AddComponent<PlayerCloudServiceHost>();
            InvokePrivateMethod(host1, "Start");

            var secondObject = new GameObject("DuplicateHostObject");
            try
            {
                var host2 = secondObject.AddComponent<PlayerCloudServiceHost>();
                InvokePrivateMethod(host2, "Start");

                Assert.IsFalse(host2.OwnsRegistration);
                Assert.IsFalse(host2.enabled);
                Assert.IsTrue(host1.OwnsRegistration);
            }
            finally
            {
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void OnDestroy_UnregistersService_WhenOwningRegistration()
        {
            var host = _hostObject.AddComponent<PlayerCloudServiceHost>();
            InvokePrivateMethod(host, "Start");

            Assert.IsTrue(ServiceLocator.TryGet<IPlayerCloudService>(out _));

            InvokePrivateMethod(host, "OnDestroy");

            Assert.IsFalse(ServiceLocator.TryGet<IPlayerCloudService>(out _));
            Assert.IsFalse(host.OwnsRegistration);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(instance, null);
        }

        private sealed class FakeInitService : ICloudInitializationService
        {
            public CloudInitializationState State => CloudInitializationState.Ready;
            public string EnvironmentName => "development";
            public string PlayerId => "test_player";
            public bool IsReady => true;

            public Task<bool> InitializeAsync() => Task.FromResult(true);
        }
    }
}
