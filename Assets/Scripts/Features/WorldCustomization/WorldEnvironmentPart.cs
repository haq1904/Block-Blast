using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    public enum WorldEnvironmentPart
    {
        WorldGround,
        LobbyControllerTab,
        LobbyNamePanel,
        PlayScorePanel,
        PlayGround
    }

    public readonly struct WorldPrefabAssignment
    {
        public WorldEnvironmentPart Part { get; }
        public GameObject Prefab { get; }

        public WorldPrefabAssignment(WorldEnvironmentPart part, GameObject prefab)
        {
            Part = part;
            Prefab = prefab;
        }
    }
}
