using System.Threading.Tasks;

namespace BlockBlast.Features.PlayerCloud.Gateway
{
    public interface IProtectedPlayerDataGateway
    {
        Task<ProtectedPlayerDataReadResult> LoadProtectedSnapshotJsonAsync();
    }
}
