using BlockBlast.Features.Lobby;
using BlockBlast.Features.Lobby.GameCenter;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
        [Test]
        public void GameCenter_SetGameCenterInteractionActive_TogglesWorldButtons()
        {
            _controller.SetGameCenterInteractionActive(true);
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsFalse(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be disabled when GameCenter interaction is active.");
            }

            _controller.SetGameCenterInteractionActive(false);
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be enabled when GameCenter interaction is inactive.");
            }
        }

        [Test]
        public void GameCenter_ReturnHomeFromGameCenter_SwitchesToHomeAndEnablesWorldButtons()
        {
            _controller.SetGameCenterInteractionActive(true);

            _controller.ReturnHomeFromGameCenter();

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be restored after returning home from GameCenter.");
            }
        }

        [Test]
        public void GameCenterPanel_BackButton_EmitsBackRequested()
        {
            var panelGo = new GameObject("TestGameCenterPanel");
            _createdObjects.Add(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var panelView = panelGo.AddComponent<LobbyGameCenterPanelView>();

            var backBtnObj = new GameObject("BackBtn");
            _createdObjects.Add(backBtnObj);
            var backBtn = backBtnObj.AddComponent<Button>();

            panelView.SetReferences(backBtn);

            int backCount = 0;
            panelView.BackRequested += () => backCount++;

            backBtn.onClick.Invoke();
            Assert.AreEqual(1, backCount, "Back button must emit BackRequested.");
        }

        [Test]
        public void GameCenter_NavigationController_LocksOnOpened_AndUnlocksOnClosedOrBack()
        {
            var panelGo = new GameObject("TestGameCenterPanel");
            _createdObjects.Add(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var panelView = panelGo.AddComponent<LobbyGameCenterPanelView>();

            var backBtnObj = new GameObject("BackBtn");
            _createdObjects.Add(backBtnObj);
            var backBtn = backBtnObj.AddComponent<Button>();
            panelView.SetReferences(backBtn);

            var navCtrl = panelGo.AddComponent<LobbyGameCenterNavigationController>();
            navCtrl.Construct(panelView, _controller);

            // Initially before opening, buttons are enabled
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled);
            }

            // Show panel -> triggers Opened -> locks interaction
            panelView.Show(0f);
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsFalse(_controller.WorldButtons[i].HitCollider.enabled);
            }

            // Click back button -> returns home and unlocks interaction
            backBtn.onClick.Invoke();
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be restored after clicking GameCenter back.");
            }
        }

        [Test]
        public void GameCenter_Lifecycle_DoesNotEmitDuplicateEvents()
        {
            var panelGo = new GameObject("TestGameCenterPanel");
            _createdObjects.Add(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var panelView = panelGo.AddComponent<LobbyGameCenterPanelView>();

            var backBtnObj = new GameObject("BackBtn");
            _createdObjects.Add(backBtnObj);
            var backBtn = backBtnObj.AddComponent<Button>();

            panelView.SetReferences(backBtn);
            panelView.SetReferences(backBtn);

            int backCount = 0;
            panelView.BackRequested += () => backCount++;

            backBtn.onClick.Invoke();
            Assert.AreEqual(1, backCount, "Repeated SetReferences must not produce duplicate back events on back click.");

            int openCount = 0;
            panelView.Opened += () => openCount++;
            panelView.Show(0f);
            panelView.Show(0f); // repeated Show while already opened
            Assert.AreEqual(1, openCount, "Repeated Show while open must not emit duplicate Opened events.");
        }
    }
}
