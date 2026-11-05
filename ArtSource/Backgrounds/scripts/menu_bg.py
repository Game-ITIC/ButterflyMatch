# Main menu region backgrounds (North / Europe) for JigsawMatch2.
# Flat "layered illustration" render: every shape is an emissive 2D silhouette at its own depth,
# misted towards the sky colour, framed by an orthographic portrait camera (864x1872, like the Asia art).
# Runs inside the live Blender over MCP: prepend a file that sets JOB = 'north' | 'europe' | 'africa' | 'latam'.
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


# ---------------------------------------------------------------- shared warm-region shapes
def palm2d(sc, name, x, z, h, lean, depth, c_trunk, c_leaf, s=1.0, seed=0):
    """Palm silhouette: tapered curved trunk and drooping fronds."""
    rr = random.Random(seed)
    left, right = [], []
    for i in range(13):
        t = i / 12
        cx, cz = x + lean * h * t * t, z + h * t
        w = 0.055 * s * (1 - 0.45 * t)
        left.append((cx - w, cz))
        right.append((cx + w, cz))
    poly(f"{name}_tr", left + right[::-1], depth, material(f"{name}_trm", c_trunk, mix(c_trunk, hexc('#ffffff'), 0.15), z, z + h), sc)
    tx, tz = x + lean * h, z + h
    lm = material(f"{name}_lf", c_leaf, mix(c_leaf, hexc('#ffffff'), 0.2), tz - 0.6 * s, tz + 0.4 * s)
    for k, deg in enumerate((-25, 10, 45, 80, 115, 150, 185, 205)):
        a = math.radians(deg + rr.uniform(-8, 8))
        L = 0.78 * s * rr.uniform(0.85, 1.12)
        droop = 0.55 * L
        side_l, side_r = [], []
        for i in range(11):
            t = i / 10
            px = tx + math.cos(a) * L * t
            pz = tz + math.sin(a) * L * t - droop * t * t
            dx, dz = math.cos(a), math.sin(a) - 2 * droop * t / L
            nl = math.hypot(dx, dz)
            nx, nz = -dz / nl, dx / nl
            w = 0.1 * s * math.sin(math.pi * min(t * 1.05, 1.0)) ** 0.7
            side_l.append((px + nx * w, pz + nz * w))
            side_r.append((px - nx * w, pz - nz * w))
        poly(f"{name}_f{k}", side_l + side_r[::-1], depth - 0.01 - k * 0.001, lm, sc)


def birds(sc, name, spots, depth, col):
    m = material(name, col, col, 0, 1, 0.85, 0.85)
    for i, (x, z, s) in enumerate(spots):
        for sgn in (-1, 1):
            poly(f"{name}{i}_{sgn}", [(x, z), (x + sgn * 0.11 * s, z + 0.05 * s), (x + sgn * 0.13 * s, z + 0.035 * s), (x + sgn * 0.02 * s, z - 0.012 * s)],
                 depth, m, sc)


def sun_disc(sc, x, z, r, depth, col='#fff2d6', glow='#fff0d0'):
    for g in range(16):
        rg = r + g * 0.07
        poly(f"sunglow{g}", circle_pts(x, z, rg, rg, 64), depth + 1 + g * 0.1,
             material(f"sunglow{g}", hexc(glow), hexc(glow), 0, 1, 0.05, 0.05), sc)
    poly("sun", circle_pts(x, z, r, r, 64), depth, material("sun", hexc(col), hexc('#fffaf0'), z - r, z + r), sc)


def sparkles(sc, rnd, n, depth, z0=0.3, z1=H * 0.9):
    sp = material("spark", hexc('#fff6ee'), hexc('#fff6ee'), 0, 1, 0.75, 0.75)
    for i in range(n):
        x, z = rnd.uniform(-W / 2, W / 2), rnd.uniform(z0, z1)
        r = rnd.uniform(0.012, 0.028)
        poly(f"spark{i}", circle_pts(x, z, r, r, 8), depth, sp, sc)


def ridge_z(pts, x):
    return min(pts[1:-1], key=lambda p: abs(p[0] - x))[1]


# ---------------------------------------------------------------- Africa
def build_africa():
    sc = setup_scene("africa", hexc('#ffd3b4'), hexc('#f2b6cc'), hexc('#a99be8'))
    rnd = random.Random(23)
    fog = hexc('#fbe2e4')
    glow = material("horizon", hexc('#ffd0ae'), hexc('#ffd0ae'), 4.6, 7.4, 0.95, 0.0)
    poly("horizon", [(-W, 4.0), (W, 4.0), (W, 7.4), (-W, 7.4)], 260, glow, sc)
    sun_disc(sc, 0.3, 5.35, 0.58, 249)
    cloud(sc, "c1", -1.4, 8.3, 0.5, 230, hexc('#fff3f4'), 0.95, 1)
    cloud(sc, "c2", 1.7, 7.4, 0.36, 229, hexc('#fff0ee'), 0.9, 2)
    cloud(sc, "c3", 0.3, 9.25, 0.28, 228, hexc('#ffffff'), 0.85, 3)
    birds(sc, "bird", [(-0.6, 6.9, 1.0), (-0.25, 7.15, 0.8), (-0.95, 7.2, 0.7), (1.6, 8.6, 0.6)], 225, hexc('#8a6fb8'))
    # far: Table Mountain mesa on the right, low dunes across
    mesa = [(1.1, 4.5), (1.35, 4.85), (1.6, 5.12), (2.55, 5.15), (2.75, 4.95), (W / 2 + 0.4, 4.75), (W / 2 + 0.4, 4.5)]
    poly("mesa", mesa, 205, material("mesa", mix(hexc('#b393d2'), fog, 0.35), hexc('#bf9fd8'), 4.5, 5.2), sc)
    poly("dune0", ridge(51, 4.55, [(-1.5, 0.25, 1.6), (0.4, 0.18, 1.3), (2.0, 0.2, 1.2)], 0.008),
         200, material("dune0", mix(hexc('#e7b0c4'), fog, 0.4), hexc('#efbfc9'), 4.2, 4.95), sc)
    # pyramids of Giza on the left: lit face and shade face
    for i, (px, hw, ph) in enumerate(((-1.25, 0.78, 1.05), (-0.25, 0.52, 0.7), (-2.05, 0.36, 0.45))):
        zb = 4.45
        lit = material(f"pyr{i}", hexc('#eba98e'), hexc('#f7c7a0'), zb, zb + ph)
        shd = material(f"pyrs{i}", hexc('#c486a6'), hexc('#d69bb0'), zb, zb + ph)
        poly(f"pyr{i}", [(px - hw, zb), (px + hw * 0.15, zb), (px, zb + ph)], 190 - i, lit, sc)
        poly(f"pyrs{i}", [(px + hw * 0.15, zb), (px + hw, zb), (px, zb + ph)], 190 - i - 0.001, shd, sc)
    mist_band(sc, "farmist", 3.9, 4.75, 185, fog, 0.75, 0.0)
    # mid dunes, near dunes with palms on the sides (centre stays clear for the island)
    specs = [(52, 3.3, '#e4a08f', '#f5c3a7', 120, 0, 0.0), (53, 2.0, '#d68f93', '#eeb19f', 80, 2, 0.62), (54, 0.65, '#c47c93', '#e39e9c', 40, 2, 0.95)]
    for i, (seed, base, c_low, c_top, depth, n_palms, ps) in enumerate(specs):
        rr = random.Random(seed)
        peaks = [(rr.choice([-1, 1]) * rr.uniform(0.9, 2.2), rr.uniform(0.3, 0.6), rr.uniform(1.0, 1.7)) for _ in range(2)]
        pts = ridge(seed, base, peaks, 0.012)
        poly(f"dune{i + 1}", pts, depth, material(f"dune{i + 1}", hexc(c_low), hexc(c_top), base - 0.8, base + 0.6), sc)
        for t in range(n_palms):
            side = -1 if t % 2 == 0 else 1
            x = side * rr.uniform(1.25, W / 2 - 0.1)
            dark = hexc(['#6d4f9a', '#5b4090', '#4a3182'][i])
            palm2d(sc, f"palm{i}_{t}", x, ridge_z(pts, x) - 0.1, rr.uniform(1.3, 1.7) * ps, -side * 0.12, depth - 1 - t * 0.05,
                   dark, mix(dark, hexc('#86c9a0'), 0.55), s=ps * 1.35, seed=seed + t)
        if i == 0:   # an obelisk on the mid dune
            x = 1.55
            z = ridge_z(pts, x) - 0.03
            poly("obelisk", [(x - 0.05, z), (x + 0.05, z), (x + 0.035, z + 0.55), (x, z + 0.62), (x - 0.035, z + 0.55)], depth - 1,
                 material("obelisk", hexc('#c98d9a'), hexc('#efb8a6'), z, z + 0.6), sc)
        mist_band(sc, f"dmist{i}", base - 0.7, base + 0.3, depth - 3, fog, 0.45, 0.0)
    sparkles(sc, rnd, 36, 5)
    return sc


# ---------------------------------------------------------------- Latin America
def build_latam():
    sc = setup_scene("latam", hexc('#ffd7c8'), hexc('#efb4d6'), hexc('#9d8fe6'))
    rnd = random.Random(29)
    fog = hexc('#f4e2f2')
    glow = material("horizon", hexc('#ffcfc0'), hexc('#ffcfc0'), 4.8, 7.6, 0.9, 0.0)
    poly("horizon", [(-W, 4.0), (W, 4.0), (W, 7.6), (-W, 7.6)], 260, glow, sc)
    sun_disc(sc, -0.85, 5.85, 0.48, 249)
    cloud(sc, "c1", 1.4, 8.4, 0.5, 230, hexc('#fff4fb'), 0.95, 5)
    cloud(sc, "c2", -1.6, 7.7, 0.38, 229, hexc('#fff0f8'), 0.9, 6)
    cloud(sc, "c3", 0.1, 9.3, 0.26, 228, hexc('#ffffff'), 0.85, 7)
    birds(sc, "bird", [(0.5, 7.0, 0.9), (0.85, 7.25, 0.75), (0.2, 7.3, 0.65)], 225, hexc('#7f68b4'))
    # far Andes with a light pink snow line, and the Huayna Picchu spike on the left
    peaks = [(-2.0, 1.0, 0.8), (-0.6, 1.3, 1.0), (0.7, 0.9, 0.7), (1.9, 1.2, 0.9)]
    top_z = 4.85 + 1.3
    m = snow_material("andes", mix(hexc('#a08ad6'), fog, 0.55), hexc('#b9a3e2'), hexc('#fbeaf6'), 4.4, 4.85 + 0.55 * 1.3, top_z)
    poly("andes", ridge(61, 4.85, peaks, 0.07), 205, m, sc)
    hp = []
    for i in range(41):
        t = i / 40
        x = -2.0 + 1.3 * t
        bell = math.sin(math.pi * t) ** 1.6
        hp.append((x, 4.3 + 1.3 * bell + 0.04 * math.sin(t * 37)))
    poly("huayna", [(-2.0, 4.0)] + hp + [(-0.7, 4.0)], 195, material("huayna", mix(hexc('#8a93cc'), fog, 0.4), hexc('#a2acd8'), 4.2, 5.6), sc)
    mist_band(sc, "farmist", 4.0, 4.95, 190, fog, 0.8, 0.0)
    # bay with Sugarloaf + Urca and a cable line on the right
    sea = material("sea", hexc('#8c86d8'), hexc('#f2c4da'), 2.8, 4.45)
    poly("sea", [(-W, -1), (W, -1), (W, 4.45), (-W, 4.45)], 150, sea, sc)
    glint = material("glint", hexc('#fff6ea'), hexc('#fff6ea'), 0, 1, 0.7, 0.7)
    for i in range(30):
        z = rnd.uniform(3.3, 4.38)
        x = rnd.uniform(-0.2, 2.2)
        w = rnd.uniform(0.04, 0.14)
        poly(f"glint{i}", [(x - w, z), (x + w, z), (x + w, z + 0.012), (x - w, z + 0.012)], 149, glint, sc)

    def dome(cx, zb, rx, h, n=40):
        return [(cx + rx * math.cos(math.pi * i / n) * (1.0 - 0.1 * math.sin(math.pi * i / n)),
                 zb + h * math.sin(math.pi * i / n) ** 0.55) for i in range(n + 1)][::-1]
    sl = dome(1.85, 4.1, 0.3, 0.95)
    poly("sugarloaf", [(1.52, 3.95)] + sl + [(2.18, 3.95)], 140, material("sugar", hexc('#8a7cc8'), hexc('#a99ad8'), 4.0, 5.05), sc)
    ur = dome(1.3, 4.1, 0.26, 0.42)
    poly("urca", [(1.0, 3.95)] + ur + [(1.6, 3.95)], 139, material("urca", hexc('#7f86c6'), hexc('#9ba2d8'), 4.0, 4.55), sc)
    cab = material("cable", hexc('#5f4f9a'), hexc('#5f4f9a'), 0, 1, 0.9, 0.9)
    a0, a1 = (1.3, 4.52), (1.85, 5.05)
    line = [(a0[0] + (a1[0] - a0[0]) * t, a0[1] + (a1[1] - a0[1]) * t - 0.06 * math.sin(math.pi * t)) for t in [k / 20 for k in range(21)]]
    poly("cableline", line + [(x, z + 0.007) for (x, z) in line[::-1]], 138, cab, sc)
    cxm, czm = line[9]
    poly("cabin", [(cxm - 0.035, czm - 0.07), (cxm + 0.035, czm - 0.07), (cxm + 0.035, czm - 0.01), (cxm - 0.035, czm - 0.01)], 137.9,
         material("cabin", hexc('#ffd77a'), hexc('#ffe7a8'), 0, 1), sc)
    mist_band(sc, "seamist", 2.9, 3.95, 130, fog, 0.5, 0.0)
    # jungle hills: a small stepped pyramid on the left, palms on the sides
    specs = [(62, 3.0, '#7586c6', '#9cb7da', 100, 0, 0.0), (63, 1.75, '#6578bd', '#8eaed6', 70, 2, 0.6), (64, 0.55, '#5466b0', '#7d9fcf', 35, 2, 0.95)]
    for i, (seed, base, c_low, c_top, depth, n_palms, ps) in enumerate(specs):
        rr = random.Random(seed)
        pk = [(rr.choice([-1, 1]) * rr.uniform(1.0, 2.2), rr.uniform(0.35, 0.7), rr.uniform(0.9, 1.5)) for _ in range(2)]
        pts = ridge(seed, base, pk, 0.03)
        poly(f"jhill{i}", pts, depth, material(f"jhill{i}", hexc(c_low), hexc(c_top), base - 0.8, base + 0.7), sc)
        if i == 0:
            x = -1.55
            z = ridge_z(pts, x) - 0.05
            pm = material("mpyr", hexc('#9c86c6'), hexc('#bfa8da'), z, z + 0.6)
            for k in range(5):
                w = 0.42 - k * 0.07
                poly(f"mpyr{k}", [(x - w, z + k * 0.11), (x + w, z + k * 0.11), (x + w, z + (k + 1) * 0.11), (x - w, z + (k + 1) * 0.11)], depth - 4 - k * 0.001, pm, sc)
            poly("mtemple", [(x - 0.1, z + 0.55), (x + 0.1, z + 0.55), (x + 0.1, z + 0.7), (x - 0.1, z + 0.7)], depth - 4.01, pm, sc)
            poly("mbase", [(x - 0.5, z - 0.3), (x + 0.5, z - 0.3), (x + 0.45, z + 0.02), (x - 0.45, z + 0.02)], depth - 3.99,
                 material("mbase", hexc(c_low), hexc(c_top), z - 0.3, z + 0.1), sc)
        for t in range(n_palms):
            side = -1 if t % 2 == 0 else 1
            x = side * rr.uniform(1.2, W / 2 - 0.1)
            dark = hexc(['#4f5ea6', '#435098', '#37428a'][i])
            palm2d(sc, f"palm{i}_{t}", x, ridge_z(pts, x) - 0.1, rr.uniform(1.3, 1.7) * ps, -side * 0.12, depth - 1 - t * 0.05,
                   dark, mix(dark, hexc('#7cc6a0'), 0.5), s=ps * 1.35, seed=seed + t)
        mist_band(sc, f"jmist{i}", base - 0.7, base + 0.3, depth - 3, fog, 0.45, 0.0)
    sparkles(sc, rnd, 36, 5)
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
scene = {"north": build_north, "europe": build_europe, "africa": build_africa, "latam": build_latam}[job]()
try:
    path = render(scene, f"{job}_menu_bg.png")
    soften(path)
finally:
    cleanup(scene)
result = {"job": job, "path": path}
