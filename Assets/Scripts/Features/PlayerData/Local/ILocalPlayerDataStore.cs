namespace BlockBlast.Features.PlayerData
{
    public interface ILocalPlayerDataStore
    {
        int LoadHighScore();
        bool SaveHighScore(int newScore);
    }
}
