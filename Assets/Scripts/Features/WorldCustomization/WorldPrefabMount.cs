using System;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    [Serializable]
    public sealed class WorldPrefabMount
    {
        [SerializeField] private WorldEnvironmentPart _part;
        [SerializeField] private Transform _anchor;

        public WorldEnvironmentPart Part => _part;
        public Transform Anchor => _anchor;

        public WorldPrefabMount() { }

        public WorldPrefabMount(WorldEnvironmentPart part, Transform anchor)
        {
            _part = part;
            _anchor = anchor;
        }
    }
}
