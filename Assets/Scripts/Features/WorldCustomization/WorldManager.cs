using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    /// <summary>
    /// Persistent manager coordinating world customization, catalog resolution,
    /// atomic multi-host swaps, and gameplay signal forwarding to active reactors.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldManager : MonoBehaviour, IWorldService, IWorldHostRegistry
    {
        [Tooltip("The static world catalog containing definitions and prefab references.")]
        [SerializeField] private WorldCatalogSO _catalog;

        private IWorldSelectionStore _selectionStore;
        private string _selectedWorldId;
        private readonly List<IWorldEnvironmentHost> _registeredHosts = new List<IWorldEnvironmentHost>();
        private bool _isChangingWorld;
        private bool _isRegisteredWithServiceLocator;

        // Bound gameplay services for reaction forwarding
        private IGridService _boundGrid;
        private IScoreService _boundScore;
        private ISpawnService _boundSpawn;
        private bool _hasTriggeredGameOverForCurrentRun;

        public string SelectedWorldId => _selectedWorldId;
        public WorldCatalogSO Catalog => _catalog;
        public IReadOnlyList<IWorldEnvironmentHost> RegisteredHosts => _registeredHosts;

        public event Action<WorldDefinition> SelectedWorldChanged;

        private void Awake()
        {
            if (ServiceLocator.TryGet<IWorldService>(out var existing) && existing != null && !ReferenceEquals(existing, this))
            {
                Debug.LogWarning("[WorldManager] An active IWorldService is already registered. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Initialize(_catalog, _selectionStore ?? new WorldSelectionStore());

            ServiceLocator.Register<IWorldService>(this);
            ServiceLocator.Register<IWorldHostRegistry>(this);
            _isRegisteredWithServiceLocator = true;
        }

        private void Start()
        {
            var existingHosts = UnityEngine.Object.FindObjectsByType<WorldEnvironmentHost>(FindObjectsInactive.Include);
            for (int i = 0; i < existingHosts.Length; i++)
            {
                if (existingHosts[i] != null)
                {
                    existingHosts[i].RegisterWithManager();
                }
            }
        }

        public void Initialize(WorldCatalogSO catalog, IWorldSelectionStore selectionStore = null)
        {
            _catalog = catalog;
            _selectionStore = selectionStore ?? new WorldSelectionStore();

            if (_catalog != null && !_catalog.ValidateCatalog(out var catalogError))
            {
                Debug.LogError($"[WorldManager] Catalog validation failed: {catalogError}");
            }

            string defaultId = _catalog != null ? _catalog.DefaultWorldId : "default";
            string savedId = _selectionStore.GetSelectedWorldId(defaultId);

            if (_catalog != null && _catalog.TryGetWorld(savedId, out _))
            {
                _selectedWorldId = savedId;
            }
            else
            {
                _selectedWorldId = defaultId;
            }
        }

        private void OnDestroy()
        {
            if (_isRegisteredWithServiceLocator)
            {
                ServiceLocator.Unregister<IWorldService>(this);
                ServiceLocator.Unregister<IWorldHostRegistry>(this);
                _isRegisteredWithServiceLocator = false;
            }

            UnbindGameplayServices();
        }

        public bool TryGetSelectedWorld(out WorldDefinition definition)
        {
            return TryGetWorld(_selectedWorldId, out definition);
        }

        public bool TryGetWorld(string worldId, out WorldDefinition definition)
        {
            definition = null;
            if (_catalog == null) return false;
            return _catalog.TryGetWorld(worldId, out definition);
        }

        public void RegisterHost(IWorldEnvironmentHost host)
        {
            if (host == null || _registeredHosts.Contains(host)) return;

            _registeredHosts.Add(host);

            // Apply current world selection immediately to newly registered host
            if (!TryGetSelectedWorld(out var currentDefinition))
            {
                Debug.LogError($"[WorldManager] Failed to apply world to host [{host.Slot}]: Selected world definition '{_selectedWorldId}' could not be resolved from catalog.");
                return;
            }

            var assignments = BuildAssignmentsForSlot(host.Slot, currentDefinition);
            if (assignments == null || assignments.Count == 0)
            {
                Debug.LogError($"[WorldManager] Failed to apply world to host [{host.Slot}]: No assignments built for slot.");
                return;
            }

            if (!host.TryStage(assignments, out var stageError))
            {
                Debug.LogError($"[WorldManager] Failed to apply world to host [{host.Slot}]: Staging failed: {stageError}");
                return;
            }

            host.CommitStaged();

            if (host.Slot == WorldEnvironmentSlot.Play)
            {
                TryBindGameplayServicesFromLocator();
            }
        }

        public void UnregisterHost(IWorldEnvironmentHost host)
        {
            if (host == null) return;
            _registeredHosts.Remove(host);

            if (host.Slot == WorldEnvironmentSlot.Play)
            {
                UnbindGameplayServices();
            }
        }

        public async Task<WorldChangeResult> ChangeWorldAsync(string worldId, CancellationToken cancellationToken = default)
        {
            if (_isChangingWorld)
            {
                return WorldChangeResult.Busy();
            }

            if (_catalog == null)
            {
                return WorldChangeResult.Failed("Catalog is not initialized.");
            }

            if (!_catalog.TryGetWorld(worldId, out var targetDefinition))
            {
                return WorldChangeResult.Failed($"Target world '{worldId}' not found in catalog.");
            }

            // Verify all 5 required parts exist
            if (!targetDefinition.TryGetPrefab(WorldEnvironmentPart.WorldGround, out var wg) || wg == null ||
                !targetDefinition.TryGetPrefab(WorldEnvironmentPart.LobbyControllerTab, out var lct) || lct == null ||
                !targetDefinition.TryGetPrefab(WorldEnvironmentPart.LobbyNamePanel, out var lnp) || lnp == null ||
                !targetDefinition.TryGetPrefab(WorldEnvironmentPart.PlayScorePanel, out var psp) || psp == null ||
                !targetDefinition.TryGetPrefab(WorldEnvironmentPart.PlayGround, out var pg) || pg == null)
            {
                return WorldChangeResult.Failed($"Target world '{worldId}' is missing required prefab references.");
            }

            if (string.Equals(_selectedWorldId, worldId, StringComparison.OrdinalIgnoreCase))
            {
                return WorldChangeResult.Success();
            }

            _isChangingWorld = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If loading screen service is registered, execute within covered lifecycle
                if (ServiceLocator.TryGet<ILoadingScreenService>(out var loadingService) && loadingService != null)
                {
                    await loadingService.RunCoveredOperationAsync(async ct =>
                    {
                        await PerformMultiHostSwapAsync(targetDefinition, ct);
                    }, cancellationToken);
                }
                else
                {
                    await PerformMultiHostSwapAsync(targetDefinition, cancellationToken);
                }

                _selectedWorldId = targetDefinition.Id;
                _selectionStore?.SaveSelectedWorldId(_selectedWorldId);
                SelectedWorldChanged?.Invoke(targetDefinition);

                return WorldChangeResult.Success();
            }
            catch (OperationCanceledException)
            {
                RollbackAllHosts();
                return WorldChangeResult.Cancelled("World change was cancelled.");
            }
            catch (Exception ex)
            {
                RollbackAllHosts();
                return WorldChangeResult.Failed(ex.Message);
            }
            finally
            {
                _isChangingWorld = false;
            }
        }

        private Task PerformMultiHostSwapAsync(WorldDefinition targetDefinition, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            for (int i = _registeredHosts.Count - 1; i >= 0; i--)
            {
                if (_registeredHosts[i] == null || (_registeredHosts[i] is MonoBehaviour mb && mb == null))
                {
                    _registeredHosts.RemoveAt(i);
                }
            }

            // 1. Stage candidates across all registered hosts
            for (int i = 0; i < _registeredHosts.Count; i++)
            {
                var host = _registeredHosts[i];
                var assignments = BuildAssignmentsForSlot(host.Slot, targetDefinition);
                string error = null;
                if (assignments == null || !host.TryStage(assignments, out error))
                {
                    RollbackAllHosts();
                    throw new InvalidOperationException($"Failed to stage slot {host.Slot}: {error ?? "Null assignments"}");
                }
            }

            ct.ThrowIfCancellationRequested();

            // 2. Commit all staged hosts
            for (int i = 0; i < _registeredHosts.Count; i++)
            {
                _registeredHosts[i].CommitStaged();
            }

            return Task.CompletedTask;
        }

        private void RollbackAllHosts()
        {
            for (int i = 0; i < _registeredHosts.Count; i++)
            {
                try
                {
                    _registeredHosts[i].RollbackStaged();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldManager] Error rolling back host {_registeredHosts[i].Slot}: {ex.Message}");
                }
            }
        }

        private List<WorldPrefabAssignment> BuildAssignmentsForSlot(WorldEnvironmentSlot slot, WorldDefinition definition)
        {
            if (definition == null) return null;
            var list = new List<WorldPrefabAssignment>();

            switch (slot)
            {
                case WorldEnvironmentSlot.World:
                    if (definition.TryGetPrefab(WorldEnvironmentPart.WorldGround, out var wg) && wg != null)
                    {
                        list.Add(new WorldPrefabAssignment(WorldEnvironmentPart.WorldGround, wg));
                    }
                    break;

                case WorldEnvironmentSlot.Lobby:
                    if (definition.TryGetPrefab(WorldEnvironmentPart.LobbyControllerTab, out var lct) && lct != null)
                    {
                        list.Add(new WorldPrefabAssignment(WorldEnvironmentPart.LobbyControllerTab, lct));
                    }
                    if (definition.TryGetPrefab(WorldEnvironmentPart.LobbyNamePanel, out var lnp) && lnp != null)
                    {
                        list.Add(new WorldPrefabAssignment(WorldEnvironmentPart.LobbyNamePanel, lnp));
                    }
                    break;

                case WorldEnvironmentSlot.Play:
                    if (definition.TryGetPrefab(WorldEnvironmentPart.PlayScorePanel, out var psp) && psp != null)
                    {
                        list.Add(new WorldPrefabAssignment(WorldEnvironmentPart.PlayScorePanel, psp));
                    }
                    if (definition.TryGetPrefab(WorldEnvironmentPart.PlayGround, out var pg) && pg != null)
                    {
                        list.Add(new WorldPrefabAssignment(WorldEnvironmentPart.PlayGround, pg));
                    }
                    break;
            }

            return list;
        }

        // --- Gameplay Presentation Signal Binding & Forwarding ---

        public void TryBindGameplayServicesFromLocator()
        {
            if (ServiceLocator.TryGet<IGridService>(out var grid))
            {
                BindGridService(grid);
            }
            if (ServiceLocator.TryGet<IScoreService>(out var score))
            {
                BindScoreService(score);
            }
            if (ServiceLocator.TryGet<ISpawnService>(out var spawn))
            {
                BindSpawnService(spawn);
            }
        }

        public void BindGridService(IGridService grid)
        {
            if (ReferenceEquals(_boundGrid, grid)) return;
            UnbindGridService();

            _boundGrid = grid;
            if (_boundGrid != null)
            {
                _boundGrid.OnLinesCleared += HandleLinesCleared;
                _boundGrid.OnPlacementResolved += HandlePlacementResolved;
                _boundGrid.OnStressStateChanged += HandleStressStateChanged;
            }
        }

        public void BindScoreService(IScoreService score)
        {
            if (ReferenceEquals(_boundScore, score)) return;
            UnbindScoreService();

            _boundScore = score;
            if (_boundScore != null)
            {
                _boundScore.OnComboChanged += HandleComboChanged;
            }
        }

        public void BindSpawnService(ISpawnService spawn)
        {
            if (ReferenceEquals(_boundSpawn, spawn)) return;
            UnbindSpawnService();

            _boundSpawn = spawn;
            if (_boundSpawn != null)
            {
                _hasTriggeredGameOverForCurrentRun = false;
                _boundSpawn.OnNoMovesLeft += HandleGameOver;
                _boundSpawn.OnBatchSpawned += HandleBatchSpawned;
            }
        }

        public void UnbindGameplayServices()
        {
            UnbindGridService();
            UnbindScoreService();
            UnbindSpawnService();
        }

        private void UnbindGridService()
        {
            if (_boundGrid != null)
            {
                _boundGrid.OnLinesCleared -= HandleLinesCleared;
                _boundGrid.OnPlacementResolved -= HandlePlacementResolved;
                _boundGrid.OnStressStateChanged -= HandleStressStateChanged;
                _boundGrid = null;
            }
        }

        private void UnbindScoreService()
        {
            if (_boundScore != null)
            {
                _boundScore.OnComboChanged -= HandleComboChanged;
                _boundScore = null;
            }
        }

        private void UnbindSpawnService()
        {
            if (_boundSpawn != null)
            {
                _boundSpawn.OnNoMovesLeft -= HandleGameOver;
                _boundSpawn.OnBatchSpawned -= HandleBatchSpawned;
                _boundSpawn = null;
            }
        }

        private void HandleLinesCleared(List<int> rows, List<int> cols, List<Vector3> comboPositions)
        {
            int total = (rows != null ? rows.Count : 0) + (cols != null ? cols.Count : 0);
            var context = new WorldLineClearContext(total, rows, cols, comboPositions);
            ForEachActiveReactor(r => r.PlayLineClear(context));
        }

        private void HandlePlacementResolved(int tilesPlaced, int totalLinesCleared, bool isAllClear)
        {
            if (isAllClear)
            {
                var context = new WorldTableClearContext(tilesPlaced, totalLinesCleared);
                ForEachActiveReactor(r => r.PlayTableClear(context));
            }
        }

        private void HandleComboChanged(int combo)
        {
            var context = new WorldComboContext(combo);
            ForEachActiveReactor(r => r.PlayComboChanged(context));
        }

        private void HandleStressStateChanged(bool inStress)
        {
            float ratio = _boundGrid?.OccupancyRatio ?? 0f;
            var context = new WorldStressContext(inStress, ratio);
            ForEachActiveReactor(r => r.PlayStressStateChanged(context));
        }

        private void HandleBatchSpawned(BlockModel[] blocks, Vector3[] positions)
        {
            _hasTriggeredGameOverForCurrentRun = false;
        }

        private void HandleGameOver()
        {
            if (_hasTriggeredGameOverForCurrentRun) return;
            _hasTriggeredGameOverForCurrentRun = true;
            ForEachActiveReactor(r => r.PlayGameOver());
        }

        private void ForEachActiveReactor(Action<IWorldGameplayReactor> action)
        {
            if (action == null) return;
            for (int i = 0; i < _registeredHosts.Count; i++)
            {
                var reactors = _registeredHosts[i].ActiveReactors;
                if (reactors != null)
                {
                    for (int j = 0; j < reactors.Count; j++)
                    {
                        var reactor = reactors[j];
                        if (reactor != null)
                        {
                            action(reactor);
                        }
                    }
                }
            }
        }
    }
}
