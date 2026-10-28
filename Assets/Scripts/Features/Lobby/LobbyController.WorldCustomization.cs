using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.WorldCustomization;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    public sealed partial class LobbyController
    {
        [Header("World Customization")]
        [Tooltip("Optional scene host reference for dynamic button rebinding.")]
        [SerializeField] private WorldEnvironmentHost _environmentHost;

        private bool _isWorldChanging;

        public WorldEnvironmentHost EnvironmentHost
        {
            get => _environmentHost;
            set => SetEnvironmentHost(value);
        }

        public void SetEnvironmentHost(WorldEnvironmentHost host)
        {
            if (_environmentHost == host) return;

            if (_isBound && _environmentHost != null)
            {
                _environmentHost.LobbyWorldButtonsChanged -= HandleHostWorldButtonsChanged;
            }

            _environmentHost = host;

            if (_isBound && _environmentHost != null)
            {
                _environmentHost.LobbyWorldButtonsChanged += HandleHostWorldButtonsChanged;
                TryApplyHostWorldButtons();
            }
        }

        private void BindWorldCustomization()
        {
            BindWorldConfirmation();
            SubscribeEnvironmentHost();
            TryApplyHostWorldButtons();
        }

        private void UnbindWorldCustomization()
        {
            var customization = WorldCustomizationOverlay;
            if (customization != null)
            {
                customization.ConfirmRequested -= HandleWorldConfirmRequested;
            }

            if (_environmentHost != null)
            {
                _environmentHost.LobbyWorldButtonsChanged -= HandleHostWorldButtonsChanged;
            }
        }

        private void BindWorldConfirmation()
        {
            var customization = WorldCustomizationOverlay;
            if (customization != null)
            {
                customization.ConfirmRequested -= HandleWorldConfirmRequested;
                customization.ConfirmRequested += HandleWorldConfirmRequested;
            }
        }

        private void SubscribeEnvironmentHost()
        {
            if (_environmentHost != null)
            {
                _environmentHost.LobbyWorldButtonsChanged -= HandleHostWorldButtonsChanged;
                _environmentHost.LobbyWorldButtonsChanged += HandleHostWorldButtonsChanged;
            }
        }

        private void TryApplyHostWorldButtons()
        {
            if (_environmentHost == null) return;
            var currentButtons = _environmentHost.LobbyWorldButtons;
            if (currentButtons != null && currentButtons.Count > 0)
            {
                TryReplaceWorldButtons(currentButtons, logFailure: false);
            }
        }

        public void HandleHostWorldButtonsChanged(IReadOnlyList<LobbyWorldButtonView> buttons)
        {
            if (buttons == null) return;
            TryReplaceWorldButtons(buttons, logFailure: true);
        }

        private bool TryReplaceWorldButtons(
            IReadOnlyList<LobbyWorldButtonView> candidates,
            bool logFailure)
        {
            if (!ValidateWorldButtons(candidates, requireEnabled: false, logFailure: logFailure))
            {
                return false;
            }

            UnbindWorldButtonClicks(_worldButtons);
            _worldButtons = CopyButtons(candidates);
            BindWorldButtonClicks(_worldButtons);
            RebindExitFeedbackViews(_worldButtons);
            _hasBoundValidButtons = true;
            if (_destroyCts != null)
            {
                _ = GateWorldButtonsUntilNavigationIdleAsync(_destroyCts.Token);
            }
            return true;
        }

        private void RebindExitFeedbackViews(IReadOnlyList<LobbyWorldButtonView> buttons)
        {
            if (_worldButtonsExitView == null) return;
            if (buttons == null)
            {
                _worldButtonsExitView.SetFeedbackViews(Array.Empty<LobbyWorldButtonFeedbackView>());
                return;
            }

            var feedbackViews = new List<LobbyWorldButtonFeedbackView>(buttons.Count);
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                if (btn != null)
                {
                    var fb = btn.GetComponentInChildren<LobbyWorldButtonFeedbackView>(true);
                    if (fb != null)
                    {
                        feedbackViews.Add(fb);
                    }
                }
            }
            _worldButtonsExitView.SetFeedbackViews(feedbackViews);
        }

        private LobbyWorldButtonView[] CopyButtons(IReadOnlyList<LobbyWorldButtonView> candidates)
        {
            if (candidates == null) return Array.Empty<LobbyWorldButtonView>();
            var array = new LobbyWorldButtonView[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                array[i] = candidates[i];
            }
            return array;
        }

        private async void HandleWorldConfirmRequested(string targetWorldId)
        {
            if (_isWorldChanging) return;
            _isWorldChanging = true;

            var customization = WorldCustomizationOverlay;
            customization?.SetInteractable(false);
            SetWorldButtonsInteractable(false);

            try
            {
                if (ServiceLocator.TryGet<IWorldService>(out var worldService) && worldService != null)
                {
                    var token = _destroyCts != null ? _destroyCts.Token : CancellationToken.None;
                    var result = await worldService.ChangeWorldAsync(targetWorldId, token);

                    if (result.IsSuccess)
                    {
                        CloseOverlay();
                        OnTabSelected(LobbyTabId.Home);
                    }
                    else
                    {
                        Debug.LogWarning($"[LobbyController] World change failed: {result.ErrorMessage}");
                    }
                }
                else
                {
                    CloseOverlay();
                    OnTabSelected(LobbyTabId.Home);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyController] Exception changing world: {ex.Message}");
            }
            finally
            {
                _isWorldChanging = false;
                customization?.SetInteractable(true);
                SetWorldButtonsInteractable(true);
            }
        }
    }
}
