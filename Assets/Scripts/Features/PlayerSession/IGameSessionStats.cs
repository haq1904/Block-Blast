using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.PlayerSession
{
    public interface IGameSessionStats
    {
        string GameSessionId { get; }
        int LinesCleared { get; }
        int BlocksPlaced { get; }
        int BestCombo { get; }
        bool IsCompleted { get; }

        void RecordPlacement(int tilesPlaced, int totalLinesCleared);
        GameResult Complete(int finalScore, int bestCombo, long completedAtUnixSeconds);
    }
}
