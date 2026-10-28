#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using BlockBlast.Features.Lobby;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.Editor
{
    /// <summary>
    /// Editor builder that idempotently constructs the World Customization modal overlay
    /// in LobbyScene matching the target visual mockup and responsive safe area constraints.
    /// </summary>
    public static class LobbyWorldCustomizationBuilder
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string CozyUiBase = "Assets/Art/UI/CozyStationeryUI/";
        private const string FontPath = "Assets/Fonts/TrueType (.ttf)/Kaph-Regular SDF.asset";

        [MenuItem("BlockBlast/Build World Customization Overlay")]
        public static void Build()
        {
            BuildInternal(includePlaceholders: false);
        }

        [MenuItem("BlockBlast/Build World Customization Overlay (With Placeholders)")]
        public static void BuildWithPlaceholders()
        {
            BuildInternal(includePlaceholders: true);
        }

        public static void BuildInternal(bool includePlaceholders)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var lobbyRoot = GameObject.Find("LobbyRoot");
            if (lobbyRoot == null)
            {
                Debug.LogError("[LobbyWorldCustomizationBuilder] LobbyRoot not found in scene.");
                return;
            }

            var rootObjs = activeScene.GetRootGameObjects();
            var canvasObj = rootObjs.FirstOrDefault(r => r.name == "Canvas_LobbySettingsCamera");
            if (canvasObj == null)
            {
                Debug.LogError("[LobbyWorldCustomizationBuilder] Canvas_LobbySettingsCamera not found in root objects.");
                return;
            }

            // 1. Validate all dependencies before any scene modification
            var dependencies = new Dictionary<string, Object>
            {
                { "panelNotebookSingle", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Panels/panel_notebook_single.png") },
                { "panelPaperMedium", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Panels/panel_paper_medium.png") },
                { "panelCardSmall", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Panels/panel_card_small.png") },
                { "btnClose", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Buttons/btn_icon_close.png") },
                { "btnPrimaryNorm", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Buttons/btn_primary_normal.png") },
                { "btnPrimaryPress", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Buttons/btn_primary_pressed.png") },
                { "btnPrimaryDisabled", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Buttons/btn_primary_disabled.png") },
                { "icCheck", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Icons/icon_check.png") },
                { "icLock", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Icons/icon_lock.png") },
                { "icHouse", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Components/Icons/icon_house.png") },
                { "bgHills", AssetDatabase.LoadAssetAtPath<Sprite>(CozyUiBase + "Backgrounds/bg_hills_portrait.png") },
                { "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) }
            };

            foreach (var kvp in dependencies)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"[LobbyWorldCustomizationBuilder] Missing required asset dependency '{kvp.Key}'. Aborting build without mutating scene.");
                    return;
                }
            }

            var panelNotebookSingle = (Sprite)dependencies["panelNotebookSingle"];
            var panelCardSmall = (Sprite)dependencies["panelCardSmall"];
            var btnClose = (Sprite)dependencies["btnClose"];
            var btnPrimaryNorm = (Sprite)dependencies["btnPrimaryNorm"];
            var btnPrimaryPress = (Sprite)dependencies["btnPrimaryPress"];
            var btnPrimaryDisabled = (Sprite)dependencies["btnPrimaryDisabled"];
            var icCheck = (Sprite)dependencies["icCheck"];
            var icLock = (Sprite)dependencies["icLock"];
            var icHouse = (Sprite)dependencies["icHouse"];
            var bgHills = (Sprite)dependencies["bgHills"];
            var font = (TMP_FontAsset)dependencies["font"];

            // 2. Remove existing WorldCustomizationOverlay if present (idempotent build)
            int siblingIndex = 1;
            var existingCustom = canvasObj.transform.Find("WorldCustomizationOverlay");
            if (existingCustom != null)
            {
                siblingIndex = existingCustom.GetSiblingIndex();
                Object.DestroyImmediate(existingCustom.gameObject);
            }

            // 3. Root WorldCustomizationOverlay (stretched full canvas)
            var overlayObj = CreateStretchUi("WorldCustomizationOverlay", canvasObj.transform);
            overlayObj.transform.SetSiblingIndex(siblingIndex);
            overlayObj.AddComponent<CanvasGroup>();
            var overlayView = overlayObj.AddComponent<LobbyWorldCustomizationOverlayView>();

            // 4. Dimmer_FullBleed (stretched full, navy alpha 0.62, raycastTarget = true, Button for close)
            var dimmerObj = CreateStretchUi("Dimmer_FullBleed", overlayObj.transform);
            dimmerObj.transform.SetSiblingIndex(0);
            var dimmerImg = dimmerObj.AddComponent<Image>();
            dimmerImg.color = new Color(0.04f, 0.07f, 0.12f, 0.62f);
            dimmerImg.raycastTarget = true;
            var dimmerBtn = dimmerObj.AddComponent<Button>();
            dimmerBtn.targetGraphic = dimmerImg;
            dimmerBtn.transition = Selectable.Transition.None;
            var dimmerNav = dimmerBtn.navigation;
            dimmerNav.mode = Navigation.Mode.None;
            dimmerBtn.navigation = dimmerNav;

            // 5. SafeArea_Customization
            var safeAreaObj = CreateStretchUi("SafeArea_Customization", overlayObj.transform);
            safeAreaObj.AddComponent<LobbySafeAreaView>();

            // 6. WorldCustomizationCard (438 x 760, centered in SafeArea)
            var cardObj = new GameObject("WorldCustomizationCard");
            cardObj.transform.SetParent(safeAreaObj.transform, false);
            var cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(438f, 760f);

            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = panelNotebookSingle;
            cardImg.type = Image.Type.Sliced;
            cardImg.raycastTarget = true; // Blocks click-through to dimmer

            // 7. Zone 1: Header (height 72, insets 20)
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardObj.transform, false);
            var headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 72f);

            // Header Title
            var titleObj = CreateStretchUi("Title_World", headerObj.transform);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.offsetMin = new Vector2(24f, 0f);
            titleRt.offsetMax = new Vector2(-80f, 0f);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "DEFAULT WORLD";
            titleTmp.font = font;
            titleTmp.fontSize = 28f;
            titleTmp.enableAutoSizing = true;
            titleTmp.fontSizeMin = 18f;
            titleTmp.fontSizeMax = 28f;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = new Color32(0x3E, 0x22, 0x10, 0xFF);
            titleTmp.raycastTarget = false;

            // Button_Close (60 x 60)
            var closeBtnObj = new GameObject("Button_Close");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            var closeBtnRt = closeBtnObj.AddComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRt.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRt.pivot = new Vector2(1f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-16f, 0f);
            closeBtnRt.sizeDelta = new Vector2(60f, 60f);

            var closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.sprite = btnClose;
            closeBtnImg.raycastTarget = true;

            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.transition = Selectable.Transition.None;
            var closeNav = closeBtn.navigation;
            closeNav.mode = Navigation.Mode.None;
            closeBtn.navigation = closeNav;

            var closeIconObj = new GameObject("Icon_Close");
            closeIconObj.transform.SetParent(closeBtnObj.transform, false);
            var closeIconRt = closeIconObj.AddComponent<RectTransform>();
            closeIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            closeIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            closeIconRt.pivot = new Vector2(0.5f, 0.5f);
            closeIconRt.sizeDelta = Vector2.zero;

            // 8. Zone 2: PreviewViewport (390 x 270, RectMask2D) + Image_WorldPreview (Cover Crop)
            var previewViewportObj = new GameObject("PreviewViewport");
            previewViewportObj.transform.SetParent(cardObj.transform, false);
            var previewViewportRt = previewViewportObj.AddComponent<RectTransform>();
            previewViewportRt.anchorMin = new Vector2(0.5f, 1f);
            previewViewportRt.anchorMax = new Vector2(0.5f, 1f);
            previewViewportRt.pivot = new Vector2(0.5f, 1f);
            previewViewportRt.anchoredPosition = new Vector2(0f, -88f);
            previewViewportRt.sizeDelta = new Vector2(390f, 270f);
            previewViewportObj.AddComponent<RectMask2D>();

            var previewImgObj = new GameObject("Image_WorldPreview");
            previewImgObj.transform.SetParent(previewViewportObj.transform, false);
            var previewImgRt = previewImgObj.AddComponent<RectTransform>();
            previewImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            previewImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            previewImgRt.pivot = new Vector2(0.5f, 0.5f);
            previewImgRt.anchoredPosition = Vector2.zero;

            var previewImg = previewImgObj.AddComponent<Image>();
            previewImg.sprite = bgHills;
            previewImg.raycastTarget = false;

            var arf = previewImgObj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = (float)bgHills.rect.width / bgHills.rect.height;

            // 9. Zone 3: Button_ViewWorldInLobby (390 x 64, gap 16 below preview)
            var viewWorldBtnObj = new GameObject("Button_ViewWorldInLobby");
            viewWorldBtnObj.transform.SetParent(cardObj.transform, false);
            var viewWorldRt = viewWorldBtnObj.AddComponent<RectTransform>();
            viewWorldRt.anchorMin = new Vector2(0.5f, 1f);
            viewWorldRt.anchorMax = new Vector2(0.5f, 1f);
            viewWorldRt.pivot = new Vector2(0.5f, 1f);
            viewWorldRt.anchoredPosition = new Vector2(0f, -374f);
            viewWorldRt.sizeDelta = new Vector2(390f, 64f);

            var viewWorldImg = viewWorldBtnObj.AddComponent<Image>();
            viewWorldImg.sprite = btnPrimaryNorm;
            viewWorldImg.type = Image.Type.Sliced;
            viewWorldImg.raycastTarget = true;

            var viewWorldBtn = viewWorldBtnObj.AddComponent<Button>();
            viewWorldBtn.targetGraphic = viewWorldImg;
            viewWorldBtn.transition = Selectable.Transition.SpriteSwap;
            var pillSs = viewWorldBtn.spriteState;
            pillSs.pressedSprite = btnPrimaryPress;
            pillSs.highlightedSprite = btnPrimaryPress;
            pillSs.disabledSprite = btnPrimaryDisabled;
            viewWorldBtn.spriteState = pillSs;
            var viewWorldNav = viewWorldBtn.navigation;
            viewWorldNav.mode = Navigation.Mode.None;
            viewWorldBtn.navigation = viewWorldNav;

            var viewWorldLabelObj = CreateStretchUi("Label", viewWorldBtnObj.transform);
            var viewWorldLabelTmp = viewWorldLabelObj.AddComponent<TextMeshProUGUI>();
            viewWorldLabelTmp.text = "VIEW WORLD IN LOBBY";
            viewWorldLabelTmp.font = font;
            viewWorldLabelTmp.fontSize = 24f;
            viewWorldLabelTmp.enableAutoSizing = true;
            viewWorldLabelTmp.fontSizeMin = 16f;
            viewWorldLabelTmp.fontSizeMax = 24f;
            viewWorldLabelTmp.alignment = TextAlignmentOptions.Center;
            viewWorldLabelTmp.color = Color.white;
            viewWorldLabelTmp.raycastTarget = false;

            // 10. Zone 4: OtherWorldsSection (390 x 286, inset 24 from card edges)
            var otherWorldsObj = new GameObject("OtherWorldsSection");
            otherWorldsObj.transform.SetParent(cardObj.transform, false);
            var otherWorldsRt = otherWorldsObj.AddComponent<RectTransform>();
            otherWorldsRt.anchorMin = new Vector2(0.5f, 1f);
            otherWorldsRt.anchorMax = new Vector2(0.5f, 1f);
            otherWorldsRt.pivot = new Vector2(0.5f, 1f);
            otherWorldsRt.anchoredPosition = new Vector2(0f, -454f);
            otherWorldsRt.sizeDelta = new Vector2(390f, 286f);

            // Divider Left
            var divLeftObj = new GameObject("Divider_Left");
            divLeftObj.transform.SetParent(otherWorldsObj.transform, false);
            var divLeftRt = divLeftObj.AddComponent<RectTransform>();
            divLeftRt.anchorMin = new Vector2(0f, 1f);
            divLeftRt.anchorMax = new Vector2(0.5f, 1f);
            divLeftRt.pivot = new Vector2(0f, 1f);
            divLeftRt.offsetMin = new Vector2(0f, -15f);
            divLeftRt.offsetMax = new Vector2(-95f, -13f);
            var divLeftImg = divLeftObj.AddComponent<Image>();
            divLeftImg.color = new Color(0.35f, 0.27f, 0.19f, 0.35f);
            divLeftImg.raycastTarget = false;

            // Label OTHER WORLDS
            var otherLabelObj = new GameObject("Label_OTHER_WORLDS");
            otherLabelObj.transform.SetParent(otherWorldsObj.transform, false);
            var otherLabelRt = otherLabelObj.AddComponent<RectTransform>();
            otherLabelRt.anchorMin = new Vector2(0.5f, 1f);
            otherLabelRt.anchorMax = new Vector2(0.5f, 1f);
            otherLabelRt.pivot = new Vector2(0.5f, 1f);
            otherLabelRt.anchoredPosition = Vector2.zero;
            otherLabelRt.sizeDelta = new Vector2(180f, 28f);

            var otherLabelTmp = otherLabelObj.AddComponent<TextMeshProUGUI>();
            otherLabelTmp.text = "OTHER WORLDS";
            otherLabelTmp.font = font;
            otherLabelTmp.fontSize = 18f;
            otherLabelTmp.enableAutoSizing = true;
            otherLabelTmp.fontSizeMin = 14f;
            otherLabelTmp.fontSizeMax = 18f;
            otherLabelTmp.alignment = TextAlignmentOptions.Center;
            otherLabelTmp.color = new Color32(0x5A, 0x44, 0x30, 0xFF);
            otherLabelTmp.raycastTarget = false;

            // Divider Right
            var divRightObj = new GameObject("Divider_Right");
            divRightObj.transform.SetParent(otherWorldsObj.transform, false);
            var divRightRt = divRightObj.AddComponent<RectTransform>();
            divRightRt.anchorMin = new Vector2(0.5f, 1f);
            divRightRt.anchorMax = new Vector2(1f, 1f);
            divRightRt.pivot = new Vector2(1f, 1f);
            divRightRt.offsetMin = new Vector2(95f, -15f);
            divRightRt.offsetMax = new Vector2(0f, -13f);
            var divRightImg = divRightObj.AddComponent<Image>();
            divRightImg.color = new Color(0.35f, 0.27f, 0.19f, 0.35f);
            divRightImg.raycastTarget = false;

            // WorldListScrollRect (Horizontal ScrollRect)
            var scrollRectObj = new GameObject("WorldListScrollRect");
            scrollRectObj.transform.SetParent(otherWorldsObj.transform, false);
            var scrollRectRt = scrollRectObj.AddComponent<RectTransform>();
            scrollRectRt.anchorMin = new Vector2(0f, 0f);
            scrollRectRt.anchorMax = new Vector2(1f, 1f);
            scrollRectRt.offsetMin = Vector2.zero;
            scrollRectRt.offsetMax = new Vector2(0f, -36f);

            var scrollRect = scrollRectObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.elasticity = 0.08f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;

            // WorldListViewport (RectMask2D + drag raycast surface)
            var viewportObj = new GameObject("WorldListViewport");
            viewportObj.transform.SetParent(scrollRectObj.transform, false);
            var viewportRt = viewportObj.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();

            var viewportDragImg = viewportObj.AddComponent<Image>();
            viewportDragImg.color = new Color(0f, 0f, 0f, 0f);
            viewportDragImg.raycastTarget = true; // Drag receiver surface

            scrollRect.viewport = viewportRt;

            // WorldListContent (Left-centered content with HLG + ContentSizeFitter)
            var contentObj = new GameObject("WorldListContent");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0.5f);
            contentRt.anchorMax = new Vector2(0f, 0.5f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 180f);

            var hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.padding = new RectOffset(12, 12, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = contentRt;

            // World Card: Default World (Selected)
            CreateWorldCard(
                contentObj.transform,
                "Card_DefaultWorld_Selected",
                "DEFAULT WORLD",
                panelCardSmall,
                icHouse,
                icCheck,
                font,
                isSelected: true,
                isLocked: false);

            if (includePlaceholders)
            {
                CreateWorldCard(
                    contentObj.transform,
                    "Card_ComingSoon_01_Disabled",
                    "COMING SOON",
                    panelCardSmall,
                    icHouse,
                    icLock,
                    font,
                    isSelected: false,
                    isLocked: true);

                CreateWorldCard(
                    contentObj.transform,
                    "Card_ComingSoon_02_Disabled",
                    "COMING SOON",
                    panelCardSmall,
                    icHouse,
                    icLock,
                    font,
                    isSelected: false,
                    isLocked: true);
            }

            // 11. Wire LobbyWorldCustomizationOverlayView internal controls
            var overlaySo = new SerializedObject(overlayView);
            overlaySo.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            overlaySo.FindProperty("_dimmerButton").objectReferenceValue = dimmerBtn;
            overlaySo.FindProperty("_viewWorldInLobbyButton").objectReferenceValue = viewWorldBtn;
            overlaySo.FindProperty("_targetWorldId").stringValue = "default";
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            // 12. Wire serialized references on LobbyRoot
            var transitionView = lobbyRoot.GetComponent<LobbyTransitionView>();
            if (transitionView != null)
            {
                var tvSo = new SerializedObject(transitionView);
                var prop = tvSo.FindProperty("_customizationOverlay");
                if (prop != null)
                {
                    prop.objectReferenceValue = overlayView;
                    tvSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var controller = lobbyRoot.GetComponent<LobbyController>();
            if (controller != null)
            {
                var ctrlSo = new SerializedObject(controller);
                var prop = ctrlSo.FindProperty("_worldCustomizationOverlay");
                if (prop != null)
                {
                    prop.objectReferenceValue = overlayView;
                    ctrlSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 13. Set overlay hidden initially
            overlayView.SetImmediate(false);

            // 14. Mark scene dirty (do NOT call SaveScene automatically)
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"[LobbyWorldCustomizationBuilder] World Customization Overlay built successfully (includePlaceholders={includePlaceholders}). Scene marked dirty for review.");
        }

        private static GameObject CreateStretchUi(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void CreateWorldCard(
            Transform parent,
            string cardName,
            string worldTitle,
            Sprite frameSprite,
            Sprite thumbnailSprite,
            Sprite badgeSprite,
            TMP_FontAsset font,
            bool isSelected,
            bool isLocked)
        {
            var cardObj = new GameObject(cardName);
            cardObj.transform.SetParent(parent, false);
            var cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(150f, 168f);

            var le = cardObj.AddComponent<LayoutElement>();
            le.preferredWidth = 150f;
            le.preferredHeight = 168f;

            var frameImg = cardObj.AddComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = isLocked ? new Color(0.75f, 0.75f, 0.75f, 0.75f) : Color.white;
            frameImg.raycastTarget = false;

            // Thumbnail
            var thumbObj = new GameObject("Thumbnail");
            thumbObj.transform.SetParent(cardObj.transform, false);
            var thumbRt = thumbObj.AddComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0.5f, 1f);
            thumbRt.anchorMax = new Vector2(0.5f, 1f);
            thumbRt.pivot = new Vector2(0.5f, 1f);
            thumbRt.anchoredPosition = new Vector2(0f, -12f);
            thumbRt.sizeDelta = new Vector2(126f, 100f);

            var thumbImg = thumbObj.AddComponent<Image>();
            thumbImg.sprite = thumbnailSprite;
            thumbImg.preserveAspect = true;
            thumbImg.color = isLocked ? new Color(0.5f, 0.55f, 0.65f, 0.55f) : Color.white;
            thumbImg.raycastTarget = false;

            // Title Label
            var nameObj = new GameObject("Label_Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            var nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 0f);
            nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, 10f);
            nameRt.sizeDelta = new Vector2(138f, 32f);

            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = worldTitle;
            nameTmp.font = font;
            nameTmp.fontSize = 14f;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 10f;
            nameTmp.fontSizeMax = 14f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = isLocked ? new Color(0.55f, 0.55f, 0.6f, 0.75f) : new Color32(0x3E, 0x22, 0x10, 0xFF);
            nameTmp.raycastTarget = false;

            // Badge / Icon
            if (badgeSprite != null)
            {
                var badgeObj = new GameObject(isSelected ? "Badge_Check" : "Badge_Lock");
                badgeObj.transform.SetParent(cardObj.transform, false);
                var badgeRt = badgeObj.AddComponent<RectTransform>();

                if (isSelected)
                {
                    badgeRt.anchorMin = new Vector2(1f, 1f);
                    badgeRt.anchorMax = new Vector2(1f, 1f);
                    badgeRt.pivot = new Vector2(1f, 1f);
                    badgeRt.anchoredPosition = new Vector2(-6f, -6f);
                    badgeRt.sizeDelta = new Vector2(28f, 28f);
                }
                else
                {
                    badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
                    badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
                    badgeRt.pivot = new Vector2(0.5f, 0.5f);
                    badgeRt.anchoredPosition = new Vector2(0f, 10f);
                    badgeRt.sizeDelta = new Vector2(36f, 36f);
                }

                var badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.sprite = badgeSprite;
                badgeImg.raycastTarget = false;
            }
        }
    }
}
#endif
