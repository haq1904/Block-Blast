using BlockBlast.Features.CloudServices;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerIdentity.Gateway;
using BlockBlast.Features.PlayerSession;
using UnityEngine;

namespace BlockBlast.Features.PlayerIdentity
{
    [DefaultExecutionOrder(-80)]
    public sealed class PlayerIdentityServiceHost : MonoBehaviour
    {
        private PlayerIdentityService _service;
        private bool _ownsRegistration;

        public bool OwnsRegistration => _ownsRegistration;

        private void Start()
        {
            if (ServiceLocator.TryGet<IPlayerIdentityService>(out _))
            {
                Debug.LogWarning("[PlayerIdentityServiceHost] Another IPlayerIdentityService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            if (!ServiceLocator.TryGet<ICloudInitializationService>(out var initService))
            {
                Debug.LogError("[PlayerIdentityServiceHost] ICloudInitializationService not found in ServiceLocator.", this);
                return;
            }

            if (!ServiceLocator.TryGet<IPlayerCloudService>(out var cloudService))
            {
                Debug.LogError("[PlayerIdentityServiceHost] IPlayerCloudService not found in ServiceLocator.", this);
                return;
            }

            if (!ServiceLocator.TryGet<IGameResultSubmissionService>(out var submissionService))
            {
                Debug.LogError("[PlayerIdentityServiceHost] IGameResultSubmissionService not found in ServiceLocator.", this);
                return;
            }

            var authGateway = new UnityPlayerIdentityAuthGateway();
            var authCodeProvider = new GooglePlayGamesAuthCodeProvider();

            _service = new PlayerIdentityService(
                initService,
                authGateway,
                authCodeProvider,
                cloudService,
                submissionService);

            ServiceLocator.Register<IPlayerIdentityService>(_service);
            _ownsRegistration = true;
        }

        private void OnDestroy()
        {
            if (_ownsRegistration && _service != null)
            {
                ServiceLocator.Unregister<IPlayerIdentityService>(_service);
                _service.Dispose();
                _service = null;
                _ownsRegistration = false;
            }
        }
    }
}
