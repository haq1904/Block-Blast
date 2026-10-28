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
        public void GameCenterPanel_Controls_BothEmitBackRequested()
        {
            var panelGo = new GameObject("TestGameCenterPanel");
            _createdObjects.Add(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var panelView = panelGo.AddComponent<LobbyGameCenterPanelView>();

            var backBtnObj = new GameObject("BackBtn");
            _createdObjects.Add(backBtnObj);
            var backBtn = backBtnObj.AddComponent<Button>();

            var dimmerBtnObj = new GameObject("DimmerBtn");
            _createdObjects.Add(dimmerBtnObj);
            var dimmerBtn = dimmerBtnObj.AddComponent<Button>();

            panelView.SetReferences(backBtn, dimmerBtn);

            int backCount = 0;
            panelView.BackRequested += () => backCount++;

            backBtn.onClick.Invoke();
            Assert.AreEqual(1, backCount, "Back button must emit BackRequested.");

            dimmerBtn.onClick.Invoke();
            Assert.AreEqual(2, backCount, "Dimmer button must emit BackRequested.");
        }

        [Test]
        public void GameCenter_WhenDimmerClicked_ReturnsHomeAndRestoresWorldButtons()
        {
            var panelGo = new GameObject("TestGameCenterPanel");
            _createdObjects.Add(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var panelView = panelGo.AddComponent<LobbyGameCenterPanelView>();

            var dimmerBtnObj = new GameObject("DimmerBtn");
            _createdObjects.Add(dimmerBtnObj);
            var dimmerBtn = dimmerBtnObj.AddComponent<Button>();

            panelView.DimmerButton = dimmerBtn;

            var navCtrl = panelGo.AddComponent<LobbyGameCenterNavigationController>();
            navCtrl.Construct(panelView, _controller);

            var onEnable = typeof(LobbyGameCenterNavigationController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onEnable?.Invoke(navCtrl, null);

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsFalse(_controller.WorldButtons[i].HitCollider.enabled);
            }

            dimmerBtn.onClick.Invoke();

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be restored after clicking GameCenter dimmer.");
            }
        }

        [Test]
        public void GameCenter_DimmerLifecycle_DoesNotEmitDuplicateBackRequested()
        {
            var panelGo = new GameObject("TestGameCenterPanel");
            _createdObjects.Add(panelGo);
            panelGo.AddComponent<CanvasGroup>();
            var panelView = panelGo.AddComponent<LobbyGameCenterPanelView>();

            var backBtnObj = new GameObject("BackBtn");
            _createdObjects.Add(backBtnObj);
            var backBtn = backBtnObj.AddComponent<Button>();

            var dimmerBtnObj = new GameObject("DimmerBtn");
            _createdObjects.Add(dimmerBtnObj);
            var dimmerBtn = dimmerBtnObj.AddComponent<Button>();

            panelView.SetReferences(backBtn, dimmerBtn);
            panelView.SetReferences(backBtn, dimmerBtn);

            int backCount = 0;
            panelView.BackRequested += () => backCount++;

            dimmerBtn.onClick.Invoke();
            Assert.AreEqual(1, backCount, "Repeated SetReferences must not produce duplicate back events on dimmer click.");
        }
    }
}
