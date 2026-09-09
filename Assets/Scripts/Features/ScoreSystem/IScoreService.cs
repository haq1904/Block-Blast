using System;

public interface IScoreService
{
    int CurrentScore { get; }
    int HighScore { get; }
    int CurrentCombo { get; }
    int MaxCombo { get; }

    event Action<int, int> OnScoreChanged;       // (currentScore, gainedPoints)
    event Action<int> OnComboChanged;            // (currentCombo)
    event Action<int> OnHighScoreChanged;        // (newHighScore)

    void ResetScore();
}
