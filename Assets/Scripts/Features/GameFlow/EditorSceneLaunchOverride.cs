#if UNITY_EDITOR
using BlockBlast.Features.GameFlow.Navigation;

namespace BlockBlast.Features.GameFlow.EditorSupport
{
    public enum SceneQuickAccessProfile
    {
        FullFlow = 0,
        Lobby = 2,
        Play = 3
    }

    /// <summary>
    /// Editor-only profile rules shared by the runtime bootstrapper and editor tooling.
    /// The containing assembly strips this file from player builds.
    /// </summary>
    public static class EditorSceneLaunchOverride
    {
        public const string ProfilePreferenceKey = "BlockBlast.SceneQuickAccess.Profile";
        public const string BootstrapScenePath = "Assets/Scenes/BoostrapScene.unity";

        public static GameSceneRoute ResolveRoute(
            GameSceneRoute serializedRoute,
            SceneQuickAccessProfile profile)
        {
            return SanitizeProfile((int)profile) switch
            {
                SceneQuickAccessProfile.Lobby => GameSceneRoute.Lobby,
                SceneQuickAccessProfile.Play => GameSceneRoute.Play,
                _ => serializedRoute
            };
        }

        public static SceneQuickAccessProfile SanitizeProfile(int rawValue)
        {
            return rawValue switch
            {
                (int)SceneQuickAccessProfile.Lobby => SceneQuickAccessProfile.Lobby,
                (int)SceneQuickAccessProfile.Play => SceneQuickAccessProfile.Play,
                _ => SceneQuickAccessProfile.FullFlow
            };
        }
    }
}
#endif
