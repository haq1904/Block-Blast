using System.Collections.Generic;
using BlockBlast.Core.Persistence;
using BlockBlast.Features.Feedback.Audio;
using NUnit.Framework;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class AudioVolumeSettingsServiceTest
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
            public readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
            public int SaveCount;

            public bool HasKey(string key) => Ints.ContainsKey(key) || Strings.ContainsKey(key);

            public int GetInt(string key, int defaultValue = 0) =>
                Ints.TryGetValue(key, out int val) ? val : defaultValue;

            public void SetInt(string key, int value) => Ints[key] = value;

            public string GetString(string key, string defaultValue = null) =>
                Strings.TryGetValue(key, out string val) ? val : defaultValue;

            public void SetString(string key, string value) => Strings[key] = value;

            public void DeleteKey(string key)
            {
                Ints.Remove(key);
                Strings.Remove(key);
            }

            public void Save() => SaveCount++;
        }

        private class FakeAudioMixerService : IAudioMixerService
        {
            public float MasterVolume = 1f;
            public float SoundFXVolume = 0.5f;
            public float BGMusicVolume = 0.8f;
            public int SetCallCount = 0;
            public AudioChannels LastSetChannel;
            public float LastSetValue;

            public void SetChannelVolume(AudioChannels channel, float normalizedVolume)
            {
                SetCallCount++;
                LastSetChannel = channel;
                LastSetValue = normalizedVolume;
                switch (channel)
                {
                    case AudioChannels.MasterVolume:
                        MasterVolume = normalizedVolume;
                        break;
                    case AudioChannels.SoundFXVolume:
                        SoundFXVolume = normalizedVolume;
                        break;
                    case AudioChannels.BGMusicVolume:
                        BGMusicVolume = normalizedVolume;
                        break;
                }
            }

            public float GetChannelVolume(AudioChannels channel)
            {
                switch (channel)
                {
                    case AudioChannels.MasterVolume:
                        return MasterVolume;
                    case AudioChannels.SoundFXVolume:
                        return SoundFXVolume;
                    case AudioChannels.BGMusicVolume:
                        return BGMusicVolume;
                    default:
                        return 1f;
                }
            }
        }

        private FakeAudioMixerService _fakeMixer;
        private FakeKeyValueStore _fakeStore;
        private AudioVolumeSettingsService _service;

        [SetUp]
        public void SetUp()
        {
            _fakeMixer = new FakeAudioMixerService();
            _fakeStore = new FakeKeyValueStore();
            _service = new AudioVolumeSettingsService(_fakeMixer, _fakeStore);
        }

        [Test]
        public void LoadAndApply_WhenKeysMissing_KeepsMixerDefaultsAndDoesNotMutateStoreOrSave()
        {
            _fakeMixer.BGMusicVolume = 0.75f;
            _fakeMixer.SoundFXVolume = 0.45f;

            _service.LoadAndApply();

            Assert.AreEqual(0, _fakeMixer.SetCallCount, "LoadAndApply should not mutate mixer when keys are missing.");
            Assert.AreEqual(0, _fakeStore.SaveCount, "LoadAndApply should not save store when keys are missing.");
            Assert.IsFalse(_fakeStore.HasKey(AudioVolumeSettingsService.MusicVolumeKey));
            Assert.IsFalse(_fakeStore.HasKey(AudioVolumeSettingsService.SfxVolumeKey));
            Assert.IsFalse(_service.IsDirty);
            Assert.AreEqual(0.75f, _service.MusicVolume, 0.001f);
            Assert.AreEqual(0.45f, _service.SfxVolume, 0.001f);
        }

        [Test]
        public void LoadAndApply_WhenKeysExist_DecodesAndAppliesBothChannels()
        {
            _fakeStore.SetInt(AudioVolumeSettingsService.MusicVolumeKey, 300);
            _fakeStore.SetInt(AudioVolumeSettingsService.SfxVolumeKey, 800);

            _service.LoadAndApply();

            Assert.AreEqual(2, _fakeMixer.SetCallCount);
            Assert.AreEqual(0.3f, _fakeMixer.BGMusicVolume, 0.001f);
            Assert.AreEqual(0.8f, _fakeMixer.SoundFXVolume, 0.001f);
            Assert.AreEqual(0.3f, _service.MusicVolume, 0.001f);
            Assert.AreEqual(0.8f, _service.SfxVolume, 0.001f);
            Assert.AreEqual(0, _fakeStore.SaveCount, "LoadAndApply must not flush to store.");
            Assert.IsFalse(_service.IsDirty);
        }

        [Test]
        public void LoadAndApply_WhenValuesCorruptOrOutOfRange_ClampsToValidRange()
        {
            _fakeStore.SetInt(AudioVolumeSettingsService.MusicVolumeKey, -500);
            _fakeStore.SetInt(AudioVolumeSettingsService.SfxVolumeKey, 2500);

            _service.LoadAndApply();

            Assert.AreEqual(0.0f, _fakeMixer.BGMusicVolume, 0.001f);
            Assert.AreEqual(1.0f, _fakeMixer.SoundFXVolume, 0.001f);
            Assert.AreEqual(0.0f, _service.MusicVolume, 0.001f);
            Assert.AreEqual(1.0f, _service.SfxVolume, 0.001f);
        }

        [Test]
        public void SetMusicVolume_OnlyUpdatesMusicChannelAndKey()
        {
            _service.SetMusicVolume(0.42f);

            Assert.AreEqual(1, _fakeMixer.SetCallCount);
            Assert.AreEqual(AudioChannels.BGMusicVolume, _fakeMixer.LastSetChannel);
            Assert.AreEqual(0.42f, _fakeMixer.BGMusicVolume, 0.001f);
            Assert.AreEqual(0.5f, _fakeMixer.SoundFXVolume, 0.001f, "SFX must remain untouched.");
            Assert.AreEqual(1f, _fakeMixer.MasterVolume, 0.001f, "Master must remain untouched.");

            Assert.IsTrue(_fakeStore.HasKey(AudioVolumeSettingsService.MusicVolumeKey));
            Assert.AreEqual(420, _fakeStore.GetInt(AudioVolumeSettingsService.MusicVolumeKey));
            Assert.IsFalse(_fakeStore.HasKey(AudioVolumeSettingsService.SfxVolumeKey));
            Assert.IsTrue(_service.IsDirty);
            Assert.AreEqual(0, _fakeStore.SaveCount, "Setting volume must not flush immediately.");
        }

        [Test]
        public void SetSfxVolume_OnlyUpdatesSfxChannelAndKey()
        {
            _service.SetSfxVolume(0.88f);

            Assert.AreEqual(1, _fakeMixer.SetCallCount);
            Assert.AreEqual(AudioChannels.SoundFXVolume, _fakeMixer.LastSetChannel);
            Assert.AreEqual(0.88f, _fakeMixer.SoundFXVolume, 0.001f);
            Assert.AreEqual(0.8f, _fakeMixer.BGMusicVolume, 0.001f, "Music must remain untouched.");
            Assert.AreEqual(1f, _fakeMixer.MasterVolume, 0.001f, "Master must remain untouched.");

            Assert.IsTrue(_fakeStore.HasKey(AudioVolumeSettingsService.SfxVolumeKey));
            Assert.AreEqual(880, _fakeStore.GetInt(AudioVolumeSettingsService.SfxVolumeKey));
            Assert.IsFalse(_fakeStore.HasKey(AudioVolumeSettingsService.MusicVolumeKey));
            Assert.IsTrue(_service.IsDirty);
            Assert.AreEqual(0, _fakeStore.SaveCount, "Setting volume must not flush immediately.");
        }

        [Test]
        public void SetVolume_SameValue_DoesNotMarkDirtyOrCallMixer()
        {
            _fakeMixer.BGMusicVolume = 0.5f;
            _service.LoadAndApply();

            int baselineSetCalls = _fakeMixer.SetCallCount;
            _service.SetMusicVolume(0.5f);

            Assert.AreEqual(baselineSetCalls, _fakeMixer.SetCallCount, "Same volume value should not trigger mixer call.");
            Assert.IsFalse(_service.IsDirty, "Same volume value should not mark dirty.");
            Assert.AreEqual(0, _fakeStore.SaveCount);
        }

        [Test]
        public void Flush_Deferred_OnlySavesWhenDirtyAndIdempotentWhenClean()
        {
            _service.Flush();
            Assert.AreEqual(0, _fakeStore.SaveCount, "Clean flush must be no-op.");

            _service.SetMusicVolume(0.2f);
            _service.SetMusicVolume(0.4f);
            _service.SetSfxVolume(0.6f);
            Assert.AreEqual(0, _fakeStore.SaveCount, "Repeated slider changes must not flush immediately.");
            Assert.IsTrue(_service.IsDirty);

            _service.Flush();
            Assert.AreEqual(1, _fakeStore.SaveCount, "Dirty flush must save exactly once.");
            Assert.IsFalse(_service.IsDirty);

            _service.Flush();
            Assert.AreEqual(1, _fakeStore.SaveCount, "Subsequent flush on clean service must be no-op.");
        }

        [Test]
        public void Service_WhenMixerIsNull_DoesNotThrowAndStoresPreferences()
        {
            var service = new AudioVolumeSettingsService(null, _fakeStore);

            Assert.DoesNotThrow(() => service.LoadAndApply());
            Assert.DoesNotThrow(() => service.SetMusicVolume(0.65f));
            Assert.AreEqual(650, _fakeStore.GetInt(AudioVolumeSettingsService.MusicVolumeKey));
            Assert.IsTrue(service.IsDirty);

            Assert.DoesNotThrow(() => service.Flush());
            Assert.AreEqual(1, _fakeStore.SaveCount);
            Assert.IsFalse(service.IsDirty);
        }

        [Test]
        public void Service_WhenStoreIsNull_DoesNotThrowAndUpdatesMixer()
        {
            var service = new AudioVolumeSettingsService(_fakeMixer, null);

            Assert.DoesNotThrow(() => service.LoadAndApply());
            Assert.DoesNotThrow(() => service.SetMusicVolume(0.35f));
            Assert.AreEqual(0.35f, _fakeMixer.BGMusicVolume, 0.001f);
            Assert.DoesNotThrow(() => service.Flush());
        }

        [Test]
        public void LoadAndApply_WithRealPlayerPrefsStore_RoundTripsSavedVolumesAcrossInstances()
        {
            var realStore = new PlayerPrefsKeyValueStore();
            try
            {
                realStore.SetInt(AudioVolumeSettingsService.MusicVolumeKey, 350);
                realStore.SetInt(AudioVolumeSettingsService.SfxVolumeKey, 750);
                realStore.Save();

                var newMixer = new FakeAudioMixerService();
                var newService = new AudioVolumeSettingsService(newMixer, realStore);

                newService.LoadAndApply();

                Assert.AreEqual(0.35f, newMixer.BGMusicVolume, 0.001f);
                Assert.AreEqual(0.75f, newMixer.SoundFXVolume, 0.001f);
                Assert.AreEqual(0.35f, newService.MusicVolume, 0.001f);
                Assert.AreEqual(0.75f, newService.SfxVolume, 0.001f);
            }
            finally
            {
                realStore.DeleteKey(AudioVolumeSettingsService.MusicVolumeKey);
                realStore.DeleteKey(AudioVolumeSettingsService.SfxVolumeKey);
                realStore.Save();
            }
        }
    }
}
