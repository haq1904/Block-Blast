using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Production adapter calling Unity Gaming Services Core and Authentication SDKs.
    /// </summary>
    public sealed class UnityServicesGateway : IUnityServicesGateway
    {
        public bool IsServicesInitialized => UnityServices.State == ServicesInitializationState.Initialized;

        public bool IsSignedIn
        {
            get
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    return false;
                }

                try
                {
                    return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
                }
                catch (ServicesInitializationException)
                {
                    return false;
                }
            }
        }

        public string PlayerId
        {
            get
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    return string.Empty;
                }

                try
                {
                    return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn
                        ? AuthenticationService.Instance.PlayerId
                        : string.Empty;
                }
                catch (ServicesInitializationException)
                {
                    return string.Empty;
                }
            }
        }

        public async Task InitializeServicesAsync(string environmentName)
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                return;
            }

            var options = new InitializationOptions();
            if (!string.IsNullOrEmpty(environmentName))
            {
                options.SetEnvironmentName(environmentName);
            }

            await UnityServices.InitializeAsync(options);
        }

        public async Task SignInAnonymouslyAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                throw new ServicesInitializationException("Unity Services must be initialized before signing in.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
