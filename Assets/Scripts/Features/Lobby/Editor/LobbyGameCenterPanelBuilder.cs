#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using BlockBlast.Features.Lobby;
using BlockBlast.Features.Lobby.GameCenter;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.Editor
{
    public static class LobbyGameCenterPanelBuilder
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string BaseUi = "Assets/Art/UI/BlockPuzzleHD/";
        private const string CozyUi = "Assets/Art/UI/CozyStationeryUI/";
        private const string FontPath = "Assets/Fonts/TrueType (.ttf)/Kaph-Regular SDF.asset";
        private const string BgHillsPath = "Assets/Art/UI/CozyStationeryUI/Backgrounds/bg_hills_portrait.png";

        private static readonly Color32 DarkBrown = new Color32(0x3E, 0x22, 0x10, 0xFF);
        private static readonly Color32 WarmBrown = new Color32(0x5A, 0x43, 0x33, 0xFF);
        private static readonly Color32 WarmGray = new Color32(0x8C, 0x76, 0x62, 0xFF);
        private static readonly Color32 GreenAccent = new Color32(0x4C, 0xB6, 0x58, 0xFF);
        private static readonly Color32 BlueBar = new Color32(0x56, 0x9B, 0xBE, 0xFF);

        [MenuItem("BlockBlast/Build Player Center Panel")]
        public static void Build()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var lobbyRoot = GameObject.Find("LobbyRoot");
            if (lobbyRoot == null)
            {
                Debug.LogError("[LobbyGameCenterPanelBuilder] LobbyRoot not found in scene.");
                return;
            }

            var lobbyController = lobbyRoot.GetComponent<LobbyController>();
            var transitionView = lobbyRoot.GetComponentInChildren<LobbyTransitionView>(true);
            if (lobbyController == null || transitionView == null)
            {
                Debug.LogError("[LobbyGameCenterPanelBuilder] LobbyController or LobbyTransitionView not found.");
                return;
            }

            var canvasObj = activeScene.GetRootGameObjects().FirstOrDefault(r => r.name == "Canvas_LobbySettingsCamera");
            if (canvasObj == null)
            {
                Debug.LogError("[LobbyGameCenterPanelBuilder] Canvas_LobbySettingsCamera not found in root objects.");
                return;
            }

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                Debug.LogError("[LobbyGameCenterPanelBuilder] CanvasScaler not found on Canvas_LobbySettingsCamera.");
                return;
            }

            var deps = new Dictionary<string, UnityEngine.Object>
            {
                { "bgHills", AssetDatabase.LoadMainAssetAtPath(BgHillsPath) },
                { "btnBackNormal", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Buttons/btn_back_normal.png") },
                { "btnBackPressed", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Buttons/btn_back_pressed.png") },
                { "panelCardLight", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/panel_card_light.png") },
                { "ribbonGold", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/ribbon_gold.png") },
                { "ribbonBlue", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/ribbon_blue.png") },
                { "ribbonPink", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/ribbon_pink.png") },
                { "iconTrophy", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUi + "Components/Icons/icon_trophy.png") },
                { "iconCheck", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUi + "Components/Icons/icon_check.png") },
                { "iconStar", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUi + "Components/Icons/icon_star.png") },
                { "iconLightning", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUi + "Components/Icons/icon_lightning.png") },
                { "iconCombo", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Icons/BlockPuzzle/icon_combo.png") },
                { "iconLineClear", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Icons/BlockPuzzle/icon_line_clear.png") },
                { "progressTrack", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Controls/progress_track_dark.png") },
                { "progressFill", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Controls/progress_fill_green.png") },
                { "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) }
            };

            foreach (var kvp in deps)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"[LobbyGameCenterPanelBuilder] Missing required asset '{kvp.Key}'. Aborting.");
                    return;
                }
            }

            // Standardize CanvasScaler to portrait 1080x1920, Match=0
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;

            // Remove existing GameCenterPanel (idempotent)
            var existingPanel = canvasObj.transform.Find("GameCenterPanel");
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            // Root GameCenterPanel
            var panelObj = CreateStretchUi("GameCenterPanel", canvasObj.transform);
            var panelView = panelObj.AddComponent<LobbyGameCenterPanelView>();
            var navController = panelObj.AddComponent<LobbyGameCenterNavigationController>();

            // Full-bleed background (blocks raycasts behind panel)
            var bgObj = CreateStretchUi("Background_FullBleed", panelObj.transform);
            if (deps["bgHills"] is Sprite bgSprite)
            {
                var img = bgObj.AddComponent<Image>();
                img.sprite = bgSprite;
                img.raycastTarget = true;
            }
            else if (deps["bgHills"] is Texture2D bgTex)
            {
                var raw = bgObj.AddComponent<RawImage>();
                raw.texture = bgTex;
                raw.raycastTarget = true;
            }

            // SafeArea
            var safeAreaObj = CreateStretchUi("SafeArea_GameCenter", panelObj.transform);
            safeAreaObj.AddComponent<LobbySafeAreaView>();

            // Header (height 168)
            var headerRt = CreateRect("Header", safeAreaObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 168f));

            var backBtnRt = CreateRect("Button_Back", headerRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(104f, 104f));
            var backBtnImg = backBtnRt.gameObject.AddComponent<Image>();
            backBtnImg.sprite = (Sprite)deps["btnBackNormal"];
            backBtnImg.raycastTarget = true;
            var backBtn = backBtnRt.gameObject.AddComponent<Button>();
            backBtn.transition = Selectable.Transition.SpriteSwap;
            backBtn.spriteState = new SpriteState { pressedSprite = (Sprite)deps["btnBackPressed"] };
            backBtn.navigation = new Navigation { mode = Navigation.Mode.None };

            var font = (TMP_FontAsset)deps["font"];
            var titleRt = CreateStretchUi("Banner_Title", headerRt).GetComponent<RectTransform>();
            titleRt.offsetMin = new Vector2(140f, 0f);
            titleRt.offsetMax = new Vector2(-140f, 0f);
            AddText(titleRt.gameObject, "PLAYER CENTER", font, 42f, DarkBrown, TextAlignmentOptions.Center, true);

            // ContentScrollView
            var scrollObj = new GameObject("ContentScrollView");
            scrollObj.transform.SetParent(safeAreaObj.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0f, -168f);

            var scrollImg = scrollObj.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0f);
            scrollImg.raycastTarget = true;

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 25f;

            var viewportRt = CreateStretchUi("Viewport", scrollObj.transform).GetComponent<RectTransform>();
            viewportRt.gameObject.AddComponent<RectMask2D>();

            var contentRt = CreateRect("Content", viewportRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var vlg = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(60, 60, 24, 72);
            vlg.spacing = 24f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentRt.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            // Cards
            var cardSprite = (Sprite)deps["panelCardLight"];
            BuildPersonalBestCard(contentRt, cardSprite, (Sprite)deps["ribbonGold"], (Sprite)deps["iconTrophy"], font);
            BuildThisWeekCard(contentRt, cardSprite, (Sprite)deps["ribbonBlue"], (Sprite)deps["iconStar"], (Sprite)deps["iconLightning"], font);
            BuildRecentGamesCard(contentRt, cardSprite, (Sprite)deps["ribbonPink"], (Sprite)deps["progressFill"], font);
            BuildAchievementsCard(contentRt, cardSprite, (Sprite)deps["ribbonGold"], (Sprite)deps["iconTrophy"], (Sprite)deps["iconCombo"], (Sprite)deps["iconLineClear"], (Sprite)deps["iconCheck"], (Sprite)deps["progressTrack"], (Sprite)deps["progressFill"], font);

            // Wire references
            panelView.BackButton = backBtn;
            navController.Construct(panelView, lobbyController);

            var serializedTransition = new SerializedObject(transitionView);
            var gameCenterProp = serializedTransition.FindProperty("_gameCenterPanel");
            if (gameCenterProp != null)
            {
                gameCenterProp.objectReferenceValue = panelView;
                serializedTransition.ApplyModifiedProperties();
            }

            panelView.SetImmediate(false);

            EditorUtility.SetDirty(canvasObj);
            EditorUtility.SetDirty(transitionView);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[LobbyGameCenterPanelBuilder] Successfully built Player Center panel under Canvas_LobbySettingsCamera.");
        }

        private static GameObject CreateStretchUi(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type = Image.Type.Simple, bool preserveAspect = false)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = size;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateCardRoot(string name, Transform parent, Sprite cardSprite, float height)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);

            var le = card.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            AddImage(card, cardSprite, Color.white, Image.Type.Sliced);
            return card;
        }

        private static void CreateRibbon(Transform parent, Sprite ribbonSprite, string title, TMP_FontAsset font, float width)
        {
            var ribbonRt = CreateRect($"Ribbon_{title.Replace(' ', '_')}", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(width, 56f));
            AddImage(ribbonRt.gameObject, ribbonSprite, Color.white, Image.Type.Sliced);

            var textObj = CreateStretchUi("Text_Title", ribbonRt);
            AddText(textObj, title, font, 22f, Color.white, TextAlignmentOptions.Center, true);
        }

        private static void BuildPersonalBestCard(Transform parent, Sprite cardSprite, Sprite ribbonSprite, Sprite iconTrophy, TMP_FontAsset font)
        {
            var card = CreateCardRoot("Card_PersonalBest", parent, cardSprite, 360f);
            CreateRibbon(card.transform, ribbonSprite, "PERSONAL BEST", font, 340f);

            var trophyRt = CreateRect("Icon_Trophy", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, 40f), new Vector2(88f, 88f));
            AddImage(trophyRt.gameObject, iconTrophy, Color.white, Image.Type.Simple, true);

            var subRt = CreateRect("Label_AllTime", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 74f), new Vector2(270f, 24f));
            AddText(subRt.gameObject, "ALL-TIME HIGH SCORE", font, 16f, WarmGray, TextAlignmentOptions.MidlineLeft);

            var scoreRt = CreateRect("Text_BestScore", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 38f), new Vector2(270f, 60f));
            AddText(scoreRt.gameObject, "128,450", font, 54f, DarkBrown, TextAlignmentOptions.MidlineLeft, true);

            var divRt = CreateRect("Divider", card.transform, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(0f, 2f));
            AddImage(divRt.gameObject, null, new Color(0.85f, 0.82f, 0.78f, 0.8f));

            var statsRt = CreateRect("StatsRow", card.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(0f, 60f));
            var statsTmp = AddText(statsRt.gameObject, "326 GAMES   |   12,840 LINES   |   BEST COMBO x17", font, 18f, WarmBrown, TextAlignmentOptions.Center, true);
            statsTmp.enableAutoSizing = true;
            statsTmp.fontSizeMin = 14f;
            statsTmp.fontSizeMax = 18f;
        }

        private static void BuildThisWeekCard(Transform parent, Sprite cardSprite, Sprite ribbonSprite, Sprite iconStar, Sprite iconLightning, TMP_FontAsset font)
        {
            var card = CreateCardRoot("Card_ThisWeek", parent, cardSprite, 330f);
            CreateRibbon(card.transform, ribbonSprite, "THIS WEEK", font, 280f);

            // Left Insight badge
            var b1Rt = CreateRect("Insight_NewPersonalBest", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-150f, -72f), new Vector2(280f, 40f));
            AddImage(b1Rt.gameObject, cardSprite, new Color(1f, 0.94f, 0.82f, 1f), Image.Type.Sliced);
            var s1Rt = CreateRect("Icon_Star", b1Rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(24f, 24f));
            AddImage(s1Rt.gameObject, iconStar, Color.white, Image.Type.Simple, true);
            var t1Rt = CreateStretchUi("Text", b1Rt).GetComponent<RectTransform>();
            t1Rt.offsetMin = new Vector2(44f, 0f);
            AddText(t1Rt.gameObject, "NEW PERSONAL BEST!", font, 15f, new Color32(0x9E, 0x5D, 0x10, 0xFF), TextAlignmentOptions.MidlineLeft, true);

            // Right Insight badge
            var b2Rt = CreateRect("Insight_AverageScore", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(150f, -72f), new Vector2(280f, 40f));
            AddImage(b2Rt.gameObject, cardSprite, new Color(0.85f, 0.96f, 0.87f, 1f), Image.Type.Sliced);
            var l1Rt = CreateRect("Icon_Lightning", b2Rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(24f, 24f));
            AddImage(l1Rt.gameObject, iconLightning, Color.white, Image.Type.Simple, true);
            var t2Rt = CreateStretchUi("Text", b2Rt).GetComponent<RectTransform>();
            t2Rt.offsetMin = new Vector2(44f, 0f);
            AddText(t2Rt.gameObject, "+18% AVG SCORE", font, 15f, new Color32(0x23, 0x72, 0x38, 0xFF), TextAlignmentOptions.MidlineLeft, true);

            // Score columns
            var col1Rt = CreateRect("Score_ThisWeek", card.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-160f, 32f), new Vector2(280f, 110f));
            var l1LabelRt = CreateRect("Label", col1Rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 24f));
            AddText(l1LabelRt.gameObject, "THIS WEEK BEST", font, 16f, WarmGray);
            var v1Rt = CreateRect("Value", col1Rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            v1Rt.offsetMax = new Vector2(0f, -24f);
            AddText(v1Rt.gameObject, "42,680", font, 44f, DarkBrown, TextAlignmentOptions.Center, true);

            var col2Rt = CreateRect("Score_LastWeek", card.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(160f, 32f), new Vector2(280f, 110f));
            var l2LabelRt = CreateRect("Label", col2Rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 24f));
            AddText(l2LabelRt.gameObject, "LAST WEEK BEST", font, 16f, WarmGray);
            var v2Rt = CreateRect("Value", col2Rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            v2Rt.offsetMax = new Vector2(0f, -24f);
            AddText(v2Rt.gameObject, "36,210", font, 44f, WarmGray, TextAlignmentOptions.Center, true);
        }

        private static void BuildRecentGamesCard(Transform parent, Sprite cardSprite, Sprite ribbonSprite, Sprite fillSprite, TMP_FontAsset font)
        {
            var card = CreateCardRoot("Card_RecentGames", parent, cardSprite, 280f);
            CreateRibbon(card.transform, ribbonSprite, "RECENT GAMES", font, 300f);

            var rowRt = CreateRect("BarsRow", card.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rowRt.offsetMin = new Vector2(40f, 16f);
            rowRt.offsetMax = new Vector2(-40f, -54f);

            var hlg = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.LowerCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            int[] scores = { 128, 94, 112, 86, 105, 72, 98 };
            string[] labels = { "128K", "94K", "112K", "86K", "105K", "72K", "98K" };

            for (int i = 0; i < scores.Length; i++)
            {
                var colRt = CreateRect($"Bar_Game{i + 1}", rowRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var le = colRt.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 36f;
                le.flexibleWidth = 1f;
                le.minHeight = 170f;

                var scoreRt = CreateRect("Text_Score", colRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(60f, 20f));
                AddText(scoreRt.gameObject, labels[i], font, 14f, DarkBrown, TextAlignmentOptions.Center, true);

                float barHeight = (scores[i] / 128f) * 105f + 18f;
                var barRt = CreateRect("Fill", colRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(34f, barHeight));
                AddImage(barRt.gameObject, fillSprite, (scores[i] == 128) ? GreenAccent : BlueBar, Image.Type.Sliced);

                var idxRt = CreateRect("Text_Index", colRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(40f, 18f));
                AddText(idxRt.gameObject, $"G{i + 1}", font, 13f, WarmGray);
            }
        }

        private static void BuildAchievementsCard(
            Transform parent,
            Sprite cardSprite,
            Sprite ribbonSprite,
            Sprite iconTrophy,
            Sprite iconCombo,
            Sprite iconLineClear,
            Sprite iconCheck,
            Sprite trackSprite,
            Sprite fillSprite,
            TMP_FontAsset font)
        {
            var card = CreateCardRoot("Card_Achievements", parent, cardSprite, 420f);
            CreateRibbon(card.transform, ribbonSprite, "ACHIEVEMENTS", font, 300f);

            BuildAchievementRow(card.transform, cardSprite, "Row_ScoreMaster", -110f, iconTrophy, "SCORE MASTER", "412K / 500K", 412f / 500f, false, iconCheck, trackSprite, fillSprite, font);
            BuildAchievementRow(card.transform, cardSprite, "Row_ComboCollector", -206f, iconCombo, "COMBO COLLECTOR", "15 / 15", 1f, true, iconCheck, trackSprite, fillSprite, font);
            BuildAchievementRow(card.transform, cardSprite, "Row_LineCleanerIII", -302f, iconLineClear, "LINE CLEANER III", "742 / 1,000", 742f / 1000f, false, iconCheck, trackSprite, fillSprite, font);
        }

        private static void BuildAchievementRow(
            Transform parent,
            Sprite cardSprite,
            string rowName,
            float yPos,
            Sprite icon,
            string title,
            string progressText,
            float ratio,
            bool isCompleted,
            Sprite iconCheck,
            Sprite trackSprite,
            Sprite fillSprite,
            TMP_FontAsset font)
        {
            var rowRt = CreateRect(rowName, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, yPos), new Vector2(-60f, 82f));
            AddImage(rowRt.gameObject, cardSprite, new Color(0.96f, 0.94f, 0.90f, 0.7f), Image.Type.Sliced);

            var iconRt = CreateRect("Icon", rowRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 0f), new Vector2(52f, 52f));
            AddImage(iconRt.gameObject, icon, Color.white, Image.Type.Simple, true);

            var titleRt = CreateRect("Title", rowRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -14f), new Vector2(340f, 24f));
            AddText(titleRt.gameObject, title, font, 18f, DarkBrown, TextAlignmentOptions.MidlineLeft, true);

            var progRt = CreateRect("ProgressText", rowRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -14f), new Vector2(200f, 24f));
            AddText(progRt.gameObject, progressText, font, 16f, WarmGray, TextAlignmentOptions.MidlineRight);

            if (isCompleted)
            {
                var checkRt = CreateRect("Icon_Completed", rowRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -13f), new Vector2(24f, 24f));
                AddImage(checkRt.gameObject, iconCheck, GreenAccent, Image.Type.Simple, true);
            }

            var trackRt = CreateRect("ProgressBar_Track", rowRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(82f, 16f), new Vector2(-104f, 18f));
            AddImage(trackRt.gameObject, trackSprite, new Color(0.85f, 0.83f, 0.80f, 1f), Image.Type.Sliced);

            var fillRt = CreateRect("Fill", trackRt, Vector2.zero, new Vector2(ratio, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(fillRt.gameObject, fillSprite, GreenAccent, Image.Type.Sliced);
        }
    }
}
#endif
