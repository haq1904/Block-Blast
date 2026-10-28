using System.Collections.Generic;
using BlockBlast.Core.Feedback;
using BlockBlast.Features.Feedback;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class FeedbackSettingsControllerTest
    {
        private class FakeFeedbackSettingsStore : IFeedbackSettingsStore
        {
            public readonly Dictionary<string, bool> Storage = new Dictionary<string, bool>();
            public int FlushCount { get; private set; }
            public bool IsDirty { get; private set; }

            public bool HasKey(string key) => Storage.ContainsKey(key);

            public bool GetBool(string key, bool defaultValue = false) =>
                Storage.TryGetValue(key, out var val) ? val : defaultValue;

            public void SetBool(string key, bool value)
            {
                Storage[key] = value;
                IsDirty = true;
            }

            public void Flush()
            {
                if (IsDirty)
                {
                    FlushCount++;
                    IsDirty = false;
                }
            }
        }

        private class FakeHapticService : IHapticService
        {
            public bool Enabled { get; set; } = true;
            public HapticCapabilities Capabilities => HapticCapabilities.BasicPulse;
            public bool TryPlay(HapticProfileSO profile, float runtimeIntensityScale = 1f) => Enabled;
        }

        private class FakeCameraShakeService : ICameraShakeService
        {
            public bool Enabled { get; set; } = true;
            public bool IsSupported => true;
            public bool TryPlay(in CameraShakeRequest request) => Enabled;
        }

        [Test]
        public void HapticSettings_WhenKeyMissing_DefaultsToServiceEnabled()
        {
            var service = new FakeHapticService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            var controller = new HapticSettingsController(service, store);

            bool result = controller.LoadAndApply();

            Assert.IsTrue(result);
            Assert.IsTrue(service.Enabled);
            Assert.IsFalse(store.HasKey(HapticSettingsController.PreferenceKey));
        }

        [Test]
        public void HapticSettings_WhenKeyPresent_AppliesStoredValue()
        {
            var service = new FakeHapticService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            store.SetBool(HapticSettingsController.PreferenceKey, false);
            var controller = new HapticSettingsController(service, store);

            bool result = controller.LoadAndApply();

            Assert.IsFalse(result);
            Assert.IsFalse(service.Enabled);
        }

        [Test]
        public void HapticSettings_SetEnabled_UpdatesServiceAndStoreWithoutFlushing()
        {
            var service = new FakeHapticService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            var controller = new HapticSettingsController(service, store);

            controller.SetEnabled(false);

            Assert.IsFalse(service.Enabled);
            Assert.IsTrue(store.HasKey(HapticSettingsController.PreferenceKey));
            Assert.IsFalse(store.GetBool(HapticSettingsController.PreferenceKey));
            Assert.AreEqual(0, store.FlushCount, "SetEnabled should not flush immediately.");

            controller.Flush();
            Assert.AreEqual(1, store.FlushCount);
        }

        [Test]
        public void CameraShakeSettings_WhenKeyMissing_DefaultsToServiceEnabled()
        {
            var service = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            var controller = new CameraShakeSettingsController(service, store);

            bool result = controller.LoadAndApply();

            Assert.IsTrue(result);
            Assert.IsTrue(service.Enabled);
            Assert.IsFalse(store.HasKey(CameraShakeSettingsController.PreferenceKey));
        }

        [Test]
        public void CameraShakeSettings_WhenKeyPresent_AppliesStoredValue()
        {
            var service = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            store.SetBool(CameraShakeSettingsController.PreferenceKey, false);
            var controller = new CameraShakeSettingsController(service, store);

            bool result = controller.LoadAndApply();

            Assert.IsFalse(result);
            Assert.IsFalse(service.Enabled);
        }

        [Test]
        public void CameraShakeSettings_SetEnabled_UpdatesServiceAndStoreWithoutFlushing()
        {
            var service = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            var controller = new CameraShakeSettingsController(service, store);

            controller.SetEnabled(false);

            Assert.IsFalse(service.Enabled);
            Assert.IsTrue(store.HasKey(CameraShakeSettingsController.PreferenceKey));
            Assert.IsFalse(store.GetBool(CameraShakeSettingsController.PreferenceKey));
            Assert.AreEqual(0, store.FlushCount, "SetEnabled should not flush immediately.");

            controller.Flush();
            Assert.AreEqual(1, store.FlushCount);
        }

        [Test]
        public void FeatureIndependence_HapticAndCameraShake_UseIndependentKeysAndServices()
        {
            var hapticService = new FakeHapticService { Enabled = true };
            var cameraService = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();

            var hapticController = new HapticSettingsController(hapticService, store);
            var cameraController = new CameraShakeSettingsController(cameraService, store);

            hapticController.SetEnabled(false);

            Assert.IsFalse(hapticService.Enabled);
            Assert.IsTrue(cameraService.Enabled, "Changing haptics must not mutate camera shake service.");
            Assert.IsFalse(store.GetBool(HapticSettingsController.PreferenceKey));
            Assert.IsFalse(store.HasKey(CameraShakeSettingsController.PreferenceKey));

            cameraController.SetEnabled(true);

            Assert.IsTrue(cameraService.Enabled);
            Assert.IsFalse(hapticService.Enabled, "Changing camera shake must not mutate haptic service.");
            Assert.IsTrue(store.GetBool(CameraShakeSettingsController.PreferenceKey));
        }

        [Test]
        public void MissingService_LoadAndApply_FailsSafelyWithoutThrowing()
        {
            var store = new FakeFeedbackSettingsStore();
            var hapticController = new HapticSettingsController(null, store);
            var cameraController = new CameraShakeSettingsController(null, store);

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(hapticController.LoadAndApply());
                Assert.IsFalse(hapticController.IsAvailable);
                hapticController.SetEnabled(true);

                Assert.IsFalse(cameraController.LoadAndApply());
                Assert.IsFalse(cameraController.IsAvailable);
                cameraController.SetEnabled(true);
            });
        }

        [Test]
        public void PlayerPrefsFeedbackSettingsStore_SetAndFlush_OnlySavesWhenDirty()
        {
            string testKey = "Test.Feedback.Key." + System.Guid.NewGuid().ToString("N");
            try
            {
                var store = new PlayerPrefsFeedbackSettingsStore();
                Assert.IsFalse(store.HasKey(testKey));
                Assert.IsTrue(store.GetBool(testKey, defaultValue: true));

                store.SetBool(testKey, false);
                Assert.IsTrue(store.HasKey(testKey));
                Assert.IsFalse(store.GetBool(testKey, defaultValue: true));

                Assert.DoesNotThrow(() => store.Flush());
            }
            finally
            {
                PlayerPrefs.DeleteKey(testKey);
                PlayerPrefs.Save();
            }
        }

        // =========================================================================
        // FeedbackSettingsService (Combined Service) Tests
        // =========================================================================

        [Test]
        public void FeedbackSettingsService_WhenKeysMissing_DefaultsToEnabledWithoutSaving()
        {
            var haptic = new FakeHapticService { Enabled = true };
            var camera = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();

            var service = new FeedbackSettingsService(store, () => haptic, () => camera);
            service.TryBindAvailableServices();

            Assert.IsTrue(service.IsHapticAvailable);
            Assert.IsTrue(service.IsHapticEnabled);
            Assert.IsTrue(service.IsCameraShakeAvailable);
            Assert.IsTrue(service.IsCameraShakeEnabled);
            Assert.IsFalse(store.HasKey(HapticSettingsController.PreferenceKey));
            Assert.IsFalse(store.HasKey(CameraShakeSettingsController.PreferenceKey));
            Assert.AreEqual(0, store.FlushCount);
        }

        [Test]
        public void FeedbackSettingsService_WhenKeysPresent_RestoresAndAppliesToServices()
        {
            var haptic = new FakeHapticService { Enabled = true };
            var camera = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            store.SetBool(HapticSettingsController.PreferenceKey, false);
            store.SetBool(CameraShakeSettingsController.PreferenceKey, true);

            var service = new FeedbackSettingsService(store, () => haptic, () => camera);
            service.TryBindAvailableServices();

            Assert.IsFalse(haptic.Enabled, "Stored false must be applied to haptic service.");
            Assert.IsTrue(camera.Enabled, "Stored true must be applied to camera shake service.");
            Assert.IsFalse(service.IsHapticEnabled);
            Assert.IsTrue(service.IsCameraShakeEnabled);
        }

        [Test]
        public void FeedbackSettingsService_HapticEarlyBind_AppliesWithoutCameraService()
        {
            var haptic = new FakeHapticService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();
            store.SetBool(HapticSettingsController.PreferenceKey, false);

            // Camera service is not available initially
            var service = new FeedbackSettingsService(store, () => haptic, () => null);
            service.TryBindAvailableServices();

            Assert.IsTrue(service.IsHapticAvailable);
            Assert.IsFalse(service.IsHapticEnabled);
            Assert.IsFalse(haptic.Enabled);

            Assert.IsFalse(service.IsCameraShakeAvailable);
            Assert.IsFalse(service.AllServicesBound);
        }

        [Test]
        public void FeedbackSettingsService_CameraLateBind_AppliesStoredValueWhenServiceBecomesAvailable()
        {
            var haptic = new FakeHapticService { Enabled = true };
            FakeCameraShakeService camera = null;
            var store = new FakeFeedbackSettingsStore();
            store.SetBool(CameraShakeSettingsController.PreferenceKey, false);

            var service = new FeedbackSettingsService(store, () => haptic, () => camera);

            // First bind: only haptic available
            service.TryBindAvailableServices();
            Assert.IsFalse(service.IsCameraShakeAvailable);

            // Camera service appears later (e.g. WorldScene loaded)
            camera = new FakeCameraShakeService { Enabled = true };
            service.TryBindAvailableServices();

            Assert.IsTrue(service.IsCameraShakeAvailable);
            Assert.IsFalse(camera.Enabled, "Camera service must receive stored false preference upon late binding.");
            Assert.IsFalse(service.IsCameraShakeEnabled);
            Assert.IsTrue(service.AllServicesBound);
        }

        [Test]
        public void FeedbackSettingsService_SetBeforeBind_SavesPreferenceAndAppliesUponLaterBind()
        {
            FakeHapticService haptic = null;
            FakeCameraShakeService camera = null;
            var store = new FakeFeedbackSettingsStore();

            var service = new FeedbackSettingsService(store, () => haptic, () => camera);

            // Consumer sets preference before services exist
            service.SetHapticEnabled(false);
            service.SetCameraShakeEnabled(false);

            Assert.IsTrue(store.HasKey(HapticSettingsController.PreferenceKey));
            Assert.IsFalse(store.GetBool(HapticSettingsController.PreferenceKey));
            Assert.IsTrue(store.HasKey(CameraShakeSettingsController.PreferenceKey));
            Assert.IsFalse(store.GetBool(CameraShakeSettingsController.PreferenceKey));

            // Services become available
            haptic = new FakeHapticService { Enabled = true };
            camera = new FakeCameraShakeService { Enabled = true };
            service.TryBindAvailableServices();

            Assert.IsFalse(haptic.Enabled, "Haptic service must receive preference recorded before bind.");
            Assert.IsFalse(camera.Enabled, "Camera service must receive preference recorded before bind.");
        }

        [Test]
        public void FeedbackSettingsService_RepeatedBind_IsIdempotent()
        {
            var haptic = new FakeHapticService { Enabled = true };
            var camera = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();

            var service = new FeedbackSettingsService(store, () => haptic, () => camera);
            service.TryBindAvailableServices();

            // Toggle haptic off
            service.SetHapticEnabled(false);
            Assert.IsFalse(haptic.Enabled);

            // Repeated bind calls must not reset state to service defaults
            service.TryBindAvailableServices();
            service.TryBindAvailableServices();

            Assert.IsFalse(haptic.Enabled, "Repeated binding must not overwrite current settings.");
            Assert.IsTrue(service.AllServicesBound);
        }

        [Test]
        public void FeedbackSettingsService_FeatureIndependence_TogglesDoNotMutateUnrelatedFeature()
        {
            var haptic = new FakeHapticService { Enabled = true };
            var camera = new FakeCameraShakeService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();

            var service = new FeedbackSettingsService(store, () => haptic, () => camera);
            service.TryBindAvailableServices();

            service.SetHapticEnabled(false);
            Assert.IsFalse(haptic.Enabled);
            Assert.IsTrue(camera.Enabled, "Changing haptic must not affect camera shake.");

            service.SetCameraShakeEnabled(false);
            Assert.IsFalse(camera.Enabled);
            Assert.IsFalse(haptic.Enabled, "Changing camera shake must not affect haptic.");
        }

        [Test]
        public void FeedbackSettingsService_DeferredFlush_OnlyFlushesWhenDirty()
        {
            var haptic = new FakeHapticService { Enabled = true };
            var store = new FakeFeedbackSettingsStore();

            var service = new FeedbackSettingsService(store, () => haptic, () => null);
            service.TryBindAvailableServices();

            Assert.AreEqual(0, store.FlushCount);

            service.SetHapticEnabled(false);
            Assert.AreEqual(0, store.FlushCount, "Setting preference must not flush immediately.");

            service.Flush();
            Assert.AreEqual(1, store.FlushCount);

            // Subsequent flush when clean is no-op
            service.Flush();
            Assert.AreEqual(1, store.FlushCount);
        }
    }
}
