using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Presentation component for the Lobby Home screen, housing Title, High Score, and Play CTA.
    /// </summary>
    public sealed class LobbyHomeView : MonoBehaviour
    {
        [Header("Primary CTA")]
        [Tooltip("Large Play Game button (Primary CTA).")]
        [SerializeField] private Button _playButton;

        [Header("High Score Display")]
        [SerializeField] private TMP_Text _highScoreText;

        public event Action PlayRequested;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        public void SetHighScore(int score)
        {
            if (_highScoreText != null)
            {
                _highScoreText.text = score > 0 ? score.ToString("#,##0") : "0";
            }
        }

        public void SetPlayInteractable(bool isInteractable)
        {
            if (_playButton != null)
            {
                _playButton.interactable = isInteractable;
            }
        }

        private void OnPlayClicked()
        {
            if (_playButton != null)
            {
                _playButton.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 2, 0.5f)
                    .SetTarget(_playButton.transform)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            PlayRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }
            PlayRequested = null;
        }
    }
}
