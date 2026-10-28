using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    [CreateAssetMenu(fileName = "WorldCatalog", menuName = "BlockBlast/World Customization/World Catalog")]
    public sealed class WorldCatalogSO : ScriptableObject
    {
        [SerializeField] private string _defaultWorldId = "default";
        [SerializeField] private List<WorldDefinition> _worlds = new List<WorldDefinition>();

        public string DefaultWorldId => _defaultWorldId;
        public IReadOnlyList<WorldDefinition> Worlds => _worlds;

        public void InitializeForTest(string defaultWorldId, List<WorldDefinition> worlds)
        {
            _defaultWorldId = defaultWorldId;
            _worlds = worlds ?? new List<WorldDefinition>();
        }

        public bool TryGetWorld(string worldId, out WorldDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(worldId) || _worlds == null) return false;

            for (int i = 0; i < _worlds.Count; i++)
            {
                var candidate = _worlds[i];
                if (candidate != null && string.Equals(candidate.Id, worldId, StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetDefaultWorld(out WorldDefinition definition)
        {
            return TryGetWorld(_defaultWorldId, out definition);
        }

        public bool ValidateCatalog(out string error)
        {
            if (string.IsNullOrWhiteSpace(_defaultWorldId))
            {
                error = "Default world ID must not be empty.";
                return false;
            }

            if (_worlds == null || _worlds.Count == 0)
            {
                error = "World catalog contains no world definitions.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool defaultFound = false;

            for (int i = 0; i < _worlds.Count; i++)
            {
                var world = _worlds[i];
                if (world == null)
                {
                    error = $"World definition at index {i} is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(world.Id))
                {
                    error = $"World definition at index {i} has empty ID.";
                    return false;
                }

                if (!ids.Add(world.Id))
                {
                    error = $"Duplicate world ID '{world.Id}' detected at index {i}.";
                    return false;
                }

                if (string.Equals(world.Id, _defaultWorldId, StringComparison.OrdinalIgnoreCase))
                {
                    defaultFound = true;
                }

                // Validate all 5 required parts
                if (!world.TryGetPrefab(WorldEnvironmentPart.WorldGround, out var wg) || wg == null)
                {
                    error = $"World '{world.Id}' is missing required WorldGround prefab.";
                    return false;
                }
                if (!world.TryGetPrefab(WorldEnvironmentPart.LobbyControllerTab, out var lct) || lct == null)
                {
                    error = $"World '{world.Id}' is missing required LobbyControllerTab prefab.";
                    return false;
                }
                if (!world.TryGetPrefab(WorldEnvironmentPart.LobbyNamePanel, out var lnp) || lnp == null)
                {
                    error = $"World '{world.Id}' is missing required LobbyNamePanel prefab.";
                    return false;
                }
                if (!world.TryGetPrefab(WorldEnvironmentPart.PlayScorePanel, out var psp) || psp == null)
                {
                    error = $"World '{world.Id}' is missing required PlayScorePanel prefab.";
                    return false;
                }
                if (!world.TryGetPrefab(WorldEnvironmentPart.PlayGround, out var pg) || pg == null)
                {
                    error = $"World '{world.Id}' is missing required PlayGround prefab.";
                    return false;
                }
            }

            if (!defaultFound)
            {
                error = $"Configured default world ID '{_defaultWorldId}' is not present in the catalog.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
