using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreView : MonoBehaviour
{
    [Header("Text Displays (Supports both 3D World TextMeshPro & Canvas TextMeshProUGUI)")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject comboContainer;

    [Header("Score Count & Juice Settings")]
    [Tooltip("Peak scale bonus applied as the rolling score reaches target (e.g. 0.2 = 1.2x scale).")]
    [SerializeField] private float scoreScaleBonus = 0.2f;

    [Tooltip("Micro-vibration shake amplitude while score rolls up.")]
    [SerializeField] private float scoreShakeStrength = 3.5f;

    [Tooltip("Minimum rolling score duration in seconds.")]
    [SerializeField] private float minCountDuration = 0.25f;

    [Tooltip("Maximum rolling score duration in seconds.")]
    [SerializeField] private float maxCountDuration = 0.65f;

    [Tooltip("Duration of elastic spring settling back to original scale.")]
    [SerializeField] private float settleDuration = 0.18f;

    [Header("Punch Scale Animation Settings")]
    [SerializeField] private bool enablePunchScale = true;
    [SerializeField] private float punchScaleAmount = 1.25f;
    [SerializeField] private float punchDuration = 0.2f;

    private IScoreService scoreService;

    private Vector3 originalScoreScale = Vector3.one;
    private Vector3 originalScoreLocalPos = Vector3.zero;
    private Vector3 originalComboScale = Vector3.one;
    private Vector3 originalHighScoreScale = Vector3.one;

    private int displayedScore = 0;

    private void Awake()
    {
        if (currentScoreText != null)
        {
            originalScoreScale = currentScoreText.transform.localScale;
            originalScoreLocalPos = currentScoreText.transform.localPosition;
        }
        if (comboText != null) originalComboScale = comboText.transform.localScale;
        if (highScoreText != null) originalHighScoreScale = highScoreText.transform.localScale;
    }

    private void Start()
    {
        scoreService = ServiceLocator.Get<IScoreService>();
        if (scoreService != null)
        {
            scoreService.OnScoreChanged += HandleScoreChanged;
            scoreService.OnComboChanged += HandleComboChanged;
            scoreService.OnHighScoreChanged += HandleHighScoreChanged;

            displayedScore = scoreService.CurrentScore;
            UpdateScoreDisplay(displayedScore);
            UpdateHighScoreDisplay(scoreService.HighScore);
            UpdateComboDisplay(scoreService.CurrentCombo);
        }
        else
        {
            displayedScore = 0;
            UpdateScoreDisplay(0);
            UpdateHighScoreDisplay(0);
            UpdateComboDisplay(0);
        }
    }

    private void OnDisable()
    {
        KillAndResetScoreTween();

        if (comboText != null)
        {
            comboText.transform.DOKill();
            comboText.transform.localScale = originalComboScale;
        }

        if (highScoreText != null)
        {
            highScoreText.transform.DOKill();
            highScoreText.transform.localScale = originalHighScoreScale;
        }
    }

    private void OnDestroy()
    {
        if (scoreService != null)
        {
            scoreService.OnScoreChanged -= HandleScoreChanged;
            scoreService.OnComboChanged -= HandleComboChanged;
            scoreService.OnHighScoreChanged -= HandleHighScoreChanged;
        }

        KillAndResetScoreTween();
    }

    private void HandleScoreChanged(int currentScore, int gainedPoints)
    {
        if (currentScoreText == null) return;

        if (!enablePunchScale || gainedPoints <= 0)
        {
            KillAndResetScoreTween();
            displayedScore = currentScore;
            UpdateScoreDisplay(displayedScore);
            return;
        }

        KillAndResetScoreTween();

        int targetScore = currentScore;
        float countDuration = Mathf.Clamp(gainedPoints * 0.04f, minCountDuration, maxCountDuration);

        Sequence scoreSeq = DOTween.Sequence();
        scoreSeq.SetTarget(currentScoreText.transform);
        scoreSeq.SetLink(gameObject, LinkBehaviour.KillOnDisable);

        // 1. Concurrently run: Number count, Scale swelling (+scoreScaleBonus), and Micro-shake
        scoreSeq.Join(DOTween.To(() => displayedScore, x => {
            displayedScore = x;
            UpdateScoreDisplay(displayedScore);
        }, targetScore, countDuration).SetEase(Ease.OutQuad));

        scoreSeq.Join(currentScoreText.transform.DOScale(originalScoreScale * (1f + scoreScaleBonus), countDuration).SetEase(Ease.InQuad));

        scoreSeq.Join(currentScoreText.transform.DOShakePosition(countDuration, scoreShakeStrength, vibrato: 20, randomness: 90f, snapping: false, fadeOut: false));

        // 2. When target is reached: enforce exact target value, restore position and spring-settle scale
        scoreSeq.AppendCallback(() => {
            displayedScore = targetScore;
            UpdateScoreDisplay(displayedScore);
            currentScoreText.transform.localPosition = originalScoreLocalPos;
        });

        scoreSeq.Append(currentScoreText.transform.DOScale(originalScoreScale, settleDuration).SetEase(Ease.OutBack));
    }

    private void HandleComboChanged(int currentCombo)
    {
        UpdateComboDisplay(currentCombo);

        if (enablePunchScale && currentCombo >= 1 && comboText != null)
        {
            PlayPunchScale(comboText.transform, originalComboScale);
        }
    }

    private void HandleHighScoreChanged(int newHighScore)
    {
        UpdateHighScoreDisplay(newHighScore);

        if (enablePunchScale && highScoreText != null)
        {
            PlayPunchScale(highScoreText.transform, originalHighScoreScale);
        }
    }

    private void PlayPunchScale(Transform target, Vector3 defaultScale)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = defaultScale;

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(gameObject, LinkBehaviour.KillOnDisable);

        float halfDuration = punchDuration * 0.5f;
        seq.Append(target.DOScale(defaultScale * punchScaleAmount, halfDuration).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(defaultScale, halfDuration).SetEase(Ease.InQuad));
    }

    private void KillAndResetScoreTween()
    {
        if (currentScoreText != null)
        {
            currentScoreText.transform.DOKill();
            currentScoreText.transform.localPosition = originalScoreLocalPos;
            currentScoreText.transform.localScale = originalScoreScale;
        }
    }

    private void UpdateScoreDisplay(int score)
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = score.ToString();
        }
    }

    private void UpdateHighScoreDisplay(int highScore)
    {
        if (highScoreText != null)
        {
            highScoreText.text = highScore.ToString();
        }
    }

    private void UpdateComboDisplay(int combo)
    {
        bool showCombo = combo >= 1;

        if (comboContainer != null)
        {
            comboContainer.SetActive(showCombo);
        }

        if (comboText != null)
        {
            if (comboContainer == null)
            {
                comboText.gameObject.SetActive(showCombo);
            }

            if (showCombo)
            {
                comboText.text = combo == 1 ? "COMBO" : $"COMBO x{combo}";
            }
        }
    }
}
