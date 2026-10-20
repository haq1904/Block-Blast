using BlockBlast.Features.GameFlow.Play;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public class PlaySceneActivationControllerTest
    {
        [Test]
        public void ActivateGameplay_WhenConfigured_ActivatesBothRootsOnlyOnce()
        {
            var root = new GameObject("PlayRoot");
            var environment = new GameObject("GamePlayEnvironment");
            var systems = new GameObject("GameplaySystems");

            try
            {
                root.SetActive(false);
                environment.SetActive(false);
                systems.SetActive(false);
                environment.transform.SetParent(root.transform);
                systems.transform.SetParent(root.transform);

                var controller = root.AddComponent<PlaySceneActivationController>();
                controller.GameplayEnvironment = environment;
                controller.GameplaySystems = systems;

                root.SetActive(true);

                Assert.IsFalse(controller.IsGameplayActive);
                Assert.IsFalse(environment.activeInHierarchy);
                Assert.IsFalse(systems.activeInHierarchy);

                controller.ActivateGameplay();

                Assert.IsTrue(controller.IsGameplayActive);
                Assert.IsTrue(environment.activeInHierarchy);
                Assert.IsTrue(systems.activeInHierarchy);

                controller.ActivateGameplay();
                Assert.IsTrue(controller.IsGameplayActive, "Activation must be idempotent.");
            }
            finally
            {
                var controller = root != null ? root.GetComponent<PlaySceneActivationController>() : null;
                if (controller != null)
                {
                    InvokeOnDisable(controller);
                }
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OnEnable_WhenOwningSceneIsActive_ActivatesContentImmediately()
        {
            var root = new GameObject("PlayRoot");
            var environment = new GameObject("GamePlayEnvironment");
            var systems = new GameObject("GameplaySystems");

            try
            {
                root.SetActive(false);
                environment.SetActive(false);
                systems.SetActive(false);
                environment.transform.SetParent(root.transform);
                systems.transform.SetParent(root.transform);

                var controller = root.AddComponent<PlaySceneActivationController>();
                controller.GameplayEnvironment = environment;
                controller.GameplaySystems = systems;

                root.SetActive(true);
                InvokeOnEnable(controller);

                Assert.IsTrue(controller.IsGameplayActive);
                Assert.IsTrue(environment.activeInHierarchy);
                Assert.IsTrue(systems.activeInHierarchy);
            }
            finally
            {
                var controller = root != null ? root.GetComponent<PlaySceneActivationController>() : null;
                if (controller != null)
                {
                    InvokeOnDisable(controller);
                }
                Object.DestroyImmediate(root);
            }
        }

        private static void InvokeOnEnable(PlaySceneActivationController controller)
        {
            typeof(PlaySceneActivationController)
                .GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }

        private static void InvokeOnDisable(PlaySceneActivationController controller)
        {
            typeof(PlaySceneActivationController)
                .GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }
    }
}
