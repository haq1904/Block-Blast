using System;
using System.Collections.Generic;
using BlockBlast.Features.Lobby.GameCenter;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.GameFlow.Editor
{
    public static class GameCenterSceneBinder
    {
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";

        [MenuItem("BlockBlast/Setup GameCenter Scene Wiring")]
        public static string BindLobbyScene()
        {
            var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            Transform panel = null;
            var queue = new Queue<Transform>();
            foreach (var r in roots) queue.Enqueue(r.transform);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.name == "GameCenterPanel")
                {
                    panel = cur;
                    break;
                }
                for (int i = 0; i < cur.childCount; i++) queue.Enqueue(cur.GetChild(i));
            }

            if (panel == null)
            {
                return "ERROR: GameCenterPanel not found";
            }

            var view = panel.GetComponent<LobbyGameCenterPanelView>();
            if (view == null)
            {
                view = panel.gameObject.AddComponent<LobbyGameCenterPanelView>();
            }

            var controller = panel.GetComponent<LobbyGameCenterDataController>();
            if (controller == null)
            {
                controller = panel.gameObject.AddComponent<LobbyGameCenterDataController>();
            }

            var headerRow = FindDeep(panel, "HeaderRow");
            var backButton = FindDeep(panel, "Button_Back_Placeholder")?.GetComponent<Button>();
            var dimmerButton = FindDeep(panel, "Dimmer_FullBleed")?.GetComponent<Button>();

            TMP_Text statusText = null;
            if (headerRow != null)
            {
                var statusTr = headerRow.Find("Text_Status");
                if (statusTr == null)
                {
                    var statusGo = new GameObject("Text_Status", typeof(RectTransform), typeof(TextMeshProUGUI));
                    statusGo.transform.SetParent(headerRow, false);
                    var rt = statusGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(25f, 0f);
                    rt.sizeDelta = new Vector2(120f, 30f);

                    statusText = statusGo.GetComponent<TextMeshProUGUI>();
                    statusText.fontSize = 12f;
                    statusText.alignment = TextAlignmentOptions.Left;
                    statusText.color = new Color(1f, 1f, 1f, 0.6f);

                    var sampleText = FindDeep(headerRow, "Text_Title")?.GetComponent<TMP_Text>();
                    if (sampleText != null && sampleText.font != null)
                    {
                        statusText.font = sampleText.font;
                    }
                }
                else
                {
                    statusText = statusTr.GetComponent<TMP_Text>();
                }
            }

            var bestScoreText = FindDeep(panel, "Text_BestScore")?.GetComponent<TMP_Text>();
            var statsRowText = FindDeep(panel, "StatsRow")?.GetComponent<TMP_Text>();

            var chipPb = FindDeep(panel, "Chip_PersonalBest");
            var chipPbText = chipPb != null ? FindDeep(chipPb, "Text")?.GetComponent<TMP_Text>() : null;

            var chipAvg = FindDeep(panel, "Chip_AvgScore");
            var chipAvgText = chipAvg != null ? FindDeep(chipAvg, "Text")?.GetComponent<TMP_Text>() : null;

            var cardThisWeek = FindDeep(panel, "Card_ThisWeekScore");
            var cardThisWeekText = cardThisWeek != null ? FindDeep(cardThisWeek, "Text")?.GetComponent<TMP_Text>() : null;

            var cardLastWeek = FindDeep(panel, "Card_LastWeekScore");
            var cardLastWeekText = cardLastWeek != null ? FindDeep(cardLastWeek, "Text")?.GetComponent<TMP_Text>() : null;

            var recentBars = new RecentGameBarBinding[7];
            for (int i = 0; i < 7; i++)
            {
                var barTr = FindDeep(panel, $"Bar_Game_{i + 1}");
                if (barTr != null)
                {
                    var scoreText = FindDeep(barTr, "Text_Score")?.GetComponent<TMP_Text>();
                    var fillRect = FindDeep(barTr, "Fill")?.GetComponent<RectTransform>();
                    recentBars[i] = new RecentGameBarBinding(scoreText, fillRect);
                }
            }

            var achievementRows = new AchievementRowBinding[3];
            string[] rowNames = { "Row_ScoreMaster", "Row_ComboCollector", "Row_LineCleanerIII" };
            for (int i = 0; i < 3; i++)
            {
                var rowTr = FindDeep(panel, rowNames[i]);
                if (rowTr != null)
                {
                    var titleText = FindDeep(rowTr, "Title")?.GetComponent<TMP_Text>();
                    var progText = FindDeep(rowTr, "ProgressText")?.GetComponent<TMP_Text>();
                    var trackTr = FindDeep(rowTr, "ProgressBar_Track");
                    var fillRect = trackTr != null ? FindDeep(trackTr, "Fill")?.GetComponent<RectTransform>() : null;
                    achievementRows[i] = new AchievementRowBinding(titleText, progText, fillRect);
                }
            }

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_backButton").objectReferenceValue = backButton;
            viewSo.FindProperty("_dimmerButton").objectReferenceValue = dimmerButton;
            viewSo.FindProperty("_bestScoreText").objectReferenceValue = bestScoreText;
            viewSo.FindProperty("_statsRowText").objectReferenceValue = statsRowText;
            viewSo.FindProperty("_personalBestChipText").objectReferenceValue = chipPbText;
            viewSo.FindProperty("_avgScoreChipText").objectReferenceValue = chipAvgText;
            viewSo.FindProperty("_thisWeekScoreText").objectReferenceValue = cardThisWeekText;
            viewSo.FindProperty("_lastWeekScoreText").objectReferenceValue = cardLastWeekText;
            viewSo.FindProperty("_statusText").objectReferenceValue = statusText;

            var recentProp = viewSo.FindProperty("_recentBars");
            recentProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
            {
                var elem = recentProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("_scoreText").objectReferenceValue = recentBars[i]?.ScoreText;
                elem.FindPropertyRelative("_fillBar").objectReferenceValue = recentBars[i]?.FillBar;
            }

            var achProp = viewSo.FindProperty("_achievementRows");
            achProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var elem = achProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("_titleText").objectReferenceValue = achievementRows[i]?.TitleText;
                elem.FindPropertyRelative("_progressText").objectReferenceValue = achievementRows[i]?.ProgressText;
                elem.FindPropertyRelative("_fillBar").objectReferenceValue = achievementRows[i]?.FillBar;
            }

            viewSo.ApplyModifiedProperties();

            var ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("_panelView").objectReferenceValue = view;
            ctrlSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            return $"SUCCESS: Saved scene={saved}. Wired View & Controller with 7 bars and 3 achievements.";
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            var q = new Queue<Transform>();
            q.Enqueue(parent);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.name == name) return cur;
                for (int i = 0; i < cur.childCount; i++) q.Enqueue(cur.GetChild(i));
            }
            return null;
        }
    }
}
