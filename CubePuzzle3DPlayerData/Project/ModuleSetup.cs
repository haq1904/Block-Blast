using Microsoft.Extensions.DependencyInjection;
using CubePuzzle3DPlayerData.Persistence;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace CubePuzzle3DPlayerData;

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
        config.Dependencies.AddSingleton<IPlayerSnapshotStore, CloudSavePlayerSnapshotStore>();
    }
}
