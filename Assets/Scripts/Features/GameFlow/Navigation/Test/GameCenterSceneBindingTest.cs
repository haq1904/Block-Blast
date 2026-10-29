using System.Collections.Generic;
using BlockBlast.Features.Lobby.GameCenter;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public sealed class GameCenterSceneBindingTest
    {
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
        private Scene _loadedScene;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == LobbyScenePath)
            {
                _loadedScene = active;
            }
            else
            {
                _loadedScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_loadedScene.isLoaded && SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(_loadedScene, true);
            }
        }

        [Test]
        public void GameCenterPanel_HasAllRequiredControllersAndView()
        {
            var panel = FindInScene<LobbyGameCenterPanelView>(_loadedScene);
            Assert.IsNotNull(panel, "GameCenterPanel must have LobbyGameCenterPanelView.");

            var navCtrl = panel.GetComponent<LobbyGameCenterNavigationController>();
            Assert.IsNotNull(navCtrl, "GameCenterPanel must have LobbyGameCenterNavigationController.");
            Assert.AreEqual(panel, navCtrl.PanelView, "NavigationController PanelView reference must match.");

            var dataCtrl = panel.GetComponent<LobbyGameCenterDataController>();
            Assert.IsNotNull(dataCtrl, "GameCenterPanel must have LobbyGameCenterDataController.");
            Assert.AreEqual(panel, dataCtrl.PanelView, "DataController PanelView reference must match.");
        }

        [Test]
        public void GameCenterPanel_BackButton_IsValidAndMeetsTouchTarget()
        {
            var panel = FindInScene<LobbyGameCenterPanelView>(_loadedScene);
            Assert.IsNotNull(panel);
            Assert.IsNotNull(panel.BackButton, "BackButton on LobbyGameCenterPanelView must not be null.");

            // Must belong to GameCenterPanel path, not Account Conflict modal
            Assert.IsTrue(panel.BackButton.transform.IsChildOf(panel.transform), "BackButton must be a child of GameCenterPanel.");
            Assert.AreNotEqual("Button_Back", panel.BackButton.gameObject.name, "Must not reference the Account Conflict modal's Button_Back.");

            var rt = panel.BackButton.GetComponent<RectTransform>();
            Assert.IsNotNull(rt);
            Assert.GreaterOrEqual(rt.sizeDelta.x, 88f, "BackButton width must be >= 88 for touch accessibility.");
            Assert.GreaterOrEqual(rt.sizeDelta.y, 88f, "BackButton height must be >= 88 for touch accessibility.");

            var img = panel.BackButton.GetComponent<Image>();
            Assert.IsNotNull(img);
            Assert.IsTrue(img.raycastTarget, "BackButton Image must have raycastTarget = true.");
        }

        [Test]
        public void GameCenterPanel_RecentBars_WiredWithExactlySevenSlots()
        {
            var panel = FindInScene<LobbyGameCenterPanelView>(_loadedScene);
            Assert.IsNotNull(panel);

            var so = new SerializedObject(panel);
            var recentProp = so.FindProperty("_recentBars");
            Assert.IsNotNull(recentProp, "RecentBars property must exist.");
            Assert.AreEqual(7, recentProp.arraySize, "Must have exactly 7 recent bar bindings.");

            for (int i = 0; i < 7; i++)
            {
                var elem = recentProp.GetArrayElementAtIndex(i);
                var scoreText = elem.FindPropertyRelative("_scoreText").objectReferenceValue;
                var fillBar = elem.FindPropertyRelative("_fillBar").objectReferenceValue;
                Assert.IsNotNull(scoreText, $"Recent bar ScoreText at slot {i} must be wired.");
                Assert.IsNotNull(fillBar, $"Recent bar FillBar at slot {i} must be wired.");
            }
        }

        [Test]
        public void GameCenterPanel_AchievementTemplateAndContainer_AreValid()
        {
            var panel = FindInScene<LobbyGameCenterPanelView>(_loadedScene);
            Assert.IsNotNull(panel);

            var so = new SerializedObject(panel);
            var templateObj = so.FindProperty("_achievementRowTemplate").objectReferenceValue as Component;
            Assert.IsNotNull(templateObj, "AchievementRowTemplate must be wired.");
            Assert.IsFalse(templateObj.gameObject.activeSelf, "AchievementRowTemplate must be disabled as a template.");

            var templateSo = new SerializedObject(templateObj);
            Assert.IsNotNull(templateSo.FindProperty("_titleText").objectReferenceValue, "Template TitleText must be wired.");
            Assert.IsNotNull(templateSo.FindProperty("_progressText").objectReferenceValue, "Template ProgressText must be wired.");
            Assert.IsNotNull(templateSo.FindProperty("_fillBar").objectReferenceValue, "Template FillBar must be wired.");
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            var queue = new Queue<Transform>();
            foreach (var r in roots) queue.Enqueue(r.transform);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var comp = cur.GetComponent<T>();
                if (comp != null) return comp;
                for (int i = 0; i < cur.childCount; i++) queue.Enqueue(cur.GetChild(i));
            }
            return null;
        }
    }
}
