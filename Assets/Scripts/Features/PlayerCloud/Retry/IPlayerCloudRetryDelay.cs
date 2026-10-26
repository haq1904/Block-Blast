using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerCloud.Retry
{
    public interface IPlayerCloudRetryDelay
    {
        Task DelayAsync(int milliseconds, CancellationToken cancellationToken);
    }
}
