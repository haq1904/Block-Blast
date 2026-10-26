using System.Threading.Tasks;
using GeneratedContracts = Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts;

namespace BlockBlast.Features.PlayerCloud.Gateway
{
    public interface IPlayerCloudCodeGateway
    {
        Task<GeneratedContracts.SubmitGameResultResponse> SubmitGameResultAsync(GeneratedContracts.GameResultRequest request);
    }
}
