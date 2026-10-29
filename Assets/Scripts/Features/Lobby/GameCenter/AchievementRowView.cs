using TMPro;
using UnityEngine;

namespace BlockBlast.Features.Lobby.GameCenter
{
    /// <summary>
    /// Passive reusable row view for an achievement item.
    /// Binds presentation DTO without accessing domain or network services.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private RectTransform _fillBar;
        [SerializeField] private GameObject _completedBadge;

        public TMP_Text TitleText => _titleText;
        public TMP_Text ProgressText => _progressText;
        public RectTransform FillBar => _fillBar;
        public GameObject CompletedBadge => _completedBadge;

        public void SetReferences(TMP_Text titleText, TMP_Text progressText, RectTransform fillBar, GameObject completedBadge = null)
        {
            _titleText = titleText;
            _progressText = progressText;
            _fillBar = fillBar;
            _completedBadge = completedBadge;
        }

        public void Bind(AchievementRowViewData data)
        {
            if (_titleText != null)
            {
                _titleText.text = data.Title;
            }

            if (_progressText != null)
            {
                _progressText.text = data.ProgressText;
            }

            if (_fillBar != null)
            {
                var anchor = _fillBar.anchorMax;
                anchor.x = Mathf.Clamp01(data.NormalizedFill);
                _fillBar.anchorMax = anchor;
            }

            if (_completedBadge != null)
            {
                _completedBadge.SetActive(data.IsUnlocked);
            }
        }
    }
}
