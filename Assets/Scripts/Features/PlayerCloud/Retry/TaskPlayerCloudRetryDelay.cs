using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerCloud.Retry
{
    public sealed class TaskPlayerCloudRetryDelay : IPlayerCloudRetryDelay
    {
        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            return Task.Delay(milliseconds, cancellationToken);
        }
    }
}
