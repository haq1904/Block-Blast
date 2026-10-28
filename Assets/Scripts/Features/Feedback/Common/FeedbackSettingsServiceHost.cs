using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockBlast.Features.Feedback
{
    /// <summary>
    /// Lifecycle host attached to SceneServices that initializes, registers,
    /// late-binds, and flushes <see cref="IFeedbackSettingsService"/> across application lifecycles.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class FeedbackSettingsServiceHost : MonoBehaviour
    {
        private FeedbackSettingsService _service;
        private bool _isRegistered;
        private bool _isSceneLoadedSubscribed;

        public IFeedbackSettingsService Service => _service;

        private void Awake()
        {
            var store = new PlayerPrefsFeedbackSettingsStore();
            _service = new FeedbackSettingsService(store);

            ServiceLocator.Register<IFeedbackSettingsService>(_service);
            _isRegistered = true;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            _isSceneLoadedSubscribed = true;

            _service.TryBindAvailableServices();
        }

        private void Start()
        {
            _service?.TryBindAvailableServices();
            CheckUnsubscribeSceneLoaded();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _service?.TryBindAvailableServices();
            CheckUnsubscribeSceneLoaded();
        }

        private void CheckUnsubscribeSceneLoaded()
        {
            if (_isSceneLoadedSubscribed && _service != null && _service.AllServicesBound)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _isSceneLoadedSubscribed = false;
            }
        }

        private void OnDestroy()
        {
            _service?.Flush();

            if (_isSceneLoadedSubscribed)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _isSceneLoadedSubscribed = false;
            }

            if (_isRegistered && _service != null)
            {
                ServiceLocator.Unregister<IFeedbackSettingsService>(_service);
                _isRegistered = false;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _service?.Flush();
            }
        }

        private void OnApplicationQuit()
        {
            _service?.Flush();
        }
    }
}
