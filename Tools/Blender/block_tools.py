bl_info = {
    "name": "Block Tools",
    "author": "Antigravity Senior Tech Artist",
    "version": (1, 2, 0),
    "blender": (4, 0, 0),
    "location": "View3D > Sidebar > Block Tools",
    "description": "Standardization, 4-Tier Interactive Classification & Geometry Tweaker for Unity Blocks",
    "category": "3D View",
}

import bpy, bmesh, math, os, subprocess
from mathutils import Vector

# ==============================================================================
# CONSTANTS & CONFIGURATION
# ==============================================================================
DEFAULT_EXPORT_DIR = r"d:\Dev game\Project\Block Blast\Assets\Art\Blocks\Crate"
DEFAULT_LEAPLAND_PALETTE = r"d:\Dev game\Project\Block Blast\Assets\Art\LeapLand\tex\palette.png"
DEFAULT_CRATE_PALETTE = r"d:\Dev game\Project\Block Blast\Assets\Art\Blocks\Crate\crate_palette.png"

CURATED_SWATCHES = [
    {"name": "Nau Go Van", "color": (0.675, 0.361, 0.259, 1.0), "uv": (0.125, 0.625)},
    {"name": "Nau Sam Vien", "color": (0.400, 0.200, 0.120, 1.0), "uv": (0.062, 0.562)},
    {"name": "Xam Sat/Dinh", "color": (0.412, 0.412, 0.412, 1.0), "uv": (0.375, 0.625)},
    {"name": "Xanh La/Reu", "color": (0.098, 0.569, 0.341, 1.0), "uv": (0.875, 0.875)},
    {"name": "Vang Loi/Rune", "color": (0.918, 0.871, 0.820, 1.0), "uv": (0.875, 0.562)},
    {"name": "Do Diem Nhan", "color": (0.984, 0.043, 0.302, 1.0), "uv": (0.125, 0.375)},
]

VG_TIERS = [
    ("BT_FRAME", "Khung Viền (Frame)", (0.85, 0.20, 0.20, 1.0)),          # 0: Red
    ("BT_CENTER_RAISED", "Thanh Lồi (Planks)", (0.20, 0.85, 0.20, 1.0)),   # 1: Green
    ("BT_CENTER_RECESSED", "Ván Nền (Backing)", (0.20, 0.45, 0.90, 1.0)),  # 2: Blue
    ("BT_ACCESSORIES", "Phụ Kiện (Props)", (0.95, 0.85, 0.15, 1.0)),      # 3: Yellow
    ("BT_OTHER", "Khác (Other)", (0.80, 0.25, 0.90, 1.0)),                 # 4: Purple
]

SIDE_AXES = {
    '+X': (Vector((1.0, 0.0, 0.0)), Vector((0.0, 0.0, -1.0)), Vector((0.0, 1.0, 0.0))),
    '-X': (Vector((-1.0, 0.0, 0.0)), Vector((0.0, 0.0, 1.0)), Vector((0.0, 1.0, 0.0))),
    '+Y': (Vector((0.0, 1.0, 0.0)), Vector((1.0, 0.0, 0.0)), Vector((0.0, 0.0, -1.0))),
    '-Y': (Vector((0.0, -1.0, 0.0)), Vector((1.0, 0.0, 0.0)), Vector((0.0, 0.0, 1.0))),
    '+Z': (Vector((0.0, 0.0, 1.0)), Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0))),
    '-Z': (Vector((0.0, 0.0, -1.0)), Vector((-1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0))),
}


# ==============================================================================
# CORE HELPERS
# ==============================================================================
def get_or_load_image(filepath):
    if not filepath or not os.path.exists(filepath): return None
    p = os.path.normpath(filepath)
    for img in bpy.data.images:
        if img.filepath and os.path.normpath(bpy.path.abspath(img.filepath)) == p: return img
    try: return bpy.data.images.load(filepath)
    except Exception: return None


def get_or_create_non_metallic_mat(img=None, mat_name="Non Metallic"):
    mat = bpy.data.materials.get(mat_name) or bpy.data.materials.new(name=mat_name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next((n for n in nodes if n.type == 'BSDF_PRINCIPLED'), None) or nodes.new('ShaderNodeBsdfPrincipled')
    output = next((n for n in nodes if n.type == 'OUTPUT_MATERIAL'), None) or nodes.new('ShaderNodeOutputMaterial')
    if "Roughness" in bsdf.inputs: bsdf.inputs["Roughness"].default_value = 0.8
    if "Metallic" in bsdf.inputs: bsdf.inputs["Metallic"].default_value = 0.0
    if not any(l.to_node == output for l in bsdf.outputs["BSDF"].links):
        links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    if img:
        tex = next((n for n in nodes if n.type == 'TEX_IMAGE'), None) or nodes.new('ShaderNodeTexImage')
        tex.image, tex.interpolation = img, 'Closest'
        if not any(l.to_node == bsdf for l in tex.outputs["Color"].links):
            links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def set_viewport_shading(context, mode='MATERIAL', color_type='MATERIAL'):
    for area in getattr(context.screen, "areas", []):
        if area.type == 'VIEW_3D':
            for space in area.spaces:
                if space.type == 'VIEW_3D':
                    space.shading.type = mode
                    space.shading.color_type = color_type


def sample_image_swatches(img, gx=16, gy=16, max_colors=64):
    if not img or img.size[0] == 0: return []
    w, h, pix, ch = img.size[0], img.size[1], list(img.pixels), img.channels
    swatches = []
    for iy in range(gy):
        for ix in range(gx):
            u, v = (ix + 0.5) / gx, (iy + 0.5) / gy
            idx = (min(int(v * h), h - 1) * w + min(int(u * w), w - 1)) * ch
            r, g, b = pix[idx], pix[idx + 1], pix[idx + 2]
            if (pix[idx + 3] if ch >= 4 else 1.0) < 0.1: continue
            if not any(((r - s['color'][0])**2 + (g - s['color'][1])**2 + (b - s['color'][2])**2)**0.5 < 0.035 for s in swatches):
                swatches.append({'name': f"Color {len(swatches)+1}", 'color': (r, g, b, 1.0), 'uv': (u, v)})
                if len(swatches) >= max_colors: return swatches
    return swatches


def get_mesh_bounds(bm):
    xs, ys, zs = [v.co.x for v in bm.verts], [v.co.y for v in bm.verts], [v.co.z for v in bm.verts]
    return min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)


def get_side_name(pt, min_x, max_x, min_y, max_y, min_z, max_z):
    dists = {'+X': max_x - pt.x, '-X': pt.x - min_x, '+Y': max_y - pt.y, '-Y': pt.y - min_y, '+Z': max_z - pt.z, '-Z': pt.z - min_z}
    return min(dists, key=dists.get)


def store_original_coordinates(obj, overwrite=False):
    if not obj or obj.type != 'MESH': return
    if bpy.context.mode == 'EDIT_MESH':
        bm = bmesh.from_edit_mesh(obj.data)
        lay = bm.verts.layers.float_vector.get("BT_ORIGINAL_CO")
        if not lay or overwrite:
            if not lay: lay = bm.verts.layers.float_vector.new("BT_ORIGINAL_CO")
            for v in bm.verts: v[lay] = v.co.copy()
            bmesh.update_edit_mesh(obj.data)
    else:
        mesh = obj.data
        attr = mesh.attributes.get("BT_ORIGINAL_CO")
        if not attr or overwrite:
            if not attr: attr = mesh.attributes.new(name="BT_ORIGINAL_CO", type='FLOAT_VECTOR', domain='POINT')
            for i, v in enumerate(mesh.vertices): attr.data[i].vector = v.co


def store_original_color(obj, overwrite=False):
    if not obj or obj.type != 'MESH': return
    mesh = obj.data
    uv_act = mesh.uv_layers.active or (mesh.uv_layers[0] if mesh.uv_layers else None)
    if not uv_act: return

    orig_lay = mesh.uv_layers.get("BT_ORIGINAL_UV")
    if not orig_lay or overwrite:
        if not orig_lay:
            orig_lay = mesh.uv_layers.new(name="BT_ORIGINAL_UV")
        for i, d in enumerate(uv_act.data):
            orig_lay.data[i].uv = d.uv.copy()

    if obj.data.materials and obj.data.materials[0]:
        mat = obj.data.materials[0]
        if not obj.get("BT_ORIGINAL_MAT") or overwrite:
            obj["BT_ORIGINAL_MAT"] = mat.name
        if mat.use_nodes and (not obj.get("BT_ORIGINAL_IMG") or overwrite):
            for n in mat.node_tree.nodes:
                if n.type == 'TEX_IMAGE' and n.image and n.image.filepath:
                    obj["BT_ORIGINAL_IMG"] = bpy.path.abspath(n.image.filepath)
                    break


def apply_bmesh_op(obj, context, op_func):
    if context.mode == 'EDIT_MESH':
        bm = bmesh.from_edit_mesh(obj.data)
        lay = bm.verts.layers.float_vector.get("BT_ORIGINAL_CO")
        if not lay:
            lay = bm.verts.layers.float_vector.new("BT_ORIGINAL_CO")
            for v in bm.verts: v[lay] = v.co.copy()
        count = op_func(bm)
        bmesh.update_edit_mesh(obj.data)
    else:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        lay = bm.verts.layers.float_vector.get("BT_ORIGINAL_CO")
        if not lay:
            lay = bm.verts.layers.float_vector.new("BT_ORIGINAL_CO")
            for v in bm.verts: v[lay] = v.co.copy()
        count = op_func(bm)
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()
    return count


# ==============================================================================
# SETTINGS & DATA MODELS
# ==============================================================================
class BlockToolsPaletteItem(bpy.types.PropertyGroup):
    name: bpy.props.StringProperty(name="Name", default="Swatch")
    color: bpy.props.FloatVectorProperty(name="Color", subtype='COLOR', size=4, default=(1.0, 1.0, 1.0, 1.0))
    uv_coord: bpy.props.FloatVectorProperty(name="UV", size=2, default=(0.5, 0.5))


class BlockToolsSettings(bpy.types.PropertyGroup):
    # Accordion Folds (Phần Rút Gọn Giao Diện)
    fold_standardize: bpy.props.BoolProperty(name="1. Chuẩn Hóa", default=False)
    fold_classification: bpy.props.BoolProperty(name="2. Bảng Phân Loại (4 Tầng)", default=True)
    fold_palette: bpy.props.BoolProperty(name="3. Bảng Màu & UV Palette", default=False)
    fold_tweaker: bpy.props.BoolProperty(name="4. Tùy Biến Hình Học", default=False)
    fold_export: bpy.props.BoolProperty(name="5. Xuất File FBX", default=False)

    # Standardize
    target_size: bpy.props.FloatProperty(name="Kích Thước", default=1.0, min=0.1, max=100.0)
    scale_mode: bpy.props.EnumProperty(name="Kiểu Scale", items=[('UNIFORM', "Giữ Tỉ Lệ", ""), ('EXACT', "1x1x1 Tuyệt Đối", "")], default='UNIFORM')
    smooth_angle: bpy.props.FloatProperty(name="Góc Mượt", default=35.0, min=0.0, max=180.0)

    # Palette
    palette_source: bpy.props.EnumProperty(name="Nguồn", items=[('LEAPLAND', "LeapLand", ""), ('CRATE', "Crate Theme", ""), ('CUSTOM', "Tùy Chọn", "")], default='CRATE')
    custom_image_path: bpy.props.StringProperty(name="Ảnh", subtype='FILE_PATH', default=DEFAULT_CRATE_PALETTE)
    grid_preset: bpy.props.EnumProperty(name="Chia Lưới", items=[('AUTO', "Tự Động", ""), ('16x16', "16x16", ""), ('8x8', "8x8", ""), ('STRIP', "Dải Ngang", "")], default='AUTO')
    swatches: bpy.props.CollectionProperty(type=BlockToolsPaletteItem)
    paint_target: bpy.props.EnumProperty(
        name="Mục Tiêu Tô",
        items=[
            ('TIER_1', "2. Nan Lồi [Xanh Lá]", "Tô vào Thanh Lồi Trung Tâm", 'COLORSET_03_VEC', 0),
            ('TIER_0', "1. Viền [Đỏ]", "Tô vào Khung Viền", 'COLORSET_01_VEC', 1),
            ('TIER_2', "3. Ván Nền [X.Dương]", "Tô vào Ván Nền Phía Sau", 'COLORSET_04_VEC', 2),
            ('TIER_3', "4. Phụ Kiện [Vàng]", "Tô vào Phụ Kiện / Vật Nổi", 'COLORSET_09_VEC', 3),
            ('TIER_4', "5. Khác [Tím]", "Tô vào Nhóm Khác / Tinh Chỉnh Lẻ", 'COLORSET_07_VEC', 4),
            ('SELECTION', "Mặt Đang Chọn", "Chỉ tô các mặt đang chọn trong Edit Mode", 'RESTRICT_SELECT_OFF', 5),
            ('ALL', "Toàn Bộ Khối", "Tô toàn bộ tất cả mặt của khối", 'OBJECT_DATAMODE', 6),
        ],
        default='TIER_1'
    )

    # Classification Counts & Debug
    debug_colors_active: bpy.props.BoolProperty(name="Màu Kiểm Tra", default=False)
    count_frame: bpy.props.IntProperty(default=0)
    count_raised: bpy.props.IntProperty(default=0)
    count_recessed: bpy.props.IntProperty(default=0)
    count_accessories: bpy.props.IntProperty(default=0)
    count_other: bpy.props.IntProperty(default=0)

    # Export
    export_dir: bpy.props.StringProperty(name="Thư Mục", subtype='DIR_PATH', default=DEFAULT_EXPORT_DIR)
    export_filename: bpy.props.StringProperty(name="Tên File", default="")


# ==============================================================================
# 5-TIER CLASSIFICATION ENGINE (SCALE-INVARIANT)
# ==============================================================================
def ensure_groups_and_attributes(obj):
    vgs = {name: obj.vertex_groups.get(name) or obj.vertex_groups.new(name=name) for name, _, _ in VG_TIERS}
    attr = obj.data.attributes.get("BT_TIER") or obj.data.attributes.new(name="BT_TIER", type='INT', domain='FACE')
    return vgs, attr


def update_counts_from_attr(obj, settings):
    if not obj or obj.type != 'MESH': return
    if bpy.context.mode == 'EDIT_MESH':
        bm = bmesh.from_edit_mesh(obj.data)
        tier_layer = bm.faces.layers.int.get("BT_TIER")
        if not tier_layer: return
        c = [0, 0, 0, 0, 0]
        for f in bm.faces: c[min(max(f[tier_layer], 0), 4)] += 1
    else:
        attr = obj.data.attributes.get("BT_TIER")
        if not attr: return
        c = [0, 0, 0, 0, 0]
        for p in obj.data.polygons: c[min(max(attr.data[p.index].value, 0), 4)] += 1
    settings.count_frame, settings.count_raised, settings.count_recessed, settings.count_accessories, settings.count_other = c[0], c[1], c[2], c[3], c[4]


def get_or_create_bmesh_color_layer(bm, name="BT_TIER_COLORS"):
    layer = bm.loops.layers.float_color.get(name)
    if layer is not None:
        return layer, 'FLOAT_COLOR'
    layer = bm.loops.layers.color.get(name)
    if layer is not None:
        return layer, 'BYTE_COLOR'
    layer = bm.loops.layers.float_color.new(name)
    return layer, 'FLOAT_COLOR'


def cleanup_color_attributes(obj):
    if not obj or obj.type != 'MESH': return None
    mesh = obj.data
    was_edit = (bpy.context.mode == 'EDIT_MESH')
    if was_edit: bpy.ops.object.mode_set(mode='OBJECT')

    for name in ["BT_DEBUG_COLORS", "Col", "Color"]:
        ca = mesh.color_attributes.get(name)
        if ca: mesh.color_attributes.remove(ca)

    tier_cas = [ca for ca in mesh.color_attributes if ca.name == "BT_TIER_COLORS" or ca.name.startswith("BT_TIER_COLORS.")]
    target_ca = None
    if tier_cas:
        if mesh.color_attributes.active_color in tier_cas:
            target_ca = mesh.color_attributes.active_color
        elif mesh.color_attributes.get("BT_TIER_COLORS"):
            target_ca = mesh.color_attributes.get("BT_TIER_COLORS")
        else:
            target_ca = tier_cas[0]

    for ca in list(mesh.color_attributes):
        if ca != target_ca and (ca.name == "BT_TIER_COLORS" or ca.name.startswith("BT_TIER_COLORS.")):
            mesh.color_attributes.remove(ca)

    if target_ca:
        if target_ca.name != "BT_TIER_COLORS":
            target_ca.name = "BT_TIER_COLORS"
        mesh.color_attributes.active_color = target_ca

    if was_edit: bpy.ops.object.mode_set(mode='EDIT')
    return target_ca


def bake_tier_colors_to_mesh(obj):
    if not obj or obj.type != 'MESH': return
    mesh = obj.data
    is_edit = (bpy.context.mode == 'EDIT_MESH')
    colors = [c for _, _, c in VG_TIERS]

    if is_edit:
        bm = bmesh.from_edit_mesh(mesh)
        tier_layer = bm.faces.layers.int.get("BT_TIER")
        color_layer, _ = get_or_create_bmesh_color_layer(bm, "BT_TIER_COLORS")
        for f in bm.faces:
            t_idx = min(max(f[tier_layer], 0), 4) if tier_layer else 0
            col = colors[t_idx]
            for l in f.loops: l[color_layer] = col
        bmesh.update_edit_mesh(mesh)
    else:
        cleanup_color_attributes(obj)
        tier_attr = mesh.attributes.get("BT_TIER")
        if not tier_attr: return
        ca = mesh.color_attributes.get("BT_TIER_COLORS")
        if not ca or ca.domain != 'CORNER':
            if ca: mesh.color_attributes.remove(ca)
            ca = mesh.color_attributes.new(name="BT_TIER_COLORS", type='FLOAT_COLOR', domain='CORNER')
        for p in mesh.polygons:
            t_idx = min(max(tier_attr.data[p.index].value, 0), 4)
            col = colors[t_idx]
            for loop_idx in p.loop_indices:
                ca.data[loop_idx].color = col
        mesh.color_attributes.active_color = ca


def try_restore_tiers_from_colors(obj):
    if not obj or obj.type != 'MESH': return False
    cleanup_color_attributes(obj)
    mesh = obj.data
    ca = mesh.color_attributes.get("BT_TIER_COLORS")
    if not ca: return False

    was_edit = (bpy.context.mode == 'EDIT_MESH')
    if was_edit: bpy.ops.object.mode_set(mode='OBJECT')

    colors_linear = [Vector(c[:3]) for _, _, c in VG_TIERS]
    colors_srgb = [Vector((c[0]**2.2, c[1]**2.2, c[2]**2.2)) for c in colors_linear]

    bm = bmesh.new()
    bm.from_mesh(mesh)
    tier_layer = bm.faces.layers.int.get("BT_TIER") or bm.faces.layers.int.new("BT_TIER")
    orig_lay = bm.verts.layers.float_vector.get("BT_ORIGINAL_CO") or bm.verts.layers.float_vector.new("BT_ORIGINAL_CO")
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()

    group_verts = {0: set(), 1: set(), 2: set(), 3: set(), 4: set()}
    matched_faces = 0

    for p in mesh.polygons:
        f = bm.faces[p.index]
        avg_col = Vector((0.0, 0.0, 0.0))
        for l_idx in p.loop_indices:
            c = ca.data[l_idx].color
            avg_col += Vector((c[0], c[1], c[2]))
        avg_col /= len(p.loop_indices)

        best_tier, min_dist = 4, float('inf')
        for t_idx in range(len(VG_TIERS)):
            dist_lin = (avg_col - colors_linear[t_idx]).length
            dist_srgb = (avg_col - colors_srgb[t_idx]).length
            d = min(dist_lin, dist_srgb)
            if d < min_dist:
                min_dist, best_tier = d, t_idx

        if min_dist < 0.38:
            f[tier_layer] = best_tier
            for v in f.verts: group_verts[best_tier].add(v.index)
            matched_faces += 1
        else:
            f[tier_layer] = 4
            for v in f.verts: group_verts[4].add(v.index)

    # Valid if at least one face matched the palette
    if matched_faces == 0:
        bm.free()
        if was_edit: bpy.ops.object.mode_set(mode='EDIT')
        return False

    for v in bm.verts: v[orig_lay] = v.co.copy()
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    for name, _, _ in VG_TIERS:
        vg = obj.vertex_groups.get(name)
        if vg: obj.vertex_groups.remove(vg)

    vgs = {name: obj.vertex_groups.new(name=name) for name, _, _ in VG_TIERS}
    v_groups_map = [
        ("BT_FRAME", list(group_verts[0])),
        ("BT_CENTER_RAISED", list(group_verts[1])),
        ("BT_CENTER_RECESSED", list(group_verts[2])),
        ("BT_ACCESSORIES", list(group_verts[3])),
        ("BT_OTHER", list(group_verts[4])),
    ]
    for name, v_indices in v_groups_map:
        if v_indices: vgs[name].add(v_indices, 1.0, 'REPLACE')

    if was_edit: bpy.ops.object.mode_set(mode='EDIT')
    return True


def auto_segment_mesh(obj, force_heuristic=False):
    if not force_heuristic and try_restore_tiers_from_colors(obj):
        return True

    cleanup_color_attributes(obj)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    tier_layer = bm.faces.layers.int.get("BT_TIER") or bm.faces.layers.int.new("BT_TIER")
    orig_lay = bm.verts.layers.float_vector.get("BT_ORIGINAL_CO") or bm.verts.layers.float_vector.new("BT_ORIGINAL_CO")
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()

    min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
    dim_x, dim_y, dim_z = max(max_x - min_x, 0.001), max(max_y - min_y, 0.001), max(max_z - min_z, 0.001)
    dim_max = max(dim_x, dim_y, dim_z)

    # 1. Connected components analysis: Chỉ lọc phụ kiện siêu nhỏ (< 18% kích thước khối)
    visited, islands = set(), []
    for f in bm.faces:
        if f in visited: continue
        isl, stack = set(), [f]
        visited.add(f)
        while stack:
            curr = stack.pop()
            isl.add(curr)
            for e in curr.edges:
                for lk in e.link_faces:
                    if lk not in visited: visited.add(lk); stack.append(lk)
        islands.append(isl)

    accessory_faces = set()
    for isl in islands:
        v_list = [v for f in isl for v in f.verts]
        dx = max(v.co.x for v in v_list) - min(v.co.x for v in v_list)
        dy = max(v.co.y for v in v_list) - min(v.co.y for v in v_list)
        dz = max(v.co.z for v in v_list) - min(v.co.z for v in v_list)
        if max(dx, dy, dz) < dim_max * 0.18:
            accessory_faces.update(isl)

    # 2. Phân loại Khung Viền dựa trên 12 cạnh khung của hộp 3D (Spatial Bounding-Frame)
    frame_margin_x = dim_x * 0.20
    frame_margin_y = dim_y * 0.20
    frame_margin_z = dim_z * 0.20
    outer_skin = dim_max * 0.06

    frame_faces, center_faces = set(), []
    main_faces = [f for f in bm.faces if f not in accessory_faces]

    for f in main_faces:
        c = f.calc_center_median()
        dx = min(abs(c.x - min_x), abs(c.x - max_x))
        dy = min(abs(c.y - min_y), abs(c.y - max_y))
        dz = min(abs(c.z - min_z), abs(c.z - max_z))

        is_near_corner_edge = (dx < frame_margin_x and dy < frame_margin_y) or \
                              (dx < frame_margin_x and dz < frame_margin_z) or \
                              (dy < frame_margin_y and dz < frame_margin_z)

        is_on_outer_surface_frame = False
        if dx < outer_skin and (dy < frame_margin_y or dz < frame_margin_z):
            is_on_outer_surface_frame = True
        elif dy < outer_skin and (dx < frame_margin_x or dz < frame_margin_z):
            is_on_outer_surface_frame = True
        elif dz < outer_skin and (dx < frame_margin_x or dy < frame_margin_y):
            is_on_outer_surface_frame = True

        if is_near_corner_edge or is_on_outer_surface_frame:
            frame_faces.add(f)
        else:
            center_faces.append(f)

    # 3. Phân tầng độ sâu cho phần trung tâm: Nan Lồi (Raised) vs Ván Nền (Recessed)
    def get_side_name_local(pt):
        dists = {
            '+X': max_x - pt.x, '-X': pt.x - min_x,
            '+Y': max_y - pt.y, '-Y': pt.y - min_y,
            '+Z': max_z - pt.z, '-Z': pt.z - min_z
        }
        return min(dists, key=dists.get)

    side_center = {}
    for f in center_faces:
        side_center.setdefault(get_side_name_local(f.calc_center_median()), []).append(f)

    raised_faces, recessed_faces = set(), set()
    for s, flist in side_center.items():
        depths = [(f, min(min(abs(f.calc_center_median().x - min_x), abs(f.calc_center_median().x - max_x)),
                         min(abs(f.calc_center_median().y - min_y), abs(f.calc_center_median().y - max_y)),
                         min(abs(f.calc_center_median().z - min_z), abs(f.calc_center_median().z - max_z)))) for f in flist]
        d_vals = [d for _, d in depths]
        min_d, max_d = min(d_vals), max(d_vals)
        if (max_d - min_d) > dim_max * 0.008:
            thresh = (min_d + max_d) * 0.5
            for f, d in depths:
                if d < thresh: raised_faces.add(f)
                else: recessed_faces.add(f)
        else:
            recessed_faces.update(flist)

    for f in frame_faces: f[tier_layer] = 0
    for f in raised_faces: f[tier_layer] = 1
    for f in recessed_faces: f[tier_layer] = 2
    for f in accessory_faces: f[tier_layer] = 3

    v_groups_map = [
        ("BT_FRAME", list(set(v.index for f in frame_faces for v in f.verts))),
        ("BT_CENTER_RAISED", list(set(v.index for f in raised_faces for v in f.verts))),
        ("BT_CENTER_RECESSED", list(set(v.index for f in recessed_faces for v in f.verts))),
        ("BT_ACCESSORIES", list(set(v.index for f in accessory_faces for v in f.verts))),
        ("BT_OTHER", []),
    ]
    for v in bm.verts:
        v[orig_lay] = v.co.copy()
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()

    vgs, _ = ensure_groups_and_attributes(obj)
    for vg in list(vgs.values()): obj.vertex_groups.remove(vg)
    vgs, _ = ensure_groups_and_attributes(obj)
    for name, v_indices in v_groups_map:
        if v_indices: vgs[name].add(v_indices, 1.0, 'REPLACE')
    bake_tier_colors_to_mesh(obj)


def update_debug_color_overlay(obj, active=True):
    if not obj or obj.type != 'MESH': return
    mesh = obj.data
    old_ca = mesh.color_attributes.get("BT_DEBUG_COLORS")
    if old_ca: mesh.color_attributes.remove(old_ca)

    if not active:
        set_viewport_shading(bpy.context, mode='MATERIAL')
        return

    bake_tier_colors_to_mesh(obj)
    set_viewport_shading(bpy.context, mode='SOLID', color_type='VERTEX')


# ==============================================================================
# OPERATORS: STANDARDIZE & ORIENTATION
# ==============================================================================
class BLOCKTOOLS_OT_standardize(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.standardize", "Chuẩn Hóa Khối (1.0m)", {'REGISTER', 'UNDO'}

    def execute(self, context):
        obj = context.active_object
        if context.mode != 'OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        verts = obj.data.vertices
        if not verts: return {'CANCELLED'}

        settings = context.scene.block_tools_settings
        min_x, max_x = min(v.co.x for v in verts), max(v.co.x for v in verts)
        min_y, max_y = min(v.co.y for v in verts), max(v.co.y for v in verts)
        min_z, max_z = min(v.co.z for v in verts), max(v.co.z for v in verts)
        dx, dy, dz = max(max_x - min_x, 0.001), max(max_y - min_y, 0.001), max(max_z - min_z, 0.001)

        scale_vec = (settings.target_size / max(dx, dy, dz),) * 3 if settings.scale_mode == 'UNIFORM' else (settings.target_size / dx, settings.target_size / dy, settings.target_size / dz)
        obj.scale = scale_vec
        bpy.ops.object.transform_apply(scale=True)

        offset = Vector(((min_x + max_x) * 0.5 * scale_vec[0], (min_y + max_y) * 0.5 * scale_vec[1], min_z * scale_vec[2]))
        for v in verts: v.co -= offset
        obj.location = (0, 0, 0)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        try: bpy.ops.object.shade_smooth_by_angle(angle=math.radians(settings.smooth_angle))
        except Exception: pass
        store_original_coordinates(obj, overwrite=True)
        store_original_color(obj, overwrite=True)
        self.report({'INFO'}, f"Đã chuẩn hóa '{obj.name}' về 1.0m (Pivot Bottom-Center)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_rotate_z(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.rotate_z", "Xoay Z", {'REGISTER', 'UNDO'}
    angle_degrees: bpy.props.FloatProperty(name="Angle", default=90.0)

    def execute(self, context):
        if context.mode != 'OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.transform.rotate(value=math.radians(self.angle_degrees), orient_axis='Z', orient_type='GLOBAL')
        bpy.ops.object.transform_apply(rotation=True)
        self.report({'INFO'}, f"Xoay Z {self.angle_degrees:+.0f}°")
        return {'FINISHED'}


# ==============================================================================
# OPERATORS: INTERACTIVE CLASSIFICATION & INSPECTOR
# ==============================================================================
class BLOCKTOOLS_OT_analyze_mesh(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.analyze_mesh", "Phân Tích & Phân Loại Mặt Khối", {'REGISTER', 'UNDO'}
    force_heuristic: bpy.props.BoolProperty(name="Phân Tích Mới Lại Từ Đầu", default=False)

    def execute(self, context):
        obj = context.active_object
        if context.mode != 'OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
        is_restored = auto_segment_mesh(obj, force_heuristic=self.force_heuristic)
        store_original_coordinates(obj, overwrite=False)
        store_original_color(obj, overwrite=False)
        settings = context.scene.block_tools_settings
        update_counts_from_attr(obj, settings)
        if settings.debug_colors_active: update_debug_color_overlay(obj, True)
        if is_restored and not self.force_heuristic:
            self.report({'INFO'}, f"Đã nạp 5 tầng từ Color Attribute (FBX): Viền={settings.count_frame}, Lồi={settings.count_raised}, Ván={settings.count_recessed}, Phụ kiện={settings.count_accessories}")
        else:
            self.report({'INFO'}, f"Phân tích xong: Viền={settings.count_frame}, Lồi={settings.count_raised}, Ván={settings.count_recessed}, Phụ kiện={settings.count_accessories}")
        return {'FINISHED'}


class BLOCKTOOLS_OT_toggle_debug_colors(bpy.types.Operator):
    bl_idname, bl_label = "blocktools.toggle_debug_colors", "Bật/Tắt Màu Kiểm Tra"

    def execute(self, context):
        obj, settings = context.active_object, context.scene.block_tools_settings
        settings.debug_colors_active = not settings.debug_colors_active
        update_debug_color_overlay(obj, settings.debug_colors_active)
        self.report({'INFO'}, f"Đã {'BẬT' if settings.debug_colors_active else 'TẮT'} Màu Kiểm Tra")
        return {'FINISHED'}


class BLOCKTOOLS_OT_group_select(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.group_select", "Chọn Mặt Trong Nhóm", {'REGISTER', 'UNDO'}
    tier_index: bpy.props.IntProperty(default=0)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}
        if context.mode != 'EDIT_MESH': bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_mode(type='FACE')

        bm = bmesh.from_edit_mesh(obj.data)
        bm.faces.ensure_lookup_table()
        tier_layer = bm.faces.layers.int.get("BT_TIER")
        if not tier_layer:
            self.report({'WARNING'}, "Chưa phân tích mặt! Bấm 'Phân Tích Khối' trước.")
            return {'CANCELLED'}

        count = 0
        for f in bm.faces:
            is_match = (f[tier_layer] == self.tier_index)
            f.select = is_match
            if is_match: count += 1

        bmesh.update_edit_mesh(obj.data)
        self.report({'INFO'}, f"Đã chọn {count} mặt trong nhóm '{VG_TIERS[self.tier_index][1]}'")
        return {'FINISHED'}


class BLOCKTOOLS_OT_group_assign(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.group_assign", "Gán Mặt Vào Nhóm", {'REGISTER', 'UNDO'}
    target_tier: bpy.props.IntProperty(default=1)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}
        if context.mode != 'EDIT_MESH':
            self.report({'WARNING'}, "Hãy chuyển sang Edit Mode (phím Tab) và chọn mặt cần gán!")
            return {'CANCELLED'}

        bm = bmesh.from_edit_mesh(obj.data)
        tier_layer = bm.faces.layers.int.get("BT_TIER") or bm.faces.layers.int.new("BT_TIER")
        dvert_lay = bm.verts.layers.deform.verify()
        bm.faces.ensure_lookup_table()
        bm.verts.ensure_lookup_table()

        sel_faces = [f for f in bm.faces if f.select]
        if not sel_faces:
            self.report({'WARNING'}, "Chưa chọn mặt nào để gán!")
            return {'CANCELLED'}

        for f in sel_faces:
            f[tier_layer] = self.target_tier

        # Cập nhật màu hiển thị loop tức thời trên Viewport trong Edit Mode
        target_color = VG_TIERS[self.target_tier][2]
        color_layer, _ = get_or_create_bmesh_color_layer(bm, "BT_TIER_COLORS")
        for f in sel_faces:
            for l in f.loops:
                l[color_layer] = target_color

        # Synchronize vertex groups
        vgs = {name: obj.vertex_groups.get(name) or obj.vertex_groups.new(name=name) for name, _, _ in VG_TIERS}
        target_vg = vgs[VG_TIERS[self.target_tier][0]]
        sel_verts = set(v for f in sel_faces for v in f.verts)
        for v in sel_verts:
            for name, vg in vgs.items():
                if vg != target_vg and vg.index in v[dvert_lay]:
                    del v[dvert_lay][vg.index]
            v[dvert_lay][target_vg.index] = 1.0

        bmesh.update_edit_mesh(obj.data)

        # Update UI counts
        settings = context.scene.block_tools_settings
        c = [0, 0, 0, 0, 0]
        for f in bm.faces:
            c[min(max(f[tier_layer], 0), 4)] += 1
        settings.count_frame, settings.count_raised, settings.count_recessed, settings.count_accessories, settings.count_other = c[0], c[1], c[2], c[3], c[4]

        if settings.debug_colors_active:
            set_viewport_shading(context, mode='SOLID', color_type='VERTEX')

        self.report({'INFO'}, f"Đã gán {len(sel_faces)} mặt vào '{VG_TIERS[self.target_tier][1]}'")
        return {'FINISHED'}


# ==============================================================================
# OPERATORS: PALETTE & COLORING
# ==============================================================================
class BLOCKTOOLS_OT_load_curated_swatches(bpy.types.Operator):
    bl_idname, bl_label = "blocktools.load_curated_swatches", "Màu Chuẩn Game"

    def execute(self, context):
        settings = context.scene.block_tools_settings
        settings.swatches.clear()
        for item in CURATED_SWATCHES:
            sw = settings.swatches.add()
            sw.name, sw.color, sw.uv_coord = item["name"], item["color"], item["uv"]
        self.report({'INFO'}, f"Nạp {len(settings.swatches)} màu chuẩn game.")
        return {'FINISHED'}


class BLOCKTOOLS_OT_scan_palette_image(bpy.types.Operator):
    bl_idname, bl_label = "blocktools.scan_palette_image", "Đọc Từ Ảnh"

    def execute(self, context):
        settings = context.scene.block_tools_settings
        path = DEFAULT_CRATE_PALETTE if settings.palette_source == 'CRATE' else (DEFAULT_LEAPLAND_PALETTE if settings.palette_source == 'LEAPLAND' else settings.custom_image_path)
        img = get_or_load_image(path)
        if not img:
            self.report({'ERROR'}, f"Không tìm thấy ảnh: {path}")
            return {'CANCELLED'}
        gx, gy = (max(img.size[0] // max(img.size[1], 1), 1), 1) if (settings.grid_preset == 'STRIP' or (settings.grid_preset == 'AUTO' and img.size[1] <= 64)) else (16, 16)
        settings.swatches.clear()
        for s in sample_image_swatches(img, gx, gy):
            sw = settings.swatches.add()
            sw.name, sw.color, sw.uv_coord = s['name'], s['color'], s['uv']
        self.report({'INFO'}, f"Trích xuất {len(settings.swatches)} swatches từ '{os.path.basename(path)}'")
        return {'FINISHED'}


class BLOCKTOOLS_OT_apply_swatch(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.apply_swatch", "Tô Màu Swatch", {'REGISTER', 'UNDO'}
    swatch_index: bpy.props.IntProperty(default=0)
    target_tier: bpy.props.StringProperty(default="")

    def execute(self, context):
        obj, settings = context.edit_object or context.active_object, context.scene.block_tools_settings
        if not obj or obj.type != 'MESH': return {'CANCELLED'}
        if self.swatch_index >= len(settings.swatches): return {'CANCELLED'}

        target = self.target_tier.strip() if self.target_tier.strip() else settings.paint_target
        store_original_color(obj, overwrite=False)
        if target.startswith('TIER_') and not obj.data.attributes.get("BT_TIER"):
            auto_segment_mesh(obj)
            update_counts_from_attr(obj, settings)

        uv = Vector(settings.swatches[self.swatch_index].uv_coord)
        mat = get_or_create_non_metallic_mat(img=get_or_load_image(DEFAULT_CRATE_PALETTE if settings.palette_source == 'CRATE' else DEFAULT_LEAPLAND_PALETTE))
        if not obj.data.materials or obj.data.materials[0] != mat:
            obj.data.materials.clear(); obj.data.materials.append(mat)

        target_name_map = {
            'SELECTION': "mặt đang chọn",
            'ALL': "toàn bộ khối",
            'TIER_0': "Khung Viền [Đỏ]",
            'TIER_1': "Thanh Lồi [Xanh Lá]",
            'TIER_2': "Ván Nền [X.Dương]",
            'TIER_3': "Phụ Kiện [Vàng]",
            'TIER_4': "Khác [Tím]",
        }

        def apply_uv(bm):
            uv_layer = bm.loops.layers.uv.verify()
            tier_layer = bm.faces.layers.int.get("BT_TIER")

            if target == 'SELECTION':
                target_faces = [f for f in bm.faces if f.select] if context.mode == 'EDIT_MESH' else bm.faces
            elif target == 'ALL':
                target_faces = bm.faces
            else:
                if not tier_layer:
                    tier_layer = bm.faces.layers.int.new("BT_TIER")
                t_idx = int(target.split('_')[1])
                target_faces = [f for f in bm.faces if f[tier_layer] == t_idx]

            if not target_faces: return 0

            for f in target_faces:
                f.material_index = 0
                for loop in f.loops: loop[uv_layer].uv = uv
            return len(target_faces)

        count = apply_bmesh_op(obj, context, apply_uv)
        if count == 0:
            self.report({'WARNING'}, f"Không có mặt nào thuộc {target_name_map.get(target, target)}! Hãy phân tích hoặc gán tầng trước.")
            return {'CANCELLED'}

        if settings.debug_colors_active:
            settings.debug_colors_active = False
            update_debug_color_overlay(obj, False)

        set_viewport_shading(context, mode='MATERIAL')
        self.report({'INFO'}, f"Đã tô '{settings.swatches[self.swatch_index].name}' cho {target_name_map.get(target, target)} ({count} mặt)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_smart_autocolor(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.smart_autocolor", "Tô Màu Thông Minh (4 Tầng)", {'REGISTER', 'UNDO'}

    def execute(self, context):
        obj, settings = context.edit_object or context.active_object, context.scene.block_tools_settings
        if not settings.swatches:
            self.report({'ERROR'}, "Chưa có palette! Bấm 'Màu Chuẩn Game' trước.")
            return {'CANCELLED'}
        if not obj.data.attributes.get("BT_TIER"): auto_segment_mesh(obj)
        store_original_color(obj, overwrite=False)
        mat = get_or_create_non_metallic_mat(img=get_or_load_image(DEFAULT_CRATE_PALETTE if settings.palette_source == 'CRATE' else DEFAULT_LEAPLAND_PALETTE))
        obj.data.materials.clear(); obj.data.materials.append(mat)

        n = len(settings.swatches)
        group_uvs = [
            Vector(settings.swatches[2 if n >= 22 else 1].uv_coord),             # 0: Frame
            Vector(settings.swatches[6 if n >= 22 else 0].uv_coord),             # 1: Raised
            Vector(settings.swatches[1 if n >= 22 else min(4, n-1)].uv_coord),   # 2: Recessed
            Vector(settings.swatches[20 if n >= 22 else 2].uv_coord),            # 3: Accessories
            Vector(settings.swatches[min(3, n-1)].uv_coord),                     # 4: Other
        ]

        def apply_smart(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            uv_layer = bm.loops.layers.uv.verify()
            for f in bm.faces:
                f.material_index = 0
                t_idx = min(max(f[tier_layer], 0), 4) if tier_layer else 0
                for loop in f.loops: loop[uv_layer].uv = group_uvs[t_idx]
            return len(bm.faces)

        apply_bmesh_op(obj, context, apply_smart)
        if settings.debug_colors_active:
            settings.debug_colors_active = False
            update_debug_color_overlay(obj, False)
        set_viewport_shading(context, mode='MATERIAL')
        self.report({'INFO'}, "Đã tự động tô màu cả khối theo 5 tầng!")
        return {'FINISHED'}


class BLOCKTOOLS_OT_reset_original_color(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.reset_original_color", "Reset Màu Gốc", {'REGISTER', 'UNDO'}

    def execute(self, context):
        obj, settings = context.edit_object or context.active_object, context.scene.block_tools_settings
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        mesh = obj.data
        orig_uv = mesh.uv_layers.get("BT_ORIGINAL_UV")
        target_uv = mesh.uv_layers.get("UVMap") or mesh.uv_layers.active

        if not orig_uv:
            self.report({'WARNING'}, "Chưa có bản lưu UV/Màu gốc! Bấm 'Chuẩn Hóa Khối' hoặc 'Phân Tích' trước.")
            return {'CANCELLED'}

        if context.mode == 'EDIT_MESH':
            bm = bmesh.from_edit_mesh(mesh)
            b_orig = bm.loops.layers.uv.get("BT_ORIGINAL_UV")
            b_target = bm.loops.layers.uv.get("UVMap") or bm.loops.layers.uv.active
            if b_orig and b_target:
                for f in bm.faces:
                    for l in f.loops:
                        l[b_target].uv = l[b_orig].uv.copy()
            bmesh.update_edit_mesh(mesh)
        else:
            for i, d in enumerate(orig_uv.data):
                target_uv.data[i].uv = d.uv.copy()

        img_path = obj.get("BT_ORIGINAL_IMG")
        if img_path and os.path.exists(img_path):
            img = get_or_load_image(img_path)
            mat = get_or_create_non_metallic_mat(img=img, mat_name=obj.get("BT_ORIGINAL_MAT", "Original Mat"))
            if not obj.data.materials or obj.data.materials[0] != mat:
                obj.data.materials.clear(); obj.data.materials.append(mat)
        else:
            img = get_or_load_image(DEFAULT_LEAPLAND_PALETTE)
            if img:
                mat = get_or_create_non_metallic_mat(img=img)
                if not obj.data.materials or obj.data.materials[0] != mat:
                    obj.data.materials.clear(); obj.data.materials.append(mat)

        if settings.debug_colors_active:
            settings.debug_colors_active = False
            update_debug_color_overlay(obj, False)

        set_viewport_shading(context, mode='MATERIAL')
        self.report({'INFO'}, f"Đã reset về màu/UV gốc ban đầu ({len(mesh.vertices)} đỉnh)")
        return {'FINISHED'}


# ==============================================================================
# OPERATORS: GEOMETRY TWEAKER
# ==============================================================================
class BLOCKTOOLS_OT_adjust_frame_thickness(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_frame_thickness", "Chỉnh Độ Dày Khung Viền", {'REGISTER', 'UNDO'}
    delta: bpy.props.FloatProperty(name="Delta", default=0.015)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def shift_frame(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
            dim_max = max(max_x - min_x, max_y - min_y, max_z - min_z)
            step = self.delta * (dim_max / 1.0)

            frame_faces = [f for f in bm.faces if f[tier_layer] == 0]
            if not frame_faces: return 0

            recessed_faces = [f for f in bm.faces if f[tier_layer] == 2]
            recessed_verts = set(v for f in recessed_faces for v in f.verts)

            v_disps = {}
            for f in frame_faces:
                s = get_side_name(f.calc_center_median(), min_x, max_x, min_y, max_y, min_z, max_z)
                dir_vec = SIDE_AXES[s][0]
                if f.normal.dot(dir_vec) > 0.5:
                    for v in f.verts:
                        if v not in recessed_verts:
                            v_disps.setdefault(v, []).append(dir_vec * step)

            for v, disps in v_disps.items():
                v.co += sum(disps, Vector((0, 0, 0))) / len(disps)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, shift_frame)
        if count == 0:
            self.report({'WARNING'}, "Chưa phân tích mặt hoặc không có mặt Khung Viền!")
            return {'CANCELLED'}
        self.report({'INFO'}, f"Đã {'tăng dày' if self.delta > 0 else 'giảm dày'} Khung Viền ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_adjust_recess_depth(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_recess_depth", "Chỉnh Độ Sâu Ván Nền", {'REGISTER', 'UNDO'}
    delta: bpy.props.FloatProperty(name="Delta", default=0.015)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def shift_recess(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
            dim_max = max(max_x - min_x, max_y - min_y, max_z - min_z)
            step = self.delta * (dim_max / 1.0)

            recessed_faces = [f for f in bm.faces if f[tier_layer] == 2]
            if not recessed_faces: return 0

            v_disps = {}
            for f in recessed_faces:
                s = get_side_name(f.calc_center_median(), min_x, max_x, min_y, max_y, min_z, max_z)
                dir_vec = SIDE_AXES[s][0]
                for v in f.verts:
                    v_disps.setdefault(v, []).append(-dir_vec * step)

            for v, disps in v_disps.items():
                v.co += sum(disps, Vector((0, 0, 0))) / len(disps)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, shift_recess)
        if count == 0:
            self.report({'WARNING'}, "Chưa phân tích mặt hoặc không có mặt Ván Nền!")
            return {'CANCELLED'}
        self.report({'INFO'}, f"Đã đẩy ván nền {'sâu hơn' if self.delta > 0 else 'nông hơn'} ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_adjust_plank_thickness(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_plank_thickness", "Chỉnh Độ Dày Thanh Lồi", {'REGISTER', 'UNDO'}
    delta: bpy.props.FloatProperty(name="Delta", default=0.015)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def shift_thick(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
            dim_max = max(max_x - min_x, max_y - min_y, max_z - min_z)
            step = self.delta * (dim_max / 1.0)

            raised_faces = [f for f in bm.faces if f[tier_layer] == 1]
            if not raised_faces: return 0

            v_disps = {}
            for f in raised_faces:
                s = get_side_name(f.calc_center_median(), min_x, max_x, min_y, max_y, min_z, max_z)
                dir_vec = SIDE_AXES[s][0]
                if f.normal.dot(dir_vec) > 0.5:
                    for v in f.verts:
                        v_disps.setdefault(v, []).append(dir_vec * step)

            for v, disps in v_disps.items():
                v.co += sum(disps, Vector((0, 0, 0))) / len(disps)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, shift_thick)
        if count == 0:
            self.report({'WARNING'}, "Chưa phân tích mặt hoặc không có mặt Thanh Lồi!")
            return {'CANCELLED'}
        self.report({'INFO'}, f"Đã {'tăng dày' if self.delta > 0 else 'giảm dày'} cho Thanh Lồi ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_adjust_plank_scale(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_plank_scale", "Chỉnh Kích Thước Thanh Lồi", {'REGISTER', 'UNDO'}
    dimension: bpy.props.EnumProperty(items=[('WIDTH', "Bề Rộng", ""), ('HEIGHT', "Chiều Cao", "")], default='WIDTH')
    factor: bpy.props.FloatProperty(name="Factor", default=1.05)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}
        sw, sh = (self.factor, 1.0) if self.dimension == 'WIDTH' else (1.0, self.factor)

        def scale_planks(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
            raised_faces = [f for f in bm.faces if f[tier_layer] == 1]
            if not raised_faces: return 0

            selected = [f for f in raised_faces if f.select]
            active_pool = selected if selected else raised_faces

            side_faces = {}
            for f in active_pool:
                side_faces.setdefault(get_side_name(f.calc_center_median(), min_x, max_x, min_y, max_y, min_z, max_z), set()).add(f)

            displacements = {}
            for s, flist in side_faces.items():
                N, T, B = SIDE_AXES[s]
                visited = set()
                for f in flist:
                    if f in visited: continue
                    cluster, stack = set(), [f]
                    visited.add(f)
                    while stack:
                        curr = stack.pop()
                        cluster.add(curr)
                        for e in curr.edges:
                            for lk in e.link_faces:
                                if lk in flist and lk not in visited:
                                    visited.add(lk); stack.append(lk)
                    c_verts = set(v for cf in cluster for v in cf.verts)
                    if not c_verts: continue
                    center = sum((v.co for v in c_verts), Vector((0, 0, 0))) / len(c_verts)

                    t_proj = [v.co.dot(T) for v in c_verts]
                    b_proj = [v.co.dot(B) for v in c_verts]
                    span_t = max(t_proj) - min(t_proj)
                    span_b = max(b_proj) - min(b_proj)
                    if span_t >= span_b:
                        axis_len, axis_width = T, B
                    else:
                        axis_len, axis_width = B, T

                    for v in c_verts:
                        D = v.co - center
                        displacements.setdefault(v, []).append(
                            center + (D.dot(axis_width) * sw) * axis_width + (D.dot(axis_len) * sh) * axis_len + D.dot(N) * N
                        )

            for v, new_cos in displacements.items():
                v.co = sum(new_cos, Vector((0, 0, 0))) / len(new_cos)
            return len(displacements)

        count = apply_bmesh_op(obj, context, scale_planks)
        if count == 0:
            self.report({'WARNING'}, "Chưa phân tích mặt hoặc không có mặt Thanh Lồi!")
            return {'CANCELLED'}
        pct = f"+{(self.factor-1.0)*100:.0f}%" if self.factor > 1.0 else f"{(self.factor-1.0)*100:.0f}%"
        self.report({'INFO'}, f"Đã chỉnh {'Bề rộng' if self.dimension=='WIDTH' else 'Chiều cao'} {pct} ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_adjust_accessory_scale(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_accessory_scale", "Chỉnh Kích Thước Phụ Kiện", {'REGISTER', 'UNDO'}
    factor: bpy.props.FloatProperty(name="Factor", default=1.05)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def scale_acc(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            acc_faces = [f for f in bm.faces if f[tier_layer] == 3]
            if not acc_faces: return 0

            visited = set()
            displacements = {}
            for f in acc_faces:
                if f in visited: continue
                cluster, stack = set(), [f]
                visited.add(f)
                while stack:
                    curr = stack.pop()
                    cluster.add(curr)
                    for e in curr.edges:
                        for lk in e.link_faces:
                            if lk in acc_faces and lk not in visited:
                                visited.add(lk); stack.append(lk)
                c_verts = set(v for cf in cluster for v in cf.verts)
                if not c_verts: continue
                center = sum((v.co for v in c_verts), Vector((0, 0, 0))) / len(c_verts)
                for v in c_verts:
                    displacements.setdefault(v, []).append(center + (v.co - center) * self.factor)

            for v, new_cos in displacements.items():
                v.co = sum(new_cos, Vector((0, 0, 0))) / len(new_cos)
            return len(displacements)

        count = apply_bmesh_op(obj, context, scale_acc)
        if count == 0:
            self.report({'WARNING'}, "Không có mặt thuộc nhóm Phụ Kiện [Vàng]!")
            return {'CANCELLED'}
        pct = f"+{(self.factor-1.0)*100:.0f}%" if self.factor > 1.0 else f"{(self.factor-1.0)*100:.0f}%"
        self.report({'INFO'}, f"Đã co giãn Phụ Kiện {pct} ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_slide_planks(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.slide_planks", "Trượt Nan Gỗ", {'REGISTER', 'UNDO'}
    direction: bpy.props.EnumProperty(
        items=[
            ('LEFT', "Trái", ""), ('RIGHT', "Phải", ""),
            ('UP', "Lên", ""), ('DOWN', "Xuống", ""),
            ('IN', "Vào", ""), ('OUT', "Ra", "")
        ],
        default='RIGHT'
    )
    delta: bpy.props.FloatProperty(name="Delta", default=0.015)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def slide(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
            dim_max = max(max_x - min_x, max_y - min_y, max_z - min_z)
            step = abs(self.delta) * (dim_max / 1.0)

            raised_faces = [f for f in bm.faces if f[tier_layer] == 1]
            if not raised_faces: return 0

            selected = [f for f in raised_faces if f.select]
            active_pool = selected if selected else raised_faces

            visited, clusters = set(), []
            flist_set = set(active_pool)
            for f in active_pool:
                if f in visited: continue
                cluster, stack = set(), [f]
                visited.add(f)
                while stack:
                    curr = stack.pop()
                    cluster.add(curr)
                    for e in curr.edges:
                        for lk in e.link_faces:
                            if lk in flist_set and lk not in visited:
                                visited.add(lk); stack.append(lk)
                clusters.append(cluster)

            v_disps = {}
            for cl in clusters:
                c_center = sum((f.calc_center_median() for f in cl), Vector((0, 0, 0))) / len(cl)
                s = get_side_name(c_center, min_x, max_x, min_y, max_y, min_z, max_z)
                N, T, B = SIDE_AXES[s]
                if self.direction == 'LEFT': move_vec = -T * step
                elif self.direction == 'RIGHT': move_vec = T * step
                elif self.direction == 'UP': move_vec = B * step
                elif self.direction == 'DOWN': move_vec = -B * step
                elif self.direction == 'IN': move_vec = -N * step
                else: move_vec = N * step

                cl_verts = set(v for f in cl for v in f.verts)
                for v in cl_verts:
                    v_disps.setdefault(v, []).append(move_vec)

            for v, disps in v_disps.items():
                v.co += sum(disps, Vector((0, 0, 0))) / len(disps)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, slide)
        if count == 0:
            self.report({'WARNING'}, "Chưa phân tích mặt hoặc không có mặt Thanh Lồi!")
            return {'CANCELLED'}
        dir_names = {'LEFT': "Trái", 'RIGHT': "Phải", 'UP': "Lên", 'DOWN': "Xuống", 'IN': "Vào", 'OUT': "Ra"}
        self.report({'INFO'}, f"Đã trượt {dir_names.get(self.direction, '')} Thanh Lồi ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_snap_planks_to_backing(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.snap_planks_to_backing", "Bám Sát Ván Nền", {'REGISTER', 'UNDO'}

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def snap_to_backing(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)

            raised_faces = [f for f in bm.faces if f[tier_layer] == 1]
            recessed_faces = [f for f in bm.faces if f[tier_layer] == 2]
            if not raised_faces or not recessed_faces: return 0

            selected = [f for f in raised_faces if f.select]
            active_pool = selected if selected else raised_faces

            recessed_by_side = {}
            for f in recessed_faces:
                s = get_side_name(f.calc_center_median(), min_x, max_x, min_y, max_y, min_z, max_z)
                recessed_by_side.setdefault(s, set()).add(f)

            visited, clusters = set(), []
            flist_set = set(active_pool)
            for f in active_pool:
                if f in visited: continue
                cluster, stack = set(), [f]
                visited.add(f)
                while stack:
                    curr = stack.pop()
                    cluster.add(curr)
                    for e in curr.edges:
                        for lk in e.link_faces:
                            if lk in flist_set and lk not in visited:
                                visited.add(lk); stack.append(lk)
                clusters.append(cluster)

            v_disps = {}
            for cl in clusters:
                c_center = sum((f.calc_center_median() for f in cl), Vector((0, 0, 0))) / len(cl)
                s = get_side_name(c_center, min_x, max_x, min_y, max_y, min_z, max_z)
                N, T, B = SIDE_AXES[s]

                b_faces = recessed_by_side.get(s)
                if not b_faces: continue
                b_verts = set(v for f in b_faces for v in f.verts)
                b_depth = max(v.co.dot(N) for v in b_verts)

                c_verts = set(v for cl_f in cl for v in cl_f.verts)
                r_back = min(v.co.dot(N) for v in c_verts)

                shift_vec = (b_depth - r_back) * N
                for v in c_verts:
                    v_disps.setdefault(v, []).append(shift_vec)

            for v, disps in v_disps.items():
                v.co += sum(disps, Vector((0, 0, 0))) / len(disps)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, snap_to_backing)
        if count == 0:
            self.report({'WARNING'}, "Chưa phân tích mặt hoặc không có mặt Ván Nền [X.Dương] / Thanh Lồi [Xanh Lá]!")
            return {'CANCELLED'}
        self.report({'INFO'}, f"Đã bám sát Thanh Lồi vào Ván Nền ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_adjust_other_depth(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_other_depth", "Chỉnh Độ Dày Mặt Khác", {'REGISTER', 'UNDO'}
    delta: bpy.props.FloatProperty(name="Delta", default=0.015)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def shift_other(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            min_x, max_x, min_y, max_y, min_z, max_z = get_mesh_bounds(bm)
            dim_max = max(max_x - min_x, max_y - min_y, max_z - min_z)
            step = self.delta * (dim_max / 1.0)

            other_faces = [f for f in bm.faces if f[tier_layer] == 4]
            if not other_faces: return 0

            selected = [f for f in other_faces if f.select]
            target_faces = selected if selected else other_faces

            v_disps = {}
            for f in target_faces:
                dir_vec = f.normal.normalized() if f.normal.length > 0.001 else Vector((0, 0, 1))
                for v in f.verts:
                    v_disps.setdefault(v, []).append(dir_vec * step)

            for v, disps in v_disps.items():
                v.co += sum(disps, Vector((0, 0, 0))) / len(disps)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, shift_other)
        if count == 0:
            self.report({'WARNING'}, "Chưa có mặt nào trong nhóm Khác [Tím]!")
            return {'CANCELLED'}
        self.report({'INFO'}, f"Đã đẩy mặt Khác {'nhô ra' if self.delta > 0 else 'thụt vào'} ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_adjust_other_scale(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.adjust_other_scale", "Chỉnh Kích Thước Mặt Khác", {'REGISTER', 'UNDO'}
    dimension: bpy.props.EnumProperty(items=[('WIDTH', "Bề Rộng", ""), ('HEIGHT', "Chiều Cao", "")], default='WIDTH')
    factor: bpy.props.FloatProperty(name="Factor", default=1.05)

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}
        sw, sh = (self.factor, 1.0) if self.dimension == 'WIDTH' else (1.0, self.factor)

        def scale_other(bm):
            tier_layer = bm.faces.layers.int.get("BT_TIER")
            if not tier_layer: return 0
            other_faces = [f for f in bm.faces if f[tier_layer] == 4]
            if not other_faces: return 0

            selected = [f for f in other_faces if f.select]
            target_faces = selected if selected else other_faces

            visited, clusters = set(), []
            flist_set = set(target_faces)
            for f in target_faces:
                if f in visited: continue
                cluster, stack = set(), [f]
                visited.add(f)
                while stack:
                    curr = stack.pop()
                    cluster.add(curr)
                    for e in curr.edges:
                        for lk in e.link_faces:
                            if lk in flist_set and lk not in visited:
                                visited.add(lk); stack.append(lk)
                clusters.append(cluster)

            v_disps = {}
            for cl in clusters:
                c_verts = set(v for f in cl for v in f.verts)
                if not c_verts: continue
                center = sum((v.co for v in c_verts), Vector((0, 0, 0))) / len(c_verts)
                avg_normal = sum((f.normal for f in cl), Vector((0, 0, 0))).normalized()
                if avg_normal.length < 0.001: avg_normal = Vector((0, 0, 1))

                if abs(avg_normal.z) < 0.9:
                    T = Vector((-avg_normal.y, avg_normal.x, 0.0)).normalized()
                else:
                    T = Vector((1.0, 0.0, 0.0))
                B = avg_normal.cross(T).normalized()

                t_proj = [v.co.dot(T) for v in c_verts]
                b_proj = [v.co.dot(B) for v in c_verts]
                span_t = max(t_proj) - min(t_proj)
                span_b = max(b_proj) - min(b_proj)
                if span_t >= span_b:
                    axis_len, axis_width = T, B
                else:
                    axis_len, axis_width = B, T

                for v in c_verts:
                    D = v.co - center
                    new_pos = center + (D.dot(axis_width) * sw) * axis_width + (D.dot(axis_len) * sh) * axis_len + D.dot(avg_normal) * avg_normal
                    v_disps.setdefault(v, []).append(new_pos)

            for v, new_cos in v_disps.items():
                v.co = sum(new_cos, Vector((0, 0, 0))) / len(new_cos)
            return len(v_disps)

        count = apply_bmesh_op(obj, context, scale_other)
        if count == 0:
            self.report({'WARNING'}, "Chưa có mặt nào trong nhóm Khác [Tím]!")
            return {'CANCELLED'}
        pct = f"+{(self.factor-1.0)*100:.0f}%" if self.factor > 1.0 else f"{(self.factor-1.0)*100:.0f}%"
        self.report({'INFO'}, f"Đã chỉnh {'Bề rộng' if self.dimension=='WIDTH' else 'Chiều dài'} mặt Khác {pct} ({count} đỉnh)")
        return {'FINISHED'}


class BLOCKTOOLS_OT_reset_original_shape(bpy.types.Operator):
    bl_idname, bl_label, bl_options = "blocktools.reset_original_shape", "Reset Hình Dạng Ban Đầu", {'REGISTER', 'UNDO'}

    def execute(self, context):
        obj = context.edit_object or context.active_object
        if not obj or obj.type != 'MESH': return {'CANCELLED'}

        def reset_coords(bm):
            lay = bm.verts.layers.float_vector.get("BT_ORIGINAL_CO")
            if not lay: return 0
            bm.verts.ensure_lookup_table()
            for v in bm.verts:
                saved = v[lay]
                if saved.length_squared > 0.000001:
                    v.co = saved.copy()
            return len(bm.verts)

        if context.mode == 'EDIT_MESH':
            bm = bmesh.from_edit_mesh(obj.data)
            count = reset_coords(bm)
            bmesh.update_edit_mesh(obj.data)
        else:
            bm = bmesh.new()
            bm.from_mesh(obj.data)
            count = reset_coords(bm)
            bm.to_mesh(obj.data)
            bm.free()
            obj.data.update()

        if count == 0:
            self.report({'WARNING'}, "Chưa có bản lưu hình dạng ban đầu! Bấm 'Chuẩn Hóa Khối' hoặc 'Phân Tích' trước.")
            return {'CANCELLED'}
        self.report({'INFO'}, f"Đã reset về hình dạng ban đầu ({count} đỉnh)")
        return {'FINISHED'}


# ==============================================================================
# OPERATORS: UNITY FBX EXPORT
# ==============================================================================
class BLOCKTOOLS_OT_export_fbx(bpy.types.Operator):
    bl_idname, bl_label = "blocktools.export_fbx", "Xuất FBX Unity"

    def execute(self, context):
        obj, settings = context.active_object, context.scene.block_tools_settings
        if not obj or obj.type != 'MESH':
            self.report({'ERROR'}, "Hãy chọn một Mesh!")
            return {'CANCELLED'}
        if context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        obj.select_set(True)
        context.view_layer.objects.active = obj

        export_dir = bpy.path.abspath(settings.export_dir)
        os.makedirs(export_dir, exist_ok=True)
        fname = (settings.export_filename.strip() or obj.name) + (".fbx" if not (settings.export_filename.strip() or obj.name).lower().endswith(".fbx") else "")
        path = os.path.join(export_dir, fname)

        # Lưu dữ liệu phân loại 5 tầng vào Color Attribute (BT_TIER_COLORS) để bảo toàn 100% khi đọc lại
        bake_tier_colors_to_mesh(obj)
        if obj.data.color_attributes.get("BT_TIER_COLORS"):
            obj.data.color_attributes.active_color_index = obj.data.color_attributes.find("BT_TIER_COLORS")

        try:
            bpy.ops.export_scene.fbx(
                filepath=path,
                use_selection=True,
                object_types={'MESH'},
                axis_forward='-Z',
                axis_up='Y',
                bake_space_transform=True,
                apply_unit_scale=True,
                apply_scale_options='FBX_SCALE_UNITS',
                colors_type='SRGB',
                bake_anim=False
            )
            self.report({'INFO'}, f"Xuất Unity FBX thành công (Kèm 5 tầng màu): {path}")
        except Exception as e:
            self.report({'ERROR'}, f"Lỗi xuất FBX: {e}"); return {'CANCELLED'}
        return {'FINISHED'}


class BLOCKTOOLS_OT_open_export_dir(bpy.types.Operator):
    bl_idname, bl_label = "blocktools.open_export_dir", "Mở Thư Mục"

    def execute(self, context):
        path = bpy.path.abspath(context.scene.block_tools_settings.export_dir)
        if os.path.exists(path):
            os.startfile(path) if os.name == 'nt' else subprocess.Popen(['xdg-open', path])
        return {'FINISHED'}


# ==============================================================================
# UI PANEL WITH COLLAPSIBLE ACCORDION SECTIONS (PHẦN RÚT GỌN GIAO DIỆN)
# ==============================================================================
class VIEW3D_PT_block_tools(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Block Tools'
    bl_label = "Block Tools (Unity Grid)"

    def draw(self, context):
        layout = self.layout
        settings = context.scene.block_tools_settings
        obj = context.active_object

        # 1. CHUẨN HÓA & CĂN TRỤC (COLLAPSIBLE)
        r = layout.row(align=True)
        r.prop(settings, "fold_standardize", text="1. Chuẩn Hóa & Căn Trục (1.0m)", icon='TRIA_DOWN' if settings.fold_standardize else 'TRIA_RIGHT', toggle=True)
        if settings.fold_standardize:
            box = layout.box()
            if obj and obj.type == 'MESH':
                box.label(text=f"Mesh: {obj.name} | {obj.dimensions.x:.2f} x {obj.dimensions.y:.2f} x {obj.dimensions.z:.2f}m", icon='INFO')
                box.prop(settings, "target_size")
                box.prop(settings, "scale_mode")
                box.prop(settings, "smooth_angle")
                box.column().operator("blocktools.standardize", text="⚡ Chuẩn Hóa Khối (1.0m)", icon='MOD_REMESH')
                r_rot = box.row(align=True)
                r_rot.operator("blocktools.rotate_z", text="+90°").angle_degrees = 90.0
                r_rot.operator("blocktools.rotate_z", text="-90°").angle_degrees = -90.0
                r_rot.operator("blocktools.rotate_z", text="180°").angle_degrees = 180.0
            else: box.label(text="Hãy chọn một Mesh!", icon='ERROR')

        # 2. BẢNG PHÂN LOẠI 4 TẦNG (COLLAPSIBLE)
        r = layout.row(align=True)
        r.prop(settings, "fold_classification", text="2. Bảng Phân Loại Mặt Khối (4 Tầng)", icon='TRIA_DOWN' if settings.fold_classification else 'TRIA_RIGHT', toggle=True)
        if settings.fold_classification:
            box = layout.box()
            if obj and obj.type == 'MESH':
                # Nút hành động chính
                r_act = box.row(align=True)
                r_act.scale_y = 1.15
                r_act.operator("blocktools.analyze_mesh", text="🔍 Phân Tích / Đọc Lại", icon='VIEWZOOM')
                r_act.operator("blocktools.toggle_debug_colors", text="Màu: BẬT" if settings.debug_colors_active else "Màu: TẮT", icon='SHADING_RENDERED' if settings.debug_colors_active else 'SHADING_SOLID')

                r_sub = box.row(align=True)
                op_re = r_sub.operator("blocktools.analyze_mesh", text="⚡ Quét Lại Từ Đầu (Bỏ Qua Màu FBX)", icon='FILE_REFRESH')
                op_re.force_heuristic = True

                # Chú thích 5 màu kiểm tra trực quan
                leg_box = box.box()
                leg_box.label(text="Quy Ước 5 Màu Kiểm Tra (Viewport):", icon='COLOR')
                col_leg = leg_box.column(align=True)
                col_leg.label(text="• Màu Đỏ: Khung Viền ngoài")
                col_leg.label(text="• Màu Xanh Lá: Thanh Lồi (nan gỗ trung tâm)")
                col_leg.label(text="• Màu Xanh Dương: Ván Nền (ván chìm phía sau)")
                col_leg.label(text="• Màu Vàng: Phụ Kiện (đinh tán, hoa văn)")
                col_leg.label(text="• Màu Tím: Khác (mặt lẻ, mặt lỗi, tinh chỉnh riêng)")

                # Bảng số lượng và công cụ gán
                tbl = box.box().column(align=True)
                tiers = [
                    (0, "1. Viền [Đỏ]", settings.count_frame),
                    (1, "2. Nan Lồi [Xanh Lá]", settings.count_raised),
                    (2, "3. Ván Nền [X.Dương]", settings.count_recessed),
                    (3, "4. Phụ Kiện [Vàng]", settings.count_accessories),
                    (4, "5. Khác [Tím]", settings.count_other),
                ]
                for idx, label, count in tiers:
                    row = tbl.row(align=True)
                    row.label(text=f"{label}: {count}")
                    op_s = row.operator("blocktools.group_select", text="Chọn")
                    op_s.tier_index = idx
                    op_a = row.operator("blocktools.group_assign", text="➕ Gán")
                    op_a.target_tier = idx

                col_hints = box.column(align=True)
                col_hints.label(text="💡 Nhấn 'Phân Tích Khối' trước để quét mặt!", icon='INFO')
                col_hints.label(text="Sửa nhầm: Chọn mặt trong Edit Mode -> Bấm ➕ Gán", icon='EDITMODE_HLT')
            else: box.label(text="Hãy chọn một Mesh!", icon='ERROR')

        # 3. BẢNG MÀU & UV PALETTE (COLLAPSIBLE)
        r = layout.row(align=True)
        r.prop(settings, "fold_palette", text="3. Bảng Màu & UV Palette", icon='TRIA_DOWN' if settings.fold_palette else 'TRIA_RIGHT', toggle=True)
        if settings.fold_palette:
            box = layout.box()
            box.prop(settings, "palette_source")
            if settings.palette_source == 'CUSTOM':
                box.prop(settings, "custom_image_path")
                box.prop(settings, "grid_preset")
            r_p = box.row(align=True)
            r_p.operator("blocktools.load_curated_swatches", text="Màu Chuẩn Game", icon='COLOR')
            r_p.operator("blocktools.scan_palette_image", text="Đọc Từ Ảnh", icon='FILE_IMAGE')

            r_act = box.row(align=True)
            r_act.scale_y = 1.15
            r_act.operator("blocktools.smart_autocolor", text="⚡ Tự Động (5 Tầng)", icon='COLOR')
            r_act.operator("blocktools.reset_original_color", text="↺ Reset Màu Gốc", icon='LOOP_BACK')

            # TÔ MÀU THEO TẦNG
            tbox = box.box()
            tbox.label(text="🎯 Mục Tiêu Khi Chọn Màu (Tô Theo Tầng):", icon='BRUSH_DATA')

            r1 = tbox.row(align=True)
            r1.prop_enum(settings, "paint_target", 'TIER_0', text="1. Viền [Đỏ]")
            r1.prop_enum(settings, "paint_target", 'TIER_1', text="2. Nan Lồi [Xanh]")
            r1.prop_enum(settings, "paint_target", 'TIER_2', text="3. Ván Nền [Lam]")

            r2 = tbox.row(align=True)
            r2.prop_enum(settings, "paint_target", 'TIER_3', text="4. Phụ Kiện [Vàng]")
            r2.prop_enum(settings, "paint_target", 'TIER_4', text="5. Khác [Tím]")

            r3 = tbox.row(align=True)
            r3.prop_enum(settings, "paint_target", 'SELECTION', text="Mặt Đang Chọn")
            r3.prop_enum(settings, "paint_target", 'ALL', text="Toàn Bộ Khối")

            target_labels = {
                'TIER_0': "1. Khung Viền [Đỏ]",
                'TIER_1': "2. Thanh Lồi [Xanh Lá]",
                'TIER_2': "3. Ván Nền [Xanh Dương]",
                'TIER_3': "4. Phụ Kiện [Vàng]",
                'TIER_4': "5. Khác [Tím]",
                'SELECTION': "Các Mặt Đang Chọn (Edit Mode)",
                'ALL': "Toàn Bộ Khối",
            }
            lbl_row = tbox.row(align=True)
            lbl_row.label(text=f"👉 Đang chọn tô: {target_labels.get(settings.paint_target, '')}", icon='RESTRICT_COLOR_OFF')

            if settings.swatches:
                box.label(text="Bảng Màu (Bấm ô để tô ngay vào mục tiêu trên):", icon='COLORSET_01_VEC')
                grid = box.box().grid_flow(row_major=True, columns=2, even_columns=True, even_rows=True)
                for i, sw in enumerate(settings.swatches):
                    row = grid.row(align=True)
                    row.prop(sw, "color", text="")
                    row.operator("blocktools.apply_swatch", text=sw.name).swatch_index = i

        # 4. TÙY BIẾN HÌNH HỌC (COLLAPSIBLE)
        r = layout.row(align=True)
        r.prop(settings, "fold_tweaker", text="4. Tùy Biến Hình Học (5 Tầng)", icon='TRIA_DOWN' if settings.fold_tweaker else 'TRIA_RIGHT', toggle=True)
        if settings.fold_tweaker:
            box = layout.box()
            if obj and obj.type == 'MESH':
                # 1. Khung Viền [Đỏ]
                box.label(text="1. Khung Viền [Đỏ]:", icon='SNAP_EDGE')
                rf = box.row(align=True)
                rf.operator("blocktools.adjust_frame_thickness", text="◄ Giảm Dày (-1.5%)").delta = -0.015
                rf.operator("blocktools.adjust_frame_thickness", text="► Tăng Dày (+1.5%)").delta = 0.015

                # 2. Thanh Lồi [Xanh Lá]
                box.label(text="2. Thanh Lồi Trung Tâm [Xanh Lá]:", icon='SNAP_VOLUME')
                rt = box.row(align=True)
                rt.operator("blocktools.adjust_plank_thickness", text="◄ Giảm Dày (-1.5%)").delta = -0.015
                rt.operator("blocktools.adjust_plank_thickness", text="► Tăng Dày (+1.5%)").delta = 0.015

                rw = box.row(align=True)
                w1 = rw.operator("blocktools.adjust_plank_scale", text="◄ Hẹp (-5%)")
                w1.dimension, w1.factor = 'WIDTH', 0.95
                w2 = rw.operator("blocktools.adjust_plank_scale", text="► Rộng (+5%)")
                w2.dimension, w2.factor = 'WIDTH', 1.05

                rh = box.row(align=True)
                h1 = rh.operator("blocktools.adjust_plank_scale", text="◄ Ngắn (-5%)")
                h1.dimension, h1.factor = 'HEIGHT', 0.95
                h2 = rh.operator("blocktools.adjust_plank_scale", text="► Dài (+5%)")
                h2.dimension, h2.factor = 'HEIGHT', 1.05

                rs = box.row(align=True)
                s1 = rs.operator("blocktools.slide_planks", text="◄ Trượt Trái (-1.5%)")
                s1.direction, s1.delta = 'LEFT', -0.015
                s2 = rs.operator("blocktools.slide_planks", text="► Trượt Phải (+1.5%)")
                s2.direction, s2.delta = 'RIGHT', 0.015

                rv = box.row(align=True)
                v1 = rv.operator("blocktools.slide_planks", text="▼ Trượt Xuống (-1.5%)")
                v1.direction, v1.delta = 'DOWN', -0.015
                v2 = rv.operator("blocktools.slide_planks", text="▲ Trượt Lên (+1.5%)")
                v2.direction, v2.delta = 'UP', 0.015

                rdepth = box.row(align=True)
                d1 = rdepth.operator("blocktools.slide_planks", text="◄ Trượt Vào (-1.5%)")
                d1.direction, d1.delta = 'IN', -0.015
                d2 = rdepth.operator("blocktools.slide_planks", text="► Trượt Ra (+1.5%)")
                d2.direction, d2.delta = 'OUT', 0.015

                box.operator("blocktools.snap_planks_to_backing", text="🧲 Bám Sát Ván Nền (Snap)", icon='SNAP_FACE')

                # 3. Ván Nền [X.Dương]
                box.label(text="3. Ván Nền Phía Sau [X.Dương]:", icon='AXIS_SIDE')
                rd = box.row(align=True)
                rd.operator("blocktools.adjust_recess_depth", text="◄ Nông (-1.5%)").delta = -0.015
                rd.operator("blocktools.adjust_recess_depth", text="► Sâu (+1.5%)").delta = 0.015

                # 4. Phụ Kiện [Vàng]
                box.label(text="4. Phụ Kiện / Vật Nổi [Vàng]:", icon='ORIENTATION_LOCAL')
                ra = box.row(align=True)
                ra.operator("blocktools.adjust_accessory_scale", text="◄ Thu Nhỏ (-5%)").factor = 0.95
                ra.operator("blocktools.adjust_accessory_scale", text="► Phóng To (+5%)").factor = 1.05

                # 5. Khác / Tinh Chỉnh Lẻ [Tím]
                box.label(text="5. Khác / Tinh Chỉnh Lẻ [Tím]:", icon='COLORSET_07_VEC')
                ro_d = box.row(align=True)
                ro_d.operator("blocktools.adjust_other_depth", text="◄ Thụt Vào (-1.5%)").delta = -0.015
                ro_d.operator("blocktools.adjust_other_depth", text="► Nhô Ra (+1.5%)").delta = 0.015

                ro_w = box.row(align=True)
                ow1 = ro_w.operator("blocktools.adjust_other_scale", text="◄ Hẹp (-5%)")
                ow1.dimension, ow1.factor = 'WIDTH', 0.95
                ow2 = ro_w.operator("blocktools.adjust_other_scale", text="► Rộng (+5%)")
                ow2.dimension, ow2.factor = 'WIDTH', 1.05

                ro_h = box.row(align=True)
                oh1 = ro_h.operator("blocktools.adjust_other_scale", text="◄ Ngắn (-5%)")
                oh1.dimension, oh1.factor = 'HEIGHT', 0.95
                oh2 = ro_h.operator("blocktools.adjust_other_scale", text="► Dài (+5%)")
                oh2.dimension, oh2.factor = 'HEIGHT', 1.05

                r_rst = box.row(align=True)
                r_rst.scale_y = 1.15
                r_rst.operator("blocktools.reset_original_shape", text="↺ Reset Dáng Ban Đầu", icon='LOOP_BACK')
                r_rst.operator("blocktools.standardize", text="⚡ Chuẩn Hóa Lại Bounds (1.0m)", icon='MOD_REMESH')
            else: box.label(text="Hãy chọn một Mesh!", icon='ERROR')

        # 5. XUẤT FBX SANG UNITY (COLLAPSIBLE)
        r = layout.row(align=True)
        r.prop(settings, "fold_export", text="5. Xuất File Sang Unity (FBX)", icon='TRIA_DOWN' if settings.fold_export else 'TRIA_RIGHT', toggle=True)
        if settings.fold_export:
            box = layout.box()
            box.prop(settings, "export_dir")
            box.prop(settings, "export_filename")
            rexp = box.row(align=True)
            rexp.operator("blocktools.export_fbx", text="💾 Xuất FBX Chuẩn", icon='CHECKMARK')
            rexp.operator("blocktools.open_export_dir", text="📂 Mở Thư Mục", icon='FILE_FOLDER')


# ==============================================================================
# REGISTRATION
# ==============================================================================
classes = (
    BlockToolsPaletteItem, BlockToolsSettings,
    BLOCKTOOLS_OT_standardize, BLOCKTOOLS_OT_rotate_z,
    BLOCKTOOLS_OT_analyze_mesh, BLOCKTOOLS_OT_toggle_debug_colors,
    BLOCKTOOLS_OT_group_select, BLOCKTOOLS_OT_group_assign,
    BLOCKTOOLS_OT_load_curated_swatches, BLOCKTOOLS_OT_scan_palette_image,
    BLOCKTOOLS_OT_apply_swatch, BLOCKTOOLS_OT_smart_autocolor,
    BLOCKTOOLS_OT_reset_original_color,
    BLOCKTOOLS_OT_adjust_frame_thickness,
    BLOCKTOOLS_OT_adjust_recess_depth, BLOCKTOOLS_OT_adjust_plank_thickness,
    BLOCKTOOLS_OT_adjust_plank_scale, BLOCKTOOLS_OT_slide_planks,
    BLOCKTOOLS_OT_snap_planks_to_backing,
    BLOCKTOOLS_OT_adjust_accessory_scale,
    BLOCKTOOLS_OT_adjust_other_depth, BLOCKTOOLS_OT_adjust_other_scale,
    BLOCKTOOLS_OT_reset_original_shape,
    BLOCKTOOLS_OT_export_fbx, BLOCKTOOLS_OT_open_export_dir,
    VIEW3D_PT_block_tools,
)

def register():
    for cls in classes: bpy.utils.register_class(cls)
    bpy.types.Scene.block_tools_settings = bpy.props.PointerProperty(type=BlockToolsSettings)
    try:
        s = bpy.context.scene.block_tools_settings
        if len(s.swatches) == 0:
            for item in CURATED_SWATCHES:
                sw = s.swatches.add()
                sw.name, sw.color, sw.uv_coord = item["name"], item["color"], item["uv"]
    except Exception: pass

def unregister():
    for cls in reversed(classes): bpy.utils.unregister_class(cls)
    del bpy.types.Scene.block_tools_settings

if __name__ == "__main__":
    register()
