using System.Collections.Generic;

namespace BlockBlast.Features.PlayerData
{
    public static class AchievementCatalog
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

        public static readonly IReadOnlyDictionary<string, long> Definitions = new Dictionary<string, long>
        {
            { FirstSteps, 5L },
            { RegularPlayer, 100L },
            { BlockVeteran, 500L },
            { ScoreStarter, 10000L },
            { ScoreClimber, 300000L },
            { ScoreMaster, 500000L },
            { ScoreLegend, 700000L },
            { LineCleaner1, 100L },
            { LineCleaner2, 500L },
            { LineCleaner3, 1000L },
            { LineCleaner4, 5000L },
            { ComboSpark, 3L },
            { ComboBuilder, 10L },
            { ComboCollector, 15L },
            { ComboMaster, 17L },
            { BlockBuilder, 1000L },
            { BlockArchitect, 3000L },
            { NewHeights, 1L }
        };

        public static class Insights
        {
            public const string NewPersonalBestThisWeek = "new_personal_best_this_week";
            public const string WeeklyAverageChange = "weekly_average_change";
            public const string RecentAverage = "recent_average";
            public const string RecentVsLifetimeAverage = "recent_vs_lifetime_average";
            public const string BestComboNextGoal = "best_combo_next_goal";
            public const string MostActiveWeek = "most_active_week";
            public const string PersonalBestBreakCount = "personal_best_break_count";
        }
    }
}
