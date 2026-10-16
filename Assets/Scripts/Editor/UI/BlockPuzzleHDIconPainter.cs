#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BlockBlast.Editor.UI
{
    internal static class BlockPuzzleHDIconPainter
    {
        private static readonly Color Cream = Hex("FFF4D8");
        private static readonly Color Ink = Hex("49342A");
        private static readonly Color Orange = Hex("FF9F43");
        private static readonly Color OrangeDark = Hex("C96524");
        private static readonly Color Blue = Hex("39BDF8");
        private static readonly Color BlueDark = Hex("1677B8");
        private static readonly Color Green = Hex("39D98A");
        private static readonly Color GreenDark = Hex("168A59");
        private static readonly Color Coral = Hex("FF647C");
        private static readonly Color CoralDark = Hex("B8324B");
        private static readonly Color Gold = Hex("FFD65A");
        private static readonly Color GoldDark = Hex("D98C22");
        private static readonly Color Navy = Hex("263556");

        public static void PaintSystem(BlockPuzzleHDRaster r, string fullName)
        {
            string name = fullName.Replace("icon_", string.Empty);
            switch (name)
            {
                case "play": Triangle(r, P(r, 0.37f, 0.28f), P(r, 0.37f, 0.72f), P(r, 0.72f, 0.50f), Cream, Ink); break;
                case "pause": L(r, P(r, .40f, .28f), P(r, .40f, .72f), .09f, Cream); L(r, P(r, .60f, .28f), P(r, .60f, .72f), .09f, Cream); break;
                case "home": Home(r); break;
                case "close": Cross(r, Cream); break;
                case "back": Arrow(r, false, Cream); break;
                case "forward": Arrow(r, true, Cream); break;
                case "retry": case "refresh": ArcArrow(r, name == "refresh", Blue); break;
                case "undo": ArcArrow(r, false, Orange); break;
                case "redo": ArcArrow(r, true, Orange); break;
                case "hint": Bulb(r); break;
                case "info": CircledMark(r, false); break;
                case "help": CircledMark(r, true); break;
                case "check": Check(r, Green); break;
                case "settings": Gear(r); break;
                case "sound_on": Speaker(r, false); break;
                case "sound_off": Speaker(r, true); break;
                case "music_on": Music(r, false); break;
                case "music_off": Music(r, true); break;
                case "vibration": Vibration(r); break;
                case "trophy": Trophy(r); break;
                case "leaderboard": Leaderboard(r); break;
                case "stats": Stats(r); break;
                case "crown": Crown(r); break;
                case "star": r.Star(r.Center, S(r, .32f), S(r, .15f), 5, Gold, Mathf.PI * .5f); break;
                case "coin": Coin(r); break;
                case "gem": Gem(r); break;
                case "gift": Gift(r); break;
                case "heart": Heart(r, Coral); break;
                case "lock": Lock(r, false); break;
                case "unlock": Lock(r, true); break;
                case "shop": Shop(r); break;
                case "palette": Palette(r); break;
                case "paw": Paw(r); break;
                case "calendar": Calendar(r, false); break;
                case "daily": Calendar(r, true); break;
                case "timer": Timer(r); break;
                case "lightning": Lightning(r); break;
                case "shield": Shield(r); break;
                case "camera": Camera(r); break;
                case "no_ads": NoAds(r); break;
                case "search": Search(r); break;
                case "filter": Filter(r); break;
                case "share": Share(r); break;
                case "mail": Mail(r); break;
                case "edit": Edit(r); break;
                case "trash": Trash(r); break;
                case "download": VerticalAction(r, false); break;
                case "upload": VerticalAction(r, true); break;
                case "plus": PlusMinus(r, true); break;
                case "minus": PlusMinus(r, false); break;
                case "chevron_up": Chevron(r, Vector2.up); break;
                case "chevron_down": Chevron(r, Vector2.down); break;
                case "chevron_left": Chevron(r, Vector2.left); break;
                case "chevron_right": Chevron(r, Vector2.right); break;
                case "more": C(r, P(r, .34f, .5f), .045f, Cream); C(r, P(r, .5f, .5f), .045f, Cream); C(r, P(r, .66f, .5f), .045f, Cream); break;
                default: PuzzleMark(r, Blue); break;
            }
        }

        public static void PaintBlock(BlockPuzzleHDRaster r, string fullName)
        {
            string name = fullName.Replace("icon_", string.Empty);
            if (name.StartsWith("block_", StringComparison.Ordinal)) { Tetromino(r, name[^1]); return; }
            if (name.StartsWith("cell_", StringComparison.Ordinal)) { CellState(r, name); return; }
            switch (name)
            {
                case "hammer": Hammer(r); break;
                case "bomb": Bomb(r); break;
                case "magic_wand": Wand(r); break;
                case "shuffle": Shuffle(r); break;
                case "rotate": ArcArrow(r, true, Green); break;
                case "eraser": Eraser(r); break;
                case "grid": Grid(r); break;
                case "line_clear": LineClear(r); break;
                case "all_clear": AllClear(r); break;
                case "combo": Combo(r); break;
                case "crate": Crate(r); break;
                case "wood": Wood(r); break;
                case "grass": Grass(r); break;
                case "jelly": Jelly(r); break;
                case "pet": Paw(r); break;
                case "rocket": Rocket(r); break;
                case "magnet": Magnet(r); break;
                default: PuzzleMark(r, Orange); break;
            }
        }

        private static void Home(BlockPuzzleHDRaster r)
        {
            L(r, P(r, .24f, .48f), P(r, .50f, .72f), .075f, Cream); L(r, P(r, .50f, .72f), P(r, .76f, .48f), .075f, Cream);
            r.RoundedRect(new Rect(r.Width * .30f, r.Height * .25f, r.Width * .40f, r.Height * .28f), S(r, .06f), Cream, Cream, 0f, Cream);
            r.RoundedRect(new Rect(r.Width * .46f, r.Height * .25f, r.Width * .10f, r.Height * .18f), S(r, .025f), OrangeDark, Orange, 0f, Orange);
        }

        private static void Cross(BlockPuzzleHDRaster r, Color color) { L(r, P(r, .30f, .30f), P(r, .70f, .70f), .09f, color); L(r, P(r, .30f, .70f), P(r, .70f, .30f), .09f, color); }
        private static void Check(BlockPuzzleHDRaster r, Color color) { L(r, P(r, .24f, .48f), P(r, .43f, .31f), .09f, color); L(r, P(r, .43f, .31f), P(r, .76f, .69f), .09f, color); }

        private static void Arrow(BlockPuzzleHDRaster r, bool right, Color color)
        {
            float a = right ? .28f : .72f, b = right ? .72f : .28f;
            L(r, P(r, a, .5f), P(r, b, .5f), .08f, color);
            L(r, P(r, b, .5f), P(r, right ? .55f : .45f, .68f), .08f, color);
            L(r, P(r, b, .5f), P(r, right ? .55f : .45f, .32f), .08f, color);
        }

        private static void ArcArrow(BlockPuzzleHDRaster r, bool clockwise, Color color)
        {
            Vector2 center = r.Center;
            float radius = S(r, .25f);
            float start = clockwise ? -.2f : Mathf.PI + .2f;
            float end = clockwise ? Mathf.PI * 1.55f : Mathf.PI * .45f;
            Vector2 previous = center + new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * radius;
            for (int i = 1; i <= 28; i++)
            {
                float angle = Mathf.Lerp(start, end, i / 28f);
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                L(r, previous, next, .065f, color); previous = next;
            }
            ArrowHead(r, previous, clockwise ? Vector2.right + Vector2.down : Vector2.left + Vector2.up, color);
        }

        private static void ArrowHead(BlockPuzzleHDRaster r, Vector2 tip, Vector2 direction, Color color)
        {
            Vector2 d = direction.normalized * S(r, .15f); Vector2 n = new Vector2(-d.y, d.x) * .65f;
            Triangle(r, tip, tip - d + n, tip - d - n, color, BlockPuzzleHDRaster.Darken(color, .65f));
        }

        private static void Bulb(BlockPuzzleHDRaster r)
        {
            C(r, P(r, .5f, .58f), .20f, Gold); L(r, P(r, .43f, .34f), P(r, .57f, .34f), .08f, GoldDark); L(r, P(r, .45f, .25f), P(r, .55f, .25f), .07f, GoldDark);
        }

        private static void CircledMark(BlockPuzzleHDRaster r, bool question)
        {
            r.Ring(r.Center, S(r, .30f), S(r, .07f), Blue);
            if (question) { L(r, P(r, .43f, .63f), P(r, .56f, .66f), .065f, Cream); L(r, P(r, .56f, .66f), P(r, .56f, .49f), .065f, Cream); L(r, P(r, .56f, .49f), P(r, .49f, .42f), .065f, Cream); }
            else L(r, P(r, .5f, .42f), P(r, .5f, .61f), .07f, Cream);
            C(r, P(r, .5f, .31f), .04f, Cream);
        }

        private static void Gear(BlockPuzzleHDRaster r)
        {
            C(r, r.Center, .23f, Cream); for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; C(r, r.Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * S(r, .27f), .075f, Cream); }
            C(r, r.Center, .09f, Navy);
        }

        private static void Speaker(BlockPuzzleHDRaster r, bool muted)
        {
            r.Polygon(new[] { P(r,.25f,.42f), P(r,.40f,.42f), P(r,.57f,.62f), P(r,.57f,.30f), P(r,.40f,.42f), P(r,.25f,.42f) }, Cream);
            L(r, P(r,.25f,.42f), P(r,.25f,.58f), .07f, Cream); L(r, P(r,.25f,.58f), P(r,.40f,.58f), .07f, Cream);
            if (muted) CrossAt(r, P(r,.72f,.5f), .13f, Coral); else { r.Ring(P(r,.56f,.5f), S(r,.17f), S(r,.045f), Blue); }
        }

        private static void Music(BlockPuzzleHDRaster r, bool muted)
        {
            C(r,P(r,.35f,.31f),.09f,Blue); C(r,P(r,.65f,.37f),.09f,Blue); L(r,P(r,.42f,.33f),P(r,.42f,.70f),.065f,Blue); L(r,P(r,.72f,.39f),P(r,.72f,.75f),.065f,Blue); L(r,P(r,.42f,.70f),P(r,.72f,.75f),.075f,Blue);
            if(muted) L(r,P(r,.25f,.75f),P(r,.75f,.25f),.06f,Coral);
        }

        private static void Vibration(BlockPuzzleHDRaster r)
        {
            r.RoundedRect(new Rect(r.Width*.37f,r.Height*.24f,r.Width*.26f,r.Height*.52f),S(r,.05f),Navy,Cream,S(r,.035f),Cream); L(r,P(r,.24f,.37f),P(r,.19f,.5f),.05f,Blue); L(r,P(r,.19f,.5f),P(r,.24f,.63f),.05f,Blue); L(r,P(r,.76f,.37f),P(r,.81f,.5f),.05f,Blue); L(r,P(r,.81f,.5f),P(r,.76f,.63f),.05f,Blue);
        }

        private static void Trophy(BlockPuzzleHDRaster r)
        {
            r.RoundedRect(new Rect(r.Width*.31f,r.Height*.47f,r.Width*.38f,r.Height*.24f),S(r,.07f),GoldDark,Gold,S(r,.035f),Cream); r.Ring(r.Center+Vector2.up*S(r,.08f),S(r,.27f),S(r,.06f),Gold); L(r,P(r,.5f,.29f),P(r,.5f,.47f),.07f,GoldDark); L(r,P(r,.35f,.23f),P(r,.65f,.23f),.08f,GoldDark);
        }

        private static void Leaderboard(BlockPuzzleHDRaster r) { Bar(r,.30f,.26f,.22f,Gold); Bar(r,.50f,.26f,.36f,Orange); Bar(r,.70f,.26f,.50f,Blue); }
        private static void Stats(BlockPuzzleHDRaster r) { Bar(r,.29f,.26f,.22f,Green); Bar(r,.49f,.26f,.36f,Blue); Bar(r,.69f,.26f,.50f,Orange); L(r,P(r,.20f,.25f),P(r,.78f,.25f),.05f,Cream); }
        private static void Bar(BlockPuzzleHDRaster r,float x,float y,float h,Color c) => r.RoundedRect(new Rect(r.Width*(x-.07f),r.Height*y,r.Width*.14f,r.Height*h),S(r,.035f),BlockPuzzleHDRaster.Darken(c,.75f),c,S(r,.02f),Cream);

        private static void Crown(BlockPuzzleHDRaster r) { r.Polygon(new[]{P(r,.22f,.31f),P(r,.18f,.68f),P(r,.38f,.50f),P(r,.50f,.75f),P(r,.62f,.50f),P(r,.82f,.68f),P(r,.76f,.31f)},Gold); L(r,P(r,.24f,.29f),P(r,.76f,.29f),.08f,GoldDark); }
        private static void Coin(BlockPuzzleHDRaster r) { C(r,r.Center,.31f,Gold); C(r,r.Center,.24f,GoldDark); r.Star(r.Center,S(r,.16f),S(r,.075f),5,Cream,Mathf.PI*.5f); }
        private static void Gem(BlockPuzzleHDRaster r) { Triangle(r,P(r,.50f,.19f),P(r,.18f,.55f),P(r,.82f,.55f),Blue,BlueDark); r.Polygon(new[]{P(r,.18f,.55f),P(r,.31f,.75f),P(r,.69f,.75f),P(r,.82f,.55f)},BlockPuzzleHDRaster.Lighten(Blue,.22f)); L(r,P(r,.31f,.75f),P(r,.50f,.19f),.04f,Cream); L(r,P(r,.69f,.75f),P(r,.50f,.19f),.04f,Cream); }
        private static void Gift(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.22f,r.Height*.22f,r.Width*.56f,r.Height*.43f),S(r,.06f),CoralDark,Coral,S(r,.035f),Cream); L(r,P(r,.50f,.22f),P(r,.50f,.72f),.07f,Gold); L(r,P(r,.20f,.62f),P(r,.80f,.62f),.08f,Gold); C(r,P(r,.40f,.76f),.09f,Gold); C(r,P(r,.60f,.76f),.09f,Gold); }
        private static void Heart(BlockPuzzleHDRaster r,Color c) { C(r,P(r,.38f,.58f),.18f,c); C(r,P(r,.62f,.58f),.18f,c); Triangle(r,P(r,.50f,.20f),P(r,.22f,.57f),P(r,.78f,.57f),c,CoralDark); }

        private static void Lock(BlockPuzzleHDRaster r,bool open) { r.RoundedRect(new Rect(r.Width*.28f,r.Height*.22f,r.Width*.44f,r.Height*.36f),S(r,.07f),GoldDark,Gold,S(r,.03f),Cream); Vector2 c=P(r,open?.43f:.5f,.60f); r.Ring(c,S(r,.17f),S(r,.065f),Gold); if(open)L(r,P(r,.56f,.60f),P(r,.68f,.60f),.065f,Gold); C(r,P(r,.5f,.39f),.045f,Ink); }
        private static void Shop(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.24f,r.Height*.24f,r.Width*.52f,r.Height*.38f),S(r,.06f),OrangeDark,Orange,S(r,.03f),Cream); r.Ring(P(r,.5f,.62f),S(r,.18f),S(r,.06f),Cream); }
        private static void Palette(BlockPuzzleHDRaster r) { C(r,r.Center,.31f,Cream); C(r,P(r,.37f,.61f),.055f,Coral); C(r,P(r,.53f,.67f),.055f,Blue); C(r,P(r,.65f,.54f),.055f,Green); C(r,P(r,.39f,.39f),.055f,Gold); C(r,P(r,.63f,.34f),.09f,Navy); }
        private static void Paw(BlockPuzzleHDRaster r) { C(r,P(r,.50f,.38f),.17f,Orange); C(r,P(r,.27f,.59f),.09f,Orange); C(r,P(r,.43f,.69f),.09f,Orange); C(r,P(r,.60f,.69f),.09f,Orange); C(r,P(r,.75f,.57f),.09f,Orange); }
        private static void Calendar(BlockPuzzleHDRaster r,bool daily) { r.RoundedRect(new Rect(r.Width*.22f,r.Height*.24f,r.Width*.56f,r.Height*.49f),S(r,.06f),Cream,Cream,S(r,.035f),BlueDark); L(r,P(r,.23f,.60f),P(r,.77f,.60f),.055f,Blue); L(r,P(r,.35f,.68f),P(r,.35f,.79f),.055f,BlueDark); L(r,P(r,.65f,.68f),P(r,.65f,.79f),.055f,BlueDark); if(daily)r.Star(P(r,.5f,.42f),S(r,.12f),S(r,.055f),5,Gold,Mathf.PI*.5f); }
        private static void Timer(BlockPuzzleHDRaster r) { r.Ring(P(r,.5f,.47f),S(r,.27f),S(r,.065f),Cream); L(r,P(r,.5f,.47f),P(r,.5f,.66f),.055f,Orange); L(r,P(r,.5f,.47f),P(r,.64f,.39f),.055f,Orange); L(r,P(r,.42f,.77f),P(r,.58f,.77f),.07f,Cream); }
        private static void Lightning(BlockPuzzleHDRaster r) { r.Polygon(new[]{P(r,.56f,.82f),P(r,.29f,.46f),P(r,.47f,.46f),P(r,.38f,.17f),P(r,.72f,.58f),P(r,.53f,.58f)},Gold); }
        private static void Shield(BlockPuzzleHDRaster r) { r.Polygon(new[]{P(r,.50f,.80f),P(r,.76f,.68f),P(r,.70f,.33f),P(r,.50f,.17f),P(r,.30f,.33f),P(r,.24f,.68f)},Blue); Check(r,Cream); }
        private static void Camera(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.20f,r.Height*.28f,r.Width*.60f,r.Height*.42f),S(r,.07f),Navy,Cream,S(r,.035f),Cream); r.RoundedRect(new Rect(r.Width*.35f,r.Height*.68f,r.Width*.24f,r.Height*.09f),S(r,.025f),Cream,Cream,0f,Cream); C(r,P(r,.5f,.49f),.15f,Blue); C(r,P(r,.5f,.49f),.08f,Navy); }
        private static void NoAds(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.22f,r.Height*.28f,r.Width*.56f,r.Height*.43f),S(r,.06f),Cream,Cream,S(r,.03f),BlueDark); Triangle(r,P(r,.43f,.39f),P(r,.43f,.60f),P(r,.61f,.50f),Blue,BlueDark); L(r,P(r,.24f,.76f),P(r,.76f,.22f),.07f,Coral); }
        private static void Search(BlockPuzzleHDRaster r) { r.Ring(P(r,.44f,.56f),S(r,.21f),S(r,.07f),Cream); L(r,P(r,.59f,.39f),P(r,.76f,.23f),.09f,Cream); }
        private static void Filter(BlockPuzzleHDRaster r) { r.Polygon(new[]{P(r,.20f,.72f),P(r,.80f,.72f),P(r,.59f,.49f),P(r,.59f,.27f),P(r,.43f,.20f),P(r,.43f,.49f)},Blue); }
        private static void Share(BlockPuzzleHDRaster r) { C(r,P(r,.30f,.50f),.085f,Blue); C(r,P(r,.68f,.70f),.085f,Green); C(r,P(r,.68f,.30f),.085f,Orange); L(r,P(r,.36f,.54f),P(r,.62f,.66f),.055f,Cream); L(r,P(r,.36f,.46f),P(r,.62f,.34f),.055f,Cream); }
        private static void Mail(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.20f,r.Height*.29f,r.Width*.60f,r.Height*.42f),S(r,.055f),BlueDark,Blue,S(r,.03f),Cream); L(r,P(r,.22f,.67f),P(r,.50f,.45f),.055f,Cream); L(r,P(r,.78f,.67f),P(r,.50f,.45f),.055f,Cream); }
        private static void Edit(BlockPuzzleHDRaster r) { L(r,P(r,.29f,.28f),P(r,.69f,.68f),.12f,Gold); Triangle(r,P(r,.24f,.23f),P(r,.37f,.27f),P(r,.27f,.37f),Cream,Ink); }
        private static void Trash(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.31f,r.Height*.22f,r.Width*.38f,r.Height*.43f),S(r,.055f),CoralDark,Coral,S(r,.03f),Cream); L(r,P(r,.27f,.68f),P(r,.73f,.68f),.08f,Cream); L(r,P(r,.42f,.77f),P(r,.58f,.77f),.07f,Cream); }
        private static void VerticalAction(BlockPuzzleHDRaster r,bool up) { L(r,P(r,.50f,.29f),P(r,.50f,.68f),.08f,Blue); ArrowHead(r,P(r,.50f,up?.72f:.25f),up?Vector2.up:Vector2.down,Blue); L(r,P(r,.28f,.20f),P(r,.72f,.20f),.07f,Cream); }
        private static void PlusMinus(BlockPuzzleHDRaster r,bool plus) { L(r,P(r,.27f,.50f),P(r,.73f,.50f),.10f,Cream); if(plus)L(r,P(r,.50f,.27f),P(r,.50f,.73f),.10f,Cream); }
        private static void Chevron(BlockPuzzleHDRaster r,Vector2 direction) { Vector2 n=new Vector2(-direction.y,direction.x); Vector2 tip=r.Center+direction*S(r,.20f); Vector2 basePoint=r.Center-direction*S(r,.14f); L(r,basePoint+n*S(r,.18f),tip,.085f,Cream); L(r,tip,basePoint-n*S(r,.18f),.085f,Cream); }

        private static void Hammer(BlockPuzzleHDRaster r) { L(r,P(r,.34f,.25f),P(r,.58f,.58f),.095f,OrangeDark); r.RoundedRect(new Rect(r.Width*.42f,r.Height*.55f,r.Width*.37f,r.Height*.18f),S(r,.045f),BlueDark,Blue,S(r,.03f),Cream); }
        private static void Bomb(BlockPuzzleHDRaster r) { C(r,P(r,.46f,.42f),.25f,Navy); L(r,P(r,.60f,.61f),P(r,.70f,.73f),.06f,GoldDark); r.Star(P(r,.74f,.77f),S(r,.09f),S(r,.04f),6,Gold); }
        private static void Wand(BlockPuzzleHDRaster r) { L(r,P(r,.30f,.25f),P(r,.64f,.64f),.09f,Blue); r.Star(P(r,.69f,.69f),S(r,.15f),S(r,.07f),5,Gold,Mathf.PI*.5f); C(r,P(r,.28f,.70f),.035f,Coral); C(r,P(r,.72f,.32f),.035f,Green); }
        private static void Shuffle(BlockPuzzleHDRaster r) { L(r,P(r,.22f,.64f),P(r,.42f,.64f),.06f,Blue); L(r,P(r,.42f,.64f),P(r,.62f,.36f),.06f,Blue); ArrowHead(r,P(r,.76f,.36f),Vector2.right,Blue); L(r,P(r,.22f,.36f),P(r,.42f,.36f),.06f,Orange); L(r,P(r,.42f,.36f),P(r,.62f,.64f),.06f,Orange); ArrowHead(r,P(r,.76f,.64f),Vector2.right,Orange); }
        private static void Eraser(BlockPuzzleHDRaster r) { r.Polygon(new[]{P(r,.25f,.36f),P(r,.53f,.70f),P(r,.76f,.55f),P(r,.48f,.22f)},Coral); L(r,P(r,.36f,.29f),P(r,.64f,.62f),.06f,Cream); }
        private static void Grid(BlockPuzzleHDRaster r) { for(int y=0;y<3;y++)for(int x=0;x<3;x++)r.BlockCell(P(r,.32f+x*.18f,.32f+y*.18f),S(r,.14f),Green,GreenDark); }

        private static void Tetromino(BlockPuzzleHDRaster r,char shape)
        {
            Vector2Int[] cells = shape switch { 'i' => new[]{new Vector2Int(-1,0),new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0)}, 'o' => new[]{new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(1,1)}, 't' => new[]{new Vector2Int(-1,0),new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1)}, 's' => new[]{new Vector2Int(-1,0),new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(1,1)}, 'z' => new[]{new Vector2Int(-1,1),new Vector2Int(0,1),new Vector2Int(0,0),new Vector2Int(1,0)}, 'j' => new[]{new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,0),new Vector2Int(1,0)}, _ => new[]{new Vector2Int(1,1),new Vector2Int(-1,0),new Vector2Int(0,0),new Vector2Int(1,0)} };
            float size=S(r,.17f); foreach(Vector2Int cell in cells)r.BlockCell(r.Center+new Vector2((cell.x-.25f)*size,(cell.y-.25f)*size),size*.92f,Orange,OrangeDark);
        }

        private static void CellState(BlockPuzzleHDRaster r,string state) { Color face=state.EndsWith("invalid")?Coral:state.EndsWith("selected")?Gold:state.EndsWith("ghost")?Blue:state.EndsWith("locked")?Navy:Green; r.BlockCell(r.Center,S(r,.48f),face,BlockPuzzleHDRaster.Darken(face,.65f),state.EndsWith("ghost")); if(state.EndsWith("locked"))Lock(r,false); }
        private static void LineClear(BlockPuzzleHDRaster r) { for(int i=0;i<4;i++)r.BlockCell(P(r,.25f+i*.17f,.5f),S(r,.15f),Green,GreenDark); L(r,P(r,.16f,.72f),P(r,.84f,.72f),.045f,Gold); }
        private static void AllClear(BlockPuzzleHDRaster r) { Grid(r); r.Star(r.Center,S(r,.28f),S(r,.12f),8,Gold); }
        private static void Combo(BlockPuzzleHDRaster r) { r.Star(r.Center,S(r,.33f),S(r,.17f),8,Orange); C(r,r.Center,.10f,Cream); }
        private static void Crate(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.24f,r.Height*.24f,r.Width*.52f,r.Height*.52f),S(r,.055f),Hex("754225"),Hex("C9793E"),S(r,.04f),Cream); L(r,P(r,.30f,.30f),P(r,.70f,.70f),.055f,Hex("754225")); L(r,P(r,.30f,.70f),P(r,.70f,.30f),.055f,Hex("754225")); }
        private static void Wood(BlockPuzzleHDRaster r) { Crate(r); r.Ring(r.Center,S(r,.15f),S(r,.035f),Cream); }
        private static void Grass(BlockPuzzleHDRaster r) { r.BlockCell(P(r,.5f,.42f),S(r,.48f),Hex("49C97A"),Hex("176B4A")); for(int i=0;i<5;i++)L(r,P(r,.31f+i*.095f,.62f),P(r,.27f+i*.11f,.79f),.035f,Green); }
        private static void Jelly(BlockPuzzleHDRaster r) { r.RoundedRect(new Rect(r.Width*.25f,r.Height*.24f,r.Width*.50f,r.Height*.50f),S(r,.15f),BlueDark,new Color(Blue.r,Blue.g,Blue.b,.78f),S(r,.035f),Cream,.82f); C(r,P(r,.40f,.49f),.035f,Navy); C(r,P(r,.60f,.49f),.035f,Navy); }
        private static void Rocket(BlockPuzzleHDRaster r) { r.Polygon(new[]{P(r,.50f,.80f),P(r,.67f,.51f),P(r,.62f,.28f),P(r,.38f,.28f),P(r,.33f,.51f)},Cream); C(r,P(r,.5f,.54f),.08f,Blue); Triangle(r,P(r,.39f,.31f),P(r,.28f,.18f),P(r,.46f,.27f),Coral,CoralDark); Triangle(r,P(r,.61f,.31f),P(r,.72f,.18f),P(r,.54f,.27f),Coral,CoralDark); }
        private static void Magnet(BlockPuzzleHDRaster r) { r.Ring(P(r,.5f,.52f),S(r,.25f),S(r,.10f),Coral); r.RoundedRect(new Rect(r.Width*.20f,r.Height*.55f,r.Width*.18f,r.Height*.19f),S(r,.04f),BlueDark,Blue,S(r,.02f),Cream); r.RoundedRect(new Rect(r.Width*.62f,r.Height*.55f,r.Width*.18f,r.Height*.19f),S(r,.04f),BlueDark,Blue,S(r,.02f),Cream); }
        private static void PuzzleMark(BlockPuzzleHDRaster r,Color color) { for(int i=0;i<4;i++)r.BlockCell(P(r,.38f+(i%2)*.24f,.38f+(i/2)*.24f),S(r,.20f),color,BlockPuzzleHDRaster.Darken(color,.65f)); }

        private static void CrossAt(BlockPuzzleHDRaster r,Vector2 c,float half,Color color) { float h=S(r,half); r.Line(c-new Vector2(h,h),c+new Vector2(h,h),S(r,.055f),color); r.Line(c+new Vector2(-h,h),c+new Vector2(h,-h),S(r,.055f),color); }
        private static void C(BlockPuzzleHDRaster r,Vector2 c,float radius,Color color) { float rr=S(r,radius); r.Circle(c+Vector2.down*S(r,.018f),rr,BlockPuzzleHDRaster.Darken(color,.60f)); r.Circle(c,rr,color,rr*.12f,BlockPuzzleHDRaster.Lighten(color,.25f)); }
        private static void L(BlockPuzzleHDRaster r,Vector2 a,Vector2 b,float thickness,Color color) { float t=S(r,thickness); r.Line(a+Vector2.down*S(r,.018f),b+Vector2.down*S(r,.018f),t,BlockPuzzleHDRaster.Darken(color,.55f)); r.Line(a,b,t,color); }
        private static void Triangle(BlockPuzzleHDRaster r,Vector2 a,Vector2 b,Vector2 c,Color face,Color edge) { Vector2 o=Vector2.down*S(r,.025f); r.Polygon(new[]{a+o,b+o,c+o},edge); r.Polygon(new[]{a,b,c},face); }
        private static Vector2 P(BlockPuzzleHDRaster r,float x,float y)=>new Vector2(r.Width*x,r.Height*y);
        private static float S(BlockPuzzleHDRaster r,float normalized)=>Mathf.Min(r.Width,r.Height)*normalized;
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out Color c); return c; }
    }
}
#endif
