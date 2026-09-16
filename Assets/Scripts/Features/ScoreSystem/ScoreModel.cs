public class ScoreModel
{
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int MaxCombo { get; private set; }
    public bool IsPrewarmed { get; private set; }
    public int TurnsRemaining { get; private set; }

    public ScoreModel(int initialHighScore = 0)
    {
        CurrentScore = 0;
        HighScore = initialHighScore;
        CurrentCombo = 0;
        MaxCombo = 0;
        IsPrewarmed = false;
        TurnsRemaining = 0;
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;
        CurrentScore += points;
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
        }
    }

    /// <summary>
    /// Processes a placement with lines cleared, handling prewarm, combo ignition, accumulation, and turn expiration.
    /// </summary>
    public void RecordPlacement(int linesCleared, int prewarmLimit, int comboLimit)
    {
        if (CurrentCombo == 0)
        {
            if (!IsPrewarmed)
            {
                if (linesCleared > 0)
                {
                    // First line clear ever or after reset -> Prewarm triggered!
                    IsPrewarmed = true;
                    TurnsRemaining = prewarmLimit;
                    // CurrentCombo stays 0 (Option A)
                }
            }
            else // IsPrewarmed
            {
                if (linesCleared > 0)
                {
                    // Cleared lines within prewarm window -> Combo ignites!
                    IsPrewarmed = false;
                    CurrentCombo = linesCleared;
                    TurnsRemaining = comboLimit;
                    if (CurrentCombo > MaxCombo)
                    {
                        MaxCombo = CurrentCombo;
                    }
                }
                else
                {
                    // No lines cleared while prewarmed -> consume 1 turn
                    TurnsRemaining--;
                    if (TurnsRemaining <= 0)
                    {
                        // Prewarm expired (lost primer)
                        IsPrewarmed = false;
                        TurnsRemaining = 0;
                    }
                }
            }
        }
        else // CurrentCombo > 0 (Active combo)
        {
            if (linesCleared > 0)
            {
                // Active combo sustained & accumulated!
                CurrentCombo += linesCleared;
                TurnsRemaining = comboLimit;
                if (CurrentCombo > MaxCombo)
                {
                    MaxCombo = CurrentCombo;
                }
            }
            else
            {
                // Missed a turn during active combo
                TurnsRemaining--;
                if (TurnsRemaining <= 0)
                {
                    // Combo ended
                    CurrentCombo = 0;
                    IsPrewarmed = false;
                    TurnsRemaining = 0;
                }
            }
        }
    }

    public void SetCombo(int combo)
    {
        CurrentCombo = combo;
        IsPrewarmed = false;
        if (CurrentCombo > MaxCombo)
        {
            MaxCombo = CurrentCombo;
        }
    }

    public void ResetCombo()
    {
        CurrentCombo = 0;
        IsPrewarmed = false;
        TurnsRemaining = 0;
    }

    public void Reset()
    {
        CurrentScore = 0;
        ResetCombo();
    }

    public void SetHighScore(int highScore)
    {
        if (highScore > HighScore)
        {
            HighScore = highScore;
        }
    }
}
