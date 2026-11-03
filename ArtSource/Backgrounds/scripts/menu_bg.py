# Main menu region backgrounds (North / Europe) for JigsawMatch2.
# Flat "layered illustration" render: every shape is an emissive 2D silhouette at its own depth,
# misted towards the sky colour, framed by an orthographic portrait camera (864x1872, like the Asia art).
# Runs inside the live Blender over MCP: prepend a file that sets JOB = 'north' | 'europe'.
# Everything is built in a temporary scene that is removed afterwards, so the open file is left as it was.
import bpy, bmesh, math, random, os

OUT_DIR = r"D:\GitHub\JigsawMatch2\ArtSource\Backgrounds"
W, H = 4.62, 10.0          # frame in scene units (x: -W/2..W/2, z: 0..H)
RES = (864, 1872)
PREFIX = "JMBG_"

created = {"objects": [], "meshes": [], "materials": [], "cameras": [], "worlds": []}


def hexc(h, a=1.0):
    h = h.lstrip('#')
    srgb = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (lin[0], lin[1], lin[2], a)


def mix(c1, c2, t):
    return tuple(c1[i] * (1 - t) + c2[i] * t for i in range(4))


def material(name, bottom, top, zmin, zmax, alpha_bottom=1.0, alpha_top=1.0, noise=0.0):
    """Emission with a vertical gradient (world Z) and an optional alpha gradient."""
    m = bpy.data.materials.new(PREFIX + name)
    created["materials"].append(m)
    m.use_nodes = True
    try:
        m.surface_render_method = 'BLENDED'
    except Exception:
        m.blend_method = 'BLEND'
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    geo = nt.nodes.new('ShaderNodeNewGeometry')
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    nt.links.new(geo.outputs['Position'], sep.inputs[0])
    z = sep.outputs['Z']
    if noise > 0:
        tex = nt.nodes.new('ShaderNodeTexNoise')
        tex.inputs['Scale'].default_value = 6.0
        nt.links.new(geo.outputs['Position'], tex.inputs['Vector'])
        add = nt.nodes.new('ShaderNodeMath')
        add.operation = 'MULTIPLY_ADD'
        nt.links.new(tex.outputs['Fac'], add.inputs[0])
        add.inputs[1].default_value = noise
        nt.links.new(z, add.inputs[2])
        z = add.outputs[0]
    mr = nt.nodes.new('ShaderNodeMapRange')
    mr.clamp = True
    mr.inputs['From Min'].default_value = zmin
    mr.inputs['From Max'].default_value = zmax
    nt.links.new(z, mr.inputs['Value'])
    cm = nt.nodes.new('ShaderNodeMix')
    cm.data_type = 'RGBA'
    cm.inputs[6].default_value = bottom
    cm.inputs[7].default_value = top
    nt.links.new(mr.outputs[0], cm.inputs[0])
    em = nt.nodes.new('ShaderNodeEmission')
    em.inputs['Strength'].default_value = 1.0
    nt.links.new(cm.outputs[2], em.inputs['Color'])
    am = nt.nodes.new('ShaderNodeMapRange')
    am.inputs['To Min'].default_value = alpha_bottom
    am.inputs['To Max'].default_value = alpha_top
    nt.links.new(mr.outputs[0], am.inputs['Value'])
    tr = nt.nodes.new('ShaderNodeBsdfTransparent')
    ms = nt.nodes.new('ShaderNodeMixShader')
    nt.links.new(am.outputs[0], ms.inputs[0])
    nt.links.new(tr.outputs[0], ms.inputs[1])
    nt.links.new(em.outputs[0], ms.inputs[2])
    nt.links.new(ms.outputs[0], out.inputs['Surface'])
    return m


def snow_material(name, rock_bottom, rock_top, snow, zmin, snowline, zmax):
    """Mountain: misty base -> rock colour -> ragged snow cap above the snowline."""
    m = material(name, rock_bottom, rock_top, zmin, snowline)
    nt = m.node_tree
    geo = [n for n in nt.nodes if n.type == 'NEW_GEOMETRY'][0]
    sep = [n for n in nt.nodes if n.type == 'SEPXYZ'][0]
    cm = [n for n in nt.nodes if n.type == 'MIX' and n.data_type == 'RGBA'][0]
    em = [n for n in nt.nodes if n.type == 'EMISSION'][0]
    tex = nt.nodes.new('ShaderNodeTexNoise')
    tex.inputs['Scale'].default_value = 3.0
    tex.inputs['Detail'].default_value = 6.0
    nt.links.new(geo.outputs['Position'], tex.inputs['Vector'])
    add = nt.nodes.new('ShaderNodeMath')
    add.operation = 'MULTIPLY_ADD'
    nt.links.new(tex.outputs['Fac'], add.inputs[0])
    add.inputs[1].default_value = (zmax - snowline) * 0.9
    nt.links.new(sep.outputs['Z'], add.inputs[2])
    step = nt.nodes.new('ShaderNodeMath')
    step.operation = 'GREATER_THAN'
    step.inputs[1].default_value = snowline + (zmax - snowline) * 0.5
    nt.links.new(add.outputs[0], step.inputs[0])
    sm = nt.nodes.new('ShaderNodeMix')
    sm.data_type = 'RGBA'
    sm.inputs[7].default_value = snow
    nt.links.new(step.outputs[0], sm.inputs[0])
    nt.links.new(cm.outputs[2], sm.inputs[6])
    nt.links.new(sm.outputs[2], em.inputs['Color'])
    return m


def poly(name, pts, depth, mat, scene):
    me = bpy.data.meshes.new(PREFIX + name)
    created["meshes"].append(me)
    bm = bmesh.new()
    verts = [bm.verts.new((x, depth, z)) for x, z in pts]
    bm.faces.new(verts)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(PREFIX + name, me)
    created["objects"].append(ob)
    scene.collection.objects.link(ob)
    me.materials.append(mat)
    return ob


def ridge(seed, base, peaks, rough, x0=-W / 2 - 0.3, x1=W / 2 + 0.3, n=160, bottom=-0.5):
    rnd = random.Random(seed)
    phases = [rnd.uniform(0, 6.28) for _ in range(4)]
    pts = []
    for i in range(n + 1):
        x = x0 + (x1 - x0) * i / n
        z = base
        for (px, ph, pw) in peaks:
            d = abs(x - px) / pw
            if d < 1:
                # pointed top, concave shoulders: a mix of a cone and a bell
                z += ph * (0.55 * (1 - d) ** 1.35 + 0.45 * (0.5 + 0.5 * math.cos(math.pi * d)) ** 2)
        z += rough * (0.5 * math.sin(x * 7.3 + phases[0]) + 0.3 * math.sin(x * 15.1 + phases[1])
                      + 0.2 * math.sin(x * 31.7 + phases[2]))
        pts.append((x, z))
    return [(x0, bottom)] + pts + [(x1, bottom)]


def circle_pts(cx, cz, rx, rz, n=40, a0=0.0, a1=2 * math.pi):
    return [(cx + rx * math.cos(a0 + (a1 - a0) * i / n), cz + rz * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]


def setup_scene(name, sky_bottom, sky_mid, sky_top):
    sc = bpy.data.scenes.new(PREFIX + name)
    sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = RES
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.filter_size = 1.8
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    try:
        sc.eevee.taa_render_samples = 32
    except Exception:
        pass
    cam_data = bpy.data.cameras.new(PREFIX + "cam")
    created["cameras"].append(cam_data)
    cam_data.type = 'ORTHO'
    cam_data.sensor_fit = 'VERTICAL'
    cam_data.ortho_scale = H
    cam_data.clip_end = 500
    cam = bpy.data.objects.new(PREFIX + "cam", cam_data)
    created["objects"].append(cam)
    sc.collection.objects.link(cam)
    cam.location = (0, -100, H / 2)
    cam.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = cam
    # Sky as a far gradient card (exact control over the gradient, no world lighting needed).
    world = bpy.data.worlds.new(PREFIX + "world")
    created["worlds"].append(world)
    sc.world = world
    sky_low = material("sky_low", sky_bottom, sky_mid, 0, H * 0.55)
    poly("sky_low", [(-W, -1), (W, -1), (W, H * 0.55), (-W, H * 0.55)], 300, sky_low, sc)
    sky_high = material("sky_high", sky_mid, sky_top, H * 0.55, H)
    poly("sky_high", [(-W, H * 0.55), (W, H * 0.55), (W, H + 1), (-W, H + 1)], 300, sky_high, sc)
    return sc


def mist_band(sc, name, z0, z1, depth, color, a_bottom=0.85, a_top=0.0):
    m = material(name, color, color, z0, z1, a_bottom, a_top)
    poly(name, [(-W, z0 - 3), (W, z0 - 3), (W, z1), (-W, z1)], depth, m, sc)


def cloud(sc, name, cx, cz, scale, depth, col, alpha=0.95, seed=0):
    rnd = random.Random(seed)
    lobes = [(0, 0, 1.0), (-0.9, -0.15, 0.7), (0.95, -0.12, 0.75), (-0.45, 0.35, 0.65), (0.4, 0.4, 0.7), (1.6, -0.3, 0.45), (-1.6, -0.3, 0.45)]
    shade = mix(col, hexc('#b9a6ee'), 0.35)
    for i, (dx, dz, r) in enumerate(lobes):
        r *= scale * rnd.uniform(0.9, 1.1)
        m = material(f"{name}_m{i}", shade, col, cz + dz * scale - r * 0.6, cz + dz * scale + r * 0.6, alpha, alpha)
        pts = circle_pts(cx + dx * scale, cz + dz * scale, r, r * 0.8, 36)
        poly(f"{name}_{i}", pts, depth - i * 0.01, m, sc)


# ---------------------------------------------------------------- North
def build_north():
    sc = setup_scene("north", hexc('#d9c8f5'), hexc('#a99be8'), hexc('#4b3f9e'))
    rnd = random.Random(7)
    # Stars
    star = material("star", hexc('#ffffff'), hexc('#ffffff'), 0, 1, 0.9, 0.9)
    for i in range(70):
        x, z = rnd.uniform(-W / 2, W / 2), rnd.uniform(H * 0.66, H)
        r = rnd.uniform(0.008, 0.022)
        poly(f"star{i}", circle_pts(x, z, r, r, 8), 250, star, sc)
    # Aurora ribbons: wavy vertical curtains, green at the bottom fading to pink/transparent at the top.
    for k, (zb, amp, ph, c1, c2, a) in enumerate([
            (6.95, 0.22, 0.0, hexc('#8ff7d2'), hexc('#9fd8ff'), 0.6),
            (7.5, 0.18, 1.7, hexc('#b4fbe3'), hexc('#d6b4ff'), 0.42)]):
        n = 90
        bottom = []
        top = []
        for i in range(n + 1):
            x = -W / 2 - 0.3 + (W + 0.6) * i / n
            zc = zb + 0.22 * x + amp * math.sin(x * 1.3 + ph) + 0.08 * math.sin(x * 3.7 + ph * 2)
            bottom.append((x, zc))
            top.append((x, zc + 1.1 + 0.25 * math.sin(x * 2.1 + ph)))
        m = material(f"aurora{k}", c1, c2, zb - 1.0, zb + 0.6, a, 0.0)
        poly(f"aurora{k}", bottom + top[::-1], 240 - k, m, sc)
    # Moon
    moon = material("moon", hexc('#fff6ff'), hexc('#ffffff'), 0, 1)
    poly("moon", circle_pts(1.25, 8.75, 0.32, 0.32, 48), 245, moon, sc)
    for g in range(14):
        r = 0.34 + g * 0.045
        glow = material(f"moonglow{g}", hexc('#fff4ff'), hexc('#fff4ff'), 0, 1, 0.035, 0.035)
        poly(f"moonglow{g}", circle_pts(1.25, 8.75, r, r, 48), 246 + g * 0.1, glow, sc)
    # Mountain layers far -> near
    fog = hexc('#e6dcfa')
    layers = [
        (11, 4.9, [(-1.9, 1.2, 1.0), (-0.9, 0.7, 0.6), (0.35, 1.9, 1.3), (1.0, 0.9, 0.5), (2.1, 1.3, 0.9)], 0.09, '#b4a7e6', '#9e90dc', 200, 0.55),
        (12, 4.25, [(-1.2, 1.5, 1.0), (-0.4, 0.6, 0.4), (1.3, 1.1, 0.9), (-2.4, 0.8, 0.6), (2.3, 0.6, 0.5)], 0.1, '#9b8bd8', '#8573c9', 160, 0.45),
        (13, 3.45, [(0.6, 1.0, 0.9), (-1.9, 0.9, 0.8), (2.2, 0.8, 0.6), (-0.6, 0.5, 0.5)], 0.11, '#8070c4', '#6b5ab3', 120, 0.35),
    ]
    for i, (seed, base, peaks, rough, c_top, c_rock, depth, mistv) in enumerate(layers):
        top_z = base + max(p[1] for p in peaks)
        m = snow_material(f"mtn{i}", mix(hexc(c_rock), fog, 0.65 - i * 0.1), hexc(c_top), hexc('#f7f3ff'),
                          base - 1.2, base + 0.35 * (top_z - base), top_z)
        poly(f"mtn{i}", ridge(seed, base, peaks, rough), depth, m, sc)
        mist_band(sc, f"mist{i}", base - 0.6, base + 0.5, depth - 5, fog, 0.9, 0.0)
    # Snowy hills with pines
    hill_specs = [(21, 2.3, '#cfc6f2', '#efeaff', 90), (22, 1.45, '#bfb3ee', '#f4f0ff', 60), (23, 0.6, '#b2a4ea', '#faf8ff', 30)]
    pine_dark = ['#5a4aa6', '#4a3a96', '#3c2d85']
    for i, (seed, base, c_low, c_top, depth) in enumerate(hill_specs):
        rr = random.Random(seed)
        peaks = [(rr.uniform(-2.2, 2.2), rr.uniform(0.25, 0.6), rr.uniform(0.8, 1.6)) for _ in range(3)]
        pts = ridge(seed, base, peaks, 0.03)
        m = material(f"hill{i}", hexc(c_low), hexc(c_top), base - 0.8, base + 0.6)
        poly(f"hill{i}", pts, depth, m, sc)
        # pines standing on the ridge, mostly to the sides (keep the centre clear for the island)
        count = [9, 8, 6][i]
        scale = [0.33, 0.5, 0.78][i]
        for t in range(count):
            side = -1 if t % 2 == 0 else 1
            x = side * rr.uniform(0.75, W / 2 + 0.1) if i > 0 else rr.uniform(-W / 2, W / 2)
            # height of ridge at x
            zx = min(pts[1:-1], key=lambda p: abs(p[0] - x))[1] - 0.05
            s = scale * rr.uniform(0.8, 1.2)
            dark = hexc(pine_dark[i])
            mp = material(f"pine{i}_{t}", mix(dark, fog, 0.25 - i * 0.08), mix(dark, hexc('#8d7fd6'), 0.35), zx, zx + 1.6 * s)
            ms = material(f"pinesnow{i}_{t}", hexc('#f3efff'), hexc('#ffffff'), 0, 1)
            trunk = [(x - 0.035 * s, zx - 0.05), (x + 0.035 * s, zx - 0.05), (x + 0.035 * s, zx + 0.3 * s), (x - 0.035 * s, zx + 0.3 * s)]
            poly(f"trunk{i}_{t}", trunk, depth - 1 - t * 0.02, mp, sc)
            for k in range(4):
                w = (0.42 - k * 0.08) * s
                zb = zx + (0.14 + k * 0.33) * s
                zt = zb + 0.55 * s
                tri = [(x - w, zb), (x + w, zb), (x, zt)]
                poly(f"pine{i}_{t}_{k}", tri, depth - 1.1 - t * 0.02 - k * 0.001, mp, sc)
                cap = [(x - w * 0.35, zt - 0.2 * s), (x + w * 0.35, zt - 0.2 * s), (x, zt)]
                poly(f"pinecap{i}_{t}_{k}", cap, depth - 1.2 - t * 0.02 - k * 0.001, ms, sc)
        mist_band(sc, f"hillmist{i}", base - 0.7, base + 0.25, depth - 3, fog, 0.65, 0.0)
    # Snowflakes
    flake = material("flake", hexc('#ffffff'), hexc('#ffffff'), 0, 1, 0.8, 0.8)
    for i in range(55):
        x, z = rnd.uniform(-W / 2, W / 2), rnd.uniform(0.2, H * 0.95)
        r = rnd.uniform(0.012, 0.03)
        poly(f"flake{i}", circle_pts(x, z, r, r, 8), 5, flake, sc)
    return sc


# ---------------------------------------------------------------- Europe
def build_europe():
    sc = setup_scene("europe", hexc('#ffd9c9'), hexc('#e9b9e8'), hexc('#a99be8'))
    rnd = random.Random(11)
    fog = hexc('#f6e3f2')
    # Sun + glow
    # Warm sunset glow over the horizon
    glow = material("horizon", hexc('#ffcfb8'), hexc('#ffcfb8'), 4.9, 7.6, 0.95, 0.0)
    poly("horizon", [(-W, 4.0), (W, 4.0), (W, 7.6), (-W, 7.6)], 260, glow, sc)
    for g in range(16):
        r = 0.5 + g * 0.07
        poly(f"sunglow{g}", circle_pts(-0.9, 5.3, r, r, 64), 250 + g * 0.1,
             material(f"sunglow{g}", hexc('#fff0d0'), hexc('#fff0d0'), 0, 1, 0.05, 0.05), sc)
    poly("sun", circle_pts(-0.9, 5.3, 0.5, 0.5, 64), 249, material("sun", hexc('#fff4d8'), hexc('#fffaf0'), 4.8, 5.8), sc)
    # Clouds
    cloud(sc, "c1", -1.3, 8.0, 0.55, 230, hexc('#fff4fb'), 0.95, 1)
    cloud(sc, "c2", 1.5, 7.2, 0.45, 229, hexc('#fff0f8'), 0.92, 2)
    cloud(sc, "c3", 0.2, 9.1, 0.32, 228, hexc('#ffffff'), 0.85, 3)
    cloud(sc, "c4", 2.2, 9.4, 0.26, 227, hexc('#ffffff'), 0.8, 4)
    # Distant mountains
    for i, (seed, base, peaks, c, depth) in enumerate([
            (31, 4.95, [(-1.8, 0.9, 1.2), (0.6, 0.7, 1.3), (2.1, 1.0, 0.9)], '#c7a9e2', 200),
            (32, 4.75, [(-0.6, 0.6, 1.0), (1.4, 0.5, 0.9)], '#b394d8', 180)]):
        m = material(f"emtn{i}", mix(hexc(c), fog, 0.55), hexc(c), base - 0.3, base + 1.0)
        poly(f"emtn{i}", ridge(seed, base, peaks, 0.04), depth, m, sc)
    # Sea with a horizon glow and sun glints
    sea = material("sea", hexc('#8a7ad6'), hexc('#f4c6d6'), 2.4, 4.95)
    poly("sea", [(-W, -1), (W, -1), (W, 4.95), (-W, 4.95)], 150, sea, sc)
    glint = material("glint", hexc('#fff6e6'), hexc('#fff6e6'), 0, 1, 0.75, 0.75)
    for i in range(46):
        z = rnd.uniform(3.2, 4.85)
        x = -0.9 + rnd.gauss(0, 0.25 + (4.9 - z) * 0.22)
        w = rnd.uniform(0.05, 0.18) * (1.0 - (z - 3.2) / 2.4 * 0.6)
        poly(f"glint{i}", [(x - w, z), (x + w, z), (x + w, z + 0.012), (x - w, z + 0.012)], 149, glint, sc)
    # Island cliff on the right with a white village (Greek style) hugging the slope
    def cliff_top(x):
        return 2.95 + 0.85 * (x + 0.1) - 0.12 * math.sin(x * 4.0)
    xs = [-1.4 + 4.1 * i / 80 for i in range(81)]
    cliff_pts = [(-1.4, -1.0)] + [(x, max(2.2, cliff_top(x))) for x in xs] + [(2.7, -1.0)]
    poly("cliff", cliff_pts, 120, material("cliff", hexc('#8e64bd'), hexc('#c08ed6'), 2.6, 5.2, noise=0.15), sc)
    white = material("house", hexc('#efe4fb'), hexc('#ffffff'), 3.0, 5.4)
    shade = material("houseshade", hexc('#d9c8f2'), hexc('#e6d9f7'), 3.0, 5.4)
    blue = material("dome", hexc('#4e64d0'), hexc('#7a90ec'), 3.4, 5.6)
    win = material("win", hexc('#6f5cc0'), hexc('#6f5cc0'), 0, 1)
    vr = random.Random(5)
    for i in range(18):
        x = 0.35 + 2.3 * i / 17 + vr.uniform(-0.05, 0.05)
        top = cliff_top(x)
        z = top - vr.uniform(0.05, 0.45)
        w = vr.uniform(0.15, 0.24)
        h = vr.uniform(0.13, 0.2)
        d = 119 - z * 0.1
        poly(f"house{i}", [(x - w / 2, z - 0.18), (x + w / 2, z - 0.18), (x + w / 2, z + h), (x - w / 2, z + h)], d, white, sc)
        poly(f"hshade{i}", [(x + w * 0.25, z - 0.18), (x + w / 2, z - 0.18), (x + w / 2, z + h), (x + w * 0.25, z + h)], d - 0.001, shade, sc)
        poly(f"win{i}", [(x - 0.03, z - 0.02), (x + 0.02, z - 0.02), (x + 0.02, z + 0.06), (x - 0.03, z + 0.06)], d - 0.002, win, sc)
        if i % 4 == 1:
            poly(f"dome{i}", circle_pts(x, z + h, w * 0.4, w * 0.42, 24, 0, math.pi), d - 0.003, blue, sc)
    mist_band(sc, "seamist", 2.6, 3.6, 110, fog, 0.55, 0.0)
    # Foreground hills with cypresses and lavender
    for i, (seed, base, c_low, c_top, depth) in enumerate([(41, 2.05, '#a77fcf', '#cfa6e6', 80), (42, 1.0, '#8f6bc4', '#b98fdc', 50), (43, 0.25, '#7a59b5', '#a47ad0', 20)]):
        rr = random.Random(seed)
        peaks = [(rr.choice([-1, 1]) * rr.uniform(1.1, 2.3), rr.uniform(0.35, 0.8), rr.uniform(0.9, 1.5)) for _ in range(2)]
        pts = ridge(seed, base, peaks, 0.025)
        poly(f"ehill{i}", pts, depth, material(f"ehill{i}", hexc(c_low), hexc(c_top), base - 0.8, base + 0.7), sc)
        count = [0, 3, 3][i]
        s = [0.45, 0.62, 0.9][i]
        for t in range(count):
            side = -1 if t % 2 == 0 else 1
            x = side * rr.uniform(0.95, W / 2 + 0.05)
            zx = min(pts[1:-1], key=lambda p: abs(p[0] - x))[1] - 0.05
            hh = s * rr.uniform(1.1, 1.5)
            dark = hexc(['#5d4a9e', '#4d3b8f', '#3f2d80'][i])
            mc = material(f"cyp{i}_{t}", dark, mix(dark, hexc('#9b86da'), 0.4), zx, zx + hh)
            pts_c = [(x + 0.13 * s * math.sin(a) * (1 - (math.cos(a) * 0.5 + 0.5) ** 3 * 0.0), zx + hh * (0.5 - 0.5 * math.cos(a))) for a in [k * math.pi / 30 for k in range(31)]]
            outline = [(x + 0.2 * s * math.sin(math.pi * (k / 30) ** 0.8) ** 0.6, zx + hh * k / 30) for k in range(31)]
            outline = outline + [(2 * x - px, pz) for (px, pz) in outline[::-1]]
            poly(f"cyp{i}_{t}", outline, depth - 1 - t * 0.02, mc, sc)
        # lavender dots along the near hills
        if i > 0:
            lav = material(f"lav{i}", hexc('#c48ef0'), hexc('#e3b8ff'), 0, 1, 0.9, 0.9)
            for t in range(140 if i == 2 else 80):
                x = rr.uniform(-W / 2, W / 2)
                if abs(x) < 0.7:
                    continue
                zx = min(pts[1:-1], key=lambda p: abs(p[0] - x))[1] - rr.uniform(0.05, 0.6)
                r = rr.uniform(0.012, 0.022) * (1.4 if i == 2 else 1.0)
                for q in range(3):
                    poly(f"lav{i}_{t}_{q}", circle_pts(x, zx + q * r * 1.6, r * (1 - q * 0.2), r * 1.2 * (1 - q * 0.2), 8), depth - 2 - t * 0.001 - q * 0.0001, lav, sc)
        mist_band(sc, f"emist{i}", base - 0.7, base + 0.3, depth - 3, fog, 0.5, 0.0)
    # Petals / sparkles in the air
    sp = material("spark", hexc('#fff3fb'), hexc('#fff3fb'), 0, 1, 0.75, 0.75)
    for i in range(40):
        x, z = rnd.uniform(-W / 2, W / 2), rnd.uniform(0.3, H * 0.9)
        r = rnd.uniform(0.012, 0.028)
        poly(f"spark{i}", circle_pts(x, z, r, r, 8), 5, sp, sc)
    return sc


def render(sc, filename):
    os.makedirs(OUT_DIR, exist_ok=True)
    sc.render.filepath = os.path.join(OUT_DIR, filename)
    sc.render.image_settings.file_format = 'PNG'
    with bpy.context.temp_override(scene=sc):
        bpy.ops.render.render(write_still=True, scene=sc.name)
    return sc.render.filepath


def soften(path, passes=2):
    """Light separable box blur so the flat shapes read softer, like the painted Asia background."""
    import numpy as np
    img = bpy.data.images.load(path, check_existing=False)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    for _ in range(passes):
        px = (np.roll(px, 1, 0) + px + np.roll(px, -1, 0)) / 3.0
        px = (np.roll(px, 1, 1) + px + np.roll(px, -1, 1)) / 3.0
    px[..., 3] = 1.0
    img.pixels[:] = px.ravel()
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


def cleanup(sc):
    for ob in created["objects"]:
        if ob.name in bpy.data.objects:
            bpy.data.objects.remove(ob, do_unlink=True)
    for key, coll in (("meshes", bpy.data.meshes), ("materials", bpy.data.materials), ("cameras", bpy.data.cameras), ("worlds", bpy.data.worlds)):
        for d in created[key]:
            if d.name in coll:
                coll.remove(d)
    bpy.data.scenes.remove(sc)


job = globals().get("JOB", "north")
scene = build_north() if job == "north" else build_europe()
try:
    path = render(scene, f"{job}_menu_bg.png")
    soften(path)
finally:
    cleanup(scene)
result = {"job": job, "path": path}
