using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;

namespace BlockBlast.Features.PlayerCloud.Gateway
{
    public sealed class UnityProtectedPlayerDataGateway : IProtectedPlayerDataGateway
    {
        private static readonly HashSet<string> TargetKeys = new HashSet<string>
        {
            PlayerCloudConstants.ProtectedPlayerDataKey
        };

        public async Task<ProtectedPlayerDataReadResult> LoadProtectedSnapshotJsonAsync()
        {
            var options = new LoadOptions(new ProtectedReadAccessClassOptions());
            var results = await CloudSaveService.Instance.Data.Player.LoadAsync(TargetKeys, options);

            if (results != null && results.TryGetValue(PlayerCloudConstants.ProtectedPlayerDataKey, out var item) && item?.Value != null)
            {
                string json = item.Value.GetAsString();
                if (!string.IsNullOrEmpty(json))
                {
                    return ProtectedPlayerDataReadResult.FoundResult(json);
                }
            }

            return ProtectedPlayerDataReadResult.NotFound();
        }
    }
}
