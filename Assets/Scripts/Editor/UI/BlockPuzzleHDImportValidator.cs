#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace BlockBlast.Editor.UI
{
    internal static class BlockPuzzleHDImportValidator
    {
        private const string AtlasFolder = BlockPuzzleHDManifest.Root + "/Atlases";

        [MenuItem("Tools/Block Blast/UI/Apply HD Import Settings")]
        public static void ApplyImportSettings()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int applied = 0;
            foreach (BlockPuzzleHDSpriteSpec spec in BlockPuzzleHDManifest.Build())
            {
                if (!File.Exists(spec.AssetPath)) continue;
                if (AssetImporter.GetAtPath(spec.AssetPath) is not TextureImporter importer) continue;

                int maxSize = spec.Width > 512 || spec.Height > 512 ? 1024 : 512;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spritePivot = new Vector2(.5f, .5f);
                importer.spriteBorder = spec.Border;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.crunchedCompression = false;

                var android = new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = maxSize,
                    format = TextureImporterFormat.ASTC_4x4,
                    compressionQuality = 100,
                    allowsAlphaSplitting = false
                };
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
                applied++;
            }
            Debug.Log($"[BlockPuzzleHDImportValidator] Applied settings to {applied} sprites.");
        }

        [MenuItem("Tools/Block Blast/UI/Rebuild HD Atlases")]
        public static void RebuildAtlases()
        {
            Directory.CreateDirectory(AtlasFolder);
            List<BlockPuzzleHDSpriteSpec> specs = BlockPuzzleHDManifest.Build();
            foreach (IGrouping<string, BlockPuzzleHDSpriteSpec> group in specs.GroupBy(x => x.Atlas))
            {
                string path = $"{AtlasFolder}/{group.Key}.spriteatlas";
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                {
                    atlas = new SpriteAtlas();
                    AssetDatabase.CreateAsset(atlas, path);
                }
                Object[] current = SpriteAtlasExtensions.GetPackables(atlas);
                if (current.Length > 0) SpriteAtlasExtensions.Remove(atlas, current);

                Object[] sprites = group.Select(x => AssetDatabase.LoadAssetAtPath<Sprite>(x.AssetPath)).Where(x => x != null).Cast<Object>().ToArray();
                if (sprites.Length > 0) SpriteAtlasExtensions.Add(atlas, sprites);
                SpriteAtlasExtensions.SetPackingSettings(atlas, new SpriteAtlasPackingSettings
                {
                    blockOffset = 1,
                    padding = 4,
                    enableRotation = false,
                    enableTightPacking = false
                });
                SpriteAtlasExtensions.SetTextureSettings(atlas, new SpriteAtlasTextureSettings
                {
                    readable = false,
                    generateMipMaps = false,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear
                });
                SpriteAtlasExtensions.SetPlatformSettings(atlas, new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = 4096,
                    format = TextureImporterFormat.ASTC_4x4,
                    compressionQuality = 100
                });
                EditorUtility.SetDirty(atlas);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BlockPuzzleHDImportValidator] Rebuilt four consumer-group atlases.");
        }

        [MenuItem("Tools/Block Blast/UI/Validate HD UI Pack")]
        public static void ValidatePack()
        {
            List<BlockPuzzleHDSpriteSpec> specs = BlockPuzzleHDManifest.Build();
            int missing = 0;
            int wrongSize = 0;
            int importErrors = 0;
            int edgeAlphaErrors = 0;
            int atlasErrors = 0;
            int prefabErrors = 0;
            int touchErrors = 0;
            foreach (BlockPuzzleHDSpriteSpec spec in specs)
            {
                if (!File.Exists(spec.AssetPath)) { missing++; Debug.LogError($"[HD UI] Missing {spec.AssetPath}"); continue; }
                byte[] bytes = File.ReadAllBytes(spec.AssetPath);
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(source, bytes, false)) { importErrors++; Object.DestroyImmediate(source); continue; }
                if (source.width != spec.Width || source.height != spec.Height)
                {
                    wrongSize++;
                    Debug.LogError($"[HD UI] Wrong size {spec.AssetPath}: {source.width}x{source.height}, expected {spec.Width}x{spec.Height}");
                }
                if (HasOpaqueEdge(source)) { edgeAlphaErrors++; Debug.LogError($"[HD UI] Opaque outer edge in {spec.AssetPath}"); }
                Object.DestroyImmediate(source);

                if (AssetImporter.GetAtPath(spec.AssetPath) is not TextureImporter importer || !HasExpectedImportSettings(importer, spec))
                {
                    importErrors++;
                    Debug.LogError($"[HD UI] Import mismatch {spec.AssetPath}");
                }
            }

            ValidateAtlases(specs, ref atlasErrors);
            ValidatePrefabs(ref prefabErrors, ref touchErrors);

            string summary = $"[BlockPuzzleHDImportValidator] count={specs.Count}/{BlockPuzzleHDManifest.ExpectedCount}, missing={missing}, wrongSize={wrongSize}, importErrors={importErrors}, opaqueEdges={edgeAlphaErrors}, atlasErrors={atlasErrors}, prefabErrors={prefabErrors}, touchErrors={touchErrors}";
            if (specs.Count == BlockPuzzleHDManifest.ExpectedCount && missing == 0 && wrongSize == 0 && importErrors == 0 && edgeAlphaErrors == 0 && atlasErrors == 0 && prefabErrors == 0 && touchErrors == 0) Debug.Log(summary + " PASS");
            else Debug.LogError(summary + " FAIL");
        }

        private static bool HasExpectedImportSettings(TextureImporter importer, BlockPuzzleHDSpriteSpec spec)
        {
            int maxSize = spec.Width > 512 || spec.Height > 512 ? 1024 : 512;
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            return importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && Mathf.Approximately(importer.spritePixelsPerUnit, 100f)
                && importer.spritePivot == new Vector2(.5f, .5f)
                && importer.spriteBorder == spec.Border
                && importer.alphaSource == TextureImporterAlphaSource.FromInput
                && importer.alphaIsTransparency
                && importer.sRGBTexture
                && !importer.mipmapEnabled
                && !importer.isReadable
                && importer.wrapMode == TextureWrapMode.Clamp
                && importer.filterMode == FilterMode.Bilinear
                && importer.maxTextureSize == maxSize
                && android.overridden
                && android.maxTextureSize == maxSize
                && android.format == TextureImporterFormat.ASTC_4x4;
        }

        private static void ValidateAtlases(List<BlockPuzzleHDSpriteSpec> specs, ref int errors)
        {
            var seen = new HashSet<string>();
            foreach (IGrouping<string, BlockPuzzleHDSpriteSpec> group in specs.GroupBy(x => x.Atlas))
            {
                string atlasPath = $"{AtlasFolder}/{group.Key}.spriteatlas";
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    errors++;
                    Debug.LogError($"[HD UI] Missing atlas {atlasPath}");
                    continue;
                }

                HashSet<string> expected = group.Select(x => x.AssetPath).ToHashSet();
                HashSet<string> actual = SpriteAtlasExtensions.GetPackables(atlas).Select(AssetDatabase.GetAssetPath).ToHashSet();
                if (!expected.SetEquals(actual))
                {
                    errors++;
                    Debug.LogError($"[HD UI] Atlas membership mismatch {group.Key}: expected={expected.Count}, actual={actual.Count}");
                }
                foreach (string path in actual) if (!seen.Add(path)) { errors++; Debug.LogError($"[HD UI] Duplicate atlas member {path}"); }
            }
            if (seen.Count != specs.Count) { errors++; Debug.LogError($"[HD UI] Atlas unique membership {seen.Count}/{specs.Count}"); }
        }

        private static void ValidatePrefabs(ref int prefabErrors, ref int touchErrors)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI/Components" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite != null) continue;
                    prefabErrors++;
                    Debug.LogError($"[HD UI] Missing sprite on {path}:{image.name}");
                }
                foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
                {
                    Rect rect = ((RectTransform)selectable.transform).rect;
                    if (rect.width >= 88f && rect.height >= 88f) continue;
                    touchErrors++;
                    Debug.LogError($"[HD UI] Touch target below 88 px on {path}:{selectable.name} ({rect.width}x{rect.height})");
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool HasOpaqueEdge(Texture2D texture)
        {
            int margin = Mathf.Max(2, Mathf.Min(texture.width, texture.height) / 128);
            for (int inset = 0; inset < margin; inset++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (texture.GetPixel(x, inset).a > .02f || texture.GetPixel(x, texture.height - 1 - inset).a > .02f) return true;
                }
                for (int y = 0; y < texture.height; y++) if (texture.GetPixel(inset, y).a > .02f || texture.GetPixel(texture.width - 1 - inset, y).a > .02f) return true;
            }
            return false;
        }
    }
}
#endif
