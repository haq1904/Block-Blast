using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerCloud.Gateway;
using NUnit.Framework;
using Unity.Services.Authentication;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using GeneratedContracts = Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class PlayerCloudAccessControlIntegrationTest
    {
        private const string EnvironmentName = "development";

        [Test]
        [Explicit("Mutates UGS development and requires deployed access policy.")]
        public async Task DevelopmentPolicy_DeniesDirectWrite_AndPreservesTrustedFlow()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                Assert.Ignore("Unity Services initialization requires PlayMode or one-shot runner.");
                return;
            }

            var options = new InitializationOptions();
            options.SetEnvironmentName(EnvironmentName);

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Assert.IsTrue(AuthenticationService.Instance.IsSignedIn, "Player must be authenticated anonymously.");

            // 1. Negative probe: direct Default Player Data SaveAsync must fail (HTTP 403 / Access Control Deny)
            string probeKey = $"access_control_probe_{Guid.NewGuid():N}";
            var probePayload = new Dictionary<string, object>
            {
                { probeKey, "probe_denied_value" }
            };

            bool directWriteFailed = false;
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(probePayload);
            }
            catch (CloudSaveException ex)
            {
                directWriteFailed = true;
                Assert.IsNotNull(ex, "Expected CloudSaveException on denied write.");
            }
            catch (Exception)
            {
                directWriteFailed = true;
            }

            Assert.IsTrue(directWriteFailed, "Direct Default Player Data write by Player must be denied by access control policy.");

            // 2. Positive probe: SubmitGameResult through generated Cloud Code binding must succeed
            var bindings = new PlayerDataModuleBindings();
            var submitRequest = new GeneratedContracts.GameResultRequest
            {
                SchemaVersion = 1,
                GameSessionId = Guid.NewGuid().ToString(),
                Score = 100,
                LinesCleared = 2,
                BlocksPlaced = 10,
                BestCombo = 1,
                CompletedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var submitResponse = await bindings.SubmitGameResult(submitRequest);
            Assert.IsNotNull(submitResponse, "Cloud Code module call response must not be null.");
            Assert.IsTrue(
                submitResponse.Status == GeneratedContracts.SubmitGameResultStatus.Accepted ||
                submitResponse.Status == GeneratedContracts.SubmitGameResultStatus.Duplicate,
                $"Unexpected submission status: {submitResponse.Status}"
            );

            // 3. Positive probe: Protected read of game_center_v1 must succeed
            var protectedGateway = new UnityProtectedPlayerDataGateway();
            var readResult = await protectedGateway.LoadProtectedSnapshotJsonAsync();
            Assert.IsTrue(readResult.Found, "Protected player data key 'game_center_v1' must be readable by Player.");
            Assert.IsFalse(string.IsNullOrEmpty(readResult.JsonValue), "Protected snapshot JSON must not be empty.");
        }
    }
}
