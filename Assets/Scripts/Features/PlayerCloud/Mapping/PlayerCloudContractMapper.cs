using System;
using System.Collections.Generic;
using BlockBlast.Features.PlayerData;
using Generated = Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts;

namespace BlockBlast.Features.PlayerCloud.Mapping
{
    public static class PlayerCloudContractMapper
    {
        public static Generated.GameResultRequest ToGeneratedRequest(GameResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new Generated.GameResultRequest
            {
                SchemaVersion = result.SchemaVersion,
                GameSessionId = result.GameSessionId,
                Score = result.Score,
                LinesCleared = result.LinesCleared,
                BlocksPlaced = result.BlocksPlaced,
                BestCombo = result.BestCombo,
                CompletedAtUnixSeconds = result.CompletedAtUnixSeconds
            };
        }

        public static SubmitGameResultResponse ToDomainResponse(Generated.SubmitGameResultResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var snapshot = ToDomainSnapshot(response.UpdatedSnapshot);

            return new SubmitGameResultResponse(
                snapshot,
                response.NewlyUnlockedAchievementIds,
                response.IsNewPersonalBest,
                response.IsDuplicate);
        }

        public static GameCenterSnapshot ToDomainSnapshot(Generated.GameCenterSnapshotContract contract)
        {
            ValidateSnapshotContract(contract);

            var lifetimeStats = new PlayerStats(
                contract.LifetimeStats.GamesPlayed,
                contract.LifetimeStats.TotalScore,
                contract.LifetimeStats.BestScore,
                contract.LifetimeStats.TotalLinesCleared,
                contract.LifetimeStats.TotalBlocksPlaced,
                contract.LifetimeStats.BestCombo,
                contract.LifetimeStats.PersonalBestBreakCount);

            var currentWeek = new WeeklyPlayerStats(
                contract.CurrentWeek.WeekStartUnixSeconds,
                contract.CurrentWeek.GamesPlayed,
                contract.CurrentWeek.TotalScore,
                contract.CurrentWeek.BestScore,
                contract.CurrentWeek.TotalLinesCleared,
                contract.CurrentWeek.TotalBlocksPlaced,
                contract.CurrentWeek.PersonalBestBreakCount);

            var previousWeek = new WeeklyPlayerStats(
                contract.PreviousWeek.WeekStartUnixSeconds,
                contract.PreviousWeek.GamesPlayed,
                contract.PreviousWeek.TotalScore,
                contract.PreviousWeek.BestScore,
                contract.PreviousWeek.TotalLinesCleared,
                contract.PreviousWeek.TotalBlocksPlaced,
                contract.PreviousWeek.PersonalBestBreakCount);

            var recentGames = new List<RecentGameResult>();
            if (contract.RecentGames != null)
            {
                foreach (var r in contract.RecentGames)
                {
                    recentGames.Add(new RecentGameResult(r.GameSessionId, r.Score, r.CompletedAtUnixSeconds));
                }
            }

            var achievements = new List<AchievementProgress>();
            if (contract.Achievements != null)
            {
                foreach (var a in contract.Achievements)
                {
                    achievements.Add(new AchievementProgress(a.AchievementId, a.CurrentValue, a.TargetValue, a.UnlockedAtUnixSeconds));
                }
            }

            var insights = new List<PlayerInsight>();
            if (contract.Insights != null)
            {
                foreach (var i in contract.Insights)
                {
                    insights.Add(new PlayerInsight(i.InsightId, i.CurrentValue, i.PreviousValue));
                }
            }

            return new GameCenterSnapshot(
                contract.Revision,
                contract.CreatedAtUnixSeconds,
                contract.UpdatedAtUnixSeconds,
                lifetimeStats,
                currentWeek,
                previousWeek,
                contract.BestWeekLinesCleared,
                recentGames,
                achievements,
                insights,
                contract.SchemaVersion);
        }

        public static GameCenterSnapshot CreateEmptySnapshot()
        {
            var emptyLifetime = new PlayerStats(0, 0, 0, 0, 0, 0, 0);
            var emptyCurrentWeek = new WeeklyPlayerStats(0, 0, 0, 0, 0, 0, 0);
            var emptyPreviousWeek = new WeeklyPlayerStats(0, 0, 0, 0, 0, 0, 0);

            var achievements = new List<AchievementProgress>();
            foreach (var kvp in AchievementCatalog.Definitions)
            {
                achievements.Add(new AchievementProgress(kvp.Key, 0L, kvp.Value, 0L));
            }

            return new GameCenterSnapshot(
                0L,
                0L,
                0L,
                emptyLifetime,
                emptyCurrentWeek,
                emptyPreviousWeek,
                0L,
                Array.Empty<RecentGameResult>(),
                achievements,
                Array.Empty<PlayerInsight>(),
                PlayerDataSchema.CurrentVersion);
        }

        public static CloudOperationResult<SubmitGameResultResponse> MapServerStatusToResult(Generated.SubmitGameResultResponse responseDto)
        {
            if (responseDto == null)
            {
                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.InvalidResponse,
                    false,
                    PlayerCloudConstants.ErrorCodes.InvalidResponse);
            }

            switch (responseDto.Status)
            {
                case Generated.SubmitGameResultStatus.Accepted:
                case Generated.SubmitGameResultStatus.Duplicate:
                    if (responseDto.UpdatedSnapshot == null)
                    {
                        return CloudOperationResult<SubmitGameResultResponse>.Failure(
                            CloudFailureKind.InvalidResponse,
                            false,
                            PlayerCloudConstants.ErrorCodes.InvalidResponse);
                    }

                    try
                    {
                        var domain = ToDomainResponse(responseDto);
                        return CloudOperationResult<SubmitGameResultResponse>.Success(domain);
                    }
                    catch (Exception)
                    {
                        return CloudOperationResult<SubmitGameResultResponse>.Failure(
                            CloudFailureKind.InvalidResponse,
                            false,
                            PlayerCloudConstants.ErrorCodes.InvalidResponse);
                    }

                case Generated.SubmitGameResultStatus.Rejected:
                    string rejectCode = responseDto.ErrorCode ?? PlayerCloudConstants.ErrorCodes.InvalidPayload;
                    if (rejectCode == PlayerCloudConstants.ErrorCodes.SchemaMismatch)
                    {
                        return CloudOperationResult<SubmitGameResultResponse>.Failure(CloudFailureKind.SchemaMismatch, false, rejectCode);
                    }
                    if (rejectCode == PlayerCloudConstants.ErrorCodes.Unauthenticated)
                    {
                        return CloudOperationResult<SubmitGameResultResponse>.Failure(CloudFailureKind.AuthenticationUnavailable, false, rejectCode);
                    }
                    return CloudOperationResult<SubmitGameResultResponse>.Failure(CloudFailureKind.InvalidPayload, false, rejectCode);

                case Generated.SubmitGameResultStatus.RetryableFailure:
                    string retryCode = responseDto.ErrorCode ?? PlayerCloudConstants.ErrorCodes.ServiceUnavailable;
                    return CloudOperationResult<SubmitGameResultResponse>.Failure(CloudFailureKind.ServiceUnavailable, true, retryCode);

                default:
                    return CloudOperationResult<SubmitGameResultResponse>.Failure(
                        CloudFailureKind.InvalidResponse,
                        false,
                        PlayerCloudConstants.ErrorCodes.InvalidResponse);
            }
        }

        private static void ValidateSnapshotContract(Generated.GameCenterSnapshotContract contract)
        {
            if (contract == null)
            {
                throw new FormatException("GameCenterSnapshotContract cannot be null.");
            }

            if (contract.SchemaVersion != PlayerDataSchema.CurrentVersion)
            {
                throw new FormatException($"Unsupported schema version: {contract.SchemaVersion}. Expected {PlayerDataSchema.CurrentVersion}.");
            }

            if (contract.Revision < 0 || contract.CreatedAtUnixSeconds < 0 || contract.UpdatedAtUnixSeconds < 0 || contract.BestWeekLinesCleared < 0)
            {
                throw new FormatException("Snapshot metrics and revision cannot be negative.");
            }

            if (contract.LifetimeStats == null || contract.CurrentWeek == null || contract.PreviousWeek == null)
            {
                throw new FormatException("Snapshot stats sections cannot be null.");
            }

            if (contract.LifetimeStats.GamesPlayed < 0 || contract.LifetimeStats.TotalScore < 0 || contract.LifetimeStats.BestScore < 0 ||
                contract.LifetimeStats.TotalLinesCleared < 0 || contract.LifetimeStats.TotalBlocksPlaced < 0 || contract.LifetimeStats.BestCombo < 0 ||
                contract.LifetimeStats.PersonalBestBreakCount < 0)
            {
                throw new FormatException("Lifetime stats metrics cannot be negative.");
            }

            if (contract.CurrentWeek.GamesPlayed < 0 || contract.CurrentWeek.TotalScore < 0 || contract.CurrentWeek.BestScore < 0 ||
                contract.CurrentWeek.TotalLinesCleared < 0 || contract.CurrentWeek.TotalBlocksPlaced < 0 || contract.CurrentWeek.PersonalBestBreakCount < 0 ||
                contract.CurrentWeek.WeekStartUnixSeconds < 0)
            {
                throw new FormatException("Current week stats metrics cannot be negative.");
            }

            if (contract.PreviousWeek.GamesPlayed < 0 || contract.PreviousWeek.TotalScore < 0 || contract.PreviousWeek.BestScore < 0 ||
                contract.PreviousWeek.TotalLinesCleared < 0 || contract.PreviousWeek.TotalBlocksPlaced < 0 || contract.PreviousWeek.PersonalBestBreakCount < 0 ||
                contract.PreviousWeek.WeekStartUnixSeconds < 0)
            {
                throw new FormatException("Previous week stats metrics cannot be negative.");
            }

            if (contract.RecentGames != null)
            {
                if (contract.RecentGames.Count > GameCenterSnapshot.MaxRecentGames)
                {
                    throw new FormatException($"Recent games count {contract.RecentGames.Count} exceeds max {GameCenterSnapshot.MaxRecentGames}.");
                }

                foreach (var r in contract.RecentGames)
                {
                    if (r == null || string.IsNullOrEmpty(r.GameSessionId) || r.Score < 0 || r.CompletedAtUnixSeconds < 0)
                    {
                        throw new FormatException("Recent game entry is invalid or contains negative metrics.");
                    }
                }
            }

            if (contract.Achievements != null)
            {
                var seenIds = new HashSet<string>();
                foreach (var a in contract.Achievements)
                {
                    if (a == null || string.IsNullOrEmpty(a.AchievementId))
                    {
                        throw new FormatException("Achievement entry is null or has empty ID.");
                    }

                    if (!seenIds.Add(a.AchievementId))
                    {
                        throw new FormatException($"Duplicate achievement ID detected: '{a.AchievementId}'.");
                    }

                    if (a.CurrentValue < 0 || a.TargetValue < 0 || a.UnlockedAtUnixSeconds < 0)
                    {
                        throw new FormatException($"Achievement '{a.AchievementId}' contains negative progress or timestamp.");
                    }
                }
            }

            if (contract.Insights != null)
            {
                if (contract.Insights.Count > GameCenterSnapshot.MaxInsights)
                {
                    throw new FormatException($"Insights count {contract.Insights.Count} exceeds max {GameCenterSnapshot.MaxInsights}.");
                }

                foreach (var i in contract.Insights)
                {
                    if (i == null || string.IsNullOrEmpty(i.InsightId))
                    {
                        throw new FormatException("Insight entry is null or has empty ID.");
                    }
                }
            }
        }
    }
}
