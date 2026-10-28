using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Core.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization.Test
{
    [TestFixture]
    public class WorldManagerTest
    {
        private GameObject _managerGo;
        private WorldManager _manager;
        private WorldCatalogSO _catalog;
        private FakeSelectionStore _selectionStore;
        private GameObject _prefabWorldGround;
        private GameObject _prefabLobbyTab;
        private GameObject _prefabLobbyName;
        private GameObject _prefabPlayScore;
        private GameObject _prefabPlayGround;

        [SetUp]
        public void SetUp()
        {
            _prefabWorldGround = new GameObject("P_WorldGround");
            _prefabLobbyTab = new GameObject("P_LobbyTab");
            _prefabLobbyName = new GameObject("P_LobbyName");
            _prefabPlayScore = new GameObject("P_PlayScore");
            _prefabPlayGround = new GameObject("P_PlayGround");

            _catalog = ScriptableObject.CreateInstance<WorldCatalogSO>();
            var defaultDef = new WorldDefinition(
                "default",
                "Default World",
                null,
                new WorldScenePrefabReferences(_prefabWorldGround),
                new LobbyScenePrefabReferences(_prefabLobbyTab, _prefabLobbyName),
                new PlayScenePrefabReferences(_prefabPlayScore, _prefabPlayGround)
            );
            _catalog.InitializeForTest("default", new List<WorldDefinition> { defaultDef });

            _selectionStore = new FakeSelectionStore();

            _managerGo = new GameObject("WorldManager");
            _manager = _managerGo.AddComponent<WorldManager>();
            _manager.Initialize(_catalog, _selectionStore);
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerGo != null) UnityEngine.Object.DestroyImmediate(_managerGo);
            if (_prefabWorldGround != null) UnityEngine.Object.DestroyImmediate(_prefabWorldGround);
            if (_prefabLobbyTab != null) UnityEngine.Object.DestroyImmediate(_prefabLobbyTab);
            if (_prefabLobbyName != null) UnityEngine.Object.DestroyImmediate(_prefabLobbyName);
            if (_prefabPlayScore != null) UnityEngine.Object.DestroyImmediate(_prefabPlayScore);
            if (_prefabPlayGround != null) UnityEngine.Object.DestroyImmediate(_prefabPlayGround);
        }

        [Test]
        public void Initialize_UnknownSavedId_FallsBackToDefault()
        {
            _selectionStore.SavedId = "non_existent_world";
            _manager.Initialize(_catalog, _selectionStore);

            Assert.AreEqual("default", _manager.SelectedWorldId);
        }

        [Test]
        public void LateHostRegistration_AppliesCurrentWorldImmediately()
        {
            var host = new FakeHost(WorldEnvironmentSlot.World);
            _manager.RegisterHost(host);

            Assert.IsTrue(host.TryStageCalled);
            Assert.IsTrue(host.CommitCalled);
            Assert.AreEqual(1, host.StagedAssignments.Count);
            Assert.AreEqual(WorldEnvironmentPart.WorldGround, host.StagedAssignments[0].Part);
        }

        [Test]
        public async Task ChangeWorldAsync_AtomicSwapAcrossMultipleHosts_Success()
        {
            var altDef = new WorldDefinition(
                "world_forest",
                "Forest",
                null,
                new WorldScenePrefabReferences(_prefabWorldGround),
                new LobbyScenePrefabReferences(_prefabLobbyTab, _prefabLobbyName),
                new PlayScenePrefabReferences(_prefabPlayScore, _prefabPlayGround)
            );
            _catalog.InitializeForTest("default", new List<WorldDefinition> { _catalog.Worlds[0], altDef });

            var host1 = new FakeHost(WorldEnvironmentSlot.World);
            var host2 = new FakeHost(WorldEnvironmentSlot.Lobby);
            _manager.RegisterHost(host1);
            _manager.RegisterHost(host2);

            host1.ResetCallFlags();
            host2.ResetCallFlags();

            var result = await _manager.ChangeWorldAsync("world_forest");

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("world_forest", _manager.SelectedWorldId);
            Assert.AreEqual("world_forest", _selectionStore.SavedId);
            Assert.IsTrue(host1.CommitCalled);
            Assert.IsTrue(host2.CommitCalled);
        }

        [Test]
        public async Task ChangeWorldAsync_OneHostFails_RollsBackAllHostsAndRetainsId()
        {
            var altDef = new WorldDefinition(
                "world_forest",
                "Forest",
                null,
                new WorldScenePrefabReferences(_prefabWorldGround),
                new LobbyScenePrefabReferences(_prefabLobbyTab, _prefabLobbyName),
                new PlayScenePrefabReferences(_prefabPlayScore, _prefabPlayGround)
            );
            _catalog.InitializeForTest("default", new List<WorldDefinition> { _catalog.Worlds[0], altDef });

            var host1 = new FakeHost(WorldEnvironmentSlot.World);
            var host2 = new FakeHost(WorldEnvironmentSlot.Lobby) { FailOnStage = true };
            _manager.RegisterHost(host1);
            _manager.RegisterHost(host2);

            host1.ResetCallFlags();
            host2.ResetCallFlags();

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Failed to apply world to host \\[Lobby\\]"));
            var result = await _manager.ChangeWorldAsync("world_forest");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("default", _manager.SelectedWorldId);
            Assert.IsTrue(host1.RollbackCalled, "Host 1 must be rolled back when Host 2 fails.");
            Assert.IsFalse(host1.CommitCalled);
        }

        [Test]
        public void GameplayForwarding_GameOver_GuardedOncePerSpawnRun()
        {
            var reactor = new FakeReactor();
            var host = new FakeHost(WorldEnvironmentSlot.Play);
            host.ActiveReactorsList.Add(reactor);
            _manager.RegisterHost(host);

            var fakeSpawn = new FakeSpawn();
            _manager.BindSpawnService(fakeSpawn);

            // First game over trigger
            fakeSpawn.TriggerGameOver();
            Assert.AreEqual(1, reactor.GameOverCallCount);

            // Second trigger in same run -> guarded, no extra call
            fakeSpawn.TriggerGameOver();
            Assert.AreEqual(1, reactor.GameOverCallCount);

            // New batch arrives -> resets guard
            fakeSpawn.TriggerBatchSpawned();
            fakeSpawn.TriggerGameOver();
            Assert.AreEqual(2, reactor.GameOverCallCount);
        }

        [Test]
        public void GameplayForwarding_TableClear_OnlyWhenAllClearIsTrue()
        {
            var reactor = new FakeReactor();
            var host = new FakeHost(WorldEnvironmentSlot.Play);
            host.ActiveReactorsList.Add(reactor);
            _manager.RegisterHost(host);

            var fakeGrid = new FakeGrid();
            _manager.BindGridService(fakeGrid);

            // Partial line clear (not all-clear)
            fakeGrid.TriggerPlacementResolved(3, 1, false);
            Assert.AreEqual(0, reactor.TableClearCallCount);

            // All clear
            fakeGrid.TriggerPlacementResolved(4, 2, true);
            Assert.AreEqual(1, reactor.TableClearCallCount);
        }

        // --- Fakes ---

        private sealed class FakeSelectionStore : IWorldSelectionStore
        {
            public string SavedId;
            public string GetSelectedWorldId(string defaultWorldId) => string.IsNullOrEmpty(SavedId) ? defaultWorldId : SavedId;
            public void SaveSelectedWorldId(string worldId) => SavedId = worldId;
        }

        private sealed class FakeHost : IWorldEnvironmentHost
        {
            public WorldEnvironmentSlot Slot { get; }
            public readonly List<IWorldGameplayReactor> ActiveReactorsList = new List<IWorldGameplayReactor>();
            public IReadOnlyList<IWorldGameplayReactor> ActiveReactors => ActiveReactorsList;
            public event Action<IWorldEnvironmentHost> InstancesCommitted;

            public bool FailOnStage;
            public bool TryStageCalled;
            public bool CommitCalled;
            public bool RollbackCalled;
            public IReadOnlyList<WorldPrefabAssignment> StagedAssignments;

            public FakeHost(WorldEnvironmentSlot slot)
            {
                Slot = slot;
            }

            public void ResetCallFlags()
            {
                TryStageCalled = false;
                CommitCalled = false;
                RollbackCalled = false;
            }

            public bool TryStage(IReadOnlyList<WorldPrefabAssignment> assignments, out string error)
            {
                TryStageCalled = true;
                StagedAssignments = assignments;
                if (FailOnStage)
                {
                    error = "Simulated stage failure";
                    return false;
                }
                error = null;
                return true;
            }

            public void CommitStaged()
            {
                CommitCalled = true;
                InstancesCommitted?.Invoke(this);
            }

            public void RollbackStaged()
            {
                RollbackCalled = true;
            }

            public void ResetOwnedInstance()
            {
                ActiveReactorsList.Clear();
            }
        }

        private sealed class FakeReactor : IWorldGameplayReactor
        {
            public int GameOverCallCount;
            public int TableClearCallCount;

            public void PlayLineClear(in WorldLineClearContext context) { }
            public void PlayTableClear(in WorldTableClearContext context) => TableClearCallCount++;
            public void PlayComboChanged(in WorldComboContext context) { }
            public void PlayGameOver() => GameOverCallCount++;
            public void PlayStressStateChanged(in WorldStressContext context) { }
            public void ResetPresentation() { }
        }

        private sealed class FakeSpawn : ISpawnService
        {
            public event Action<BlockModel[], Vector3[]> OnBatchSpawned;
            public event Action OnNoMovesLeft;

            public void SpawnBatch(int score) { }
            public void MarkSlotEmpty(int slotIndex) { }

            public void TriggerGameOver() => OnNoMovesLeft?.Invoke();
            public void TriggerBatchSpawned() => OnBatchSpawned?.Invoke(null, null);
        }

        private sealed class FakeGrid : IGridService
        {
            public int GridWidth => 8;
            public int GridHeight => 8;
            public int OccupiedCellCount => 0;
            public float OccupancyRatio => 0f;
            public bool IsInStress => false;

            public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
            public event Action<List<CellPlacementData>> OnBlockPlaced;
            public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
            public event Action<List<int>, List<int>> OnPreviewLinesToClear;
            public event Action<int, int, bool> OnPlacementResolved;
            public event Action<bool> OnStressStateChanged;

            public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
            public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
            public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;
            public bool CanPlaceBlocks(List<Vector2Int> gridPositions) => true;
            public bool IsCellOccupied(int col, int row) => false;
            public void RequestPreview(List<CellPlacementData> cells) { }
            public void RequestPreview(List<Vector2Int> gridPositions) { }
            public void PlaceBlocks(List<CellPlacementData> cells) { }
            public void PlaceBlocks(List<Vector2Int> gridPositions) { }

            public void TriggerPlacementResolved(int tiles, int lines, bool allClear)
            {
                OnPlacementResolved?.Invoke(tiles, lines, allClear);
            }
        }
    }
}
