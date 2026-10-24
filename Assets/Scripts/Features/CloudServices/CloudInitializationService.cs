using System;
using System.Threading.Tasks;

namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Coordinates cloud initialization and anonymous authentication with single-flight execution,
    /// safe exception containment, cached session reuse, and failure retry support.
    /// </summary>
    public sealed class CloudInitializationService : ICloudInitializationService
    {
        private readonly IUnityServicesGateway _gateway;
        private readonly string _environmentName;
        private readonly object _lock = new object();

        private CloudInitializationState _state = CloudInitializationState.NotStarted;
        private string _playerId = string.Empty;
        private Task<bool> _initializationTask;

        public CloudInitializationState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        public string EnvironmentName => _environmentName;

        public string PlayerId
        {
            get
            {
                lock (_lock)
                {
                    return _playerId;
                }
            }
        }

        public bool IsReady
        {
            get
            {
                lock (_lock)
                {
                    return _state == CloudInitializationState.Ready;
                }
            }
        }

        public CloudInitializationService(IUnityServicesGateway gateway, string environmentName)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _environmentName = !string.IsNullOrEmpty(environmentName)
                ? environmentName
                : CloudEnvironmentResolver.DevelopmentEnvironment;
        }

        public Task<bool> InitializeAsync()
        {
            lock (_lock)
            {
                if (_state == CloudInitializationState.Ready)
                {
                    return Task.FromResult(true);
                }

                if (_state == CloudInitializationState.Initializing && _initializationTask != null)
                {
                    return _initializationTask;
                }

                _state = CloudInitializationState.Initializing;
                _playerId = string.Empty;
                _initializationTask = RunInitializationAsync();
                return _initializationTask;
            }
        }

        private async Task<bool> RunInitializationAsync()
        {
            try
            {
                if (!_gateway.IsServicesInitialized)
                {
                    await _gateway.InitializeServicesAsync(_environmentName);
                }

                if (!_gateway.IsSignedIn)
                {
                    await _gateway.SignInAnonymouslyAsync();
                }

                string playerId = _gateway.PlayerId;
                if (string.IsNullOrEmpty(playerId))
                {
                    throw new InvalidOperationException("Authentication completed but PlayerId is null or empty.");
                }

                lock (_lock)
                {
                    _playerId = playerId;
                    _state = CloudInitializationState.Ready;
                    _initializationTask = null;
                }

                return true;
            }
            catch (Exception)
            {
                lock (_lock)
                {
                    _playerId = string.Empty;
                    _state = CloudInitializationState.Unavailable;
                    _initializationTask = null;
                }

                return false;
            }
        }
    }
}
