using System.Threading.Tasks;

namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Test seam abstracting Unity Gaming Services SDK initialization and authentication calls.
    /// Prevents direct static SDK coupling and enables isolated unit testing.
    /// </summary>
    public interface IUnityServicesGateway
    {
        bool IsServicesInitialized { get; }
        bool IsSignedIn { get; }
        string PlayerId { get; }

        Task InitializeServicesAsync(string environmentName);
        Task SignInAnonymouslyAsync();
    }
}
