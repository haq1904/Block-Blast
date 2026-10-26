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
    public static class LobbyWorldCustomizationBuilder
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string BaseUi = "Assets/Art/UI/BlockPuzzleHD/";
        private const string BgHillsPath = "Assets/Art/UI/CozyStationeryUI/Backgrounds/bg_hills_portrait.png";

        [MenuItem("BlockBlast/Build World Customization Overlay")]
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
                { "panelModalLight", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/panel_modal_light.png") },
                { "panelTopHeader", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/panel_top_header.png") },
                { "frameEquipped", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/frame_inventory_equipped.png") },
                { "frameNormal", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/frame_inventory_normal.png") },
                { "frameLocked", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Panels/frame_inventory_locked.png") },
                { "btnCloseNorm", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Buttons/btn_close_normal.png") },
                { "btnClosePress", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Buttons/btn_close_pressed.png") },
                { "btnPillNorm", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Buttons/btn_pill_primary_normal.png") },
                { "btnPillPress", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Buttons/btn_pill_primary_pressed.png") },
                { "icClose", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Icons/System/icon_close.png") },
                { "icCheck", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Icons/System/icon_check.png") },
                { "icLock", AssetDatabase.LoadAssetAtPath<Sprite>(BaseUi + "Icons/System/icon_lock.png") },
                { "bgHills", AssetDatabase.LoadMainAssetAtPath(BgHillsPath) },
                { "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TrueType (.ttf)/Kaph-Regular SDF.asset") }
            };

            foreach (var kvp in dependencies)
            {
                if (kvp.Value == null)
                {
                    Debug.LogError($"[LobbyWorldCustomizationBuilder] Missing required asset dependency '{kvp.Key}'. Aborting build without mutating scene.");
                    return;
                }
            }

            var panelModalLight = (Sprite)dependencies["panelModalLight"];
            var panelTopHeader = (Sprite)dependencies["panelTopHeader"];
            var frameEquipped = (Sprite)dependencies["frameEquipped"];
            var frameNormal = (Sprite)dependencies["frameNormal"];
            var frameLocked = (Sprite)dependencies["frameLocked"];
            var btnCloseNorm = (Sprite)dependencies["btnCloseNorm"];
            var btnClosePress = (Sprite)dependencies["btnClosePress"];
            var btnPillNorm = (Sprite)dependencies["btnPillNorm"];
            var btnPillPress = (Sprite)dependencies["btnPillPress"];
            var icClose = (Sprite)dependencies["icClose"];
            var icCheck = (Sprite)dependencies["icCheck"];
            var icLock = (Sprite)dependencies["icLock"];
            var bgHills = AssetDatabase.LoadAssetAtPath<Sprite>(BgHillsPath);
            var font = (TMP_FontAsset)dependencies["font"];

            // 2. Remove existing WorldCustomizationOverlay if present (idempotent build)
            var existingCustom = canvasObj.transform.Find("WorldCustomizationOverlay");
            if (existingCustom != null)
            {
                Object.DestroyImmediate(existingCustom.gameObject);
            }

            // 3. Root WorldCustomizationOverlay (stretched full canvas)
            var overlayObj = CreateStretchUi("WorldCustomizationOverlay", canvasObj.transform);
            var overlayView = overlayObj.AddComponent<LobbyWorldCustomizationOverlayView>();

            // 4. Dimmer_FullBleed (stretched full, navy alpha 0.62, raycastTarget = true, no Button)
            var dimmerObj = CreateStretchUi("Dimmer_FullBleed", overlayObj.transform);
            var dimmerImg = dimmerObj.AddComponent<Image>();
            dimmerImg.color = new Color(0.04f, 0.07f, 0.12f, 0.62f);
            dimmerImg.raycastTarget = true;

            // 5. SafeArea_Customization
            var safeAreaObj = CreateStretchUi("SafeArea_Customization", overlayObj.transform);
            safeAreaObj.AddComponent<LobbySafeAreaView>();

            // 6. WorldCustomizationCard (900 x 1300, centered)
            var cardObj = new GameObject("WorldCustomizationCard");
            cardObj.transform.SetParent(safeAreaObj.transform, false);
            var cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(900f, 1300f);

            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = panelModalLight;
            cardImg.type = Image.Type.Sliced;
            cardImg.raycastTarget = false; // Non-raycast target

            // 7. Zone 1: Header (panel_top_header, height 144)
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardObj.transform, false);
            var headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 144f);

            var headerImg = headerObj.AddComponent<Image>();
            headerImg.sprite = panelTopHeader;
            headerImg.type = Image.Type.Sliced;
            headerImg.raycastTarget = false;

            // Header Title
            var titleObj = CreateStretchUi("Title_World", headerObj.transform);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.offsetMin = new Vector2(56f, 0f);
            titleRt.offsetMax = new Vector2(-128f, 0f);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "COZY FOREST";
            titleTmp.font = font;
            titleTmp.fontSize = 44f;
            titleTmp.enableAutoSizing = true;
            titleTmp.fontSizeMin = 30f;
            titleTmp.fontSizeMax = 44f;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = new Color32(0x3E, 0x22, 0x10, 0xFF);
            titleTmp.raycastTarget = false;

            // Button_Close (104 x 104)
            var closeBtnObj = new GameObject("Button_Close");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            var closeBtnRt = closeBtnObj.AddComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRt.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRt.pivot = new Vector2(1f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-28f, 0f);
            closeBtnRt.sizeDelta = new Vector2(104f, 104f);

            var closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.sprite = btnCloseNorm;
            closeBtnImg.raycastTarget = true;

            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.transition = Selectable.Transition.SpriteSwap;
            var closeSs = closeBtn.spriteState;
            closeSs.pressedSprite = btnClosePress;
            closeSs.highlightedSprite = btnClosePress;
            closeBtn.spriteState = closeSs;

            var closeIconObj = new GameObject("Icon_Close");
            closeIconObj.transform.SetParent(closeBtnObj.transform, false);
            var closeIconRt = closeIconObj.AddComponent<RectTransform>();
            closeIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            closeIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            closeIconRt.pivot = new Vector2(0.5f, 0.5f);
            closeIconRt.sizeDelta = new Vector2(48f, 48f);
            var closeIconImg = closeIconObj.AddComponent<Image>();
            closeIconImg.sprite = icClose;
            closeIconImg.raycastTarget = false;

            // 8. Zone 2: PreviewViewport (808 x 520, RectMask2D) + Image_WorldPreview
            var previewViewportObj = new GameObject("PreviewViewport");
            previewViewportObj.transform.SetParent(cardObj.transform, false);
            var previewViewportRt = previewViewportObj.AddComponent<RectTransform>();
            previewViewportRt.anchorMin = new Vector2(0.5f, 1f);
            previewViewportRt.anchorMax = new Vector2(0.5f, 1f);
            previewViewportRt.pivot = new Vector2(0.5f, 1f);
            previewViewportRt.anchoredPosition = new Vector2(0f, -168f);
            previewViewportRt.sizeDelta = new Vector2(808f, 520f);
            previewViewportObj.AddComponent<RectMask2D>();

            var previewImgObj = CreateStretchUi("Image_WorldPreview", previewViewportObj.transform);
            var previewImg = previewImgObj.AddComponent<Image>();
            previewImg.sprite = bgHills;
            previewImg.preserveAspect = true;
            previewImg.raycastTarget = false;

            // 9. Zone 3: Button_ViewWorldInLobby (680 x 112)
            var viewWorldBtnObj = new GameObject("Button_ViewWorldInLobby");
            viewWorldBtnObj.transform.SetParent(cardObj.transform, false);
            var viewWorldRt = viewWorldBtnObj.AddComponent<RectTransform>();
            viewWorldRt.anchorMin = new Vector2(0.5f, 1f);
            viewWorldRt.anchorMax = new Vector2(0.5f, 1f);
            viewWorldRt.pivot = new Vector2(0.5f, 1f);
            viewWorldRt.anchoredPosition = new Vector2(0f, -712f);
            viewWorldRt.sizeDelta = new Vector2(680f, 112f);

            var viewWorldImg = viewWorldBtnObj.AddComponent<Image>();
            viewWorldImg.sprite = btnPillNorm;
            viewWorldImg.type = Image.Type.Sliced;
            viewWorldImg.raycastTarget = true;

            var viewWorldBtn = viewWorldBtnObj.AddComponent<Button>();
            viewWorldBtn.targetGraphic = viewWorldImg;
            viewWorldBtn.transition = Selectable.Transition.SpriteSwap;
            var pillSs = viewWorldBtn.spriteState;
            pillSs.pressedSprite = btnPillPress;
            pillSs.highlightedSprite = btnPillPress;
            viewWorldBtn.spriteState = pillSs;

            var viewWorldLabelObj = CreateStretchUi("Label", viewWorldBtnObj.transform);
            var viewWorldLabelTmp = viewWorldLabelObj.AddComponent<TextMeshProUGUI>();
            viewWorldLabelTmp.text = "VIEW WORLD IN LOBBY";
            viewWorldLabelTmp.font = font;
            viewWorldLabelTmp.fontSize = 32f;
            viewWorldLabelTmp.enableAutoSizing = true;
            viewWorldLabelTmp.fontSizeMin = 22f;
            viewWorldLabelTmp.fontSizeMax = 32f;
            viewWorldLabelTmp.alignment = TextAlignmentOptions.Center;
            viewWorldLabelTmp.color = Color.white;
            viewWorldLabelTmp.raycastTarget = false;

            // 10. Zone 4: OtherWorldsSection (808 x 400)
            var otherWorldsObj = new GameObject("OtherWorldsSection");
            otherWorldsObj.transform.SetParent(cardObj.transform, false);
            var otherWorldsRt = otherWorldsObj.AddComponent<RectTransform>();
            otherWorldsRt.anchorMin = new Vector2(0.5f, 1f);
            otherWorldsRt.anchorMax = new Vector2(0.5f, 1f);
            otherWorldsRt.pivot = new Vector2(0.5f, 1f);
            otherWorldsRt.anchoredPosition = new Vector2(0f, -848f);
            otherWorldsRt.sizeDelta = new Vector2(808f, 400f);

            var otherLabelObj = new GameObject("Label_OTHER_WORLDS");
            otherLabelObj.transform.SetParent(otherWorldsObj.transform, false);
            var otherLabelRt = otherLabelObj.AddComponent<RectTransform>();
            otherLabelRt.anchorMin = new Vector2(0f, 1f);
            otherLabelRt.anchorMax = new Vector2(1f, 1f);
            otherLabelRt.pivot = new Vector2(0.5f, 1f);
            otherLabelRt.anchoredPosition = Vector2.zero;
            otherLabelRt.sizeDelta = new Vector2(0f, 48f);

            var otherLabelTmp = otherLabelObj.AddComponent<TextMeshProUGUI>();
            otherLabelTmp.text = "OTHER WORLDS";
            otherLabelTmp.font = font;
            otherLabelTmp.fontSize = 26f;
            otherLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            otherLabelTmp.color = new Color32(0x5A, 0x44, 0x30, 0xFF);
            otherLabelTmp.raycastTarget = false;

            // WorldListViewport (808 x 300, RectMask2D)
            var viewportObj = new GameObject("WorldListViewport");
            viewportObj.transform.SetParent(otherWorldsObj.transform, false);
            var viewportRt = viewportObj.AddComponent<RectTransform>();
            viewportRt.anchorMin = new Vector2(0f, 1f);
            viewportRt.anchorMax = new Vector2(1f, 1f);
            viewportRt.pivot = new Vector2(0.5f, 1f);
            viewportRt.anchoredPosition = new Vector2(0f, -56f);
            viewportRt.sizeDelta = new Vector2(808f, 300f);
            viewportObj.AddComponent<RectMask2D>();

            // WorldListContent (HorizontalLayoutGroup, spacing 20)
            var contentObj = CreateStretchUi("WorldListContent", viewportObj.transform);
            var hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Card 1: Cozy Forest (Selected)
            CreateWorldCard(
                contentObj.transform,
                "Card_CozyForest_Selected",
                "COZY FOREST",
                frameEquipped,
                bgHills,
                icCheck,
                font,
                isSelected: true,
                isLocked: false);

            // Card 2: Sunny Farm (Available)
            CreateWorldCard(
                contentObj.transform,
                "Card_SunnyFarm_Available",
                "SUNNY FARM",
                frameNormal,
                bgHills,
                null,
                font,
                isSelected: false,
                isLocked: false);

            // Card 3: Coral Cove (Locked)
            CreateWorldCard(
                contentObj.transform,
                "Card_CoralCove_Locked",
                "CORAL COVE",
                frameLocked,
                bgHills,
                icLock,
                font,
                isSelected: false,
                isLocked: true);

            // 11. Wire LobbyWorldCustomizationOverlayView internal buttons
            var overlaySo = new SerializedObject(overlayView);
            overlaySo.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            overlaySo.FindProperty("_viewWorldInLobbyButton").objectReferenceValue = viewWorldBtn;
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            // 12. Wire serialized references on LobbyRoot (only the 2 specific references)
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
            Debug.Log("[LobbyWorldCustomizationBuilder] World Customization Overlay built and wired successfully. Scene marked dirty for review.");
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
            cardRt.sizeDelta = new Vector2(248f, 276f);

            var frameImg = cardObj.AddComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.raycastTarget = false;

            // Thumbnail
            var thumbObj = new GameObject("Thumbnail");
            thumbObj.transform.SetParent(cardObj.transform, false);
            var thumbRt = thumbObj.AddComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0.5f, 0.5f);
            thumbRt.anchorMax = new Vector2(0.5f, 0.5f);
            thumbRt.pivot = new Vector2(0.5f, 0.5f);
            thumbRt.anchoredPosition = new Vector2(0f, 18f);
            thumbRt.sizeDelta = new Vector2(200f, 160f);

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
            nameRt.anchoredPosition = new Vector2(0f, 16f);
            nameRt.sizeDelta = new Vector2(220f, 40f);

            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = worldTitle;
            nameTmp.font = font;
            nameTmp.fontSize = 20f;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 14f;
            nameTmp.fontSizeMax = 20f;
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
                    badgeRt.anchoredPosition = new Vector2(-12f, -12f);
                    badgeRt.sizeDelta = new Vector2(36f, 36f);
                }
                else
                {
                    badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
                    badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
                    badgeRt.pivot = new Vector2(0.5f, 0.5f);
                    badgeRt.anchoredPosition = new Vector2(0f, 18f);
                    badgeRt.sizeDelta = new Vector2(44f, 44f);
                }

                var badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.sprite = badgeSprite;
                badgeImg.raycastTarget = false;
            }
        }
    }
}
#endif
