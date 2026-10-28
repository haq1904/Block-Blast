using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.GameCenter
{
    [Serializable]
    public sealed class AchievementRowBinding
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private RectTransform _fillBar;

        public TMP_Text TitleText => _titleText;
        public TMP_Text ProgressText => _progressText;
        public RectTransform FillBar => _fillBar;

        public AchievementRowBinding() { }

        public AchievementRowBinding(TMP_Text titleText, TMP_Text progressText, RectTransform fillBar)
        {
            _titleText = titleText;
            _progressText = progressText;
            _fillBar = fillBar;
        }

        public void Bind(AchievementRowViewData data)
        {
            if (_titleText != null) _titleText.text = data.Title;
            if (_progressText != null) _progressText.text = data.ProgressText;
            if (_fillBar != null)
            {
                var anchor = _fillBar.anchorMax;
                anchor.x = Mathf.Clamp01(data.NormalizedFill);
                _fillBar.anchorMax = anchor;
            }
        }
    }

    [Serializable]
    public sealed class RecentGameBarBinding
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private RectTransform _fillBar;

        public TMP_Text ScoreText => _scoreText;
        public RectTransform FillBar => _fillBar;

        public RecentGameBarBinding() { }

        public RecentGameBarBinding(TMP_Text scoreText, RectTransform fillBar)
        {
            _scoreText = scoreText;
            _fillBar = fillBar;
        }

        public void Bind(RecentGameBarViewData data)
        {
            if (_scoreText != null) _scoreText.text = data.Label;
            if (_fillBar != null)
            {
                var anchor = _fillBar.anchorMax;
                anchor.y = Mathf.Clamp01(data.NormalizedFill);
                _fillBar.anchorMax = anchor;
            }
        }
    }

    /// <summary>
    /// Presentation view component for the Player Center panel in Lobby.
    /// Exposes navigation events, manages panel visibility lifecycle,
    /// and renders presentation DTOs without touching networking or persistence.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LobbyGameCenterPanelView : LobbyPanelView
    {
        [Header("Controls")]
        [Tooltip("Header back button to return to Lobby home.")]
        [SerializeField] private Button _backButton;

        [Tooltip("Full-screen dimmer button behind the dialog card.")]
        [SerializeField] private Button _dimmerButton;

        [Header("Overview Stats")]
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private TMP_Text _statsRowText;
        [SerializeField] private TMP_Text _totalGamesText;
        [SerializeField] private TMP_Text _totalLinesText;
        [SerializeField] private TMP_Text _bestComboText;

        [Header("Insights & Weekly")]
        [SerializeField] private TMP_Text _personalBestChipText;
        [SerializeField] private TMP_Text _avgScoreChipText;
        [SerializeField] private TMP_Text _thisWeekScoreText;
        [SerializeField] private TMP_Text _lastWeekScoreText;

        [Header("Status")]
        [SerializeField] private TMP_Text _statusText;

        [Header("Recent Games (7 slots)")]
        [SerializeField] private RecentGameBarBinding[] _recentBars;

        [Header("Achievements (3 highlighted)")]
        [SerializeField] private AchievementRowBinding[] _achievementRows;

        public event Action BackRequested;
        public event Action Opened;

        public Button BackButton
        {
            get => _backButton;
            set
            {
                if (_backButton != null)
                {
                    _backButton.onClick.RemoveListener(HandleBackClicked);
                }
                _backButton = value;
                if (_backButton != null)
                {
                    _backButton.onClick.RemoveListener(HandleBackClicked);
                    _backButton.onClick.AddListener(HandleBackClicked);
                }
            }
        }

        public Button DimmerButton
        {
            get => _dimmerButton;
            set
            {
                if (_dimmerButton != null)
                {
                    _dimmerButton.onClick.RemoveListener(HandleBackClicked);
                }
                _dimmerButton = value;
                if (_dimmerButton != null)
                {
                    _dimmerButton.onClick.RemoveListener(HandleBackClicked);
                    _dimmerButton.onClick.AddListener(HandleBackClicked);
                }
            }
        }

        public TMP_Text BestScoreText => _bestScoreText;
        public TMP_Text StatsRowText => _statsRowText;
        public TMP_Text TotalGamesText => _totalGamesText;
        public TMP_Text TotalLinesText => _totalLinesText;
        public TMP_Text BestComboText => _bestComboText;
        public TMP_Text PersonalBestChipText => _personalBestChipText;
        public TMP_Text AvgScoreChipText => _avgScoreChipText;
        public TMP_Text ThisWeekScoreText => _thisWeekScoreText;
        public TMP_Text LastWeekScoreText => _lastWeekScoreText;
        public TMP_Text StatusText => _statusText;
        public RecentGameBarBinding[] RecentBars => _recentBars;
        public AchievementRowBinding[] AchievementRows => _achievementRows;

        protected override void Awake()
        {
            base.Awake();
            RegisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
            BackRequested = null;
            Opened = null;
        }

        public override void Show(float duration = 0.25f)
        {
            base.Show(duration);
            Opened?.Invoke();
        }

        public override void SetImmediate(bool isVisible)
        {
            base.SetImmediate(isVisible);
            if (isVisible)
            {
                Opened?.Invoke();
            }
        }

        public void RequestBack()
        {
            HandleBackClicked();
        }

        public void SetReferences(Button backButton, Button dimmerButton)
        {
            UnregisterListeners();
            _backButton = backButton;
            _dimmerButton = dimmerButton;
            RegisterListeners();
        }

        public void SetBindingReferences(
            TMP_Text bestScore,
            TMP_Text totalGames,
            TMP_Text totalLines,
            TMP_Text bestCombo,
            TMP_Text personalBestChip,
            TMP_Text avgScoreChip,
            TMP_Text thisWeekScore,
            TMP_Text lastWeekScore,
            TMP_Text statusText,
            RecentGameBarBinding[] recentBars,
            AchievementRowBinding[] achievementRows)
        {
            _bestScoreText = bestScore;
            _totalGamesText = totalGames;
            _totalLinesText = totalLines;
            _bestComboText = bestCombo;
            _personalBestChipText = personalBestChip;
            _avgScoreChipText = avgScoreChip;
            _thisWeekScoreText = thisWeekScore;
            _lastWeekScoreText = lastWeekScore;
            _statusText = statusText;
            _recentBars = recentBars;
            _achievementRows = achievementRows;
        }

        public void Render(GameCenterViewData data)
        {
            if (data == null)
            {
                return;
            }

            if (_bestScoreText != null) _bestScoreText.text = data.BestScoreText;
            if (_statsRowText != null) _statsRowText.text = $"{data.TotalGamesText} GAMES      {data.TotalLinesText} LINES      x{data.BestComboText} COMBO";
            if (_totalGamesText != null) _totalGamesText.text = data.TotalGamesText;
            if (_totalLinesText != null) _totalLinesText.text = data.TotalLinesText;
            if (_bestComboText != null) _bestComboText.text = data.BestComboText;

            if (_personalBestChipText != null) _personalBestChipText.text = $"BEST: {data.BestScoreText}";
            if (_avgScoreChipText != null) _avgScoreChipText.text = $"AVG: {data.AverageScoreText}";

            if (_thisWeekScoreText != null) _thisWeekScoreText.text = $"THIS WEEK\n{data.CurrentWeekScoreText}";
            if (_lastWeekScoreText != null) _lastWeekScoreText.text = $"LAST WEEK\n{data.PreviousWeekScoreText}";

            if (_statusText != null) _statusText.text = data.SyncStatusText;

            if (_recentBars != null && data.RecentGames != null)
            {
                int count = Mathf.Min(_recentBars.Length, data.RecentGames.Count);
                for (int i = 0; i < count; i++)
                {
                    _recentBars[i]?.Bind(data.RecentGames[i]);
                }
            }

            if (_achievementRows != null && data.Achievements != null)
            {
                int count = Mathf.Min(_achievementRows.Length, data.Achievements.Count);
                for (int i = 0; i < count; i++)
                {
                    _achievementRows[i]?.Bind(data.Achievements[i]);
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = interactable;
                cg.blocksRaycasts = interactable;
            }
            if (_backButton != null) _backButton.interactable = interactable;
            if (_dimmerButton != null) _dimmerButton.interactable = interactable;
        }

        private void RegisterListeners()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
                _backButton.onClick.AddListener(HandleBackClicked);
            }

            if (_dimmerButton != null)
            {
                _dimmerButton.onClick.RemoveListener(HandleBackClicked);
                _dimmerButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void UnregisterListeners()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }

            if (_dimmerButton != null)
            {
                _dimmerButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
