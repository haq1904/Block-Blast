#if UNITY_EDITOR
using BlockBlast.Features.UI.Animation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockBlast.Editor.UI
{
    public static class BlockPuzzleHDShowcaseBuilder
    {
        private const string Root = "Assets/Art/UI/BlockPuzzleHD";
        private const string Prefabs = "Assets/Prefabs/UI/Components";
        private const string ScenePath = "Assets/Scenes/UIPack_ShowcaseScene.unity";

        [MenuItem("Tools/Block Blast/UI/Build HD Prefabs And Showcase")]
        public static void Build()
        {
            BuildPrefabs();
            BuildShowcaseSafely();
            AssetDatabase.SaveAssets();
            Debug.Log("[BlockPuzzleHD] Rewired UI prefabs and rebuilt the showcase without saving the active consumer scene.");
        }

        private static void BuildPrefabs()
        {
            EnsureFolder(Prefabs);
            SaveButton("UIButton_Primary", new Vector2(620, 144), B("btn_primary_normal"), B("btn_primary_pressed"), "PLAY", true);
            SaveButton("UIButton_Secondary", new Vector2(420, 120), B("btn_secondary_normal"), B("btn_secondary_pressed"), "RETRY", true);
            SaveButton("UIButton_Green", new Vector2(420, 120), B("btn_success_normal"), B("btn_success_pressed"), "CLAIM", true);
            SaveButton("UIButton_Danger", new Vector2(420, 120), B("btn_danger_normal"), B("btn_danger_pressed"), "DELETE", true);
            SaveButton("UIButton_Circle", new Vector2(104, 104), B("btn_circle_primary_normal"), B("btn_circle_primary_pressed"), string.Empty, false, S("icon_settings"));
            SaveButton("UIButton_Square", new Vector2(112, 112), B("btn_square_secondary_normal"), B("btn_square_secondary_pressed"), string.Empty, false, S("icon_gift"));
            SavePanel("UICard_Container", new Vector2(760, 360), P("panel_card_cream"));
            SavePanel("UICard_Light", new Vector2(760, 360), P("panel_card_light"));
            SavePanel("UICard_Dark", new Vector2(760, 360), P("panel_card_dark"));
            SavePanel("UIModal_Dialog", new Vector2(900, 760), P("panel_modal_light"), true);
            SaveCounter();
            SaveBadge();
            SaveToggle();
            SaveProgress();
            SaveLoading();
        }

        private static void SaveButton(string name, Vector2 size, string normalPath, string pressedPath, string label, bool sliced, string iconPath = null)
        {
            var go = RectObject(name, size, typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonFeedbackView));
            var image = go.GetComponent<Image>();
            image.sprite = Sprite(normalPath);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = sliced ? 3f : 1f;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Sprite(pressedPath), selectedSprite = Sprite(normalPath) };
            var colors = button.colors;
            colors.disabledColor = new Color(1, 1, 1, .45f);
            button.colors = colors;
            if (!string.IsNullOrEmpty(iconPath))
            {
                var icon = ImageChild(go.transform, "Icon", size * .50f, Sprite(iconPath), Image.Type.Simple);
                icon.preserveAspect = true;
            }
            if (!string.IsNullOrEmpty(label)) TextChild(go.transform, "Label", size, label, Mathf.Clamp(size.y * .34f, 32, 56), Color.white);
            SaveAndDestroy(go, Prefabs + "/" + name + ".prefab");
        }

        private static void SavePanel(string name, Vector2 size, string path, bool animated = false)
        {
            var types = animated
                ? new[] { typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(UIPanelTransitionView) }
                : new[] { typeof(CanvasRenderer), typeof(Image) };
            var go = RectObject(name, size, types);
            var image = go.GetComponent<Image>();
            image.sprite = Sprite(path);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.raycastTarget = false;
            SaveAndDestroy(go, Prefabs + "/" + name + ".prefab");
        }

        private static void SaveCounter()
        {
            var go = RectObject("UICounter_Pill", new Vector2(260, 96), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = Sprite(P("panel_counter_coin"));
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2.5f;
            var icon = ImageChild(go.transform, "Icon", new Vector2(64, 64), Sprite(S("icon_coin")), Image.Type.Simple);
            icon.rectTransform.anchoredPosition = new Vector2(-78, 0);
            TextChild(go.transform, "Value", new Vector2(150, 80), "12,450", 34, new Color(.20f, .16f, .13f)).rectTransform.anchoredPosition = new Vector2(35, 0);
            SaveAndDestroy(go, Prefabs + "/UICounter_Pill.prefab");
        }

        private static void SaveBadge()
        {
            var go = RectObject("UIBadge_Status", new Vector2(180, 72), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = Sprite(A("badge_best"));
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2.5f;
            TextChild(go.transform, "Label", new Vector2(150, 60), "BEST", 27, Color.white);
            SaveAndDestroy(go, Prefabs + "/UIBadge_Status.prefab");
        }

        private static void SaveToggle()
        {
            var go = RectObject("UIToggle_Switch", new Vector2(180, 88), typeof(CanvasRenderer), typeof(Image), typeof(Toggle), typeof(UIToggleFeedbackView));
            var track = go.GetComponent<Image>();
            track.sprite = Sprite(C("toggle_track_off"));
            track.type = Image.Type.Sliced;
            track.pixelsPerUnitMultiplier = 2.5f;
            var knob = ImageChild(go.transform, "Knob", new Vector2(76, 76), Sprite(C("toggle_knob_normal")), Image.Type.Simple);
            knob.rectTransform.anchoredPosition = new Vector2(-43, 0);
            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.graphic = null;
            var feedback = new SerializedObject(go.GetComponent<UIToggleFeedbackView>());
            feedback.FindProperty("_toggle").objectReferenceValue = toggle;
            feedback.FindProperty("_track").objectReferenceValue = track;
            feedback.FindProperty("_knob").objectReferenceValue = knob;
            feedback.FindProperty("_offSprite").objectReferenceValue = Sprite(C("toggle_track_off"));
            feedback.FindProperty("_onSprite").objectReferenceValue = Sprite(C("toggle_track_on"));
            feedback.ApplyModifiedPropertiesWithoutUndo();
            SaveAndDestroy(go, Prefabs + "/UIToggle_Switch.prefab");
        }

        private static void SaveProgress()
        {
            var go = RectObject("UIProgressBar_Capsule", new Vector2(760, 64), typeof(UIProgressBarView));
            var track = ImageChild(go.transform, "Track", new Vector2(760, 64), Sprite(C("progress_track_dark")), Image.Type.Sliced);
            track.pixelsPerUnitMultiplier = 3f;
            Stretch(track.rectTransform);
            var fill = ImageChild(track.transform, "Fill", new Vector2(760, 64), Sprite(C("progress_fill_green")), Image.Type.Filled);
            Stretch(fill.rectTransform);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = .72f;
            var serialized = new SerializedObject(go.GetComponent<UIProgressBarView>());
            serialized.FindProperty("_fill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SaveAndDestroy(go, Prefabs + "/UIProgressBar_Capsule.prefab");
        }

        private static void SaveLoading()
        {
            var go = RectObject("UILoading_Blocks", new Vector2(160, 160), typeof(CanvasRenderer), typeof(Image), typeof(UILoadingIndicatorView));
            var image = go.GetComponent<Image>();
            image.sprite = Sprite(L("loading_blocks_01"));
            image.preserveAspect = true;
            var loading = new SerializedObject(go.GetComponent<UILoadingIndicatorView>());
            loading.FindProperty("_image").objectReferenceValue = image;
            SerializedProperty frames = loading.FindProperty("_frames");
            frames.arraySize = 8;
            for (int i = 0; i < 8; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = Sprite(L($"loading_blocks_{i:00}"));
            loading.ApplyModifiedPropertiesWithoutUndo();
            SaveAndDestroy(go, Prefabs + "/UILoading_Blocks.prefab");
        }

        private static void BuildShowcaseSafely()
        {
            Scene original = SceneManager.GetActiveScene();
            Scene showcase = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !showcase.IsValid() || !showcase.isLoaded;
            if (openedHere) showcase = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(showcase);
            foreach (var root in showcase.GetRootGameObjects()) Object.DestroyImmediate(root);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var previewCamera = cameraGo.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.13f, .22f, .27f);
            int uiLayer = LayerMask.NameToLayer("UI");
            previewCamera.cullingMask = uiLayer >= 0 ? 1 << uiLayer : ~0;
            cameraGo.transform.position = new Vector3(0, 0, -10);
            var light = new GameObject("Main Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;

            var canvasGo = new GameObject("HD_UI_Showcase", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = previewCamera;
            canvas.planeDistance = 100f;
            canvas.sortingOrder = 500;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var background = ImageChild(canvasGo.transform, "Background", Vector2.zero, Sprite(P("panel_card_green")), Image.Type.Sliced);
            Stretch(background.rectTransform);
            background.color = new Color(.72f, .95f, .83f, 1);

            var content = RectObject("SafeArea_Content", new Vector2(960, 1760)).GetComponent<RectTransform>();
            content.SetParent(canvasGo.transform, false);
            TextChild(content, "Title", new Vector2(920, 110), "BLOCK PUZZLE UI — HD COMPONENT PACK", 48, new Color(.17f, .20f, .30f)).rectTransform.anchoredPosition = new Vector2(0, 790);
            TextChild(content, "Subtitle", new Vector2(920, 54), "Separate 512–1024 px PNG • normal / pressed • 9-slice", 25, new Color(.25f, .35f, .42f)).rectTransform.anchoredPosition = new Vector2(0, 720);

            PlacePrefab(content, "UIButton_Primary", new Vector2(0, 600), new Vector2(620, 144));
            PlacePrefab(content, "UIButton_Secondary", new Vector2(-235, 455), new Vector2(420, 120));
            PlacePrefab(content, "UIButton_Green", new Vector2(235, 455), new Vector2(420, 120));
            PlacePrefab(content, "UIButton_Circle", new Vector2(-350, 320), new Vector2(104, 104));
            PlacePrefab(content, "UIButton_Square", new Vector2(-220, 320), new Vector2(112, 112));
            PlacePrefab(content, "UICounter_Pill", new Vector2(40, 320), new Vector2(260, 96));
            PlacePrefab(content, "UIBadge_Status", new Vector2(315, 320), new Vector2(180, 72));

            var card = ImageChild(content, "NineSlice_Stress_Card", new Vector2(860, 330), Sprite(P("panel_card_light")), Image.Type.Sliced);
            card.pixelsPerUnitMultiplier = 2f;
            card.rectTransform.anchoredPosition = new Vector2(0, 75);
            TextChild(card.transform, "Heading", new Vector2(760, 60), "9-SLICE PANEL TEST", 36, new Color(.20f, .20f, .28f)).rectTransform.anchoredPosition = new Vector2(0, 90);
            var rowIcons = new[] { "icon_trophy", "icon_coin", "icon_gem", "icon_gift", "icon_paw", "icon_settings", "icon_hammer", "icon_bomb", "icon_magic_wand" };
            for (int i = 0; i < rowIcons.Length; i++)
            {
                string path = i < 6 ? S(rowIcons[i]) : G(rowIcons[i]);
                var icon = ImageChild(card.transform, rowIcons[i], new Vector2(76, 76), Sprite(path), Image.Type.Simple);
                icon.preserveAspect = true;
                icon.rectTransform.anchoredPosition = new Vector2(-320 + i * 80, 0);
            }
            PlacePrefab(content, "UIProgressBar_Capsule", new Vector2(0, -150), new Vector2(760, 64));
            PlacePrefab(content, "UIToggle_Switch", new Vector2(-300, -270), new Vector2(180, 88));
            PlacePrefab(content, "UILoading_Blocks", new Vector2(0, -285), new Vector2(160, 160));

            var fx = ImageChild(content, "FX_Combo", new Vector2(260, 260), Sprite(F("fx_combo_burst")), Image.Type.Simple);
            fx.preserveAspect = true;
            fx.rectTransform.anchoredPosition = new Vector2(300, -285);
            var modal = PlacePrefab(content, "UIModal_Dialog", new Vector2(0, -575), new Vector2(850, 360));
            TextChild(modal.transform, "ModalLabel", new Vector2(700, 100), "COMPONENTS READY", 40, new Color(.20f, .20f, .28f));

            if (uiLayer >= 0) SetLayerRecursively(canvasGo, uiLayer);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.MarkSceneDirty(showcase);
            EditorSceneManager.SaveScene(showcase, ScenePath);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            if (openedHere) EditorSceneManager.CloseScene(showcase, true);
        }

        private static GameObject PlacePrefab(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var rect = instance.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return instance;
        }

        private static GameObject RectObject(string name, Vector2 size, params System.Type[] extraTypes)
        {
            var go = new GameObject(name, typeof(RectTransform));
            foreach (var type in extraTypes) if (go.GetComponent(type) == null) go.AddComponent(type);
            go.GetComponent<RectTransform>().sizeDelta = size;
            return go;
        }

        private static Image ImageChild(Transform parent, string name, Vector2 size, Sprite sprite, Image.Type type)
        {
            var go = RectObject(name, size, typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI TextChild(Transform parent, string name, Vector2 size, string value, float fontSize, Color color)
        {
            var go = RectObject(name, size, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SaveAndDestroy(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        private static string B(string name) => Root + "/Buttons/" + name + ".png";
        private static string P(string name) => Root + "/Panels/" + name + ".png";
        private static string C(string name) => Root + "/Controls/" + name + ".png";
        private static string S(string name) => Root + "/Icons/System/" + name + ".png";
        private static string G(string name) => Root + "/Icons/BlockPuzzle/" + name + ".png";
        private static string A(string name) => Root + "/Badges/" + name + ".png";
        private static string F(string name) => Root + "/Effects/" + name + ".png";
        private static string L(string name) => Root + "/Loading/" + name + ".png";

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
