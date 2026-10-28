namespace BlockBlast.Features.WorldCustomization
{
    public interface IWorldHostRegistry
    {
        void RegisterHost(IWorldEnvironmentHost host);
        void UnregisterHost(IWorldEnvironmentHost host);
    }
}
