#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.Editor
{
    public static class LobbySettingsIconWiring
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
        private const string IconsBasePath = "Assets/Art/UI/CozyStationeryUI/Components/Icons/";

        [MenuItem("BlockBlast/Apply Settings Icons To Scene")]
        public static void ApplyIconsToScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var iconMusic = AssetDatabase.LoadAssetAtPath<Sprite>(IconsBasePath + "icon_music.png");
            var iconSfx = AssetDatabase.LoadAssetAtPath<Sprite>(IconsBasePath + "icon_sfx.png");
            var iconVibration = AssetDatabase.LoadAssetAtPath<Sprite>(IconsBasePath + "icon_vibration.png");
            var iconCameraShake = AssetDatabase.LoadAssetAtPath<Sprite>(IconsBasePath + "icon_camera_shake.png");
            var iconHelp = AssetDatabase.LoadAssetAtPath<Sprite>(IconsBasePath + "icon_help.png");
            var iconPrivacy = AssetDatabase.LoadAssetAtPath<Sprite>(IconsBasePath + "icon_privacy.png");

            Debug.Log($"[LobbySettingsIconWiring] Loaded sprites: music={iconMusic != null}, sfx={iconSfx != null}, vib={iconVibration != null}, cam={iconCameraShake != null}, help={iconHelp != null}, priv={iconPrivacy != null}");

            // Row_Music
            var rowMusicObj = GameObject.Find("Row_Music");
            if (rowMusicObj != null)
            {
                var iconTr = rowMusicObj.transform.Find("Icon");
                if (iconTr != null && iconTr.TryGetComponent<Image>(out var img))
                {
                    Undo.RecordObject(img, "Update Row_Music Icon");
                    img.sprite = iconMusic;
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[LobbySettingsIconWiring] Row_Music Icon sprite wired.");
                }
                else
                {
                    Debug.LogError("[LobbySettingsIconWiring] Row_Music/Icon or Image not found.");
                }
            }
            else
            {
                Debug.LogError("[LobbySettingsIconWiring] Row_Music not found in scene.");
            }

            // Row_SFX
            var rowSfxObj = GameObject.Find("Row_SFX");
            if (rowSfxObj != null)
            {
                var iconTr = rowSfxObj.transform.Find("Icon");
                if (iconTr != null && iconTr.TryGetComponent<Image>(out var img))
                {
                    Undo.RecordObject(img, "Update Row_SFX Icon");
                    img.sprite = iconSfx;
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[LobbySettingsIconWiring] Row_SFX Icon sprite wired.");
                }
                else
                {
                    Debug.LogError("[LobbySettingsIconWiring] Row_SFX/Icon or Image not found.");
                }
            }
            else
            {
                Debug.LogError("[LobbySettingsIconWiring] Row_SFX not found in scene.");
            }

            // Row_Vibration
            var rowVibObj = GameObject.Find("Row_Vibration");
            if (rowVibObj != null)
            {
                var iconTr = rowVibObj.transform.Find("Row_Top/Icon");
                if (iconTr != null && iconTr.TryGetComponent<Image>(out var img))
                {
                    Undo.RecordObject(img, "Update Row_Vibration Icon");
                    img.sprite = iconVibration;
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[LobbySettingsIconWiring] Row_Vibration Icon sprite wired.");
                }
                else
                {
                    Debug.LogError("[LobbySettingsIconWiring] Row_Vibration/Row_Top/Icon or Image not found.");
                }
            }
            else
            {
                Debug.LogError("[LobbySettingsIconWiring] Row_Vibration not found in scene.");
            }

            // Row_CameraShake
            var rowCamObj = GameObject.Find("Row_CameraShake");
            if (rowCamObj != null)
            {
                var iconTr = rowCamObj.transform.Find("Row_Top/Icon");
                if (iconTr != null && iconTr.TryGetComponent<Image>(out var img))
                {
                    Undo.RecordObject(img, "Update Row_CameraShake Icon");
                    img.sprite = iconCameraShake;
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[LobbySettingsIconWiring] Row_CameraShake Icon sprite wired.");
                }
                else
                {
                    Debug.LogError("[LobbySettingsIconWiring] Row_CameraShake/Row_Top/Icon or Image not found.");
                }
            }
            else
            {
                Debug.LogError("[LobbySettingsIconWiring] Row_CameraShake not found in scene.");
            }

            // Row_Help
            var rowHelpObj = GameObject.Find("Row_Help");
            if (rowHelpObj != null)
            {
                var iconTr = rowHelpObj.transform.Find("Icon");
                Image img = null;
                if (iconTr == null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(iconGo, "Create Row_Help Icon");
                    iconGo.transform.SetParent(rowHelpObj.transform, false);
                    iconGo.transform.SetSiblingIndex(0);
                    var rt = iconGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(52, 52);
                    img = iconGo.GetComponent<Image>();
                }
                else
                {
                    img = iconTr.GetComponent<Image>();
                }

                if (img != null)
                {
                    Undo.RecordObject(img, "Update Row_Help Icon");
                    img.sprite = iconHelp;
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[LobbySettingsIconWiring] Row_Help Icon sprite wired.");
                }
            }
            else
            {
                Debug.LogError("[LobbySettingsIconWiring] Row_Help not found in scene.");
            }

            // Row_Privacy
            var rowPrivObj = GameObject.Find("Row_Privacy");
            if (rowPrivObj != null)
            {
                var iconTr = rowPrivObj.transform.Find("Icon");
                Image img = null;
                if (iconTr == null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(iconGo, "Create Row_Privacy Icon");
                    iconGo.transform.SetParent(rowPrivObj.transform, false);
                    iconGo.transform.SetSiblingIndex(0);
                    var rt = iconGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(52, 52);
                    img = iconGo.GetComponent<Image>();
                }
                else
                {
                    img = iconTr.GetComponent<Image>();
                }

                if (img != null)
                {
                    Undo.RecordObject(img, "Update Row_Privacy Icon");
                    img.sprite = iconPrivacy;
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[LobbySettingsIconWiring] Row_Privacy Icon sprite wired.");
                }
            }
            else
            {
                Debug.LogError("[LobbySettingsIconWiring] Row_Privacy not found in scene.");
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[LobbySettingsIconWiring] Scene saved successfully with updated settings icons.");
        }
    }
}
#endif
