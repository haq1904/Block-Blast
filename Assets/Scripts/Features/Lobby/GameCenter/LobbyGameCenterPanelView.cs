using System;
using System.Collections.Generic;
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

        [Header("Overview Stats")]
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private TMP_Text _statsRowText;
        [SerializeField] private TMP_Text _totalGamesText;
        [SerializeField] private TMP_Text _totalLinesText;
        [SerializeField] private TMP_Text _totalBlocksText;
        [SerializeField] private TMP_Text _bestComboText;

        [Header("Insights & Weekly")]
        [SerializeField] private TMP_Text _personalBestChipText;
        [SerializeField] private TMP_Text _avgScoreChipText;
        [SerializeField] private TMP_Text _thisWeekScoreText;
        [SerializeField] private TMP_Text _lastWeekScoreText;
        [SerializeField] private TMP_Text _performanceTrendText;

        [Header("Core Stats")]
        [SerializeField] private TMP_Text _linesPerGameText;
        [SerializeField] private TMP_Text _linesPerBlockText;

        [Header("Recent Games (7 slots)")]
        [SerializeField] private TMP_Text _recentInsightText;
        [SerializeField] private RecentGameBarBinding[] _recentBars;

        [Header("Highlights (up to 2 cards)")]
        [SerializeField] private GameObject _highlight1Card;
        [SerializeField] private TMP_Text _highlight1Title;
        [SerializeField] private TMP_Text _highlight1Desc;
        [SerializeField] private GameObject _highlight2Card;
        [SerializeField] private TMP_Text _highlight2Title;
        [SerializeField] private TMP_Text _highlight2Desc;

        [Header("Achievements Data-Driven")]
        [SerializeField] private Transform _achievementRowsContainer;
        [SerializeField] private AchievementRowView _achievementRowTemplate;
        [SerializeField] private AchievementRowBinding[] _achievementRows;

        [Header("Status")]
        [SerializeField] private TMP_Text _statusText;

        private bool _isOpened;
        private readonly List<AchievementRowView> _spawnedAchievementRows = new List<AchievementRowView>();

        public event Action BackRequested;
        public event Action Opened;
        public event Action Closed;

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

        public TMP_Text BestScoreText => _bestScoreText;
        public TMP_Text StatsRowText => _statsRowText;
        public TMP_Text TotalGamesText => _totalGamesText;
        public TMP_Text TotalLinesText => _totalLinesText;
        public TMP_Text TotalBlocksText => _totalBlocksText;
        public TMP_Text BestComboText => _bestComboText;
        public TMP_Text PersonalBestChipText => _personalBestChipText;
        public TMP_Text AvgScoreChipText => _avgScoreChipText;
        public TMP_Text ThisWeekScoreText => _thisWeekScoreText;
        public TMP_Text LastWeekScoreText => _lastWeekScoreText;
        public TMP_Text PerformanceTrendText => _performanceTrendText;
        public TMP_Text LinesPerGameText => _linesPerGameText;
        public TMP_Text LinesPerBlockText => _linesPerBlockText;
        public TMP_Text RecentInsightText => _recentInsightText;
        public TMP_Text StatusText => _statusText;
        public RecentGameBarBinding[] RecentBars => _recentBars;
        public AchievementRowBinding[] AchievementRows => _achievementRows;
        public AchievementRowView AchievementRowTemplate => _achievementRowTemplate;
        public Transform AchievementRowsContainer => _achievementRowsContainer;

        protected override void Awake()
        {
            base.Awake();
            RegisterListeners();
        }

        private void OnDestroy()
        {
            if (_isOpened)
            {
                _isOpened = false;
                Closed?.Invoke();
            }

            UnregisterListeners();
            BackRequested = null;
            Opened = null;
            Closed = null;
        }

        public override void Show(float duration = 0.25f)
        {
            base.Show(duration);
            if (!_isOpened)
            {
                _isOpened = true;
                Opened?.Invoke();
            }
        }

        public override void Hide(float duration = 0.25f)
        {
            if (_isOpened)
            {
                _isOpened = false;
                Closed?.Invoke();
            }
            base.Hide(duration);
        }

        public override void SetImmediate(bool isVisible)
        {
            base.SetImmediate(isVisible);
            if (isVisible && !_isOpened)
            {
                _isOpened = true;
                Opened?.Invoke();
            }
            else if (!isVisible && _isOpened)
            {
                _isOpened = false;
                Closed?.Invoke();
            }
        }

        public void RequestBack()
        {
            HandleBackClicked();
        }

        public void SetReferences(Button backButton)
        {
            UnregisterListeners();
            _backButton = backButton;
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

        public void SetExtendedReferences(
            TMP_Text performanceTrend,
            TMP_Text totalBlocks,
            TMP_Text linesPerGame,
            TMP_Text linesPerBlock,
            TMP_Text recentInsight,
            GameObject highlight1Card,
            TMP_Text highlight1Title,
            TMP_Text highlight1Desc,
            GameObject highlight2Card,
            TMP_Text highlight2Title,
            TMP_Text highlight2Desc,
            Transform achievementContainer,
            AchievementRowView achievementTemplate)
        {
            _performanceTrendText = performanceTrend;
            _totalBlocksText = totalBlocks;
            _linesPerGameText = linesPerGame;
            _linesPerBlockText = linesPerBlock;
            _recentInsightText = recentInsight;
            _highlight1Card = highlight1Card;
            _highlight1Title = highlight1Title;
            _highlight1Desc = highlight1Desc;
            _highlight2Card = highlight2Card;
            _highlight2Title = highlight2Title;
            _highlight2Desc = highlight2Desc;
            _achievementRowsContainer = achievementContainer;
            _achievementRowTemplate = achievementTemplate;
        }

        public void Render(GameCenterViewData data)
        {
            if (data == null)
            {
                return;
            }

            // Overview stats
            if (_bestScoreText != null) _bestScoreText.text = data.BestScoreText;
            if (_statsRowText != null) _statsRowText.text = $"{data.TotalGamesText} GAMES      {data.TotalLinesText} LINES      x{data.BestComboText} COMBO";
            if (_totalGamesText != null) _totalGamesText.text = data.TotalGamesText;
            if (_totalLinesText != null) _totalLinesText.text = data.TotalLinesText;
            if (_totalBlocksText != null) _totalBlocksText.text = data.TotalBlocksText;
            if (_bestComboText != null) _bestComboText.text = data.BestComboText;

            // Chips & weekly
            if (_personalBestChipText != null) _personalBestChipText.text = $"BEST: {data.BestScoreText}";
            if (_avgScoreChipText != null) _avgScoreChipText.text = $"AVG: {data.AverageScoreText}";
            if (_thisWeekScoreText != null) _thisWeekScoreText.text = $"THIS WEEK\n{data.CurrentWeekScoreText}";
            if (_lastWeekScoreText != null) _lastWeekScoreText.text = $"LAST WEEK\n{data.PreviousWeekScoreText}";
            // The current panel has no dedicated, non-overlapping slot for this optional label.
            // Keep the value in the DTO for a future layout revision, but do not render it over the weekly card.
            if (_performanceTrendText != null)
            {
                _performanceTrendText.text = string.Empty;
                _performanceTrendText.gameObject.SetActive(false);
            }

            // Core stats
            if (_linesPerGameText != null) _linesPerGameText.text = data.LinesPerGameText;
            if (_linesPerBlockText != null) _linesPerBlockText.text = data.LinesPerBlockText;

            // Recent insight
            if (_recentInsightText != null) _recentInsightText.text = data.RecentInsightText;

            // Status
            // The status placeholder currently overlaps the title artwork. Synchronization state remains
            // available to the data layer until the panel receives a dedicated status slot.
            if (_statusText != null)
            {
                _statusText.text = string.Empty;
                _statusText.gameObject.SetActive(false);
            }

            // Recent 7 games bars
            if (_recentBars != null && data.RecentGames != null)
            {
                int count = Mathf.Min(_recentBars.Length, data.RecentGames.Count);
                for (int i = 0; i < count; i++)
                {
                    _recentBars[i]?.Bind(data.RecentGames[i]);
                }
            }

            // Highlights
            RenderHighlights(data.Highlights);

            // Achievements data-driven list
            RenderAchievements(data.Achievements);
        }

        private void RenderHighlights(IReadOnlyList<InsightCardViewData> highlights)
        {
            if (highlights != null && highlights.Count > 0)
            {
                if (_highlight1Card != null)
                {
                    _highlight1Card.SetActive(true);
                    if (_highlight1Title != null) _highlight1Title.text = highlights[0].Title;
                    if (_highlight1Desc != null) _highlight1Desc.text = highlights[0].Description;
                }

                if (_highlight2Card != null)
                {
                    if (highlights.Count > 1)
                    {
                        _highlight2Card.SetActive(true);
                        if (_highlight2Title != null) _highlight2Title.text = highlights[1].Title;
                        if (_highlight2Desc != null) _highlight2Desc.text = highlights[1].Description;
                    }
                    else
                    {
                        _highlight2Card.SetActive(false);
                    }
                }
            }
            else
            {
                if (_highlight1Card != null) _highlight1Card.SetActive(false);
                if (_highlight2Card != null) _highlight2Card.SetActive(false);
            }
        }

        private void RenderAchievements(IReadOnlyList<AchievementRowViewData> achievements)
        {
            if (achievements == null) return;

            // If we have an instantiated row template, use data-driven 18 rows
            if (_achievementRowTemplate != null)
            {
                var container = _achievementRowsContainer != null ? _achievementRowsContainer : _achievementRowTemplate.transform.parent;

                while (_spawnedAchievementRows.Count < achievements.Count)
                {
                    var newRow = Instantiate(_achievementRowTemplate, container);
                    newRow.gameObject.SetActive(true);
                    _spawnedAchievementRows.Add(newRow);
                }

                for (int i = 0; i < _spawnedAchievementRows.Count; i++)
                {
                    if (i < achievements.Count)
                    {
                        _spawnedAchievementRows[i].gameObject.SetActive(true);
                        _spawnedAchievementRows[i].Bind(achievements[i]);
                    }
                    else
                    {
                        _spawnedAchievementRows[i].gameObject.SetActive(false);
                    }
                }

                if (container is RectTransform containerRect)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
                    if (containerRect.parent is RectTransform contentRect)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                    }
                }
            }
            else if (_achievementRows != null)
            {
                // Fallback to static array bindings
                int count = Mathf.Min(_achievementRows.Length, achievements.Count);
                for (int i = 0; i < count; i++)
                {
                    _achievementRows[i]?.Bind(achievements[i]);
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
        }

        private void RegisterListeners()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void UnregisterListeners()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
