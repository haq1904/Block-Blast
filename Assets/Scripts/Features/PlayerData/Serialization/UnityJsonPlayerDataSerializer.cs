using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast.Features.PlayerData
{
    public sealed class UnityJsonPlayerDataSerializer : IPlayerDataSerializer
    {
        public string SerializeGameResult(GameResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            var dto = new GameResultDto
            {
                schemaVersion = result.SchemaVersion,
                gameSessionId = result.GameSessionId,
                score = result.Score,
                linesCleared = result.LinesCleared,
                blocksPlaced = result.BlocksPlaced,
                bestCombo = result.BestCombo,
                completedAtUnixSeconds = result.CompletedAtUnixSeconds
            };

            return JsonUtility.ToJson(dto);
        }

        public GameResult DeserializeGameResult(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            GameResultDto dto;
            try
            {
                dto = JsonUtility.FromJson<GameResultDto>(json);
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to deserialize GameResult from JSON.", ex);
            }

            if (dto == null || string.IsNullOrEmpty(dto.gameSessionId))
            {
                throw new FormatException("Deserialized GameResult is invalid or missing required fields.");
            }

            return new GameResult(
                dto.gameSessionId,
                dto.score,
                dto.linesCleared,
                dto.blocksPlaced,
                dto.bestCombo,
                dto.completedAtUnixSeconds,
                dto.schemaVersion);
        }

        public string SerializeSnapshot(GameCenterSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var dto = new GameCenterSnapshotDto
            {
                schemaVersion = snapshot.SchemaVersion,
                revision = snapshot.Revision,
                createdAtUnixSeconds = snapshot.CreatedAtUnixSeconds,
                updatedAtUnixSeconds = snapshot.UpdatedAtUnixSeconds,
                bestWeekLinesCleared = snapshot.BestWeekLinesCleared,
                lifetimeStats = ToDto(snapshot.LifetimeStats),
                currentWeek = ToDto(snapshot.CurrentWeek),
                previousWeek = ToDto(snapshot.PreviousWeek),
                recentGames = new List<RecentGameResultDto>(),
                achievements = new List<AchievementProgressDto>(),
                insights = new List<PlayerInsightDto>()
            };

            if (snapshot.RecentGames != null)
            {
                foreach (var r in snapshot.RecentGames)
                {
                    if (r != null)
                    {
                        dto.recentGames.Add(new RecentGameResultDto
                        {
                            gameSessionId = r.GameSessionId,
                            score = r.Score,
                            completedAtUnixSeconds = r.CompletedAtUnixSeconds
                        });
                    }
                }
            }

            if (snapshot.Achievements != null)
            {
                foreach (var a in snapshot.Achievements)
                {
                    if (a != null)
                    {
                        dto.achievements.Add(new AchievementProgressDto
                        {
                            achievementId = a.AchievementId,
                            currentValue = a.CurrentValue,
                            targetValue = a.TargetValue,
                            unlockedAtUnixSeconds = a.UnlockedAtUnixSeconds
                        });
                    }
                }
            }

            if (snapshot.Insights != null)
            {
                foreach (var i in snapshot.Insights)
                {
                    if (i != null)
                    {
                        dto.insights.Add(new PlayerInsightDto
                        {
                            insightId = i.InsightId,
                            currentValue = i.CurrentValue,
                            previousValue = i.PreviousValue
                        });
                    }
                }
            }

            return JsonUtility.ToJson(dto);
        }

        public GameCenterSnapshot DeserializeSnapshot(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            GameCenterSnapshotDto dto;
            try
            {
                dto = JsonUtility.FromJson<GameCenterSnapshotDto>(json);
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to deserialize GameCenterSnapshot from JSON.", ex);
            }

            if (dto == null)
            {
                throw new FormatException("Deserialized GameCenterSnapshot is null.");
            }

            var recentGames = new List<RecentGameResult>();
            if (dto.recentGames != null)
            {
                foreach (var r in dto.recentGames)
                {
                    if (r != null && !string.IsNullOrEmpty(r.gameSessionId))
                    {
                        recentGames.Add(new RecentGameResult(r.gameSessionId, r.score, r.completedAtUnixSeconds));
                    }
                }
            }

            var achievements = new List<AchievementProgress>();
            if (dto.achievements != null)
            {
                foreach (var a in dto.achievements)
                {
                    if (a != null && !string.IsNullOrEmpty(a.achievementId))
                    {
                        achievements.Add(new AchievementProgress(a.achievementId, a.currentValue, a.targetValue, a.unlockedAtUnixSeconds));
                    }
                }
            }

            var insights = new List<PlayerInsight>();
            if (dto.insights != null)
            {
                foreach (var i in dto.insights)
                {
                    if (i != null && !string.IsNullOrEmpty(i.insightId))
                    {
                        insights.Add(new PlayerInsight(i.insightId, i.currentValue, i.previousValue));
                    }
                }
            }

            return new GameCenterSnapshot(
                dto.revision,
                dto.createdAtUnixSeconds,
                dto.updatedAtUnixSeconds,
                FromDto(dto.lifetimeStats),
                FromDto(dto.currentWeek),
                FromDto(dto.previousWeek),
                dto.bestWeekLinesCleared,
                recentGames,
                achievements,
                insights,
                dto.schemaVersion);
        }

        private static PlayerStatsDto ToDto(PlayerStats stats)
        {
            if (stats == null) return new PlayerStatsDto();
            return new PlayerStatsDto
            {
                gamesPlayed = stats.GamesPlayed,
                totalScore = stats.TotalScore,
                bestScore = stats.BestScore,
                totalLinesCleared = stats.TotalLinesCleared,
                totalBlocksPlaced = stats.TotalBlocksPlaced,
                bestCombo = stats.BestCombo,
                personalBestBreakCount = stats.PersonalBestBreakCount
            };
        }

        private static PlayerStats FromDto(PlayerStatsDto dto)
        {
            if (dto == null) return new PlayerStats(0, 0, 0, 0, 0, 0, 0);
            return new PlayerStats(
                dto.gamesPlayed,
                dto.totalScore,
                dto.bestScore,
                dto.totalLinesCleared,
                dto.totalBlocksPlaced,
                dto.bestCombo,
                dto.personalBestBreakCount);
        }

        private static WeeklyPlayerStatsDto ToDto(WeeklyPlayerStats stats)
        {
            if (stats == null) return new WeeklyPlayerStatsDto();
            return new WeeklyPlayerStatsDto
            {
                weekStartUnixSeconds = stats.WeekStartUnixSeconds,
                gamesPlayed = stats.GamesPlayed,
                totalScore = stats.TotalScore,
                bestScore = stats.BestScore,
                totalLinesCleared = stats.TotalLinesCleared,
                totalBlocksPlaced = stats.TotalBlocksPlaced,
                personalBestBreakCount = stats.PersonalBestBreakCount
            };
        }

        private static WeeklyPlayerStats FromDto(WeeklyPlayerStatsDto dto)
        {
            if (dto == null) return new WeeklyPlayerStats(0, 0, 0, 0, 0, 0, 0);
            return new WeeklyPlayerStats(
                dto.weekStartUnixSeconds,
                dto.gamesPlayed,
                dto.totalScore,
                dto.bestScore,
                dto.totalLinesCleared,
                dto.totalBlocksPlaced,
                dto.personalBestBreakCount);
        }

        [Serializable]
        private class GameResultDto
        {
            public int schemaVersion;
            public string gameSessionId;
            public int score;
            public int linesCleared;
            public int blocksPlaced;
            public int bestCombo;
            public long completedAtUnixSeconds;
        }

        [Serializable]
        private class PlayerStatsDto
        {
            public long gamesPlayed;
            public long totalScore;
            public int bestScore;
            public long totalLinesCleared;
            public long totalBlocksPlaced;
            public int bestCombo;
            public long personalBestBreakCount;
        }

        [Serializable]
        private class WeeklyPlayerStatsDto
        {
            public long weekStartUnixSeconds;
            public long gamesPlayed;
            public long totalScore;
            public int bestScore;
            public long totalLinesCleared;
            public long totalBlocksPlaced;
            public long personalBestBreakCount;
        }

        [Serializable]
        private class RecentGameResultDto
        {
            public string gameSessionId;
            public int score;
            public long completedAtUnixSeconds;
        }

        [Serializable]
        private class AchievementProgressDto
        {
            public string achievementId;
            public long currentValue;
            public long targetValue;
            public long unlockedAtUnixSeconds;
        }

        [Serializable]
        private class PlayerInsightDto
        {
            public string insightId;
            public double currentValue;
            public double previousValue;
        }

        [Serializable]
        private class GameCenterSnapshotDto
        {
            public int schemaVersion;
            public long revision;
            public long createdAtUnixSeconds;
            public long updatedAtUnixSeconds;
            public PlayerStatsDto lifetimeStats;
            public WeeklyPlayerStatsDto currentWeek;
            public WeeklyPlayerStatsDto previousWeek;
            public long bestWeekLinesCleared;
            public List<RecentGameResultDto> recentGames;
            public List<AchievementProgressDto> achievements;
            public List<PlayerInsightDto> insights;
        }
    }
}
