using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility tool to migrate and validate Pet block prefabs with an AnimationPivot wrapper hierarchy.
/// Ensures 100% visual, material, reference, and transform consistency.
/// </summary>
public static class PetAnimationPivotMigrationTool
{
    public const string PetPrefabsFolder = "Assets/Prefabs/Blocks/Pet";
    public const string PetFbxFolder = "Assets/Art/Blocks/Pet";
    public const string PetAssetPath = "Assets/ScriptableObjects/Effects/Clear/Animations/Wood/BlockTypes/Pet.asset";
    public const string PivotName = "AnimationPivot";
    public const float BoundsTolerance = 0.001f;

    public struct ValidationReport
    {
        public string prefabName;
        public string prefabPath;
        public string guid;
        public bool isMigrated;
        public bool isValid;
        public string message;
        public Vector3 pivotLocalPos;
        public Vector3 boundsCenter;
        public Vector3 boundsSize;
    }

    [MenuItem("Tools/Block Blast/Pet/Validate Animation Pivots")]
    public static void ValidateAllMenu()
    {
        var reports = ValidateAll(out int validCount, out int pendingCount, out int errorCount);
        var sb = new StringBuilder();
        sb.AppendLine($"[PetAnimationPivotMigrationTool] Validation Complete: Total={reports.Count}, Valid={validCount}, Pending={pendingCount}, Errors={errorCount}");
        foreach (var r in reports)
        {
            string status = r.isValid ? (r.isMigrated ? "<color=green>MIGRATED_VALID</color>" : "<color=yellow>PENDING_MIGRATION</color>") : "<color=red>INVALID</color>";
            sb.AppendLine($"• {r.prefabName} [{status}]: {r.message} (Pivot={r.pivotLocalPos.ToString("F4")}, Bounds={r.boundsCenter.ToString("F4")})");
        }
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Block Blast/Pet/Migrate Animation Pivots")]
    public static void MigrateAllMenu()
    {
        int migrated = MigrateAll(out string summary);
        Debug.Log($"[PetAnimationPivotMigrationTool] Migration Finished: {migrated} prefabs processed.\n{summary}");
    }

    public static List<ValidationReport> ValidateAll(out int validCount, out int pendingCount, out int errorCount)
    {
        validCount = 0;
        pendingCount = 0;
        errorCount = 0;

        var reports = new List<ValidationReport>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PetPrefabsFolder });

        var petAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PetAssetPath);
        var petSo = petAsset != null ? new SerializedObject(petAsset) : null;
        var variantsProp = petSo?.FindProperty("variants");

        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var report = ValidatePrefab(go, path, guid, variantsProp);
            reports.Add(report);

            if (!report.isValid) errorCount++;
            else if (report.isMigrated) validCount++;
            else pendingCount++;
        }

        return reports;
    }

    public static ValidationReport ValidatePrefab(GameObject go, string path, string guid, SerializedProperty variantsProp)
    {
        var report = new ValidationReport
        {
            prefabName = go.name,
            prefabPath = path,
            guid = guid,
            isValid = false,
            isMigrated = false
        };

        Transform pivot = go.transform.Find(PivotName);
        var renderers = go.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            report.message = "Missing renderer in prefab.";
            return report;
        }

        Bounds bounds = CalculateCombinedBounds(go.transform, go);
        report.boundsCenter = bounds.center;
        report.boundsSize = bounds.size;

        // Check reference in Pet.asset
        bool foundInPetAsset = false;
        if (variantsProp != null)
        {
            for (int i = 0; i < variantsProp.arraySize; i++)
            {
                var p = variantsProp.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue;
                if (p == go) { foundInPetAsset = true; break; }
            }
        }

        if (!foundInPetAsset)
        {
            report.message = "Prefab not referenced in Pet.asset.";
            return report;
        }

        if (pivot == null)
        {
            // Pending migration candidate
            string expectedFbx = $"{PetFbxFolder}/{go.name}.fbx";
            if (!File.Exists(expectedFbx))
            {
                report.message = $"Source FBX not found at {expectedFbx}.";
                return report;
            }

            report.isValid = true;
            report.isMigrated = false;
            report.message = "Ready for migration (no AnimationPivot yet).";
            return report;
        }

        // Has pivot: verify structure
        report.pivotLocalPos = pivot.localPosition;
        if (pivot.parent != go.transform)
        {
            report.message = "AnimationPivot is not a direct child of root.";
            return report;
        }

        if (pivot.childCount != 1)
        {
            report.message = $"AnimationPivot must have exactly 1 child model (found {pivot.childCount}).";
            return report;
        }

        Transform modelInstance = pivot.GetChild(0);
        var modelRenderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        if (modelRenderers.Length != 1)
        {
            report.message = $"Nested model must have exactly 1 renderer (found {modelRenderers.Length}).";
            return report;
        }

        float pivotOffsetDelta = Vector3.Distance(pivot.localPosition, bounds.center);
        if (pivotOffsetDelta > BoundsTolerance)
        {
            report.message = $"Pivot localPosition ({pivot.localPosition}) drifts from bounds center ({bounds.center}) by {pivotOffsetDelta:F4}.";
            return report;
        }

        report.isMigrated = true;
        report.isValid = true;
        report.message = "Hierarchy valid and correctly centered.";
        return report;
    }

    public static int MigrateAll(out string summary)
    {
        var sb = new StringBuilder();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PetPrefabsFolder });
        int migratedCount = 0;

        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(path);

            try
            {
                bool success = MigrateSinglePrefab(prefabName, out string msg);
                if (success)
                {
                    migratedCount++;
                    sb.AppendLine($"[PASS] {prefabName}: {msg}");
                }
                else
                {
                    sb.AppendLine($"[SKIP/FAIL] {prefabName}: {msg}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[ERROR] {prefabName}: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        summary = sb.ToString();
        return migratedCount;
    }

    public static bool MigrateSinglePrefab(string prefabName, out string message)
    {
        string prefabPath = $"{PetPrefabsFolder}/{prefabName}.prefab";
        string fbxPath = $"{PetFbxFolder}/{prefabName}.fbx";

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab == null)
        {
            message = $"Prefab not found at {prefabPath}";
            return false;
        }

        // Check if already migrated
        Transform existingPivot = existingPrefab.transform.Find(PivotName);
        if (existingPivot != null && existingPivot.childCount == 1)
        {
            message = "Already migrated with AnimationPivot.";
            return false;
        }

        GameObject sourceFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (sourceFbx == null)
        {
            message = $"Source FBX not found at {fbxPath}";
            return false;
        }

        // Pre-migration snapshot
        Bounds preBounds = CalculateCombinedBounds(existingPrefab.transform, existingPrefab);
        var preRenderers = existingPrefab.GetComponentsInChildren<Renderer>(true);
        int preRendererCount = preRenderers.Length;
        var preMeshFilter = existingPrefab.GetComponentInChildren<MeshFilter>(true);
        Mesh preMesh = preMeshFilter != null ? preMeshFilter.sharedMesh : null;
        Material[] preMaterials = preRenderers.Length > 0 ? preRenderers[0].sharedMaterials : new Material[0];

        // Construct temporary wrapper
        GameObject tempRoot = new GameObject(prefabName);
        try
        {
            tempRoot.transform.position = Vector3.zero;
            tempRoot.transform.rotation = Quaternion.identity;
            tempRoot.transform.localScale = Vector3.one;

            GameObject pivotGo = new GameObject(PivotName);
            pivotGo.transform.SetParent(tempRoot.transform, false);
            pivotGo.transform.localPosition = Vector3.zero;
            pivotGo.transform.localRotation = Quaternion.identity;
            pivotGo.transform.localScale = Vector3.one;

            // Instantiate source FBX as nested prefab
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourceFbx);
            modelInstance.transform.SetParent(tempRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // Restore material from snapshot if custom (e.g. palette.001)
            var postRend = modelInstance.GetComponentInChildren<Renderer>(true);
            if (postRend != null && preMaterials != null && preMaterials.Length > 0)
            {
                postRend.sharedMaterials = preMaterials;
            }

            // Calculate center in tempRoot space
            Bounds combinedBounds = CalculateCombinedBounds(tempRoot.transform, modelInstance);
            Vector3 center = combinedBounds.center;

            pivotGo.transform.localPosition = center;
            pivotGo.transform.localRotation = Quaternion.identity;
            pivotGo.transform.localScale = Vector3.one;

            // Reparent model under pivot with world position preserved
            modelInstance.transform.SetParent(pivotGo.transform, true);

            // Bounds tolerance check
            Bounds postBounds = CalculateCombinedBounds(tempRoot.transform, tempRoot);
            float centerDelta = Vector3.Distance(preBounds.center, postBounds.center);
            Vector3 sizeDelta = new Vector3(
                Mathf.Abs(preBounds.size.x - postBounds.size.x),
                Mathf.Abs(preBounds.size.y - postBounds.size.y),
                Mathf.Abs(preBounds.size.z - postBounds.size.z)
            );

            if (centerDelta > BoundsTolerance || sizeDelta.x > BoundsTolerance || sizeDelta.y > BoundsTolerance || sizeDelta.z > BoundsTolerance)
            {
                throw new InvalidOperationException($"Bounds drift exceeded tolerance! CenterDelta={centerDelta:F5}, SizeDelta={sizeDelta}");
            }

            // Save over existing prefab path preserving GUID and .meta
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);
            if (savedPrefab == null)
            {
                throw new InvalidOperationException($"Failed to save prefab asset at {prefabPath}");
            }

            // Rewire Pet.asset reference to the new root
            RewirePetAsset(prefabName, savedPrefab);

            message = $"Successfully migrated. PivotCenter={center.ToString("F4")}, CenterDelta={centerDelta:F5}";
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tempRoot);
        }
    }

    public static void RewirePetAsset(string prefabName, GameObject savedPrefab)
    {
        var petAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PetAssetPath);
        if (petAsset == null) return;

        var so = new SerializedObject(petAsset);
        var variantsProp = so.FindProperty("variants");
        if (variantsProp == null) return;

        bool updated = false;
        for (int i = 0; i < variantsProp.arraySize; i++)
        {
            var item = variantsProp.GetArrayElementAtIndex(i);
            var prefabProp = item.FindPropertyRelative("prefab");
            var currentObj = prefabProp.objectReferenceValue;

            if (currentObj != null && currentObj.name == prefabName)
            {
                prefabProp.objectReferenceValue = savedPrefab;
                updated = true;
                break;
            }
        }

        if (updated)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(petAsset);
        }
    }

    public static Bounds CalculateCombinedBounds(Transform rootSpace, GameObject target)
    {
        Bounds combinedBounds = new Bounds();
        bool initialized = false;

        var meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;
            Bounds localBounds = mf.sharedMesh.bounds;
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            Vector3[] corners = new Vector3[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            Matrix4x4 localToRoot = rootSpace.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 pt = localToRoot.MultiplyPoint3x4(corners[i]);
                if (!initialized)
                {
                    combinedBounds = new Bounds(pt, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(pt);
                }
            }
        }

        if (!initialized)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                Bounds b = r.bounds;
                Vector3 c = rootSpace.InverseTransformPoint(b.center);
                if (!initialized)
                {
                    combinedBounds = new Bounds(c, b.size);
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(new Bounds(c, b.size));
                }
            }
        }

        return combinedBounds;
    }
}
