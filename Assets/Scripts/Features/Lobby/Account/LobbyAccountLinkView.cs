using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.Account
{
    public class LobbyAccountLinkView : MonoBehaviour
    {
        [Header("Row Controls")]
        [SerializeField] private GameObject _accountRowRoot;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _linkButton;
        [SerializeField] private TMP_Text _linkButtonText;

        [Header("Conflict Modal")]
        [SerializeField] private GameObject _conflictModalRoot;
        [SerializeField] private Button _modalDimmerButton;
        [SerializeField] private TMP_Text _modalTitleText;
        [SerializeField] private TMP_Text _modalWarningText;
        [SerializeField] private Button _keepProgressButton;
        [SerializeField] private Button _loadExistingButton;
        [SerializeField] private Button _cancelButton;

        [Header("Keep Confirmation Sub-card")]
        [SerializeField] private GameObject _keepConfirmationRoot;
        [SerializeField] private Button _confirmKeepButton;
        [SerializeField] private Button _backFromKeepButton;

        public event Action LinkRequested;
        public event Action KeepProgressConfirmed;
        public event Action SwitchProgressRequested;
        public event Action ConflictCancelled;

        private void Awake()
        {
            if (_linkButton != null)
            {
                _linkButton.onClick.AddListener(() => LinkRequested?.Invoke());
            }

            if (_keepProgressButton != null)
            {
                _keepProgressButton.onClick.AddListener(() => ShowKeepConfirmation(true));
            }

            if (_confirmKeepButton != null)
            {
                _confirmKeepButton.onClick.AddListener(() => KeepProgressConfirmed?.Invoke());
            }

            if (_backFromKeepButton != null)
            {
                _backFromKeepButton.onClick.AddListener(() => ShowKeepConfirmation(false));
            }

            if (_loadExistingButton != null)
            {
                _loadExistingButton.onClick.AddListener(() => SwitchProgressRequested?.Invoke());
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(() => ConflictCancelled?.Invoke());
            }

            if (_modalDimmerButton != null)
            {
                _modalDimmerButton.onClick.AddListener(() => ConflictCancelled?.Invoke());
            }
        }

        public void Render(BlockBlast.Features.PlayerIdentity.PlayerIdentitySnapshot snapshot, bool isSupported)
        {
            if (_accountRowRoot != null)
            {
                _accountRowRoot.SetActive(true);
            }

            if (!isSupported)
            {
                if (_statusText != null)
                {
                    _statusText.text = "Android only";
                }

                if (_linkButton != null)
                {
                    _linkButton.interactable = false;
                }

                if (_linkButtonText != null)
                {
                    _linkButtonText.text = "Link";
                }

                ShowConflictModal(false);
                return;
            }

            if (snapshot == null)
            {
                return;
            }

            switch (snapshot.Status)
            {
                case BlockBlast.Features.PlayerIdentity.PlayerIdentityStatus.Linked:
                    if (_statusText != null)
                    {
                        _statusText.text = "Google Play Games linked";
                    }

                    if (_linkButton != null)
                    {
                        _linkButton.interactable = false;
                    }

                    if (_linkButtonText != null)
                    {
                        _linkButtonText.text = "Linked";
                    }

                    ShowConflictModal(false);
                    break;

                case BlockBlast.Features.PlayerIdentity.PlayerIdentityStatus.Conflict:
                    ShowConflictModal(true);
                    break;

                case BlockBlast.Features.PlayerIdentity.PlayerIdentityStatus.Linking:
                    if (_statusText != null)
                    {
                        _statusText.text = "Linking...";
                    }

                    if (_linkButton != null)
                    {
                        _linkButton.interactable = false;
                    }

                    break;

                case BlockBlast.Features.PlayerIdentity.PlayerIdentityStatus.Switching:
                    if (_statusText != null)
                    {
                        _statusText.text = "Switching account...";
                    }

                    SetLoading(true);
                    break;

                case BlockBlast.Features.PlayerIdentity.PlayerIdentityStatus.Error:
                    if (_statusText != null)
                    {
                        _statusText.text = string.IsNullOrEmpty(snapshot.ErrorCode) ? "Error" : snapshot.ErrorCode;
                    }

                    if (_linkButton != null)
                    {
                        _linkButton.interactable = true;
                    }

                    if (_linkButtonText != null)
                    {
                        _linkButtonText.text = "Retry";
                    }

                    break;

                default:
                    if (_statusText != null)
                    {
                        _statusText.text = "Not linked";
                    }

                    if (_linkButton != null)
                    {
                        _linkButton.interactable = true;
                    }

                    if (_linkButtonText != null)
                    {
                        _linkButtonText.text = "Link";
                    }

                    break;
            }
        }

        public void SetLoading(bool isLoading)
        {
            if (_linkButton != null)
            {
                _linkButton.interactable = !isLoading;
            }

            if (_keepProgressButton != null)
            {
                _keepProgressButton.interactable = !isLoading;
            }

            if (_confirmKeepButton != null)
            {
                _confirmKeepButton.interactable = !isLoading;
            }

            if (_loadExistingButton != null)
            {
                _loadExistingButton.interactable = !isLoading;
            }

            if (_cancelButton != null)
            {
                _cancelButton.interactable = !isLoading;
            }

            if (_modalDimmerButton != null)
            {
                _modalDimmerButton.interactable = !isLoading;
            }
        }

        public void ShowConflictModal(bool show)
        {
            if (_conflictModalRoot != null)
            {
                _conflictModalRoot.SetActive(show);
            }

            if (!show)
            {
                ShowKeepConfirmation(false);
            }
        }

        public void ShowKeepConfirmation(bool show)
        {
            if (_keepConfirmationRoot != null)
            {
                _keepConfirmationRoot.SetActive(show);
            }
        }
    }
}
