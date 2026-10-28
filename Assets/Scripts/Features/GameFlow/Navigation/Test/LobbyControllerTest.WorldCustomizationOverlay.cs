using BlockBlast.Features.Lobby;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    public partial class LobbyControllerTest
    {
        private LobbyWorldCustomizationOverlayView CreateWorldCustomizationOverlayView()
        {
            var go = new GameObject("TestWorldCustomizationOverlay");
            _createdObjects.Add(go);
            go.AddComponent<CanvasGroup>();
            var view = go.AddComponent<LobbyWorldCustomizationOverlayView>();
            return view;
        }

        [Test]
        public void WorldCustomizationOverlay_WhenCloseRequested_InvokesCloseOverlayAndRestoresWorldButtons()
        {
            var customView = CreateWorldCustomizationOverlayView();
            _controller.WorldCustomizationOverlay = customView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            _controller.OpenOverlay(LobbyOverlayId.Customization);
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsFalse(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be disabled when Customization overlay is open.");
            }

            customView.RequestClose();

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be restored after Customization overlay is closed.");
            }
        }

        [Test]
        public void WorldCustomizationOverlay_WhenDimmerClicked_InvokesCloseOverlayAndRestoresWorldButtons()
        {
            var customView = CreateWorldCustomizationOverlayView();
            var dimmerBtnObj = new GameObject("DimmerBtn");
            _createdObjects.Add(dimmerBtnObj);
            var dimmerBtn = dimmerBtnObj.AddComponent<Button>();
            customView.DimmerButton = dimmerBtn;

            _controller.WorldCustomizationOverlay = customView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            _controller.OpenOverlay(LobbyOverlayId.Customization);
            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsFalse(_controller.WorldButtons[i].HitCollider.enabled);
            }

            dimmerBtn.onClick.Invoke();

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled, $"Button {i} should be restored after clicking Customization dimmer.");
            }
        }

        [Test]
        public void WorldCustomizationOverlay_EventLifecycle_DisableThenEnable_PreservesSingleBinding()
        {
            var customView = CreateWorldCustomizationOverlayView();
            _controller.WorldCustomizationOverlay = customView;

            var onDisable = typeof(LobbyController).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var onEnable = typeof(LobbyController).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);
            onDisable?.Invoke(_controller, null);
            onEnable?.Invoke(_controller, null);

            _controller.OpenOverlay(LobbyOverlayId.Customization);
            customView.RequestClose();

            for (int i = 0; i < _controller.WorldButtons.Length; i++)
            {
                Assert.IsTrue(_controller.WorldButtons[i].HitCollider.enabled);
            }
        }

        [Test]
        public void WorldCustomizationOverlay_CloseButtons_AllEmitCloseRequested()
        {
            var customView = CreateWorldCustomizationOverlayView();

            var closeBtnObj = new GameObject("CloseBtn");
            _createdObjects.Add(closeBtnObj);
            var closeBtn = closeBtnObj.AddComponent<Button>();

            var ctaBtnObj = new GameObject("CtaBtn");
            _createdObjects.Add(ctaBtnObj);
            var ctaBtn = ctaBtnObj.AddComponent<Button>();

            var dimmerBtnObj = new GameObject("DimmerBtn");
            _createdObjects.Add(dimmerBtnObj);
            var dimmerBtn = dimmerBtnObj.AddComponent<Button>();

            customView.SetReferences(closeBtn, ctaBtn, dimmerBtn);

            int closeCount = 0;
            customView.CloseRequested += () => closeCount++;

            closeBtn.onClick.Invoke();
            Assert.AreEqual(1, closeCount, "Close button must emit CloseRequested.");

            dimmerBtn.onClick.Invoke();
            Assert.AreEqual(2, closeCount, "Dimmer button must emit CloseRequested.");

            ctaBtn.onClick.Invoke();
            Assert.AreEqual(3, closeCount, "ViewWorldInLobby CTA button must emit CloseRequested.");
        }

        [Test]
        public void WorldCustomizationOverlay_DimmerLifecycle_DoesNotEmitDuplicateCloseRequested()
        {
            var customView = CreateWorldCustomizationOverlayView();

            var closeBtnObj = new GameObject("CloseBtn");
            _createdObjects.Add(closeBtnObj);
            var closeBtn = closeBtnObj.AddComponent<Button>();

            var ctaBtnObj = new GameObject("CtaBtn");
            _createdObjects.Add(ctaBtnObj);
            var ctaBtn = ctaBtnObj.AddComponent<Button>();

            var dimmerBtnObj = new GameObject("DimmerBtn");
            _createdObjects.Add(dimmerBtnObj);
            var dimmerBtn = dimmerBtnObj.AddComponent<Button>();

            customView.SetReferences(closeBtn, ctaBtn, dimmerBtn);
            customView.SetReferences(closeBtn, ctaBtn, dimmerBtn);

            int closeCount = 0;
            customView.CloseRequested += () => closeCount++;

            dimmerBtn.onClick.Invoke();
            Assert.AreEqual(1, closeCount, "Repeated SetReferences must not produce duplicate close events on dimmer click.");
        }
    }
}
