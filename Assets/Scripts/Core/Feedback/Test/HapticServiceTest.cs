using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Core.Feedback.Test
{
    [TestFixture]
    public sealed class HapticServiceTest
    {
        private sealed class FakeClock : IFeedbackClock
        {
            public double CurrentTime { get; set; }
            public double RealtimeSinceStartup => CurrentTime;
        }

        private sealed class FakeHapticBackend : IHapticBackend
        {
            public HapticCapabilities Capabilities { get; set; } = HapticCapabilities.Pattern | HapticCapabilities.Intensity;
            public int PlayCount { get; private set; }
            public HapticRequest LastRequest { get; private set; }
            public bool ReturnValue { get; set; } = true;

            public bool TryPlay(in HapticRequest request)
            {
                PlayCount++;
                LastRequest = request;
                return ReturnValue;
            }

            public void Reset()
            {
                PlayCount = 0;
                LastRequest = default;
                ReturnValue = true;
            }
        }

        private FakeClock _clock;
        private FakeHapticBackend _backend;
        private HapticSystemSettingsSO _settings;
        private HapticProfileSO _profileLight;
        private HapticProfileSO _profileHeavy;
        private HapticProfileSO _profileNone;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock { CurrentTime = 1.0 };
            _backend = new FakeHapticBackend();

            _settings = ScriptableObject.CreateInstance<HapticSystemSettingsSO>();
            _settings.InitializeForTesting(enabledByDefault: true, globalIntensity: 1f, minimumIntervalSeconds: 0.05f);

            _profileLight = ScriptableObject.CreateInstance<HapticProfileSO>();
            _profileLight.InitializeForTesting(HapticPattern.ImpactLight, 0.4f);

            _profileHeavy = ScriptableObject.CreateInstance<HapticProfileSO>();
            _profileHeavy.InitializeForTesting(HapticPattern.ImpactHeavy, 1.0f);

            _profileNone = ScriptableObject.CreateInstance<HapticProfileSO>();
            _profileNone.InitializeForTesting(HapticPattern.None, 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
            if (_profileLight != null) Object.DestroyImmediate(_profileLight);
            if (_profileHeavy != null) Object.DestroyImmediate(_profileHeavy);
            if (_profileNone != null) Object.DestroyImmediate(_profileNone);
        }

        [Test]
        public void InitialState_ReflectsSettingsAndBackendCapabilities()
        {
            _settings.InitializeForTesting(enabledByDefault: false, globalIntensity: 0.8f, minimumIntervalSeconds: 0.1f);
            var service = new HapticService(_backend, _clock, _settings);

            Assert.IsFalse(service.Enabled);
            Assert.AreEqual(_backend.Capabilities, service.Capabilities);
        }

        [Test]
        public void TryPlay_WhenDisabled_ReturnsFalseAndDoesNotCallBackend()
        {
            var service = new HapticService(_backend, _clock, _settings)
            {
                Enabled = false
            };

            bool result = service.TryPlay(_profileLight);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenBackendHasNoCapabilities_ReturnsFalse()
        {
            _backend.Capabilities = HapticCapabilities.None;
            var service = new HapticService(_backend, _clock, _settings);

            bool result = service.TryPlay(_profileLight);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenProfileNullOrPatternNone_ReturnsFalse()
        {
            var service = new HapticService(_backend, _clock, _settings);

            Assert.IsFalse(service.TryPlay(null));
            Assert.IsFalse(service.TryPlay(_profileNone));
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenRuntimeIntensityZeroOrNegative_ReturnsFalse()
        {
            var service = new HapticService(_backend, _clock, _settings);

            Assert.IsFalse(service.TryPlay(_profileLight, 0f));
            Assert.IsFalse(service.TryPlay(_profileLight, -0.5f));
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_NormalizesIntensity_ClampedCorrectly()
        {
            _settings.InitializeForTesting(enabledByDefault: true, globalIntensity: 0.5f, minimumIntervalSeconds: 0.05f);
            var service = new HapticService(_backend, _clock, _settings);

            bool result = service.TryPlay(_profileLight, runtimeIntensityScale: 0.5f);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _backend.PlayCount);
            Assert.AreEqual(HapticPattern.ImpactLight, _backend.LastRequest.Pattern);
            // 0.5 (global) * 0.4 (profile) * 0.5 (runtime) = 0.1
            Assert.AreEqual(0.1f, _backend.LastRequest.Intensity, 0.001f);
        }

        [Test]
        public void TryPlay_EnforcesCooldownFromSettings()
        {
            _settings.InitializeForTesting(enabledByDefault: true, globalIntensity: 1f, minimumIntervalSeconds: 0.05f);
            var service = new HapticService(_backend, _clock, _settings);

            _clock.CurrentTime = 10.0;
            bool first = service.TryPlay(_profileLight);
            Assert.IsTrue(first);
            Assert.AreEqual(1, _backend.PlayCount);

            // Attempt during cooldown (t = 10.02 < 10.05)
            _clock.CurrentTime = 10.02;
            bool second = service.TryPlay(_profileLight);
            Assert.IsFalse(second);
            Assert.AreEqual(1, _backend.PlayCount);

            // Attempt after cooldown elapsed (t = 10.051 >= 10.05)
            _clock.CurrentTime = 10.051;
            bool third = service.TryPlay(_profileLight);
            Assert.IsTrue(third);
            Assert.AreEqual(2, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenBackendFails_DoesNotUpdateCooldown()
        {
            _settings.InitializeForTesting(enabledByDefault: true, globalIntensity: 1f, minimumIntervalSeconds: 0.05f);
            var service = new HapticService(_backend, _clock, _settings);

            _backend.ReturnValue = false;
            _clock.CurrentTime = 5.0;

            bool first = service.TryPlay(_profileLight);
            Assert.IsFalse(first);
            Assert.AreEqual(1, _backend.PlayCount);

            // Cooldown was NOT updated, so immediate subsequent attempt at t = 5.01 should reach backend
            _backend.ReturnValue = true;
            _clock.CurrentTime = 5.01;

            bool second = service.TryPlay(_profileLight);
            Assert.IsTrue(second);
            Assert.AreEqual(2, _backend.PlayCount);
        }

        [Test]
        public void NullHapticBackend_ReportsNoneAndReturnsFalse()
        {
            var nullBackend = NullHapticBackend.Instance;
            Assert.AreEqual(HapticCapabilities.None, nullBackend.Capabilities);
            Assert.IsFalse(nullBackend.TryPlay(new HapticRequest(HapticPattern.ImpactHeavy, 1f)));
        }
    }
}
