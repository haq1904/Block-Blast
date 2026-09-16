using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreView : MonoBehaviour
{
    [Header("Text Displays (Supports both 3D World TextMeshPro & Canvas TextMeshProUGUI)")]
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject comboContainer;

    [Header("Animation Settings")]
    [SerializeField] private bool enablePunchScale = true;
    [SerializeField] private float punchScaleAmount = 1.25f;
    [SerializeField] private float punchDuration = 0.2f;

    private IScoreService scoreService;
    private Coroutine scorePunchRoutine;
    private Coroutine comboPunchRoutine;
    private Coroutine highScorePunchRoutine;

    private Vector3 originalScoreScale = Vector3.one;
    private Vector3 originalComboScale = Vector3.one;
    private Vector3 originalHighScoreScale = Vector3.one;

    private void Awake()
    {
        if (currentScoreText != null) originalScoreScale = currentScoreText.transform.localScale;
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

            // Initialize initial displays
            UpdateScoreDisplay(scoreService.CurrentScore);
            UpdateHighScoreDisplay(scoreService.HighScore);
            UpdateComboDisplay(scoreService.CurrentCombo);
        }
        else
        {
            // Default initial state if score service is delayed
            UpdateScoreDisplay(0);
            UpdateHighScoreDisplay(0);
            UpdateComboDisplay(0);
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
    }

    private void HandleScoreChanged(int currentScore, int gainedPoints)
    {
        UpdateScoreDisplay(currentScore);

        if (enablePunchScale && gainedPoints > 0 && currentScoreText != null)
        {
            if (scorePunchRoutine != null) StopCoroutine(scorePunchRoutine);
            scorePunchRoutine = StartCoroutine(PunchScaleCoroutine(currentScoreText.transform, originalScoreScale));
        }
    }

    private void HandleComboChanged(int currentCombo)
    {
        UpdateComboDisplay(currentCombo);

        if (enablePunchScale && currentCombo >= 1 && comboText != null)
        {
            if (comboPunchRoutine != null) StopCoroutine(comboPunchRoutine);
            comboPunchRoutine = StartCoroutine(PunchScaleCoroutine(comboText.transform, originalComboScale));
        }
    }

    private void HandleHighScoreChanged(int newHighScore)
    {
        UpdateHighScoreDisplay(newHighScore);

        if (enablePunchScale && highScoreText != null)
        {
            if (highScorePunchRoutine != null) StopCoroutine(highScorePunchRoutine);
            highScorePunchRoutine = StartCoroutine(PunchScaleCoroutine(highScoreText.transform, originalHighScoreScale));
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

    private IEnumerator PunchScaleCoroutine(Transform target, Vector3 defaultScale)
    {
        if (target == null) yield break;

        Vector3 punchScale = defaultScale * punchScaleAmount;
        float halfDuration = punchDuration * 0.5f;
        float elapsed = 0f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            target.localScale = Vector3.Lerp(defaultScale, punchScale, t);
            yield return null;
        }

        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            target.localScale = Vector3.Lerp(punchScale, defaultScale, t);
            yield return null;
        }

        target.localScale = defaultScale;
    }
}
