using System.Threading;
using BlockBlast.Core;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerData;
using UnityEngine;

namespace BlockBlast.Features.PlayerSession
{
    [DefaultExecutionOrder(-90)]
    public sealed class GameResultSubmissionHost : MonoBehaviour
    {
        private GameResultSubmissionService _service;
        private bool _ownsRegistration;
        private CancellationTokenSource _lifecycleCts;

        public bool OwnsRegistration => _ownsRegistration;

        private void Start()
        {
            if (ServiceLocator.TryGet<IGameResultSubmissionService>(out _))
            {
                Debug.LogWarning("[GameResultSubmissionHost] Another IGameResultSubmissionService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            if (!ServiceLocator.TryGet<IPlayerCloudService>(out var cloudService))
            {
                Debug.LogError("[GameResultSubmissionHost] IPlayerCloudService not found in ServiceLocator.", this);
                return;
            }

            _lifecycleCts = new CancellationTokenSource();

            var queue = new FilePendingGameResultQueue();
            _service = new GameResultSubmissionService(queue, cloudService);

            ServiceLocator.Register<IGameResultSubmissionService>(_service);
            _ownsRegistration = true;

            _ = _service.FlushPendingAsync(_lifecycleCts.Token);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _service != null && _lifecycleCts != null && !_lifecycleCts.IsCancellationRequested)
            {
                _ = _service.FlushPendingAsync(_lifecycleCts.Token);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _service != null && _lifecycleCts != null && !_lifecycleCts.IsCancellationRequested)
            {
                _ = _service.FlushPendingAsync(_lifecycleCts.Token);
            }
        }

        private void OnDestroy()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;

            if (_ownsRegistration && _service != null)
            {
                ServiceLocator.Unregister<IGameResultSubmissionService>(_service);
                _ownsRegistration = false;
                _service.Dispose();
                _service = null;
            }
        }
    }
}
