using System.Collections.Generic;
using CubePuzzle3DPlayerData.Domain;
using NUnit.Framework;

namespace CubePuzzle3DPlayerData.Tests;

[TestFixture]
public class AchievementDefinitionsTest
{
    private static readonly Dictionary<string, long> ExpectedPhase1Catalog = new()
    {
        { "first_steps", 5L },
        { "regular_player", 100L },
        { "block_veteran", 500L },
        { "score_starter", 10000L },
        { "score_climber", 300000L },
        { "score_master", 500000L },
        { "score_legend", 700000L },
        { "line_cleaner_1", 100L },
        { "line_cleaner_2", 500L },
        { "line_cleaner_3", 1000L },
        { "line_cleaner_4", 5000L },
        { "combo_spark", 3L },
        { "combo_builder", 10L },
        { "combo_collector", 15L },
        { "combo_master", 17L },
        { "block_builder", 1000L },
        { "block_architect", 3000L },
        { "new_heights", 1L }
    };

    [Test]
    public void All_ContainsExactly18Achievements()
    {
        Assert.That(AchievementDefinitions.All.Count, Is.EqualTo(18));
    }

    [Test]
    public void All_MatchesPhase1DefinitionsExactly()
    {
        foreach (var def in AchievementDefinitions.All)
        {
            Assert.That(ExpectedPhase1Catalog.ContainsKey(def.Id), Is.True, $"Unexpected achievement: {def.Id}");
            Assert.That(def.TargetValue, Is.EqualTo(ExpectedPhase1Catalog[def.Id]), $"Target mismatch for {def.Id}");
        }

        Assert.That(AchievementDefinitions.All.Count, Is.EqualTo(ExpectedPhase1Catalog.Count));
    }

    [Test]
    public void All_OrderIsDeterministic()
    {
        var ids = new List<string>();
        foreach (var a in AchievementDefinitions.All)
        {
            ids.Add(a.Id);
        }

        Assert.That(ids[0], Is.EqualTo(AchievementDefinitions.FirstSteps));
        Assert.That(ids[17], Is.EqualTo(AchievementDefinitions.NewHeights));
    }
}
