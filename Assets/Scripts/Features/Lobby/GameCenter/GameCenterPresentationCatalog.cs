using System.Collections.Generic;
using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.Lobby.GameCenter
{
    public sealed class AchievementPresentationMeta
    {
        public string Id { get; }
        public string Title { get; }
        public string Group { get; }
        public long DefaultTarget { get; }
        public string Description { get; }

        public AchievementPresentationMeta(string id, string title, string group, long defaultTarget, string description)
        {
            Id = id;
            Title = title;
            Group = group;
            DefaultTarget = defaultTarget;
            Description = description;
        }
    }

    public static class GameCenterPresentationCatalog
    {
        public static readonly IReadOnlyList<AchievementPresentationMeta> Achievements = new AchievementPresentationMeta[]
        {
            new AchievementPresentationMeta(AchievementCatalog.FirstSteps, "First Steps", "Games", 5L, "Play 5 games"),
            new AchievementPresentationMeta(AchievementCatalog.RegularPlayer, "Regular Player", "Games", 100L, "Play 100 games"),
            new AchievementPresentationMeta(AchievementCatalog.BlockVeteran, "Block Veteran", "Games", 500L, "Play 500 games"),
            new AchievementPresentationMeta(AchievementCatalog.ScoreStarter, "Score Starter", "Score", 10000L, "Reach score 10,000"),
            new AchievementPresentationMeta(AchievementCatalog.ScoreClimber, "Score Climber", "Score", 300000L, "Reach score 300,000"),
            new AchievementPresentationMeta(AchievementCatalog.ScoreMaster, "Score Master", "Score", 500000L, "Reach score 500,000"),
            new AchievementPresentationMeta(AchievementCatalog.ScoreLegend, "Score Legend", "Score", 700000L, "Reach score 700,000"),
            new AchievementPresentationMeta(AchievementCatalog.LineCleaner1, "Line Cleaner I", "Lines", 100L, "Clear 100 lines"),
            new AchievementPresentationMeta(AchievementCatalog.LineCleaner2, "Line Cleaner II", "Lines", 500L, "Clear 500 lines"),
            new AchievementPresentationMeta(AchievementCatalog.LineCleaner3, "Line Cleaner III", "Lines", 1000L, "Clear 1,000 lines"),
            new AchievementPresentationMeta(AchievementCatalog.LineCleaner4, "Line Cleaner IV", "Lines", 5000L, "Clear 5,000 lines"),
            new AchievementPresentationMeta(AchievementCatalog.ComboSpark, "Combo Spark", "Combo", 3L, "Reach 3x combo"),
            new AchievementPresentationMeta(AchievementCatalog.ComboBuilder, "Combo Builder", "Combo", 10L, "Reach 10x combo"),
            new AchievementPresentationMeta(AchievementCatalog.ComboCollector, "Combo Collector", "Combo", 15L, "Reach 15x combo"),
            new AchievementPresentationMeta(AchievementCatalog.ComboMaster, "Combo Master", "Combo", 17L, "Reach 17x combo"),
            new AchievementPresentationMeta(AchievementCatalog.BlockBuilder, "Block Builder", "Blocks", 1000L, "Place 1,000 blocks"),
            new AchievementPresentationMeta(AchievementCatalog.BlockArchitect, "Block Architect", "Blocks", 3000L, "Place 3,000 blocks"),
            new AchievementPresentationMeta(AchievementCatalog.NewHeights, "New Heights", "Milestone", 1L, "Break your personal best")
        };
    }
}
