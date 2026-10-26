using System;
using System.Collections.Generic;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Mapping;
using NUnit.Framework;
using Generated = Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class PlayerCloudContractMapperTest
    {
        [Test]
        public void ToGeneratedRequest_MapsAllSevenFieldsAccurately()
        {
            var result = new GameResult("session_123", 1500, 12, 45, 4, 1700000000L);

            var request = PlayerCloudContractMapper.ToGeneratedRequest(result);

            Assert.AreEqual(PlayerDataSchema.CurrentVersion, request.SchemaVersion);
            Assert.AreEqual("session_123", request.GameSessionId);
            Assert.AreEqual(1500, request.Score);
            Assert.AreEqual(12, request.LinesCleared);
            Assert.AreEqual(45, request.BlocksPlaced);
            Assert.AreEqual(4, request.BestCombo);
            Assert.AreEqual(1700000000L, request.CompletedAtUnixSeconds);
        }

        [Test]
        public void CreateEmptySnapshot_ContainsAllEighteenAchievementsWithZeroProgress()
        {
            var empty = PlayerCloudContractMapper.CreateEmptySnapshot();

            Assert.AreEqual(PlayerDataSchema.CurrentVersion, empty.SchemaVersion);
            Assert.AreEqual(0L, empty.Revision);
            Assert.AreEqual(0L, empty.CreatedAtUnixSeconds);
            Assert.AreEqual(0L, empty.UpdatedAtUnixSeconds);
            Assert.AreEqual(0L, empty.LifetimeStats.GamesPlayed);
            Assert.AreEqual(0, empty.RecentGames.Count);
            Assert.AreEqual(0, empty.Insights.Count);

            Assert.AreEqual(AchievementCatalog.Definitions.Count, empty.Achievements.Count);
            foreach (var ach in empty.Achievements)
            {
                Assert.IsTrue(AchievementCatalog.Definitions.ContainsKey(ach.AchievementId));
                Assert.AreEqual(0L, ach.CurrentValue);
                Assert.AreEqual(AchievementCatalog.Definitions[ach.AchievementId], ach.TargetValue);
                Assert.IsFalse(ach.IsUnlocked);
            }
        }

        [Test]
        public void ToDomainSnapshot_PreservesUnknownFutureAchievementIds()
        {
            var contract = CreateValidContract();
            contract.Achievements.Add(new Generated.AchievementProgressContract
            {
                AchievementId = "future_expansion_pack_ach",
                CurrentValue = 50L,
                TargetValue = 100L,
                UnlockedAtUnixSeconds = 0L
            });

            var domain = PlayerCloudContractMapper.ToDomainSnapshot(contract);

            Assert.IsNotNull(domain);
            bool foundFuture = false;
            foreach (var a in domain.Achievements)
            {
                if (a.AchievementId == "future_expansion_pack_ach")
                {
                    foundFuture = true;
                    Assert.AreEqual(50L, a.CurrentValue);
                    Assert.AreEqual(100L, a.TargetValue);
                }
            }
            Assert.IsTrue(foundFuture);
        }

        [Test]
        public void ToDomainSnapshot_ThrowsOnDuplicateAchievementIds()
        {
            var contract = CreateValidContract();
            contract.Achievements.Add(new Generated.AchievementProgressContract
            {
                AchievementId = AchievementCatalog.FirstSteps,
                CurrentValue = 1L,
                TargetValue = 5L,
                UnlockedAtUnixSeconds = 0L
            });
            contract.Achievements.Add(new Generated.AchievementProgressContract
            {
                AchievementId = AchievementCatalog.FirstSteps,
                CurrentValue = 2L,
                TargetValue = 5L,
                UnlockedAtUnixSeconds = 0L
            });

            Assert.Throws<FormatException>(() => PlayerCloudContractMapper.ToDomainSnapshot(contract));
        }

        [Test]
        public void ToDomainSnapshot_ThrowsOnUnsupportedSchemaVersion()
        {
            var contract = CreateValidContract();
            contract.SchemaVersion = 999;

            Assert.Throws<FormatException>(() => PlayerCloudContractMapper.ToDomainSnapshot(contract));
        }

        [Test]
        public void MapServerStatusToResult_Accepted_ReturnsSuccess()
        {
            var responseDto = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Accepted,
                UpdatedSnapshot = CreateValidContract(),
                NewlyUnlockedAchievementIds = new List<string> { AchievementCatalog.FirstSteps },
                IsNewPersonalBest = true,
                IsDuplicate = false
            };

            var result = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.None, result.FailureKind);
            Assert.IsNotNull(result.Value.UpdatedSnapshot);
            Assert.IsTrue(result.Value.IsNewPersonalBest);
            Assert.IsFalse(result.Value.IsDuplicate);
            Assert.AreEqual(1, result.Value.NewlyUnlockedAchievementIds.Count);
        }

        [Test]
        public void MapServerStatusToResult_Duplicate_ReturnsSuccessWithDuplicateFlag()
        {
            var responseDto = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Duplicate,
                UpdatedSnapshot = CreateValidContract(),
                NewlyUnlockedAchievementIds = new List<string>(),
                IsNewPersonalBest = false,
                IsDuplicate = true
            };

            var result = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value.IsDuplicate);
        }

        [Test]
        public void MapServerStatusToResult_Rejected_SchemaMismatch_MapsToSchemaMismatchNonRetryable()
        {
            var responseDto = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Rejected,
                ErrorCode = "SCHEMA_MISMATCH"
            };

            var result = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.SchemaMismatch, result.FailureKind);
            Assert.IsFalse(result.IsRetryable);
            Assert.AreEqual("SCHEMA_MISMATCH", result.ErrorCode);
        }

        [Test]
        public void MapServerStatusToResult_Rejected_Unauthenticated_MapsToAuthUnavailableNonRetryable()
        {
            var responseDto = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Rejected,
                ErrorCode = "UNAUTHENTICATED"
            };

            var result = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.AuthenticationUnavailable, result.FailureKind);
            Assert.IsFalse(result.IsRetryable);
            Assert.AreEqual("UNAUTHENTICATED", result.ErrorCode);
        }

        [Test]
        public void MapServerStatusToResult_Rejected_OtherCode_MapsToInvalidPayloadNonRetryable()
        {
            var responseDto = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Rejected,
                ErrorCode = "INVALID_SESSION_ID"
            };

            var result = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.InvalidPayload, result.FailureKind);
            Assert.IsFalse(result.IsRetryable);
            Assert.AreEqual("INVALID_SESSION_ID", result.ErrorCode);
        }

        [Test]
        public void MapServerStatusToResult_RetryableFailure_MapsToServiceUnavailableRetryable()
        {
            var responseDto = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.RetryableFailure,
                ErrorCode = "WRITE_CONFLICT_RETRY_EXHAUSTED"
            };

            var result = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.ServiceUnavailable, result.FailureKind);
            Assert.IsTrue(result.IsRetryable);
            Assert.AreEqual("WRITE_CONFLICT_RETRY_EXHAUSTED", result.ErrorCode);
        }

        [Test]
        public void MapServerStatusToResult_NullResponseOrMalformedSnapshot_MapsToInvalidResponse()
        {
            var nullResult = PlayerCloudContractMapper.MapServerStatusToResult(null);
            Assert.IsFalse(nullResult.IsSuccess);
            Assert.AreEqual(CloudFailureKind.InvalidResponse, nullResult.FailureKind);

            var malformedResult = PlayerCloudContractMapper.MapServerStatusToResult(new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Accepted,
                UpdatedSnapshot = null
            });
            Assert.IsFalse(malformedResult.IsSuccess);
            Assert.AreEqual(CloudFailureKind.InvalidResponse, malformedResult.FailureKind);
        }

        private static Generated.GameCenterSnapshotContract CreateValidContract()
        {
            return new Generated.GameCenterSnapshotContract
            {
                SchemaVersion = PlayerDataSchema.CurrentVersion,
                Revision = 1L,
                CreatedAtUnixSeconds = 1000L,
                UpdatedAtUnixSeconds = 1000L,
                LifetimeStats = new Generated.PlayerStatsContract
                {
                    GamesPlayed = 1L,
                    TotalScore = 100L,
                    BestScore = 100,
                    TotalLinesCleared = 2L,
                    TotalBlocksPlaced = 5L,
                    BestCombo = 1,
                    PersonalBestBreakCount = 1L
                },
                CurrentWeek = new Generated.WeeklyPlayerStatsContract
                {
                    WeekStartUnixSeconds = 1000L,
                    GamesPlayed = 1L,
                    TotalScore = 100L,
                    BestScore = 100,
                    TotalLinesCleared = 2L,
                    TotalBlocksPlaced = 5L,
                    PersonalBestBreakCount = 1L
                },
                PreviousWeek = new Generated.WeeklyPlayerStatsContract
                {
                    WeekStartUnixSeconds = 500L,
                    GamesPlayed = 0L,
                    TotalScore = 0L,
                    BestScore = 0,
                    TotalLinesCleared = 0L,
                    TotalBlocksPlaced = 0L,
                    PersonalBestBreakCount = 0L
                },
                BestWeekLinesCleared = 2L,
                RecentGames = new List<Generated.RecentGameResultContract>
                {
                    new Generated.RecentGameResultContract
                    {
                        GameSessionId = "session_1",
                        Score = 100,
                        CompletedAtUnixSeconds = 1000L
                    }
                },
                Achievements = new List<Generated.AchievementProgressContract>(),
                Insights = new List<Generated.PlayerInsightContract>()
            };
        }
    }
}
