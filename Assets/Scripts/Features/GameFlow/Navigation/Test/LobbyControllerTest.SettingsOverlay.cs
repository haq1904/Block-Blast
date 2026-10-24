using BlockBlast.Features.Lobby;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
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

        private LobbySettingsOverlayView CreateSettingsOverlayView(out Slider musicSlider, out Slider sfxSlider)
        {
            var go = new GameObject("TestSettingsOverlay");
            _createdObjects.Add(go);
            go.AddComponent<CanvasGroup>();
            var view = go.AddComponent<LobbySettingsOverlayView>();

            var musicGo = new GameObject("MusicSlider");
            musicGo.transform.SetParent(go.transform);
            musicSlider = musicGo.AddComponent<Slider>();

            var sfxGo = new GameObject("SfxSlider");
            sfxGo.transform.SetParent(go.transform);
            sfxSlider = sfxGo.AddComponent<Slider>();

            view.SetSlidersForTest(musicSlider, sfxSlider);
            return view;
        }

        [Test]
        public void SettingsOverlay_WhenCloseRequested_InvokesCloseOverlayAndRestoresWorldButtons()
        {
            var settingsView = CreateSettingsOverlayView(out _, out _);
            _controller.SettingsOverlay = settingsView;

            // Re-enable to bind with new SettingsOverlay
            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            // Open overlay -> buttons should be disabled
            _controller.OpenOverlay(LobbyOverlayId.Setting);
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsFalse(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be non-interactable when overlay is open.");
            }

            // Close requested from view
            settingsView.RequestClose();

            // Buttons should be restored
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be restored to interactable after close.");
            }
        }

        [Test]
        public void SettingsOverlay_InitialSync_AppliesServiceVolumeWithoutInvokingSetChannelVolume()
        {
            var fakeAudio = new FakeAudioMixerService();
            fakeAudio.BGMusicVolume = 0.65f;
            fakeAudio.SoundFXVolume = 0.45f;
            ServiceLocator.Register<IAudioMixerService>(fakeAudio);

            var settingsView = CreateSettingsOverlayView(out var musicSlider, out var sfxSlider);
            _controller.SetAudioMixerServiceForTest(fakeAudio);
            _controller.SettingsOverlay = settingsView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            Assert.AreEqual(0.65f, musicSlider.value, 0.001f);
            Assert.AreEqual(0.45f, sfxSlider.value, 0.001f);
            Assert.AreEqual(0, fakeAudio.SetCallCount, "Initial sync must use SetValueWithoutNotify and not call setter.");
        }

        [Test]
        public void SettingsOverlay_ChangingMusicSlider_OnlyUpdatesBGMusicVolume()
        {
            var fakeAudio = new FakeAudioMixerService();
            ServiceLocator.Register<IAudioMixerService>(fakeAudio);

            var settingsView = CreateSettingsOverlayView(out var musicSlider, out var sfxSlider);
            _controller.SetAudioMixerServiceForTest(fakeAudio);
            _controller.SettingsOverlay = settingsView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            int initialSetCalls = fakeAudio.SetCallCount;
            musicSlider.value = 0.25f;

            Assert.AreEqual(initialSetCalls + 1, fakeAudio.SetCallCount);
            Assert.AreEqual(AudioChannels.BGMusicVolume, fakeAudio.LastSetChannel);
            Assert.AreEqual(0.25f, fakeAudio.BGMusicVolume, 0.001f);
            Assert.AreEqual(0.5f, fakeAudio.SoundFXVolume, 0.001f);
        }

        [Test]
        public void SettingsOverlay_ChangingSfxSlider_OnlyUpdatesSoundFXVolume()
        {
            var fakeAudio = new FakeAudioMixerService();
            ServiceLocator.Register<IAudioMixerService>(fakeAudio);

            var settingsView = CreateSettingsOverlayView(out var musicSlider, out var sfxSlider);
            _controller.SetAudioMixerServiceForTest(fakeAudio);
            _controller.SettingsOverlay = settingsView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            int initialSetCalls = fakeAudio.SetCallCount;
            sfxSlider.value = 0.95f;

            Assert.AreEqual(initialSetCalls + 1, fakeAudio.SetCallCount);
            Assert.AreEqual(AudioChannels.SoundFXVolume, fakeAudio.LastSetChannel);
            Assert.AreEqual(0.95f, fakeAudio.SoundFXVolume, 0.001f);
            Assert.AreEqual(0.8f, fakeAudio.BGMusicVolume, 0.001f);
        }

        [Test]
        public void SettingsOverlay_EventLifecycle_DisableThenEnable_PreservesSingleBinding()
        {
            var fakeAudio = new FakeAudioMixerService();
            ServiceLocator.Register<IAudioMixerService>(fakeAudio);

            var settingsView = CreateSettingsOverlayView(out var musicSlider, out var sfxSlider);
            _controller.SetAudioMixerServiceForTest(fakeAudio);
            _controller.SettingsOverlay = settingsView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            int baselineSetCalls = fakeAudio.SetCallCount;
            musicSlider.value = 0.33f;

            Assert.AreEqual(baselineSetCalls + 1, fakeAudio.SetCallCount, "Slider change must trigger exactly one service call after multiple re-enables.");

            _controller.OpenOverlay(LobbyOverlayId.Setting);
            settingsView.RequestClose();

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled);
            }
        }

        [Test]
        public void SettingsOverlay_WithoutAudioService_DoesNotThrowAndPreservesCloseBehavior()
        {
            ServiceLocator.Unregister<IAudioMixerService>();

            var settingsView = CreateSettingsOverlayView(out var musicSlider, out var sfxSlider);
            _controller.SetAudioMixerServiceForTest(null);
            _controller.SettingsOverlay = settingsView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.DoesNotThrow(() =>
            {
                onDisable?.Invoke(_controller, null);
                onEnable?.Invoke(_controller, null);
                musicSlider.value = 0.1f;
                sfxSlider.value = 0.2f;
                _controller.OpenOverlay(LobbyOverlayId.Setting);
                settingsView.RequestClose();
            });

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled);
            }
        }

        [Test]
        public void SettingsOverlay_OpenOverlaySetting_SyncsLatestMixerVolume()
        {
            var fakeAudio = new FakeAudioMixerService();
            fakeAudio.BGMusicVolume = 0.2f;
            fakeAudio.SoundFXVolume = 0.3f;
            ServiceLocator.Register<IAudioMixerService>(fakeAudio);

            var settingsView = CreateSettingsOverlayView(out var musicSlider, out var sfxSlider);
            _controller.SetAudioMixerServiceForTest(fakeAudio);
            _controller.SettingsOverlay = settingsView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            // Change underlying audio mixer service state externally
            fakeAudio.BGMusicVolume = 0.88f;
            fakeAudio.SoundFXVolume = 0.77f;

            _controller.OpenOverlay(LobbyOverlayId.Setting);

            Assert.AreEqual(0.88f, musicSlider.value, 0.001f);
            Assert.AreEqual(0.77f, sfxSlider.value, 0.001f);
        }
    }
}
