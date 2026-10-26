using CubePuzzle3DPlayerData.Contracts;
using CubePuzzle3DPlayerData.Domain;
using NUnit.Framework;

namespace CubePuzzle3DPlayerData.Tests;

[TestFixture]
public class GameResultValidatorTest
{
    private const string ValidPlayerId = "player_abc_123";
    private const long ServerNow = 1761465600L; // 2025-10-26 08:00:00 UTC

    private static GameResultRequest CreateValidRequest()
    {
        return new GameResultRequest
        {
            SchemaVersion = 1,
            GameSessionId = "session_001_valid",
            Score = 1500,
            LinesCleared = 5,
            BlocksPlaced = 12,
            BestCombo = 2,
            CompletedAtUnixSeconds = ServerNow - 10
        };
    }

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var req = CreateValidRequest();
        var result = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorCode, Is.Null);
    }

    [Test]
    public void Validate_NullOrWhitespacePlayerId_ReturnsUnauthenticated()
    {
        var req = CreateValidRequest();
        var res1 = GameResultValidator.Validate(null, req, ServerNow);
        var res2 = GameResultValidator.Validate("", req, ServerNow);
        var res3 = GameResultValidator.Validate("   ", req, ServerNow);

        Assert.That(res1.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.Unauthenticated));
        Assert.That(res2.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.Unauthenticated));
        Assert.That(res3.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.Unauthenticated));
    }

    [Test]
    public void Validate_NullRequest_ReturnsInvalidPayload()
    {
        var res = GameResultValidator.Validate(ValidPlayerId, null, ServerNow);
        Assert.That(res.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.InvalidPayload));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(-1)]
    public void Validate_SchemaMismatch_ReturnsSchemaMismatch(int schemaVersion)
    {
        var req = CreateValidRequest();
        req.SchemaVersion = schemaVersion;
        var res = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);

        Assert.That(res.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.SchemaMismatch));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("short")] // < 8 chars
    [TestCase("session with spaces")]
    [TestCase("session$special#")]
    [TestCase("session@123")]
    public void Validate_InvalidSessionId_ReturnsInvalidSessionId(string? sessionId)
    {
        var req = CreateValidRequest();
        req.GameSessionId = sessionId!;
        var res = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);

        Assert.That(res.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.InvalidSessionId));
    }

    [Test]
    public void Validate_SessionIdBoundaryLengths_Succeeds()
    {
        var req = CreateValidRequest();
        req.GameSessionId = new string('a', 8); // exactly 8
        var res1 = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res1.IsValid, Is.True);

        req.GameSessionId = new string('z', 64); // exactly 64
        var res2 = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res2.IsValid, Is.True);

        req.GameSessionId = new string('z', 65); // 65 too long
        var res3 = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res3.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.InvalidSessionId));
    }

    [TestCase(-1, 0, 0, 0)]
    [TestCase(0, -1, 0, 0)]
    [TestCase(0, 0, -1, 0)]
    [TestCase(0, 0, 0, -1)]
    [TestCase(2_000_000_001, 10, 10, 2)] // Score > 2B
    [TestCase(100, 10_000_001, 10, 2)] // Lines > 10M
    [TestCase(100, 10, 10_000_001, 2)] // Blocks > 10M
    [TestCase(100, 10, 10, 10_000_001)] // Combo > 10M
    public void Validate_MetricOutOfRange_ReturnsMetricOutOfRange(int score, int lines, int blocks, int combo)
    {
        var req = CreateValidRequest();
        req.Score = score;
        req.LinesCleared = lines;
        req.BlocksPlaced = blocks;
        req.BestCombo = combo;

        var res = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.MetricOutOfRange));
    }

    [Test]
    public void Validate_BestComboGreaterThanLinesCleared_ReturnsInvalidMetricRelation()
    {
        var req = CreateValidRequest();
        req.LinesCleared = 2;
        req.BestCombo = 3; // bestCombo > linesCleared
        var res = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);

        Assert.That(res.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.InvalidMetricRelation));
    }

    [Test]
    public void Validate_ZeroBlocksPlacedWithNonZeroMetrics_ReturnsInvalidMetricRelation()
    {
        var req = CreateValidRequest();
        req.BlocksPlaced = 0;
        req.Score = 100;
        req.LinesCleared = 0;
        req.BestCombo = 0;
        var res1 = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res1.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.InvalidMetricRelation));

        req.Score = 0;
        req.LinesCleared = 1;
        var res2 = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res2.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.InvalidMetricRelation));

        // When blocks placed is 0 and everything is 0, it is valid (empty game)
        req.LinesCleared = 0;
        req.Score = 0;
        req.BestCombo = 0;
        var res3 = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(res3.IsValid, Is.True);
    }

    [Test]
    public void Validate_TimestampBoundaries_TestedCorrectly()
    {
        var req = CreateValidRequest();

        // 365 days ago exactly -> valid
        req.CompletedAtUnixSeconds = ServerNow - PlayerDataConstants.AllowedPastSeconds;
        var resPastOk = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(resPastOk.IsValid, Is.True);

        // 365 days + 1 second ago -> invalid
        req.CompletedAtUnixSeconds = ServerNow - PlayerDataConstants.AllowedPastSeconds - 1;
        var resPastFail = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(resPastFail.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.TimestampOutOfRange));

        // 5 minutes in future exactly -> valid
        req.CompletedAtUnixSeconds = ServerNow + PlayerDataConstants.AllowedFutureSeconds;
        var resFutureOk = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(resFutureOk.IsValid, Is.True);

        // 5 minutes + 1 second in future -> invalid
        req.CompletedAtUnixSeconds = ServerNow + PlayerDataConstants.AllowedFutureSeconds + 1;
        var resFutureFail = GameResultValidator.Validate(ValidPlayerId, req, ServerNow);
        Assert.That(resFutureFail.ErrorCode, Is.EqualTo(PlayerDataConstants.RejectionCodes.TimestampOutOfRange));
    }
}
