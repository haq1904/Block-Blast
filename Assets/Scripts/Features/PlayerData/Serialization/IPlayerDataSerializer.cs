namespace BlockBlast.Features.PlayerData
{
    public interface IPlayerDataSerializer
    {
        string SerializeGameResult(GameResult result);
        GameResult DeserializeGameResult(string json);
        string SerializeSnapshot(GameCenterSnapshot snapshot);
        GameCenterSnapshot DeserializeSnapshot(string json);
    }
}
