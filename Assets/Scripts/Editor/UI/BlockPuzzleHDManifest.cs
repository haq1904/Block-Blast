#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast.Editor.UI
{
    internal sealed class BlockPuzzleHDSpriteSpec
    {
        public BlockPuzzleHDSpriteSpec(string folder, string name, int width, int height, Vector4 border, string atlas)
        {
            Folder = folder;
            Name = name;
            Width = width;
            Height = height;
            Border = border;
            Atlas = atlas;
        }

        public string Folder { get; }
        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public Vector4 Border { get; }
        public string Atlas { get; }
        public string AssetPath => $"Assets/Art/UI/BlockPuzzleHD/{Folder}/{Name}.png";
        public bool IsSliced => Border != Vector4.zero;
    }

    internal static class BlockPuzzleHDManifest
    {
        public const string Root = "Assets/Art/UI/BlockPuzzleHD";
        public const int ExpectedCount = 234;

        public static List<BlockPuzzleHDSpriteSpec> Build()
        {
            var result = new List<BlockPuzzleHDSpriteSpec>(ExpectedCount);
            AddButtons(result);
            AddPanels(result);
            AddControls(result);
            AddSystemIcons(result);
            AddBlockIcons(result);
            AddBadges(result);
            AddEffects(result);
            AddLoading(result);
            return result;
        }

        private static void AddButtons(List<BlockPuzzleHDSpriteSpec> result)
        {
            AddStates(result, "Buttons", new[] { "btn_primary" }, 1024, 512, B(160, 128, 160, 176), "UI_Common");
            AddStates(result, "Buttons", new[] { "btn_secondary", "btn_success", "btn_danger", "btn_neutral", "btn_dark", "btn_wood", "btn_glass", "btn_block" }, 1024, 512, B(160, 128, 160, 176), "UI_Common");
            AddStates(result, "Buttons", new[] { "btn_pill_primary", "btn_pill_secondary", "btn_pill_neutral" }, 768, 384, B(192, 96, 192, 120), "UI_Common");
            AddStates(result, "Buttons", new[] { "btn_tab_active", "btn_tab_inactive" }, 768, 384, B(160, 96, 160, 120), "UI_Common");
            AddStates(result, "Buttons", new[] { "btn_square_primary", "btn_square_secondary", "btn_square_danger", "btn_square_neutral" }, 512, 512, Vector4.zero, "UI_Common");
            AddStates(result, "Buttons", new[] { "btn_circle_primary", "btn_circle_secondary", "btn_circle_gold", "btn_circle_neutral" }, 512, 512, Vector4.zero, "UI_Common");
            AddStates(result, "Buttons", new[] { "btn_close", "btn_back" }, 512, 512, Vector4.zero, "UI_Common");
        }

        private static void AddPanels(List<BlockPuzzleHDSpriteSpec> result)
        {
            AddMany(result, "Panels", new[] { "panel_card_light", "panel_card_dark", "panel_card_wood", "panel_card_green", "panel_card_blue", "panel_card_cream" }, 1024, 1024, B(144, 144, 144, 144), "UI_Overlays");
            AddMany(result, "Panels", new[] { "panel_modal_light", "panel_modal_dark" }, 1024, 1024, B(176, 176, 176, 176), "UI_Overlays");
            Add(result, "Panels", "panel_glass_action", 1024, 1024, B(160, 160, 160, 160), "UI_Lobby");
            AddMany(result, "Panels", new[] { "panel_top_header", "panel_footer_action", "panel_bottom_nav" }, 1024, 512, B(128, 112, 128, 112), "UI_Lobby");
            AddMany(result, "Panels", new[] { "panel_list_row_light", "panel_list_row_dark" }, 1024, 384, B(128, 96, 128, 96), "UI_Overlays");
            AddMany(result, "Panels", new[] { "panel_counter_coin", "panel_counter_gem", "panel_counter_heart", "panel_counter_score" }, 768, 384, B(192, 96, 192, 96), "UI_Lobby");
            Add(result, "Panels", "panel_tooltip", 768, 512, B(128, 128, 128, 128), "UI_Overlays");
            AddMany(result, "Panels", new[] { "panel_speech_left", "panel_speech_right" }, 1024, 640, Vector4.zero, "UI_Overlays");
            AddMany(result, "Panels", new[] { "panel_speech_tail_left", "panel_speech_tail_right" }, 256, 256, Vector4.zero, "UI_Overlays");
            AddMany(result, "Panels", new[] { "frame_inventory_normal", "frame_inventory_selected", "frame_inventory_locked", "frame_inventory_equipped" }, 512, 512, B(96, 96, 96, 96), "UI_Overlays");
            Add(result, "Panels", "frame_portrait", 768, 1024, Vector4.zero, "UI_Lobby");
            Add(result, "Panels", "frame_leaderboard", 1024, 512, B(128, 112, 128, 112), "UI_Overlays");
            AddMany(result, "Panels", new[] { "panel_toast_success", "panel_toast_warning", "panel_toast_error" }, 1024, 384, B(128, 96, 128, 96), "UI_Overlays");
            AddMany(result, "Panels", new[] { "ribbon_gold", "ribbon_blue", "ribbon_pink" }, 1024, 384, Vector4.zero, "UI_Overlays");
            AddMany(result, "Panels", new[] { "divider_wood", "divider_cream" }, 1024, 128, B(96, 32, 96, 32), "UI_Overlays");
        }

        private static void AddControls(List<BlockPuzzleHDSpriteSpec> result)
        {
            AddMany(result, "Controls", new[] { "toggle_track_off", "toggle_track_on" }, 768, 384, B(192, 96, 192, 96), "UI_Common");
            AddMany(result, "Controls", new[] { "toggle_knob_normal", "toggle_knob_pressed" }, 384, 384, Vector4.zero, "UI_Common");
            AddMany(result, "Controls", new[] { "checkbox_off", "checkbox_on", "checkbox_pressed", "checkbox_disabled", "radio_off", "radio_on", "radio_pressed", "radio_disabled" }, 384, 384, Vector4.zero, "UI_Common");
            Add(result, "Controls", "slider_track", 1024, 256, B(128, 64, 128, 64), "UI_Common");
            AddMany(result, "Controls", new[] { "slider_fill_orange", "slider_fill_blue", "slider_fill_green", "slider_fill_red" }, 1024, 256, B(128, 64, 128, 64), "UI_Common");
            AddMany(result, "Controls", new[] { "slider_thumb_normal", "slider_thumb_pressed" }, 384, 384, Vector4.zero, "UI_Common");
            AddMany(result, "Controls", new[] { "progress_track_light", "progress_track_dark" }, 1024, 256, B(128, 64, 128, 64), "UI_Gameplay");
            AddMany(result, "Controls", new[] { "progress_fill_green", "progress_fill_yellow", "progress_fill_red", "progress_fill_blue", "progress_fill_energy" }, 1024, 256, B(128, 64, 128, 64), "UI_Gameplay");
            AddMany(result, "Controls", new[] { "scrollbar_track", "scrollbar_thumb" }, 256, 1024, B(64, 128, 64, 128), "UI_Overlays");
            AddMany(result, "Controls", new[] { "pagination_dot_off", "pagination_dot_on" }, 192, 192, Vector4.zero, "UI_Common");
            AddMany(result, "Controls", new[] { "rating_star_empty", "rating_star_full", "life_heart_empty", "life_heart_full" }, 384, 384, Vector4.zero, "UI_Gameplay");
        }

        private static void AddSystemIcons(List<BlockPuzzleHDSpriteSpec> result)
        {
            string[] names = {
                "play", "pause", "home", "close", "back", "forward", "retry", "refresh", "undo", "redo", "hint", "info", "help", "check", "settings",
                "sound_on", "sound_off", "music_on", "music_off", "vibration", "trophy", "leaderboard", "stats", "crown", "star", "coin", "gem", "gift", "heart",
                "lock", "unlock", "shop", "palette", "paw", "calendar", "daily", "timer", "lightning", "shield", "camera", "no_ads", "search", "filter", "share",
                "mail", "edit", "trash", "download", "upload", "plus", "minus", "chevron_up", "chevron_down", "chevron_left", "chevron_right", "more"
            };
            foreach (string name in names) Add(result, "Icons/System", "icon_" + name, 512, 512, Vector4.zero, name is "trophy" or "crown" or "coin" or "gem" or "gift" ? "UI_Lobby" : "UI_Common");
        }

        private static void AddBlockIcons(List<BlockPuzzleHDSpriteSpec> result)
        {
            string[] names = {
                "hammer", "bomb", "magic_wand", "shuffle", "rotate", "eraser", "grid", "block_i", "block_j", "block_l", "block_o", "block_s", "block_t", "block_z",
                "cell_normal", "cell_selected", "cell_ghost", "cell_invalid", "cell_locked", "line_clear", "all_clear", "combo", "crate", "wood", "grass", "jelly", "pet", "rocket", "magnet"
            };
            foreach (string name in names) Add(result, "Icons/BlockPuzzle", "icon_" + name, 512, 512, Vector4.zero, "UI_Gameplay");
        }

        private static void AddBadges(List<BlockPuzzleHDSpriteSpec> result)
        {
            string[] names = { "best", "new", "locked", "equipped", "selected", "sale", "reward", "daily", "claim", "level", "complete", "streak" };
            foreach (string name in names) Add(result, "Badges", "badge_" + name, 768, 384, B(160, 96, 160, 96), "UI_Overlays");
        }

        private static void AddEffects(List<BlockPuzzleHDSpriteSpec> result)
        {
            AddMany(result, "Effects", new[] { "fx_combo_burst", "fx_high_score_burst", "fx_success_burst", "fx_error_puff" }, 1024, 1024, Vector4.zero, "UI_Gameplay");
            AddMany(result, "Effects", new[] { "fx_sparkle", "fx_star_trail", "fx_confetti", "fx_cloud_puff", "fx_tap_ripple", "fx_soft_shine" }, 512, 512, Vector4.zero, "UI_Gameplay");
        }

        private static void AddLoading(List<BlockPuzzleHDSpriteSpec> result)
        {
            for (int i = 0; i < 8; i++) Add(result, "Loading", $"loading_blocks_{i:00}", 512, 512, Vector4.zero, "UI_Common");
        }

        private static void AddStates(List<BlockPuzzleHDSpriteSpec> result, string folder, IEnumerable<string> bases, int width, int height, Vector4 border, string atlas)
        {
            foreach (string baseName in bases)
            {
                Add(result, folder, baseName + "_normal", width, height, border, atlas);
                Add(result, folder, baseName + "_pressed", width, height, border, atlas);
            }
        }

        private static void AddMany(List<BlockPuzzleHDSpriteSpec> result, string folder, IEnumerable<string> names, int width, int height, Vector4 border, string atlas)
        {
            foreach (string name in names) Add(result, folder, name, width, height, border, atlas);
        }

        private static void Add(List<BlockPuzzleHDSpriteSpec> result, string folder, string name, int width, int height, Vector4 border, string atlas)
        {
            result.Add(new BlockPuzzleHDSpriteSpec(folder, name, width, height, border, atlas));
        }

        private static Vector4 B(float left, float top, float right, float bottom) => new Vector4(left, bottom, right, top);
    }
}
#endif
