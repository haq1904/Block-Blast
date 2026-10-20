#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.EditorSupport
{
    public sealed class SceneQuickAccessWindow : EditorWindow
    {
        private SceneQuickAccessProfile _profile;
        private string _statusMessage;
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/Block Blast/Scene Quick Access")]
        private static void OpenWindow()
        {
            GetWindow<SceneQuickAccessWindow>("Scene Quick Access");
        }

        private void OnEnable()
        {
            _profile = BootstrapSceneAutoLoader.ReadProfile();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Play Mode Launch Profile", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            _profile = DrawProfilePopup(_profile);

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(GetProfileDescription(_profile), MessageType.Info);
            EditorGUILayout.LabelField(
                "Effective start",
                EditorSceneLaunchOverride.BootstrapScenePath,
                EditorStyles.wordWrappedLabel);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply"))
                {
                    ApplySelection(false);
                }

                using (new EditorGUI.DisabledScope(!CanApplyAndPlay()))
                {
                    if (GUILayout.Button("Apply & Play"))
                    {
                        ApplySelection(true);
                    }
                }
            }

            if (GUILayout.Button("Reset to Full Flow"))
            {
                _profile = SceneQuickAccessProfile.FullFlow;
                ApplySelection(false);
            }
        }

        private static SceneQuickAccessProfile DrawProfilePopup(SceneQuickAccessProfile profile)
        {
            string[] labels =
            {
                "Full Flow",
                "Lobby",
                "Play"
            };
            SceneQuickAccessProfile[] profiles =
            {
                SceneQuickAccessProfile.FullFlow,
                SceneQuickAccessProfile.Lobby,
                SceneQuickAccessProfile.Play
            };
            int selectedIndex = System.Array.IndexOf(profiles, profile);
            selectedIndex = EditorGUILayout.Popup("Profile", Mathf.Max(0, selectedIndex), labels);
            return profiles[selectedIndex];
        }

        private static string GetProfileDescription(SceneQuickAccessProfile profile)
        {
            return profile switch
            {
                SceneQuickAccessProfile.FullFlow => "Uses the Bootstrapper's serialized route (the normal player flow).",
                SceneQuickAccessProfile.Lobby => "Starts through Bootstrap and composes World + Lobby.",
                SceneQuickAccessProfile.Play => "Starts through Bootstrap and composes World + Play.",
                _ => "Uses the normal player flow."
            };
        }

        private static bool CanApplyAndPlay()
        {
            return !EditorApplication.isCompiling
                && !EditorApplication.isUpdating
                && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private void ApplySelection(bool enterPlayMode)
        {
            bool applied = BootstrapSceneAutoLoader.SelectProfile(
                _profile,
                out string error);

            if (!applied)
            {
                _profile = SceneQuickAccessProfile.FullFlow;
                _statusMessage = $"{error} Reset to Full Flow.";
                _statusType = MessageType.Error;
                Repaint();
                return;
            }

            _statusMessage = $"Applied {_profile}.";
            _statusType = MessageType.Info;

            if (enterPlayMode)
            {
                EditorApplication.isPlaying = true;
            }
        }
    }
}
#endif
