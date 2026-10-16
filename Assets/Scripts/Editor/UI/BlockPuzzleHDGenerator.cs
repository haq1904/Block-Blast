#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlockBlast.Editor.UI
{
    internal static class BlockPuzzleHDGenerator
    {
        private static readonly Color Orange = Hex("FF9F43");
        private static readonly Color OrangeDark = Hex("C96524");
        private static readonly Color Blue = Hex("39BDF8");
        private static readonly Color BlueDark = Hex("1677B8");
        private static readonly Color Green = Hex("39D98A");
        private static readonly Color GreenDark = Hex("168A59");
        private static readonly Color Coral = Hex("FF647C");
        private static readonly Color CoralDark = Hex("B8324B");
        private static readonly Color Cream = Hex("FFF4D8");
        private static readonly Color Ink = Hex("49342A");
        private static readonly Color Navy = Hex("263556");
        private static readonly Color Wood = Hex("C9793E");
        private static readonly Color WoodDark = Hex("754225");
        private static readonly Color Gold = Hex("FFD65A");
        private static readonly Color GoldDark = Hex("D98C22");

        private static readonly HashSet<string> ImageGenAnchors = new()
        {
            "btn_primary_normal", "btn_primary_pressed", "btn_secondary_normal", "btn_secondary_pressed",
            "btn_success_normal", "btn_circle_primary_normal", "panel_card_light", "icon_coin", "fx_combo_burst"
        };

        [MenuItem("Tools/Block Blast/UI/Generate HD Component Pack")]
        public static void GenerateAll()
        {
            List<BlockPuzzleHDSpriteSpec> specs = BlockPuzzleHDManifest.Build();
            if (specs.Count != BlockPuzzleHDManifest.ExpectedCount)
            {
                throw new InvalidOperationException($"HD UI manifest count is {specs.Count}; expected {BlockPuzzleHDManifest.ExpectedCount}.");
            }

            int generated = 0;
            foreach (BlockPuzzleHDSpriteSpec spec in specs)
            {
                if (ImageGenAnchors.Contains(spec.Name) && File.Exists(spec.AssetPath)) continue;
                Generate(spec);
                generated++;
            }

            CleanImageGenAlpha(false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BlockPuzzleHDImportValidator.ApplyImportSettings();
            BlockPuzzleHDImportValidator.RebuildAtlases();
            Debug.Log($"[BlockPuzzleHDGenerator] Generated {generated} sprites; preserved {specs.Count - generated} imagegen anchors.");
        }

        [MenuItem("Tools/Block Blast/UI/Clean HD ImageGen Alpha")]
        public static void CleanImageGenAlphaMenu()
        {
            CleanImageGenAlpha(true);
        }

        private static void CleanImageGenAlpha(bool refresh)
        {
            const byte alphaCutoff = 16;
            int changedPixels = 0;
            foreach (BlockPuzzleHDSpriteSpec spec in BlockPuzzleHDManifest.Build())
            {
                if (!ImageGenAnchors.Contains(spec.Name) || !File.Exists(spec.AssetPath)) continue;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(File.ReadAllBytes(spec.AssetPath), false))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    continue;
                }

                Color32[] pixels = texture.GetPixels32();
                bool changed = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a >= alphaCutoff || pixels[i].a == 0) continue;
                    pixels[i] = new Color32(0, 0, 0, 0);
                    changed = true;
                    changedPixels++;
                }

                if (changed)
                {
                    texture.SetPixels32(pixels);
                    texture.Apply(false, false);
                    File.WriteAllBytes(spec.AssetPath, texture.EncodeToPNG());
                }
                UnityEngine.Object.DestroyImmediate(texture);
            }

            if (refresh)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                BlockPuzzleHDImportValidator.ApplyImportSettings();
                Debug.Log($"[BlockPuzzleHDGenerator] Removed {changedPixels} low-alpha fringe pixels from ImageGen anchors.");
            }
        }

        private static void Generate(BlockPuzzleHDSpriteSpec spec)
        {
            var raster = new BlockPuzzleHDRaster(spec.Width, spec.Height);
            if (spec.Folder == "Buttons") GenerateButton(raster, spec.Name);
            else if (spec.Folder == "Panels") GeneratePanel(raster, spec.Name);
            else if (spec.Folder == "Controls") GenerateControl(raster, spec.Name);
            else if (spec.Folder == "Icons/System") BlockPuzzleHDIconPainter.PaintSystem(raster, spec.Name);
            else if (spec.Folder == "Icons/BlockPuzzle") BlockPuzzleHDIconPainter.PaintBlock(raster, spec.Name);
            else if (spec.Folder == "Badges") GenerateBadge(raster, spec.Name);
            else if (spec.Folder == "Effects") GenerateEffect(raster, spec.Name);
            else if (spec.Folder == "Loading") GenerateLoading(raster, spec.Name);
            raster.Save(spec.AssetPath);
        }

        private static void GenerateButton(BlockPuzzleHDRaster r, string name)
        {
            bool pressed = name.EndsWith("_pressed", StringComparison.Ordinal);
            (Color face, Color edge) = ButtonColors(name);
            bool circular = name.Contains("circle") || name.Contains("close") || name.Contains("back");
            if (name.Contains("square"))
            {
                float size = Mathf.Min(r.Width, r.Height) * .76f;
                float depth = size * (pressed ? .05f : .11f);
                Rect baseRect = new Rect((r.Width - size) * .5f, (r.Height - size) * .5f, size, size);
                r.RoundedRect(baseRect, size * .22f, BlockPuzzleHDRaster.Darken(edge, .65f), edge, 0f, edge);
                Rect faceRect = new Rect(baseRect.x, baseRect.y + depth, baseRect.width, baseRect.height - depth);
                r.RoundedRect(faceRect, size * .22f, BlockPuzzleHDRaster.Darken(face, .90f), BlockPuzzleHDRaster.Lighten(face, .15f), size * .035f, Cream);
            }
            else r.BeveledButton(face, edge, Cream, pressed, circular);

            if (name.Contains("close")) DrawCross(r);
            if (name.Contains("back")) DrawBack(r);
        }

        private static void GeneratePanel(BlockPuzzleHDRaster r, string name)
        {
            (Color face, Color edge, Color rim, bool dark) = PanelColors(name);
            if (name.StartsWith("divider_", StringComparison.Ordinal))
            {
                r.RoundedRect(new Rect(r.Width * .05f, r.Height * .30f, r.Width * .90f, r.Height * .40f), r.Height * .18f, edge, face, r.Height * .08f, rim);
                return;
            }
            float opacity = name.Contains("glass") ? .72f : 1f;
            r.BeveledPanel(face, edge, rim, dark, opacity);

            if (name.Contains("speech_tail"))
            {
                bool left = name.EndsWith("left", StringComparison.Ordinal);
                Vector2 center = r.Center;
                float direction = left ? -1f : 1f;
                r.Polygon(new[] { center + new Vector2(direction * r.Width * .34f, r.Height * .24f), center + new Vector2(direction * r.Width * .05f, r.Height * .18f), center + new Vector2(direction * r.Width * .20f, -r.Height * .30f) }, face);
            }
            else if (name.Contains("speech_"))
            {
                bool left = name.EndsWith("left", StringComparison.Ordinal);
                float direction = left ? -1f : 1f;
                r.Polygon(new[] { new Vector2(r.Width * (left ? .12f : .88f), r.Height * .27f), new Vector2(r.Width * (left ? .29f : .71f), r.Height * .31f), new Vector2(r.Width * (left ? .16f : .84f), r.Height * .09f) }, edge);
            }

            if (name.StartsWith("ribbon_", StringComparison.Ordinal)) DrawRibbonEnds(r, face, edge);
        }

        private static void GenerateControl(BlockPuzzleHDRaster r, string name)
        {
            if (name.StartsWith("toggle_track", StringComparison.Ordinal))
            {
                Color color = name.EndsWith("on", StringComparison.Ordinal) ? Green : Navy;
                r.RoundedRect(new Rect(r.Width * .06f, r.Height * .20f, r.Width * .88f, r.Height * .60f), r.Height * .29f, BlockPuzzleHDRaster.Darken(color, .65f), color, r.Height * .07f, Cream);
                return;
            }
            if (name.Contains("knob") || name.Contains("thumb"))
            {
                bool pressed = name.EndsWith("pressed", StringComparison.Ordinal);
                r.BeveledButton(Cream, Hex("C7BFAF"), Color.white, pressed, true);
                return;
            }
            if (name.StartsWith("checkbox", StringComparison.Ordinal) || name.StartsWith("radio", StringComparison.Ordinal))
            {
                bool radio = name.StartsWith("radio", StringComparison.Ordinal);
                bool active = name.Contains("_on") || name.Contains("pressed");
                bool disabled = name.Contains("disabled");
                Color selectionBody = disabled ? Hex("A7AFB9") : active ? Green : Cream;
                if (radio)
                {
                    r.Circle(r.Center, Mathf.Min(r.Width, r.Height) * .34f, selectionBody, Mathf.Min(r.Width, r.Height) * .055f, active ? GreenDark : Navy);
                    if (active) r.Circle(r.Center, Mathf.Min(r.Width, r.Height) * .13f, Cream);
                }
                else
                {
                    float size = Mathf.Min(r.Width, r.Height) * .68f;
                    r.RoundedRect(new Rect((r.Width-size)*.5f,(r.Height-size)*.5f,size,size),size*.20f,BlockPuzzleHDRaster.Darken(selectionBody,.90f),selectionBody,size*.06f,active?GreenDark:Navy);
                    if (active) DrawCheck(r);
                }
                return;
            }
            if (name.Contains("pagination_dot"))
            {
                Color color = name.EndsWith("on", StringComparison.Ordinal) ? Orange : Hex("A7AFB9");
                r.Circle(r.Center, Mathf.Min(r.Width, r.Height) * .28f, color, Mathf.Min(r.Width, r.Height) * .04f, Cream);
                return;
            }
            if (name.Contains("rating_star"))
            {
                Color color = name.EndsWith("full", StringComparison.Ordinal) ? Gold : Hex("8490A6");
                r.Star(r.Center, Mathf.Min(r.Width,r.Height)*.33f, Mathf.Min(r.Width,r.Height)*.15f, 5, color, Mathf.PI*.5f);
                return;
            }
            if (name.Contains("life_heart")) { DrawHeart(r, name.EndsWith("full", StringComparison.Ordinal) ? Coral : Hex("8490A6")); return; }

            Color track = name.Contains("dark") || name.Contains("track") ? Navy : Cream;
            Color fill = name.Contains("green") ? Green : name.Contains("yellow") || name.Contains("energy") ? Gold : name.Contains("red") ? Coral : name.Contains("blue") ? Blue : Orange;
            Color body = name.Contains("fill") ? fill : track;
            float horizontalPad = r.Width * .035f;
            float verticalPad = r.Height * .22f;
            r.RoundedRect(new Rect(horizontalPad, verticalPad, r.Width-horizontalPad*2f, r.Height-verticalPad*2f), (r.Height-verticalPad*2f)*.48f, BlockPuzzleHDRaster.Darken(body,.62f), body, r.Height*.045f, name.Contains("fill")?Cream:Hex("59647A"));
        }

        private static void GenerateBadge(BlockPuzzleHDRaster r, string name)
        {
            Color face = name.Contains("locked") ? Navy : name.Contains("sale") || name.Contains("new") ? Coral : name.Contains("complete") || name.Contains("equipped") ? Green : name.Contains("selected") ? Blue : Gold;
            Color edge = BlockPuzzleHDRaster.Darken(face, .62f);
            r.BeveledButton(face, edge, Cream, false, false);
            r.Star(new Vector2(r.Width*.14f,r.Height*.68f),r.Height*.12f,r.Height*.055f,5,Cream,Mathf.PI*.5f);
            r.Star(new Vector2(r.Width*.86f,r.Height*.32f),r.Height*.09f,r.Height*.04f,5,Cream,Mathf.PI*.5f);
        }

        private static void GenerateEffect(BlockPuzzleHDRaster r, string name)
        {
            Color primary = name.Contains("error") ? Coral : name.Contains("success") ? Green : name.Contains("high_score") ? Gold : Blue;
            if (name.Contains("tap_ripple")) { r.Ring(r.Center, Mathf.Min(r.Width,r.Height)*.28f, Mathf.Min(r.Width,r.Height)*.045f, new Color(primary.r,primary.g,primary.b,.72f)); return; }
            if (name.Contains("cloud_puff") || name.Contains("error_puff")) { for(int i=0;i<7;i++){float a=i*Mathf.PI*2f/7f;Vector2 p=r.Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Min(r.Width,r.Height)*.16f;r.Circle(p,Mathf.Min(r.Width,r.Height)*.14f,new Color(primary.r,primary.g,primary.b,.82f));} return; }
            if (name.Contains("confetti") || name.Contains("star_trail")) { for(int i=0;i<12;i++){float a=i*Mathf.PI*2f/12f;Vector2 p=r.Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Min(r.Width,r.Height)*(.18f+(i%3)*.07f);r.Star(p,Mathf.Min(r.Width,r.Height)*.055f,Mathf.Min(r.Width,r.Height)*.025f,5,i%2==0?primary:Orange,Mathf.PI*.5f);} return; }
            if (name.Contains("soft_shine")) { r.Star(r.Center,Mathf.Min(r.Width,r.Height)*.34f,Mathf.Min(r.Width,r.Height)*.055f,4,new Color(Cream.r,Cream.g,Cream.b,.72f),Mathf.PI*.25f); return; }
            for(int i=0;i<12;i++){float a=i*Mathf.PI*2f/12f;float outer=Mathf.Min(r.Width,r.Height)*.36f;Vector2 p=r.Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*outer;r.Star(p,Mathf.Min(r.Width,r.Height)*.07f,Mathf.Min(r.Width,r.Height)*.03f,5,i%2==0?primary:Orange,Mathf.PI*.5f);}
        }

        private static void GenerateLoading(BlockPuzzleHDRaster r, string name)
        {
            int frame = int.Parse(name.Substring(name.Length - 2));
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f + Mathf.PI * .5f;
                Vector2 center = r.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Min(r.Width,r.Height) * .27f;
                int distance = (i - frame + 8) % 8;
                Color face = distance == 0 ? Orange : distance <= 2 ? Blue : Hex("8490A6");
                r.BlockCell(center, Mathf.Min(r.Width,r.Height)*.16f, face, BlockPuzzleHDRaster.Darken(face,.62f));
            }
        }

        private static (Color face, Color edge) ButtonColors(string name)
        {
            if (name.Contains("secondary") || name.Contains("tab_active")) return (Blue, BlueDark);
            if (name.Contains("success")) return (Green, GreenDark);
            if (name.Contains("danger") || name.Contains("close")) return (Coral, CoralDark);
            if (name.Contains("dark") || name.Contains("tab_inactive")) return (Navy, Hex("172038"));
            if (name.Contains("wood")) return (Wood, WoodDark);
            if (name.Contains("gold")) return (Gold, GoldDark);
            if (name.Contains("neutral") || name.Contains("back")) return (Cream, Hex("B9AD97"));
            if (name.Contains("glass")) return (new Color(Blue.r,Blue.g,Blue.b,.72f), new Color(BlueDark.r,BlueDark.g,BlueDark.b,.78f));
            if (name.Contains("block")) return (Hex("49C97A"), Hex("176B4A"));
            return (Orange, OrangeDark);
        }

        private static (Color face, Color edge, Color rim, bool dark) PanelColors(string name)
        {
            if (name.Contains("dark") || name.Contains("bottom_nav")) return (Navy, Hex("172038"), Hex("53617A"), true);
            if (name.Contains("wood") || name.Contains("leaderboard") || name.Contains("divider_wood")) return (Wood, WoodDark, Cream, false);
            if (name.Contains("green") || name.Contains("success") || name.Contains("equipped")) return (Green, GreenDark, Cream, false);
            if (name.Contains("blue") || name.Contains("selected") || name.Contains("glass")) return (Blue, BlueDark, Cream, false);
            if (name.Contains("error") || name.Contains("pink") || name.Contains("locked")) return (Coral, CoralDark, Cream, false);
            if (name.Contains("gold") || name.Contains("coin") || name.Contains("warning")) return (Gold, GoldDark, Cream, false);
            return (Cream, Hex("D2B987"), Color.white, false);
        }

        private static void DrawRibbonEnds(BlockPuzzleHDRaster r, Color face, Color edge)
        {
            float y0=r.Height*.22f,y1=r.Height*.78f;
            r.Polygon(new[]{new Vector2(r.Width*.04f,y0),new Vector2(r.Width*.22f,y0),new Vector2(r.Width*.22f,y1),new Vector2(r.Width*.04f,y1),new Vector2(r.Width*.12f,r.Height*.5f)},edge);
            r.Polygon(new[]{new Vector2(r.Width*.96f,y0),new Vector2(r.Width*.78f,y0),new Vector2(r.Width*.78f,y1),new Vector2(r.Width*.96f,y1),new Vector2(r.Width*.88f,r.Height*.5f)},edge);
        }

        private static void DrawCross(BlockPuzzleHDRaster r) { float t=Mathf.Min(r.Width,r.Height)*.075f;r.Line(new Vector2(r.Width*.34f,r.Height*.34f),new Vector2(r.Width*.66f,r.Height*.66f),t,Cream);r.Line(new Vector2(r.Width*.34f,r.Height*.66f),new Vector2(r.Width*.66f,r.Height*.34f),t,Cream); }
        private static void DrawBack(BlockPuzzleHDRaster r) { float t=Mathf.Min(r.Width,r.Height)*.075f;r.Line(new Vector2(r.Width*.68f,r.Height*.5f),new Vector2(r.Width*.34f,r.Height*.5f),t,Ink);r.Line(new Vector2(r.Width*.34f,r.Height*.5f),new Vector2(r.Width*.50f,r.Height*.66f),t,Ink);r.Line(new Vector2(r.Width*.34f,r.Height*.5f),new Vector2(r.Width*.50f,r.Height*.34f),t,Ink); }
        private static void DrawCheck(BlockPuzzleHDRaster r) { float t=Mathf.Min(r.Width,r.Height)*.075f;r.Line(new Vector2(r.Width*.30f,r.Height*.50f),new Vector2(r.Width*.44f,r.Height*.35f),t,Cream);r.Line(new Vector2(r.Width*.44f,r.Height*.35f),new Vector2(r.Width*.72f,r.Height*.68f),t,Cream); }
        private static void DrawHeart(BlockPuzzleHDRaster r,Color color) { float s=Mathf.Min(r.Width,r.Height);r.Circle(new Vector2(r.Width*.39f,r.Height*.59f),s*.17f,color);r.Circle(new Vector2(r.Width*.61f,r.Height*.59f),s*.17f,color);r.Polygon(new[]{new Vector2(r.Width*.22f,r.Height*.58f),new Vector2(r.Width*.78f,r.Height*.58f),new Vector2(r.Width*.50f,r.Height*.20f)},color); }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
    }
}
#endif
