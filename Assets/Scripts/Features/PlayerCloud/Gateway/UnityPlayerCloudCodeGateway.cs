using System;
using System.Threading.Tasks;
using Unity.Services.CloudCode.GeneratedBindings;
using GeneratedContracts = Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts;

namespace BlockBlast.Features.PlayerCloud.Gateway
{
    public sealed class UnityPlayerCloudCodeGateway : IPlayerCloudCodeGateway
    {
        private readonly PlayerDataModuleBindings _bindings;

        public UnityPlayerCloudCodeGateway()
            : this(new PlayerDataModuleBindings())
        {
        }

        public UnityPlayerCloudCodeGateway(PlayerDataModuleBindings bindings)
        {
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public Task<GeneratedContracts.SubmitGameResultResponse> SubmitGameResultAsync(GeneratedContracts.GameResultRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return _bindings.SubmitGameResult(request);
        }
    }
}
