using System.Collections.Generic;

namespace CubePuzzle3DPlayerData.Domain;

public enum AchievementMetric
{
    GamesPlayed,
    BestScore,
    TotalLinesCleared,
    BestCombo,
    TotalBlocksPlaced,
    PersonalBestBreakCount
}

public sealed class AchievementDef
{
    public string Id { get; }
    public AchievementMetric Metric { get; }
    public long TargetValue { get; }

    public AchievementDef(string id, AchievementMetric metric, long targetValue)
    {
        Id = id;
        Metric = metric;
        TargetValue = targetValue;
    }
}

public static class AchievementDefinitions
{
    public const string FirstSteps = "first_steps";
    public const string RegularPlayer = "regular_player";
    public const string BlockVeteran = "block_veteran";
    public const string ScoreStarter = "score_starter";
    public const string ScoreClimber = "score_climber";
    public const string ScoreMaster = "score_master";
    public const string ScoreLegend = "score_legend";
    public const string LineCleaner1 = "line_cleaner_1";
    public const string LineCleaner2 = "line_cleaner_2";
    public const string LineCleaner3 = "line_cleaner_3";
    public const string LineCleaner4 = "line_cleaner_4";
    public const string ComboSpark = "combo_spark";
    public const string ComboBuilder = "combo_builder";
    public const string ComboCollector = "combo_collector";
    public const string ComboMaster = "combo_master";
    public const string BlockBuilder = "block_builder";
    public const string BlockArchitect = "block_architect";
    public const string NewHeights = "new_heights";

    public static readonly IReadOnlyList<AchievementDef> All = new List<AchievementDef>
    {
        new(FirstSteps, AchievementMetric.GamesPlayed, 5L),
        new(RegularPlayer, AchievementMetric.GamesPlayed, 100L),
        new(BlockVeteran, AchievementMetric.GamesPlayed, 500L),
        new(ScoreStarter, AchievementMetric.BestScore, 10000L),
        new(ScoreClimber, AchievementMetric.BestScore, 300000L),
        new(ScoreMaster, AchievementMetric.BestScore, 500000L),
        new(ScoreLegend, AchievementMetric.BestScore, 700000L),
        new(LineCleaner1, AchievementMetric.TotalLinesCleared, 100L),
        new(LineCleaner2, AchievementMetric.TotalLinesCleared, 500L),
        new(LineCleaner3, AchievementMetric.TotalLinesCleared, 1000L),
        new(LineCleaner4, AchievementMetric.TotalLinesCleared, 5000L),
        new(ComboSpark, AchievementMetric.BestCombo, 3L),
        new(ComboBuilder, AchievementMetric.BestCombo, 10L),
        new(ComboCollector, AchievementMetric.BestCombo, 15L),
        new(ComboMaster, AchievementMetric.BestCombo, 17L),
        new(BlockBuilder, AchievementMetric.TotalBlocksPlaced, 1000L),
        new(BlockArchitect, AchievementMetric.TotalBlocksPlaced, 3000L),
        new(NewHeights, AchievementMetric.PersonalBestBreakCount, 1L),
    }.AsReadOnly();

    public static class Insights
    {
        public const string NewPersonalBestThisWeek = "new_personal_best_this_week";
        public const string WeeklyAverageChange = "weekly_average_change";
        public const string RecentVsLifetimeAverage = "recent_vs_lifetime_average";
        public const string BestComboNextGoal = "best_combo_next_goal";
        public const string MostActiveWeek = "most_active_week";
        public const string PersonalBestBreakCount = "personal_best_break_count";
        public const string RecentAverage = "recent_average";
    }
}
