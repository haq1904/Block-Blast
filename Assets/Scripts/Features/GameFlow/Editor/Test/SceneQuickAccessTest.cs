#if UNITY_EDITOR
using BlockBlast.Features.GameFlow.Navigation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BlockBlast.Features.GameFlow.EditorSupport.Tests
{
    public sealed class SceneQuickAccessTest
    {
        private bool _hadProfilePreference;
        private int _profilePreference;
        private SceneAsset _playModeStartScene;

        [SetUp]
        public void SetUp()
        {
            _hadProfilePreference = EditorPrefs.HasKey(EditorSceneLaunchOverride.ProfilePreferenceKey);
            _profilePreference = EditorPrefs.GetInt(EditorSceneLaunchOverride.ProfilePreferenceKey);
            _playModeStartScene = EditorSceneManager.playModeStartScene;
        }

        [TearDown]
        public void TearDown()
        {
            RestoreIntPreference(
                EditorSceneLaunchOverride.ProfilePreferenceKey,
                _hadProfilePreference,
                _profilePreference);
            EditorSceneManager.playModeStartScene = _playModeStartScene;
        }

        [TestCase(SceneQuickAccessProfile.FullFlow, GameSceneRoute.Intro, GameSceneRoute.Intro)]
        [TestCase(SceneQuickAccessProfile.Lobby, GameSceneRoute.Intro, GameSceneRoute.Lobby)]
        [TestCase(SceneQuickAccessProfile.Play, GameSceneRoute.Intro, GameSceneRoute.Play)]
        public void ResolveRoute_ReturnsExpectedRoute(
            SceneQuickAccessProfile profile,
            GameSceneRoute serializedRoute,
            GameSceneRoute expectedRoute)
        {
            Assert.AreEqual(
                expectedRoute,
                EditorSceneLaunchOverride.ResolveRoute(serializedRoute, profile));
        }

        [Test]
        public void SanitizeProfile_InvalidValueFallsBackToFullFlow()
        {
            Assert.AreEqual(
                SceneQuickAccessProfile.FullFlow,
                EditorSceneLaunchOverride.SanitizeProfile(999));
        }

        [Test]
        public void ReadProfile_InvalidPreferenceRepairsToFullFlow()
        {
            EditorPrefs.SetInt(EditorSceneLaunchOverride.ProfilePreferenceKey, 999);

            Assert.AreEqual(SceneQuickAccessProfile.FullFlow, BootstrapSceneAutoLoader.ReadProfile());
            Assert.AreEqual(
                (int)SceneQuickAccessProfile.FullFlow,
                EditorPrefs.GetInt(EditorSceneLaunchOverride.ProfilePreferenceKey));
        }

        [Test]
        public void ApplyManagedProfiles_UseBootstrapStartScene()
        {
            foreach (SceneQuickAccessProfile profile in new[]
                     {
                         SceneQuickAccessProfile.FullFlow,
                         SceneQuickAccessProfile.Lobby,
                         SceneQuickAccessProfile.Play
                     })
            {
                Assert.IsTrue(BootstrapSceneAutoLoader.SelectProfile(profile, out string error), error);
                Assert.AreEqual(
                    EditorSceneLaunchOverride.BootstrapScenePath,
                    AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            }
        }

        private static void RestoreIntPreference(string key, bool hadValue, int value)
        {
            if (hadValue)
            {
                EditorPrefs.SetInt(key, value);
            }
            else
            {
                EditorPrefs.DeleteKey(key);
            }
        }

    }
}
#endif
