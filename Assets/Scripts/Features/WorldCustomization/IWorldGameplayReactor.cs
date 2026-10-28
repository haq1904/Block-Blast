namespace BlockBlast.Features.WorldCustomization
{
    public interface IWorldGameplayReactor
    {
        void PlayLineClear(in WorldLineClearContext context);
        void PlayTableClear(in WorldTableClearContext context);
        void PlayComboChanged(in WorldComboContext context);
        void PlayGameOver();
        void PlayStressStateChanged(in WorldStressContext context);
        void ResetPresentation();
    }
}
