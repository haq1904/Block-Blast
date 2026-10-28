using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.WorldCustomization.Test
{
    [TestFixture]
    public class WorldEnvironmentHostTest
    {
        private GameObject _hostGo;
        private WorldEnvironmentHost _host;
        private GameObject _anchorGo;
        private GameObject _prefabGo;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("TestHost");
            _host = _hostGo.AddComponent<WorldEnvironmentHost>();

            _anchorGo = new GameObject("Anchor");
            _anchorGo.transform.SetParent(_hostGo.transform);
            _anchorGo.transform.localPosition = new Vector3(-1.86761f, -0.64f, 12.58734f);

            _prefabGo = new GameObject("PrefabPart");
            _prefabGo.AddComponent<DefaultWorldReactionView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hostGo != null) Object.DestroyImmediate(_hostGo);
            if (_prefabGo != null) Object.DestroyImmediate(_prefabGo);
        }

        [Test]
        public void TryStage_InstantiatesAtExactLocalTransformZero()
        {
            var mount = new WorldPrefabMount(WorldEnvironmentPart.LobbyNamePanel, _anchorGo.transform);
            _host.ConfigureForTest(WorldEnvironmentSlot.Lobby, new[] { mount });

            var assignments = new[]
            {
                new WorldPrefabAssignment(WorldEnvironmentPart.LobbyNamePanel, _prefabGo)
            };

            bool staged = _host.TryStage(assignments, out var error);

            Assert.IsTrue(staged, error);
            Assert.AreEqual(1, _anchorGo.transform.childCount);

            var candidate = _anchorGo.transform.GetChild(0);
            Assert.AreEqual(Vector3.zero, candidate.localPosition);
            Assert.AreEqual(Quaternion.identity, candidate.localRotation);
            Assert.AreEqual(Vector3.one, candidate.localScale);
            Assert.IsFalse(candidate.gameObject.activeSelf, "Candidate must remain inactive while staged.");
        }

        [Test]
        public void CommitStaged_ActivatesCandidate_AndReplacesOldInstance()
        {
            var mount = new WorldPrefabMount(WorldEnvironmentPart.WorldGround, _anchorGo.transform);
            _host.ConfigureForTest(WorldEnvironmentSlot.World, new[] { mount });

            // First commit
            var firstAssignment = new[] { new WorldPrefabAssignment(WorldEnvironmentPart.WorldGround, _prefabGo) };
            _host.TryStage(firstAssignment, out _);
            _host.CommitStaged();

            Assert.AreEqual(1, _anchorGo.transform.childCount);
            var firstInstance = _anchorGo.transform.GetChild(0).gameObject;
            Assert.IsTrue(firstInstance.activeSelf);
            Assert.AreEqual(1, _host.ActiveReactors.Count);

            // Second stage and commit
            var secondPrefab = new GameObject("SecondPrefab");
            var secondAssignment = new[] { new WorldPrefabAssignment(WorldEnvironmentPart.WorldGround, secondPrefab) };
            _host.TryStage(secondAssignment, out _);
            _host.CommitStaged();

            Assert.AreEqual(1, _anchorGo.transform.childCount);
            var secondInstance = _anchorGo.transform.GetChild(0).gameObject;
            Assert.AreNotSame(firstInstance, secondInstance);
            Assert.IsTrue(secondInstance.activeSelf);

            Object.DestroyImmediate(secondPrefab);
        }

        [Test]
        public void RollbackStaged_DestroysCandidate_AndPreservesCurrentInstance()
        {
            var mount = new WorldPrefabMount(WorldEnvironmentPart.WorldGround, _anchorGo.transform);
            _host.ConfigureForTest(WorldEnvironmentSlot.World, new[] { mount });

            var assignment = new[] { new WorldPrefabAssignment(WorldEnvironmentPart.WorldGround, _prefabGo) };
            _host.TryStage(assignment, out _);
            _host.CommitStaged();

            var currentInstance = _anchorGo.transform.GetChild(0).gameObject;

            // Stage another candidate then rollback
            var newPrefab = new GameObject("NewPrefab");
            _host.TryStage(new[] { new WorldPrefabAssignment(WorldEnvironmentPart.WorldGround, newPrefab) }, out _);
            Assert.AreEqual(2, _anchorGo.transform.childCount);

            _host.RollbackStaged();

            Assert.AreEqual(1, _anchorGo.transform.childCount);
            Assert.AreSame(currentInstance, _anchorGo.transform.GetChild(0).gameObject);

            Object.DestroyImmediate(newPrefab);
        }

        [Test]
        public void TryStage_MismatchAssignmentCount_ReturnsFalse()
        {
            var mount = new WorldPrefabMount(WorldEnvironmentPart.WorldGround, _anchorGo.transform);
            _host.ConfigureForTest(WorldEnvironmentSlot.World, new[] { mount });

            // Provide 0 or 2 assignments for a 1-mount host
            bool staged = _host.TryStage(new WorldPrefabAssignment[0], out var error);
            Assert.IsFalse(staged);
            StringAssert.Contains("empty", error);

            staged = _host.TryStage(new[]
            {
                new WorldPrefabAssignment(WorldEnvironmentPart.WorldGround, _prefabGo),
                new WorldPrefabAssignment(WorldEnvironmentPart.PlayGround, _prefabGo)
            }, out error);
            Assert.IsFalse(staged);
            StringAssert.Contains("expected 1", error);
        }
    }
}
