public class ScoreModel
{
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int MaxCombo { get; private set; }

    public ScoreModel(int initialHighScore = 0)
    {
        CurrentScore = 0;
        HighScore = initialHighScore;
        CurrentCombo = 0;
        MaxCombo = 0;
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

    public void SetCombo(int combo)
    {
        CurrentCombo = combo;
        if (CurrentCombo > MaxCombo)
        {
            MaxCombo = CurrentCombo;
        }
    }

    public void Reset()
    {
        CurrentScore = 0;
        CurrentCombo = 0;
    }

    public void SetHighScore(int highScore)
    {
        if (highScore > HighScore)
        {
            HighScore = highScore;
        }
    }
}
