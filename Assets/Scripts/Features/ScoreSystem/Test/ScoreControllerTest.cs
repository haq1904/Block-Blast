using System;
using System.Collections.Generic;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.ScoreSystem.Test
{
    [TestFixture]
    public class ScoreControllerTest
    {
        private class FakeLocalPlayerDataStore : ILocalPlayerDataStore
        {
            public int HighScore;
            public int SaveCount;

            public int LoadHighScore() => HighScore;

            public bool SaveHighScore(int newScore)
            {
                if (newScore > HighScore)
                {
                    HighScore = newScore;
                    SaveCount++;
                    return true;
                }
                return false;
            }
        }

        private class MockScoreTestGrid : IGridService
        {
            public int GridWidth => 8;
            public int GridHeight => 8;
            public int OccupiedCellCount => 0;
            public float OccupancyRatio => 0f;

#pragma warning disable CS0067
            public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
            public event Action<List<CellPlacementData>> OnBlockPlaced;
            public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
            public event Action<List<int>, List<int>> OnPreviewLinesToClear;
#pragma warning restore CS0067
            public event Action<int, int, bool> OnPlacementResolved;

            public void TriggerPlacementResolved(int tiles, int lines, bool allClear)
            {
                OnPlacementResolved?.Invoke(tiles, lines, allClear);
            }

            public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
            public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
            public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;
            public bool CanPlaceBlocks(List<Vector2Int> gridPositions) => true;
            public bool IsCellOccupied(int col, int row) => false;
            public void RequestPreview(List<CellPlacementData> cells) { }
            public void RequestPreview(List<Vector2Int> gridPositions) { }
            public void PlaceBlocks(List<CellPlacementData> cells) { }
            public void PlaceBlocks(List<Vector2Int> gridPositions) { }
        }

        [Test]
        public void Score_Placement_AddsTileCountScore_WithoutAffectingCombo()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            int scoreEventCurrent = -1, scoreEventGained = -1;
            controller.OnScoreChanged += (curr, gained) =>
            {
                scoreEventCurrent = curr;
                scoreEventGained = gained;
            };

            controller.HandlePlacementResolved(4, 0, false);

            Assert.AreEqual(4, controller.CurrentScore);
            Assert.AreEqual(0, controller.CurrentCombo);
            Assert.AreEqual(4, scoreEventCurrent);
            Assert.AreEqual(4, scoreEventGained);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_Prewarm_FirstClear_TriggersPrewarm_WithoutCombo()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.comboBonusStep = 10;
            config.prewarmWindowTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            int comboEventFired = -1;
            controller.OnComboChanged += c => comboEventFired = c;

            controller.HandlePlacementResolved(2, 1, false);

            Assert.AreEqual(12, controller.CurrentScore);
            Assert.AreEqual(0, controller.CurrentCombo, "Prewarm does not grant a combo yet.");
            Assert.IsTrue(controller.IsPrewarmed, "Board should enter prewarmed state.");
            Assert.AreEqual(3, controller.TurnsRemaining);
            Assert.AreEqual(-1, comboEventFired, "Combo event should not fire when entering prewarm.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_Prewarm_ClearWithinWindow_IgnitesComboAndAwardsBonus()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.points2Lines = 30;
            config.comboBonusStep = 10;
            config.prewarmWindowTurns = 3;
            config.comboGraceTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            Assert.IsTrue(controller.IsPrewarmed);
            Assert.AreEqual(0, controller.CurrentCombo);
            Assert.AreEqual(11, controller.CurrentScore);

            controller.HandlePlacementResolved(2, 0, false);
            Assert.IsTrue(controller.IsPrewarmed);
            Assert.AreEqual(2, controller.TurnsRemaining);

            controller.HandlePlacementResolved(3, 2, false);
            Assert.IsFalse(controller.IsPrewarmed);
            Assert.AreEqual(2, controller.CurrentCombo);
            Assert.AreEqual(3, controller.TurnsRemaining);
            Assert.AreEqual(66, controller.CurrentScore);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_Prewarm_ExpiresAfter3NonClearingTurns()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.prewarmWindowTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            Assert.IsTrue(controller.IsPrewarmed);
            Assert.AreEqual(3, controller.TurnsRemaining);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(2, controller.TurnsRemaining);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(1, controller.TurnsRemaining);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.IsFalse(controller.IsPrewarmed, "Prewarm must expire after 3 non-clearing placements.");
            Assert.AreEqual(0, controller.TurnsRemaining);
            Assert.AreEqual(0, controller.CurrentCombo);

            controller.HandlePlacementResolved(1, 1, false);
            Assert.IsTrue(controller.IsPrewarmed, "Must restart prewarm cycle.");
            Assert.AreEqual(0, controller.CurrentCombo);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_ActiveCombo_AccumulatesLinesAndRefreshesCounter()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.points2Lines = 30;
            config.points3Lines = 60;
            config.comboBonusStep = 10;
            config.prewarmWindowTurns = 3;
            config.comboGraceTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            Assert.AreEqual(0, controller.CurrentCombo);

            controller.HandlePlacementResolved(3, 3, false);
            Assert.AreEqual(3, controller.CurrentCombo);
            Assert.AreEqual(3, controller.TurnsRemaining);

            controller.HandlePlacementResolved(2, 2, false);
            Assert.AreEqual(5, controller.CurrentCombo);
            Assert.AreEqual(5, controller.MaxCombo);
            Assert.AreEqual(3, controller.TurnsRemaining);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_ActiveCombo_GraceTurns_MaintainsComboUntilThirdMiss()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.comboBonusStep = 10;
            config.prewarmWindowTurns = 3;
            config.comboGraceTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            controller.HandlePlacementResolved(1, 1, false);
            Assert.AreEqual(1, controller.CurrentCombo);
            Assert.AreEqual(3, controller.TurnsRemaining);

            int comboResetCount = 0;
            controller.OnComboChanged += c =>
            {
                if (c == 0) comboResetCount++;
            };

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(1, controller.CurrentCombo);
            Assert.AreEqual(2, controller.TurnsRemaining);
            Assert.AreEqual(0, comboResetCount);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(1, controller.CurrentCombo);
            Assert.AreEqual(1, controller.TurnsRemaining);
            Assert.AreEqual(0, comboResetCount);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(0, controller.CurrentCombo);
            Assert.AreEqual(0, controller.TurnsRemaining);
            Assert.AreEqual(1, comboResetCount);
            Assert.AreEqual(1, controller.MaxCombo);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_ActiveCombo_ClearWithinGracePeriod_ResetsCounter()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.points2Lines = 30;
            config.comboBonusStep = 10;
            config.prewarmWindowTurns = 3;
            config.comboGraceTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            controller.HandlePlacementResolved(1, 1, false);
            Assert.AreEqual(1, controller.CurrentCombo);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(2, controller.TurnsRemaining);

            controller.HandlePlacementResolved(1, 0, false);
            Assert.AreEqual(1, controller.TurnsRemaining);

            controller.HandlePlacementResolved(2, 2, false);
            Assert.AreEqual(3, controller.CurrentCombo);
            Assert.AreEqual(3, controller.TurnsRemaining);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_AllClear_GrantsLargeBonus()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.comboBonusStep = 10;
            config.allClearBonus = 300;
            config.prewarmWindowTurns = 3;
            config.comboGraceTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            controller.HandlePlacementResolved(2, 1, true);

            Assert.AreEqual(333, controller.CurrentScore);
            Assert.AreEqual(1, controller.CurrentCombo);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_AllClear_MultipliesBonusByCombo()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points2Lines = 30;
            config.comboBonusStep = 10;
            config.allClearBonus = 300;
            config.prewarmWindowTurns = 3;
            config.comboGraceTurns = 3;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(1, 1, false);
            controller.HandlePlacementResolved(2, 2, true);

            Assert.AreEqual(2, controller.CurrentCombo);
            Assert.AreEqual(663, controller.CurrentScore);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_HighScore_UpdatesAndPersists_WhenExceeded()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            int newHighFired = -1;
            controller.OnHighScoreChanged += high => newHighFired = high;

            controller.HandlePlacementResolved(50, 0, false);

            Assert.AreEqual(50, controller.CurrentScore);
            Assert.AreEqual(50, controller.HighScore);
            Assert.AreEqual(50, newHighFired);
            Assert.AreEqual(50, store.HighScore);
            Assert.AreEqual(1, store.SaveCount);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_ResetScore_ResetsCurrentScoreAndCombo_RetainsHighScore()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            controller.HandlePlacementResolved(100, 0, false);
            Assert.AreEqual(100, controller.CurrentScore);
            Assert.AreEqual(100, controller.HighScore);

            controller.ResetScore();
            Assert.AreEqual(0, controller.CurrentScore);
            Assert.AreEqual(100, controller.HighScore, "ResetScore must retain the existing HighScore.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Score_GridEvent_OnPlacementResolved_InvokesScoreCalculation()
        {
            var config = ScriptableObject.CreateInstance<ScoreConfiguration>();
            config.pointsPerTile = 1;
            config.points1Line = 10;
            config.comboBonusStep = 10;

            var go = new GameObject("ScoreControllerTest");
            var controller = go.AddComponent<ScoreController>();
            var grid = new MockScoreTestGrid();
            var store = new FakeLocalPlayerDataStore();
            controller.Initialize(config, grid, store);

            grid.TriggerPlacementResolved(3, 1, false);
            Assert.AreEqual(13, controller.CurrentScore);
            Assert.AreEqual(0, controller.CurrentCombo);
            Assert.IsTrue(controller.IsPrewarmed);

            grid.TriggerPlacementResolved(2, 1, false);
            Assert.AreEqual(35, controller.CurrentScore);
            Assert.AreEqual(1, controller.CurrentCombo);

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ScoreView_ComponentCanBeInstantiated()
        {
            var go = new GameObject("ScoreViewTest");
            var view = go.AddComponent<ScoreView>();
            Assert.IsNotNull(view);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
