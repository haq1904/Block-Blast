#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.EditorSupport
{
    /// <summary>
    /// Applies the locally selected scene launch profile without opening or saving scenes.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapSceneAutoLoader
    {
        private static bool _invalidProfileWarningLogged;

        static BootstrapSceneAutoLoader()
        {
            ApplySelectedProfileAndReportErrors();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static SceneQuickAccessProfile ReadProfile()
        {
            int rawProfile = EditorPrefs.GetInt(
                EditorSceneLaunchOverride.ProfilePreferenceKey,
                (int)SceneQuickAccessProfile.FullFlow);
            SceneQuickAccessProfile sanitizedProfile = EditorSceneLaunchOverride.SanitizeProfile(rawProfile);

            if (rawProfile != (int)sanitizedProfile)
            {
                EditorPrefs.SetInt(
                    EditorSceneLaunchOverride.ProfilePreferenceKey,
                    (int)SceneQuickAccessProfile.FullFlow);

                if (!_invalidProfileWarningLogged)
                {
                    Debug.LogWarning(
                        $"[Scene Quick Access] Unknown profile value {rawProfile}; reset to Full Flow.");
                    _invalidProfileWarningLogged = true;
                }
            }

            return sanitizedProfile;
        }

        public static bool SelectProfile(SceneQuickAccessProfile profile, out string error)
        {
            SceneQuickAccessProfile sanitizedProfile =
                EditorSceneLaunchOverride.SanitizeProfile((int)profile);
            EditorPrefs.SetInt(
                EditorSceneLaunchOverride.ProfilePreferenceKey,
                (int)sanitizedProfile);
            return TryApplySelectedProfile(out error);
        }

        public static bool TryApplySelectedProfile(out string error)
        {
            ReadProfile();
            return TrySetPlayModeStartScene(
                EditorSceneLaunchOverride.BootstrapScenePath,
                out error);
        }

        public static void ResetToFullFlow()
        {
            EditorPrefs.SetInt(
                EditorSceneLaunchOverride.ProfilePreferenceKey,
                (int)SceneQuickAccessProfile.FullFlow);
            TrySetPlayModeStartScene(EditorSceneLaunchOverride.BootstrapScenePath, out _);
        }

        private static bool TrySetPlayModeStartScene(string scenePath, out string error)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                error = $"Required scene asset not found at path: {scenePath}";
                return false;
            }

            EditorSceneManager.playModeStartScene = sceneAsset;
            error = null;
            return true;
        }

        private static void ApplySelectedProfileAndReportErrors()
        {
            if (!TryApplySelectedProfile(out string error))
            {
                Debug.LogWarning($"[Scene Quick Access] {error} Falling back to Full Flow.");
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                ApplySelectedProfileAndReportErrors();
            }
        }
    }
}
#endif
