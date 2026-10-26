#if UNITY_EDITOR
using System.Linq;
using BlockBlast.Features.Lobby.Account;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.Editor
{
    public static class LobbyAccountLinkPanelBuilder
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";

        [MenuItem("BlockBlast/Build Account Link UI")]
        public static void Build()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var canvasObj = activeScene.GetRootGameObjects().FirstOrDefault(r => r.name == "Canvas_LobbySettingsCamera");
            if (canvasObj == null)
            {
                Debug.LogError("[LobbyAccountLinkPanelBuilder] Canvas_LobbySettingsCamera not found.");
                return;
            }

            var settingsOverlay = canvasObj.transform.Find("SettingsOverlay");
            if (settingsOverlay == null)
            {
                Debug.LogError("[LobbyAccountLinkPanelBuilder] SettingsOverlay not found.");
                return;
            }

            var content = settingsOverlay.Find("SafeArea_Content/SettingsCard/ContentScrollView/Viewport/Content");
            if (content == null)
            {
                Debug.LogError("[LobbyAccountLinkPanelBuilder] ContentScrollView Content not found.");
                return;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TrueType (.ttf)/Kaph-Regular SDF.asset");
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/CozyStationeryUI/Atlases/btn_rect_cozy_brown_default.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/BlockPuzzleHD/Buttons/btn_brown_rect.png");
            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/CozyStationeryUI/Backgrounds/panel_notebook_single_0.png");

            var colorBrown = new Color32(0x3E, 0x22, 0x10, 0xFF);
            var colorLabel = new Color32(0x5A, 0x43, 0x33, 0xFF);
            var colorDimmer = new Color(0f, 0f, 0f, 0.75f);

            // 1. Account Section in Content
            var rowAccountTrans = content.Find("Row_AccountLink");
            GameObject rowAccountObj;
            if (rowAccountTrans == null)
            {
                rowAccountObj = new GameObject("Row_AccountLink", typeof(RectTransform));
                rowAccountObj.transform.SetParent(content, false);
                var rowRect = rowAccountObj.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0f, 60f);

                // Put before VersionLabel if present
                var versionLabel = content.Find("VersionLabel");
                if (versionLabel != null)
                {
                    rowAccountObj.transform.SetSiblingIndex(versionLabel.GetSiblingIndex());
                }
            }
            else
            {
                rowAccountObj = rowAccountTrans.gameObject;
            }

            // Label
            var labelTrans = rowAccountObj.transform.Find("Label");
            TMP_Text labelText;
            if (labelTrans == null)
            {
                var labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(rowAccountObj.transform, false);
                var labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0.45f, 1f);
                labelRect.offsetMin = new Vector2(10f, 0f);
                labelRect.offsetMax = Vector2.zero;

                labelText = labelObj.GetComponent<TextMeshProUGUI>();
                labelText.font = fontAsset;
                labelText.fontSize = 18f;
                labelText.color = colorLabel;
                labelText.alignment = TextAlignmentOptions.MidlineLeft;
                labelText.text = "Google Play Games";
                labelText.raycastTarget = false;
            }
            else
            {
                labelText = labelTrans.GetComponent<TextMeshProUGUI>();
            }

            // Status Text
            var statusTrans = rowAccountObj.transform.Find("Text_Status");
            TMP_Text statusText;
            if (statusTrans == null)
            {
                var statusObj = new GameObject("Text_Status", typeof(RectTransform), typeof(TextMeshProUGUI));
                statusObj.transform.SetParent(rowAccountObj.transform, false);
                var statusRect = statusObj.GetComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0.45f, 0f);
                statusRect.anchorMax = new Vector2(0.72f, 1f);
                statusRect.offsetMin = Vector2.zero;
                statusRect.offsetMax = Vector2.zero;

                statusText = statusObj.GetComponent<TextMeshProUGUI>();
                statusText.font = fontAsset;
                statusText.fontSize = 14f;
                statusText.color = colorLabel;
                statusText.alignment = TextAlignmentOptions.MidlineLeft;
                statusText.text = "Not linked";
                statusText.raycastTarget = false;
            }
            else
            {
                statusText = statusTrans.GetComponent<TextMeshProUGUI>();
            }

            // Link Button
            var linkBtnTrans = rowAccountObj.transform.Find("Button_Link");
            Button linkButton;
            TMP_Text linkBtnText;
            if (linkBtnTrans == null)
            {
                var linkBtnObj = new GameObject("Button_Link", typeof(RectTransform), typeof(Image), typeof(Button));
                linkBtnObj.transform.SetParent(rowAccountObj.transform, false);
                var btnRect = linkBtnObj.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.74f, 0.1f);
                btnRect.anchorMax = new Vector2(0.98f, 0.9f);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;

                var btnImage = linkBtnObj.GetComponent<Image>();
                if (buttonSprite != null)
                {
                    btnImage.sprite = buttonSprite;
                    btnImage.type = Image.Type.Sliced;
                }
                btnImage.raycastTarget = true;

                linkButton = linkBtnObj.GetComponent<Button>();

                var btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                btnTextObj.transform.SetParent(linkBtnObj.transform, false);
                var btnTextRect = btnTextObj.GetComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero;
                btnTextRect.offsetMax = Vector2.zero;

                linkBtnText = btnTextObj.GetComponent<TextMeshProUGUI>();
                linkBtnText.font = fontAsset;
                linkBtnText.fontSize = 16f;
                linkBtnText.color = Color.white;
                linkBtnText.alignment = TextAlignmentOptions.Center;
                linkBtnText.text = "Link";
                linkBtnText.raycastTarget = false;
            }
            else
            {
                linkButton = linkBtnTrans.GetComponent<Button>();
                linkBtnText = linkBtnTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // 2. Conflict Modal under SettingsOverlay
            var modalTrans = settingsOverlay.Find("AccountConflictModal");
            GameObject modalObj;
            if (modalTrans == null)
            {
                modalObj = new GameObject("AccountConflictModal", typeof(RectTransform));
                modalObj.transform.SetParent(settingsOverlay, false);
                var modalRect = modalObj.GetComponent<RectTransform>();
                modalRect.anchorMin = Vector2.zero;
                modalRect.anchorMax = Vector2.one;
                modalRect.offsetMin = Vector2.zero;
                modalRect.offsetMax = Vector2.zero;
            }
            else
            {
                modalObj = modalTrans.gameObject;
            }
            modalObj.SetActive(false);

            // Modal Dimmer
            var dimmerTrans = modalObj.transform.Find("Dimmer_Blocker");
            Button dimmerButton;
            if (dimmerTrans == null)
            {
                var dimmerObj = new GameObject("Dimmer_Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
                dimmerObj.transform.SetParent(modalObj.transform, false);
                var dimmerRect = dimmerObj.GetComponent<RectTransform>();
                dimmerRect.anchorMin = Vector2.zero;
                dimmerRect.anchorMax = Vector2.one;
                dimmerRect.offsetMin = Vector2.zero;
                dimmerRect.offsetMax = Vector2.zero;

                var dimmerImg = dimmerObj.GetComponent<Image>();
                dimmerImg.color = colorDimmer;
                dimmerImg.raycastTarget = true;

                dimmerButton = dimmerObj.GetComponent<Button>();
            }
            else
            {
                dimmerButton = dimmerTrans.GetComponent<Button>();
            }

            // Conflict Card
            var cardTrans = modalObj.transform.Find("ConflictCard");
            GameObject cardObj;
            if (cardTrans == null)
            {
                cardObj = new GameObject("ConflictCard", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(modalObj.transform, false);
                var cardRect = cardObj.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.1f, 0.25f);
                cardRect.anchorMax = new Vector2(0.9f, 0.75f);
                cardRect.offsetMin = Vector2.zero;
                cardRect.offsetMax = Vector2.zero;

                var cardImg = cardObj.GetComponent<Image>();
                if (cardSprite != null)
                {
                    cardImg.sprite = cardSprite;
                    cardImg.type = Image.Type.Sliced;
                }
                cardImg.raycastTarget = true;
            }
            else
            {
                cardObj = cardTrans.gameObject;
            }

            // Title
            var titleTrans = cardObj.transform.Find("Text_Title");
            TMP_Text titleText;
            if (titleTrans == null)
            {
                var titleObj = new GameObject("Text_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(cardObj.transform, false);
                var titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.05f, 0.82f);
                titleRect.anchorMax = new Vector2(0.95f, 0.95f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;

                titleText = titleObj.GetComponent<TextMeshProUGUI>();
                titleText.font = fontAsset;
                titleText.fontSize = 22f;
                titleText.color = colorBrown;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.text = "Account Conflict";
                titleText.raycastTarget = false;
            }
            else
            {
                titleText = titleTrans.GetComponent<TextMeshProUGUI>();
            }

            // Warning
            var warningTrans = cardObj.transform.Find("Text_Warning");
            TMP_Text warningText;
            if (warningTrans == null)
            {
                var warningObj = new GameObject("Text_Warning", typeof(RectTransform), typeof(TextMeshProUGUI));
                warningObj.transform.SetParent(cardObj.transform, false);
                var warnRect = warningObj.GetComponent<RectTransform>();
                warnRect.anchorMin = new Vector2(0.08f, 0.55f);
                warnRect.anchorMax = new Vector2(0.92f, 0.8f);
                warnRect.offsetMin = Vector2.zero;
                warnRect.offsetMax = Vector2.zero;

                warningText = warningObj.GetComponent<TextMeshProUGUI>();
                warningText.font = fontAsset;
                warningText.fontSize = 14f;
                warningText.color = colorLabel;
                warningText.alignment = TextAlignmentOptions.Center;
                warningText.textWrappingMode = TextWrappingModes.Normal;
                warningText.text = "This Google Play Games account is already linked to another progress. Choose which progress to keep.";
                warningText.raycastTarget = false;
            }
            else
            {
                warningText = warningTrans.GetComponent<TextMeshProUGUI>();
            }

            // Helper to make button
            Button MakeButton(string name, Vector2 anchorMin, Vector2 anchorMax, string text, Transform parent)
            {
                var btnChild = parent.Find(name);
                if (btnChild != null) return btnChild.GetComponent<Button>();

                var bObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                bObj.transform.SetParent(parent, false);
                var bRect = bObj.GetComponent<RectTransform>();
                bRect.anchorMin = anchorMin;
                bRect.anchorMax = anchorMax;
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                var bImg = bObj.GetComponent<Image>();
                if (buttonSprite != null)
                {
                    bImg.sprite = buttonSprite;
                    bImg.type = Image.Type.Sliced;
                }
                bImg.raycastTarget = true;

                var tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                tObj.transform.SetParent(bObj.transform, false);
                var tRect = tObj.GetComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;

                var tComp = tObj.GetComponent<TextMeshProUGUI>();
                tComp.font = fontAsset;
                tComp.fontSize = 15f;
                tComp.color = Color.white;
                tComp.alignment = TextAlignmentOptions.Center;
                tComp.text = text;
                tComp.raycastTarget = false;

                return bObj.GetComponent<Button>();
            }

            var keepBtn = MakeButton("Button_KeepThisProgress", new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.5f), "Keep Current Progress", cardObj.transform);
            var switchBtn = MakeButton("Button_LoadExistingProgress", new Vector2(0.1f, 0.23f), new Vector2(0.9f, 0.35f), "Load Cloud Progress", cardObj.transform);
            var cancelBtn = MakeButton("Button_Cancel", new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.2f), "Cancel", cardObj.transform);

            // Sub-card for keep confirmation
            var keepConfirmTrans = cardObj.transform.Find("KeepConfirmationSubCard");
            GameObject keepConfirmObj;
            if (keepConfirmTrans == null)
            {
                keepConfirmObj = new GameObject("KeepConfirmationSubCard", typeof(RectTransform), typeof(Image));
                keepConfirmObj.transform.SetParent(cardObj.transform, false);
                var kcRect = keepConfirmObj.GetComponent<RectTransform>();
                kcRect.anchorMin = new Vector2(0.05f, 0.05f);
                kcRect.anchorMax = new Vector2(0.95f, 0.85f);
                kcRect.offsetMin = Vector2.zero;
                kcRect.offsetMax = Vector2.zero;

                var kcImg = keepConfirmObj.GetComponent<Image>();
                kcImg.color = new Color32(0xFA, 0xF5, 0xEF, 0xFF);
                kcImg.raycastTarget = true;

                var kcWarnObj = new GameObject("Text_ConfirmWarning", typeof(RectTransform), typeof(TextMeshProUGUI));
                kcWarnObj.transform.SetParent(keepConfirmObj.transform, false);
                var kwRect = kcWarnObj.GetComponent<RectTransform>();
                kwRect.anchorMin = new Vector2(0.08f, 0.45f);
                kwRect.anchorMax = new Vector2(0.92f, 0.92f);
                kwRect.offsetMin = Vector2.zero;
                kwRect.offsetMax = Vector2.zero;

                var kwText = kcWarnObj.GetComponent<TextMeshProUGUI>();
                kwText.font = fontAsset;
                kwText.fontSize = 14f;
                kwText.color = new Color32(0xA0, 0x20, 0x20, 0xFF);
                kwText.alignment = TextAlignmentOptions.Center;
                kwText.textWrappingMode = TextWrappingModes.Normal;
                kwText.text = "Warning: Keeping this progress will overwrite and permanently unlink the previous cloud progress from Google Play Games.";
                kwText.raycastTarget = false;
            }
            else
            {
                keepConfirmObj = keepConfirmTrans.gameObject;
            }
            keepConfirmObj.SetActive(false);

            var confirmKeepBtn = MakeButton("Button_ConfirmKeep", new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.4f), "Yes, Keep This Progress", keepConfirmObj.transform);
            var backBtn = MakeButton("Button_Back", new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.22f), "Go Back", keepConfirmObj.transform);

            // Wire LobbyAccountLinkView and LobbyAccountLinkController
            var view = rowAccountObj.GetComponent<LobbyAccountLinkView>();
            if (view == null) view = rowAccountObj.AddComponent<LobbyAccountLinkView>();

            var controller = rowAccountObj.GetComponent<LobbyAccountLinkController>();
            if (controller == null) controller = rowAccountObj.AddComponent<LobbyAccountLinkController>();

            // Set serialized fields via SerializedObject
            var so = new SerializedObject(view);
            so.FindProperty("_accountRowRoot").objectReferenceValue = rowAccountObj;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_linkButton").objectReferenceValue = linkButton;
            so.FindProperty("_linkButtonText").objectReferenceValue = linkBtnText;
            so.FindProperty("_conflictModalRoot").objectReferenceValue = modalObj;
            so.FindProperty("_modalDimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("_modalTitleText").objectReferenceValue = titleText;
            so.FindProperty("_modalWarningText").objectReferenceValue = warningText;
            so.FindProperty("_keepProgressButton").objectReferenceValue = keepBtn;
            so.FindProperty("_loadExistingButton").objectReferenceValue = switchBtn;
            so.FindProperty("_cancelButton").objectReferenceValue = cancelBtn;
            so.FindProperty("_keepConfirmationRoot").objectReferenceValue = keepConfirmObj;
            so.FindProperty("_confirmKeepButton").objectReferenceValue = confirmKeepBtn;
            so.FindProperty("_backFromKeepButton").objectReferenceValue = backBtn;
            so.ApplyModifiedProperties();

            var soCtrl = new SerializedObject(controller);
            soCtrl.FindProperty("_view").objectReferenceValue = view;
            soCtrl.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[LobbyAccountLinkPanelBuilder] Account Link UI built successfully.");
        }
    }
}
#endif
