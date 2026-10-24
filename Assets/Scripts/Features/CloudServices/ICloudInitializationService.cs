using System.Threading.Tasks;

namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Service contract providing access to cloud initialization state, environment name, and authenticated PlayerId.
    /// </summary>
    public interface ICloudInitializationService
    {
        CloudInitializationState State { get; }
        string EnvironmentName { get; }
        string PlayerId { get; }
        bool IsReady { get; }

        /// <summary>
        /// Initiates or joins an existing cloud initialization and anonymous sign-in task.
        /// Returns true if services and authentication are ready; otherwise returns false without throwing.
        /// </summary>
        Task<bool> InitializeAsync();
    }
}
