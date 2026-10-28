using System;
using System.Collections.Generic;
using BlockBlast.Features.Lobby;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    /// <summary>
    /// Scene-owned bridge hosting environment prefabs for a specific scene slot.
    /// Manages mount anchors, candidate staging, atomic commit, and reactor collection.
    /// Does not query catalog, persistence, or gameplay services directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldEnvironmentHost : MonoBehaviour, IWorldEnvironmentHost
    {
        [Tooltip("The scene slot this host is responsible for.")]
        [SerializeField] private WorldEnvironmentSlot _slot;

        [Tooltip("Serialized mapping of parts to explicit scene anchor transforms.")]
        [SerializeField] private WorldPrefabMount[] _mounts = Array.Empty<WorldPrefabMount>();

        private readonly Dictionary<WorldEnvironmentPart, GameObject> _ownedInstances = new Dictionary<WorldEnvironmentPart, GameObject>();
        private readonly List<StagedCandidate> _stagedInstances = new List<StagedCandidate>();
        private readonly List<IWorldGameplayReactor> _activeReactors = new List<IWorldGameplayReactor>();
        private readonly List<LobbyWorldButtonView> _lobbyWorldButtons = new List<LobbyWorldButtonView>();

        private bool _isRegistered;

        public WorldEnvironmentSlot Slot => _slot;
        public IReadOnlyList<WorldPrefabMount> Mounts => _mounts;
        public IReadOnlyList<IWorldGameplayReactor> ActiveReactors => _activeReactors;
        public IReadOnlyList<LobbyWorldButtonView> LobbyWorldButtons => _lobbyWorldButtons;

        public event Action<IWorldEnvironmentHost> InstancesCommitted;
        public event Action<IReadOnlyList<LobbyWorldButtonView>> LobbyWorldButtonsChanged;

        private struct StagedCandidate
        {
            public WorldEnvironmentPart Part;
            public GameObject Instance;
        }

        public void ConfigureForTest(WorldEnvironmentSlot slot, WorldPrefabMount[] mounts)
        {
            _slot = slot;
            _mounts = mounts ?? Array.Empty<WorldPrefabMount>();
        }

        private void Awake()
        {
            RegisterWithManager();
        }

        private void OnEnable()
        {
            RegisterWithManager();
        }

        private void OnDisable()
        {
            UnregisterWithManager();
        }

        private void OnDestroy()
        {
            UnregisterWithManager();
            ResetOwnedInstance();
        }

        public void RegisterWithManager()
        {
            if (_isRegistered) return;

            if (ServiceLocator.TryGet<IWorldHostRegistry>(out var registry) && registry != null)
            {
                registry.RegisterHost(this);
                _isRegistered = true;
            }
        }

        public void UnregisterWithManager()
        {
            if (!_isRegistered) return;

            if (ServiceLocator.TryGet<IWorldHostRegistry>(out var registry) && registry != null)
            {
                registry.UnregisterHost(this);
            }
            _isRegistered = false;
        }

        public bool TryStage(IReadOnlyList<WorldPrefabAssignment> assignments, out string error)
        {
            if (assignments == null || assignments.Count == 0)
            {
                error = $"Host [{name}] received empty or null assignments.";
                return false;
            }

            if (_mounts == null || _mounts.Length == 0)
            {
                error = $"Host [{name}] has no serialized mounts.";
                return false;
            }

            if (assignments.Count != _mounts.Length)
            {
                error = $"Host [{name}] expected {_mounts.Length} assignments for slot {_slot}, but received {assignments.Count}.";
                return false;
            }

            // Verify mapping
            var partToAnchor = new Dictionary<WorldEnvironmentPart, Transform>();
            for (int i = 0; i < _mounts.Length; i++)
            {
                var mount = _mounts[i];
                if (mount == null || mount.Anchor == null)
                {
                    error = $"Host [{name}] mount at index {i} has missing anchor.";
                    return false;
                }
                if (partToAnchor.ContainsKey(mount.Part))
                {
                    error = $"Host [{name}] has duplicate mount for part {mount.Part}.";
                    return false;
                }
                partToAnchor.Add(mount.Part, mount.Anchor);
            }

            var assignedParts = new HashSet<WorldEnvironmentPart>();
            for (int i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                if (assignment.Prefab == null)
                {
                    error = $"Host [{name}] received null prefab for part {assignment.Part}.";
                    return false;
                }
                if (!partToAnchor.ContainsKey(assignment.Part))
                {
                    error = $"Host [{name}] received unmapped part {assignment.Part}.";
                    return false;
                }
                if (!assignedParts.Add(assignment.Part))
                {
                    error = $"Host [{name}] received duplicate assignment for part {assignment.Part}.";
                    return false;
                }
            }

            // Rollback previous staged if any
            RollbackStaged();

            // Instantiate candidates
            try
            {
                for (int i = 0; i < assignments.Count; i++)
                {
                    var assignment = assignments[i];
                    var anchor = partToAnchor[assignment.Part];
                    var candidate = Instantiate(assignment.Prefab, anchor, false);

                    // Force exact placement invariant
                    candidate.transform.localPosition = Vector3.zero;
                    candidate.transform.localRotation = Quaternion.identity;
                    candidate.transform.localScale = Vector3.one;
                    candidate.SetActive(false);

                    _stagedInstances.Add(new StagedCandidate
                    {
                        Part = assignment.Part,
                        Instance = candidate
                    });
                }

                error = null;
                return true;
            }
            catch (Exception ex)
            {
                RollbackStaged();
                error = $"Host [{name}] failed to instantiate candidate: {ex.Message}";
                return false;
            }
        }

        public void CommitStaged()
        {
            if (_stagedInstances.Count == 0) return;

            for (int i = 0; i < _stagedInstances.Count; i++)
            {
                var staged = _stagedInstances[i];
                if (_ownedInstances.TryGetValue(staged.Part, out var oldInstance) && oldInstance != null)
                {
                    SafeDestroy(oldInstance);
                }

                if (staged.Instance != null)
                {
                    staged.Instance.SetActive(true);
                    _ownedInstances[staged.Part] = staged.Instance;
                }
            }

            _stagedInstances.Clear();
            CollectActiveReactors();

            if (_slot == WorldEnvironmentSlot.Lobby)
            {
                CollectLobbyButtons();
            }

            InstancesCommitted?.Invoke(this);
        }

        public void RollbackStaged()
        {
            for (int i = 0; i < _stagedInstances.Count; i++)
            {
                if (_stagedInstances[i].Instance != null)
                {
                    SafeDestroy(_stagedInstances[i].Instance);
                }
            }
            _stagedInstances.Clear();
        }

        public void ResetOwnedInstance()
        {
            RollbackStaged();

            foreach (var kvp in _ownedInstances)
            {
                if (kvp.Value != null)
                {
                    SafeDestroy(kvp.Value);
                }
            }
            _ownedInstances.Clear();
            _activeReactors.Clear();
            _lobbyWorldButtons.Clear();

            InstancesCommitted?.Invoke(this);
        }

        public IReadOnlyList<LobbyWorldButtonView> GetLobbyWorldButtons()
        {
            CollectLobbyButtons();
            return _lobbyWorldButtons;
        }

        private void CollectActiveReactors()
        {
            _activeReactors.Clear();
            foreach (var kvp in _ownedInstances)
            {
                if (kvp.Value != null)
                {
                    var reactors = kvp.Value.GetComponentsInChildren<IWorldGameplayReactor>(true);
                    for (int i = 0; i < reactors.Length; i++)
                    {
                        if (reactors[i] != null)
                        {
                            _activeReactors.Add(reactors[i]);
                        }
                    }
                }
            }
        }

        private void CollectLobbyButtons()
        {
            _lobbyWorldButtons.Clear();
            foreach (var kvp in _ownedInstances)
            {
                if (kvp.Value != null)
                {
                    var buttons = kvp.Value.GetComponentsInChildren<LobbyWorldButtonView>(true);
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i] != null)
                        {
                            _lobbyWorldButtons.Add(buttons[i]);
                        }
                    }
                }
            }
            LobbyWorldButtonsChanged?.Invoke(_lobbyWorldButtons);
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }
    }
}
