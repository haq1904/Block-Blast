using BlockBlast.Features.Lobby;
using NUnit.Framework;

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
    }
}
