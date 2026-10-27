#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockBlast.Features.Lobby.Editor
{
    public static class LobbyGameCenterPanelBuilder
    {
        private const string GameCenterPanelName = "GameCenterPanel";
        private const string MainPanelName = "MainPanel_PlayerCenter";

        private static readonly string[] RequiredSpritePaths =
        {
            "Assets/Art/UI/CozyStationeryUI/Components/Panels/panel_player_center_main.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Buttons/btn_square_coral_normal.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Buttons/btn_square_coral_pressed.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Buttons/btn_square_coral_disabled.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Panels/panel_section_tab_coral.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Icons/icon_back_cream.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Icons/icon_line_cleaner.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Controls/badge_completed_circle.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Controls/progress_empty.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Controls/progress_fill_green.png",
            "Assets/Art/UI/CozyStationeryUI/Components/Controls/progress_fill_blue.png"
        };

        [MenuItem("BlockBlast/Validate Player Center Assets")]
        public static void ValidateApprovedPlayerCenterAssets()
        {
            var activeScene = SceneManager.GetActiveScene();
            var failures = new List<string>();

            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                failures.Add("No loaded active scene is available for Player Center validation.");
            }
            else
            {
                var gameCenterPanel = FindInScene(activeScene, GameCenterPanelName);
                if (gameCenterPanel == null)
                {
                    failures.Add($"'{GameCenterPanelName}' was not found in the active scene.");
                }
                else if (FindInChildren(gameCenterPanel.transform, MainPanelName) == null)
                {
                    failures.Add($"'{MainPanelName}' was not found under '{GameCenterPanelName}'.");
                }
            }

            foreach (var spritePath in RequiredSpritePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) == null)
                {
                    failures.Add($"Required Sprite is missing or not imported as Sprite: {spritePath}");
                }
            }

            if (failures.Count > 0)
            {
                Debug.LogError($"[PlayerCenterAssetValidator] Validation failed:\n- {string.Join("\n- ", failures)}");
                return;
            }

            Debug.Log("[PlayerCenterAssetValidator] Approved Player Center assets and existing layout validated. No scene objects were changed.");
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }

                var child = FindInChildren(root.transform, objectName);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildren(Transform root, string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
#endif
