using NUnit.Framework;
using UnityEngine;
using BlockBlast.Core.Feedback;

namespace BlockBlast.Core.Feedback.Test
{
    [TestFixture]
    public sealed class CameraShakeServiceTest
    {
        private sealed class FakeCameraShakeBackend : ICameraShakeBackend
        {
            public bool IsSupported { get; set; } = true;
            public int PlayCount { get; private set; }
            public CameraShakeRequest LastRequest { get; private set; }
            public bool ReturnValue { get; set; } = true;

            public bool TryPlay(in CameraShakeRequest request)
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

        private FakeCameraShakeBackend _backend;
        private CameraShakeSystemSettingsSO _settings;
        private TestCameraShakeProfileSO _profileStandard;
        private TestCameraShakeProfileSO _profileZero;

        [SetUp]
        public void SetUp()
        {
            _backend = new FakeCameraShakeBackend();

            _settings = ScriptableObject.CreateInstance<CameraShakeSystemSettingsSO>();
            _settings.InitializeForTesting(enabledByDefault: true, globalIntensity: 1f);

            _profileStandard = ScriptableObject.CreateInstance<TestCameraShakeProfileSO>();
            _profileStandard.Initialize(0.5f);

            _profileZero = ScriptableObject.CreateInstance<TestCameraShakeProfileSO>();
            _profileZero.Initialize(0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
            if (_profileStandard != null) Object.DestroyImmediate(_profileStandard);
            if (_profileZero != null) Object.DestroyImmediate(_profileZero);
        }

        [Test]
        public void InitialState_ReflectsSettingsAndBackendSupport()
        {
            _settings.InitializeForTesting(enabledByDefault: false, globalIntensity: 0.7f);
            var service = new CameraShakeService(_backend, _settings);

            Assert.IsFalse(service.Enabled);
            Assert.IsTrue(service.IsSupported);

            _backend.IsSupported = false;
            Assert.IsFalse(service.IsSupported);
        }

        [Test]
        public void TryPlay_WhenDisabled_ReturnsFalseAndDoesNotCallBackend()
        {
            var service = new CameraShakeService(_backend, _settings)
            {
                Enabled = false
            };

            var request = new CameraShakeRequest(_profileStandard, 1f);
            bool result = service.TryPlay(in request);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenBackendUnsupported_ReturnsFalse()
        {
            _backend.IsSupported = false;
            var service = new CameraShakeService(_backend, _settings);

            var request = new CameraShakeRequest(_profileStandard, 1f);
            bool result = service.TryPlay(in request);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenProfileNull_ReturnsFalse()
        {
            var service = new CameraShakeService(_backend, _settings);

            var request = new CameraShakeRequest(null, 1f);
            bool result = service.TryPlay(in request);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_WhenRuntimeScaleZeroOrNegative_ReturnsFalse()
        {
            var service = new CameraShakeService(_backend, _settings);

            var reqZero = new CameraShakeRequest(_profileStandard, 0f);
            var reqNegative = new CameraShakeRequest(_profileStandard, -0.5f);

            Assert.IsFalse(service.TryPlay(in reqZero));
            Assert.IsFalse(service.TryPlay(in reqNegative));
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_CalculatesEffectiveAmplitude_AndForwardsRequestCorrectly()
        {
            _settings.InitializeForTesting(enabledByDefault: true, globalIntensity: 0.8f);
            var service = new CameraShakeService(_backend, _settings);

            Vector3 explicitPos = new Vector3(10f, 20f, 30f);
            Vector3 explicitDir = new Vector3(0f, 1f, 0f);

            var request = new CameraShakeRequest(
                _profileStandard,
                runtimeIntensityScale: 0.5f,
                explicitPosition: explicitPos,
                explicitDirection: explicitDir);

            bool result = service.TryPlay(in request);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _backend.PlayCount);

            // 0.5 (base amplitude) * 0.8 (global) * 0.5 (runtime) = 0.2
            Assert.AreEqual(0.2f, _backend.LastRequest.EffectiveAmplitude, 0.001f);
            Assert.AreSame(_profileStandard, _backend.LastRequest.Profile);
            Assert.AreEqual(explicitPos, _backend.LastRequest.ExplicitPosition);
            Assert.AreEqual(explicitDir, _backend.LastRequest.ExplicitDirection);
        }

        [Test]
        public void TryPlay_WhenEffectiveAmplitudeZero_ReturnsFalse()
        {
            var service = new CameraShakeService(_backend, _settings);

            var request = new CameraShakeRequest(_profileZero, 1f);
            bool result = service.TryPlay(in request);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _backend.PlayCount);
        }

        [Test]
        public void TryPlay_PropagatesBackendFailure()
        {
            _backend.ReturnValue = false;
            var service = new CameraShakeService(_backend, _settings);

            var request = new CameraShakeRequest(_profileStandard, 1f);
            bool result = service.TryPlay(in request);

            Assert.IsFalse(result);
            Assert.AreEqual(1, _backend.PlayCount);
        }

        [Test]
        public void NullCameraShakeBackend_IsUnsupportedAndReturnsFalse()
        {
            var nullBackend = NullCameraShakeBackend.Instance;
            Assert.IsFalse(nullBackend.IsSupported);

            var request = new CameraShakeRequest(_profileStandard, 1f);
            Assert.IsFalse(nullBackend.TryPlay(in request));
        }
    }
}
