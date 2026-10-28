namespace BlockBlast.Features.WorldCustomization
{
    public interface IWorldSelectionStore
    {
        string GetSelectedWorldId(string defaultWorldId);
        void SaveSelectedWorldId(string worldId);
    }
}
