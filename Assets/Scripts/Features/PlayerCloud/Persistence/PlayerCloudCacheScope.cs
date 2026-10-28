using System;
using System.IO;
using System.Text;

namespace BlockBlast.Features.PlayerCloud.Persistence
{
    /// <summary>
    /// Represents an isolated persistence scope defined by environment name and player ID.
    /// </summary>
    public readonly struct PlayerCloudCacheScope : IEquatable<PlayerCloudCacheScope>
    {
        public string EnvironmentName { get; }
        public string PlayerId { get; }

        public PlayerCloudCacheScope(string environmentName, string playerId)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                throw new ArgumentException("EnvironmentName cannot be null, empty, or whitespace.", nameof(environmentName));
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("PlayerId cannot be null, empty, or whitespace.", nameof(playerId));
            }

            EnvironmentName = environmentName.Trim();
            PlayerId = playerId.Trim();
        }

        public string GetRelativeDirectoryPath()
        {
            string safeEnv = SanitizeSegment(EnvironmentName);
            string safeId = SanitizeSegment(PlayerId);
            return Path.Combine(safeEnv, safeId);
        }

        public static string SanitizeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) return "unknown";

            var sb = new StringBuilder(segment.Length);
            char[] invalidChars = Path.GetInvalidFileNameChars();

            for (int i = 0; i < segment.Length; i++)
            {
                char c = segment[i];
                if (c == '.' || c == '/' || c == '\\' || Array.IndexOf(invalidChars, c) >= 0)
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "unknown" : result;
        }

        public bool Equals(PlayerCloudCacheScope other)
        {
            return string.Equals(EnvironmentName, other.EnvironmentName, StringComparison.Ordinal)
                && string.Equals(PlayerId, other.PlayerId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerCloudCacheScope other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (EnvironmentName != null ? StringComparer.Ordinal.GetHashCode(EnvironmentName) : 0);
                hash = hash * 31 + (PlayerId != null ? StringComparer.Ordinal.GetHashCode(PlayerId) : 0);
                return hash;
            }
        }

        public static bool operator ==(PlayerCloudCacheScope left, PlayerCloudCacheScope right) => left.Equals(right);
        public static bool operator !=(PlayerCloudCacheScope left, PlayerCloudCacheScope right) => !left.Equals(right);

        public override string ToString() => $"[Scope: Env={EnvironmentName}, PlayerId={PlayerId}]";
    }
}
