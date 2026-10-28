using System;
using BlockBlast.Features.GameFlow.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Editor
{
    public static class PlayRockEntranceBinder
    {
        private const string PrefabPath = "Assets/Prefabs/Worlds/DefaultWorld/DefaultPlayGround.prefab";
        private const string ScenePath = "Assets/Scenes/PlayScene.unity";

        [MenuItem("BlockBlast/Setup Play Rock Entrance")]
        public static string ExecuteSetup()
        {
            var report = new System.Text.StringBuilder();

            // 1. Configure Prefab
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = prefabRoot.GetComponent<PlaySceneRockRiseEntranceView>();
                if (view == null)
                {
                    view = prefabRoot.AddComponent<PlaySceneRockRiseEntranceView>();
                }

                Transform centerRock = null;
                Transform leftRock = null;
                Transform rightRock = null;

                for (int i = 0; i < prefabRoot.transform.childCount; i++)
                {
                    var child = prefabRoot.transform.GetChild(i);
                    if (child.name == "trail_rock_straight (2)")
                    {
                        centerRock = child;
                    }
                    else if (child.name == "trail_rock_straight")
                    {
                        leftRock = child;
                    }
                    else if (child.name == "trail_rock_straight (1)")
                    {
                        rightRock = child;
                    }
                }

                if (centerRock == null || leftRock == null || rightRock == null)
                {
                    return $"ERROR: Rocks missing in prefab: center={centerRock != null}, left={leftRock != null}, right={rightRock != null}";
                }

                var so = new SerializedObject(view);
                var rocksProp = so.FindProperty("_rocks");
                rocksProp.arraySize = 3;
                rocksProp.GetArrayElementAtIndex(0).objectReferenceValue = centerRock;
                rocksProp.GetArrayElementAtIndex(1).objectReferenceValue = leftRock;
                rocksProp.GetArrayElementAtIndex(2).objectReferenceValue = rightRock;

                so.FindProperty("_initialDelay").floatValue = 0.45f;
                so.FindProperty("_stagger").floatValue = 0.08f;
                so.FindProperty("_riseDistance").floatValue = 1.25f;
                so.FindProperty("_riseDuration").floatValue = 0.38f;
                so.FindProperty("_settleDuration").floatValue = 0.10f;
                so.FindProperty("_startScaleMultiplier").vector3Value = new Vector3(0.82f, 1.14f, 0.82f);
                so.FindProperty("_overshootScaleMultiplier").vector3Value = new Vector3(1.06f, 0.94f, 1.06f);
                so.FindProperty("_tiltAngle").floatValue = 4f;

                so.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                report.AppendLine("Prefab configured and saved.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            // 2. Clean Scene
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            GameObject gpEnv = null;
            var q = new System.Collections.Generic.Queue<Transform>();
            foreach (var r in roots)
            {
                q.Enqueue(r.transform);
            }

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.name == "GamePlayEnvironment")
                {
                    gpEnv = cur.gameObject;
                    break;
                }

                for (int i = 0; i < cur.childCount; i++)
                {
                    q.Enqueue(cur.GetChild(i));
                }
            }

            if (gpEnv == null)
            {
                return "ERROR: GamePlayEnvironment not found in PlayScene";
            }

            var sceneView = gpEnv.GetComponent<PlaySceneRockRiseEntranceView>();
            if (sceneView != null)
            {
                UnityEngine.Object.DestroyImmediate(sceneView, true);
                report.AppendLine("Removed PlaySceneRockRiseEntranceView from GamePlayEnvironment in PlayScene.");
            }
            else
            {
                report.AppendLine("PlaySceneRockRiseEntranceView was already not present on GamePlayEnvironment.");
            }

            EditorUtility.SetDirty(gpEnv);
            EditorSceneManager.MarkSceneDirty(scene);
            bool sceneSaved = EditorSceneManager.SaveScene(scene);
            report.AppendLine($"PlayScene saved: {sceneSaved}");

            return report.ToString();
        }
    }
}
