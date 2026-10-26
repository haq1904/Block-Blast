using System;
using System.Collections.Generic;
using System.Linq;
using CubePuzzle3DPlayerData.Contracts;
using CubePuzzle3DPlayerData.Domain;
using NUnit.Framework;

namespace CubePuzzle3DPlayerData.Tests;

[TestFixture]
public class PlayerSnapshotReducerTest
{
    private const long BaseTime = 1761465600L; // 2025-10-26 08:00:00 UTC (Monday)

    [Test]
    public void Reduce_FirstGame_EstablishesPersonalBestBaselineWithoutBreakCount()
    {
        var defaultSnapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        var request = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "first_session_1",
            Score = 5000,
            LinesCleared = 10,
            BlocksPlaced = 25,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime
        };

        var result = PlayerSnapshotReducer.Reduce(defaultSnapshot, request, BaseTime);

        Assert.That(result.Snapshot.Revision, Is.EqualTo(1L));
        Assert.That(result.Snapshot.LifetimeStats.GamesPlayed, Is.EqualTo(1L));
        Assert.That(result.Snapshot.LifetimeStats.BestScore, Is.EqualTo(5000));
        Assert.That(result.Snapshot.LifetimeStats.PersonalBestBreakCount, Is.EqualTo(0L));
        Assert.That(result.IsNewPersonalBest, Is.False);
        Assert.That(result.Snapshot.HasReceipt("first_session_1"), Is.True);
    }

    [Test]
    public void Reduce_SecondGame_LowerOrEqualScore_DoesNotBreakPersonalBest()
    {
        var defaultSnapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        var req1 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_1",
            Score = 5000,
            LinesCleared = 10,
            BlocksPlaced = 25,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime
        };
        var res1 = PlayerSnapshotReducer.Reduce(defaultSnapshot, req1, BaseTime);

        var req2 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_2",
            Score = 5000, // equal score
            LinesCleared = 8,
            BlocksPlaced = 20,
            BestCombo = 1,
            CompletedAtUnixSeconds = BaseTime + 100
        };
        var res2 = PlayerSnapshotReducer.Reduce(res1.Snapshot, req2, BaseTime + 100);

        Assert.That(res2.Snapshot.Revision, Is.EqualTo(2L));
        Assert.That(res2.Snapshot.LifetimeStats.GamesPlayed, Is.EqualTo(2L));
        Assert.That(res2.Snapshot.LifetimeStats.BestScore, Is.EqualTo(5000));
        Assert.That(res2.Snapshot.LifetimeStats.PersonalBestBreakCount, Is.EqualTo(0L));
        Assert.That(res2.IsNewPersonalBest, Is.False);
    }

    [Test]
    public void Reduce_SecondGame_HigherScore_BreaksPersonalBestAndUnlocksNewHeights()
    {
        var defaultSnapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        var req1 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_1",
            Score = 5000,
            LinesCleared = 10,
            BlocksPlaced = 25,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime
        };
        var res1 = PlayerSnapshotReducer.Reduce(defaultSnapshot, req1, BaseTime);

        var req2 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_2",
            Score = 6000, // higher score
            LinesCleared = 12,
            BlocksPlaced = 30,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime + 100
        };
        var res2 = PlayerSnapshotReducer.Reduce(res1.Snapshot, req2, BaseTime + 100);

        Assert.That(res2.Snapshot.Revision, Is.EqualTo(2L));
        Assert.That(res2.Snapshot.LifetimeStats.BestScore, Is.EqualTo(6000));
        Assert.That(res2.Snapshot.LifetimeStats.PersonalBestBreakCount, Is.EqualTo(1L));
        Assert.That(res2.IsNewPersonalBest, Is.True);
        Assert.That(res2.NewlyUnlockedAchievementIds.Contains(AchievementDefinitions.NewHeights), Is.True);
    }

    [Test]
    public void Reduce_All18Achievements_UnlockThresholdsAndTimestampPreservation()
    {
        var current = StoredPlayerSnapshot.CreateDefault(BaseTime);

        // 1. Below target
        var req1 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_part1",
            Score = 9999, // score_starter target is 10000
            LinesCleared = 99, // line_cleaner_1 target is 100
            BlocksPlaced = 999, // block_builder target is 1000
            BestCombo = 2, // combo_spark target is 3
            CompletedAtUnixSeconds = BaseTime
        };
        var res1 = PlayerSnapshotReducer.Reduce(current, req1, BaseTime);

        Assert.That(res1.NewlyUnlockedAchievementIds, Is.Empty);
        var starterAch = res1.Snapshot.Achievements.First(a => a.AchievementId == AchievementDefinitions.ScoreStarter);
        Assert.That(starterAch.UnlockedAtUnixSeconds, Is.EqualTo(0L));
        Assert.That(starterAch.CurrentValue, Is.EqualTo(9999L));

        // 2. Exactly at target
        long unlockTime = BaseTime + 500;
        var req2 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_part2",
            Score = 10001, // breaks PB and crosses 10000
            LinesCleared = 1, // total lines becomes 100
            BlocksPlaced = 1, // total blocks becomes 1000
            BestCombo = 3, // best combo becomes 3
            CompletedAtUnixSeconds = unlockTime
        };
        var res2 = PlayerSnapshotReducer.Reduce(res1.Snapshot, req2, unlockTime);

        Assert.That(res2.NewlyUnlockedAchievementIds.Contains(AchievementDefinitions.ScoreStarter), Is.True);
        Assert.That(res2.NewlyUnlockedAchievementIds.Contains(AchievementDefinitions.LineCleaner1), Is.True);
        Assert.That(res2.NewlyUnlockedAchievementIds.Contains(AchievementDefinitions.BlockBuilder), Is.True);
        Assert.That(res2.NewlyUnlockedAchievementIds.Contains(AchievementDefinitions.ComboSpark), Is.True);

        var unlockedStarter = res2.Snapshot.Achievements.First(a => a.AchievementId == AchievementDefinitions.ScoreStarter);
        Assert.That(unlockedStarter.UnlockedAtUnixSeconds, Is.EqualTo(unlockTime));

        // 3. Subsequent game: must preserve original unlock timestamp
        long laterTime = BaseTime + 1000;
        var req3 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_part3",
            Score = 15000,
            LinesCleared = 5,
            BlocksPlaced = 10,
            BestCombo = 3,
            CompletedAtUnixSeconds = laterTime
        };
        var res3 = PlayerSnapshotReducer.Reduce(res2.Snapshot, req3, laterTime);

        Assert.That(res3.NewlyUnlockedAchievementIds.Contains(AchievementDefinitions.ScoreStarter), Is.False);
        var preservedStarter = res3.Snapshot.Achievements.First(a => a.AchievementId == AchievementDefinitions.ScoreStarter);
        Assert.That(preservedStarter.UnlockedAtUnixSeconds, Is.EqualTo(unlockTime)); // still original unlockTime!
    }

    [Test]
    public void Reduce_RecentGames_BoundedToMax7AndOldestTrimmed()
    {
        var snapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        for (int i = 1; i <= 10; i++)
        {
            var req = new GameResultRequest
            {
                SchemaVersion = 1,
                GameSessionId = $"session_{i:00}",
                Score = i * 100,
                LinesCleared = 1,
                BlocksPlaced = 5,
                BestCombo = 1,
                CompletedAtUnixSeconds = BaseTime + (i * 10)
            };
            var res = PlayerSnapshotReducer.Reduce(snapshot, req, BaseTime + (i * 10));
            snapshot = res.Snapshot;
        }

        Assert.That(snapshot.RecentGames.Count, Is.EqualTo(PlayerDataConstants.MaxRecentGames));
        // Should contain sessions 04 to 10
        Assert.That(snapshot.RecentGames[0].GameSessionId, Is.EqualTo("session_04"));
        Assert.That(snapshot.RecentGames[6].GameSessionId, Is.EqualTo("session_10"));
    }

    [Test]
    public void Reduce_WeeklyRollover_NextWeekRotatesCurrentToPrevious()
    {
        var snapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        var req1 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "week1_session",
            Score = 1000,
            LinesCleared = 20,
            BlocksPlaced = 50,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime
        };
        var res1 = PlayerSnapshotReducer.Reduce(snapshot, req1, BaseTime);
        snapshot = res1.Snapshot;

        long nextWeekTime = BaseTime + UtcWeek.SecondsPerWeek + 100; // Next Monday
        var req2 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "week2_session",
            Score = 2000,
            LinesCleared = 30,
            BlocksPlaced = 60,
            BestCombo = 2,
            CompletedAtUnixSeconds = nextWeekTime
        };
        var res2 = PlayerSnapshotReducer.Reduce(snapshot, req2, nextWeekTime);
        snapshot = res2.Snapshot;

        Assert.That(snapshot.PreviousWeek.GamesPlayed, Is.EqualTo(1L));
        Assert.That(snapshot.PreviousWeek.TotalScore, Is.EqualTo(1000L));
        Assert.That(snapshot.PreviousWeek.TotalLinesCleared, Is.EqualTo(20L));

        Assert.That(snapshot.CurrentWeek.GamesPlayed, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentWeek.TotalScore, Is.EqualTo(2000L));
        Assert.That(snapshot.CurrentWeek.TotalLinesCleared, Is.EqualTo(30L));
    }

    [Test]
    public void Reduce_WeeklyRollover_MultiWeekGap_ResetsPreviousWeek()
    {
        var snapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        var req1 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "week1_session",
            Score = 1000,
            LinesCleared = 20,
            BlocksPlaced = 50,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime
        };
        var res1 = PlayerSnapshotReducer.Reduce(snapshot, req1, BaseTime);
        snapshot = res1.Snapshot;

        long multiWeekTime = BaseTime + (3L * UtcWeek.SecondsPerWeek); // 3 weeks later
        var req2 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "week4_session",
            Score = 3000,
            LinesCleared = 40,
            BlocksPlaced = 80,
            BestCombo = 2,
            CompletedAtUnixSeconds = multiWeekTime
        };
        var res2 = PlayerSnapshotReducer.Reduce(snapshot, req2, multiWeekTime);
        snapshot = res2.Snapshot;

        Assert.That(snapshot.PreviousWeek.GamesPlayed, Is.EqualTo(0L));
        Assert.That(snapshot.PreviousWeek.TotalScore, Is.EqualTo(0L));
        Assert.That(snapshot.CurrentWeek.GamesPlayed, Is.EqualTo(1L));
        Assert.That(snapshot.CurrentWeek.TotalScore, Is.EqualTo(3000L));
    }

    [Test]
    public void Reduce_Insights_EmptyUntilFiveGamesPlayed()
    {
        var snapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        for (int i = 1; i <= 4; i++)
        {
            var req = new GameResultRequest
            {
                SchemaVersion = 1,
                GameSessionId = $"session_{i}",
                Score = i * 1000,
                LinesCleared = 5,
                BlocksPlaced = 10,
                BestCombo = 1,
                CompletedAtUnixSeconds = BaseTime + i
            };
            var res = PlayerSnapshotReducer.Reduce(snapshot, req, BaseTime + i);
            snapshot = res.Snapshot;
            Assert.That(snapshot.Insights, Is.Empty);
        }

        // 5th game produces insights
        var req5 = new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_5",
            Score = 6000,
            LinesCleared = 10,
            BlocksPlaced = 20,
            BestCombo = 2,
            CompletedAtUnixSeconds = BaseTime + 5
        };
        var res5 = PlayerSnapshotReducer.Reduce(snapshot, req5, BaseTime + 5);

        Assert.That(res5.Snapshot.Insights.Count, Is.GreaterThan(0));
        Assert.That(res5.Snapshot.Insights.Count, Is.LessThanOrEqualTo(PlayerDataConstants.MaxInsights));
    }

    [Test]
    public void Reduce_ReceiptsRing_BoundedToMax256()
    {
        var snapshot = StoredPlayerSnapshot.CreateDefault(BaseTime);
        for (int i = 1; i <= 260; i++)
        {
            var req = new GameResultRequest
            {
                SchemaVersion = 1,
                GameSessionId = $"session_{i:000}",
                Score = 10,
                LinesCleared = 1,
                BlocksPlaced = 1,
                BestCombo = 1,
                CompletedAtUnixSeconds = BaseTime + i
            };
            var res = PlayerSnapshotReducer.Reduce(snapshot, req, BaseTime + i);
            snapshot = res.Snapshot;
        }

        Assert.That(snapshot.ProcessedGameReceipts.Count, Is.EqualTo(PlayerDataConstants.MaxReceipts));
        Assert.That(snapshot.HasReceipt("session_001"), Is.False); // pushed out of ring
        Assert.That(snapshot.HasReceipt("session_260"), Is.True);
    }
}
