# Europe region kit (France / Greece / Italy islands). Concatenated after
# ArtSource/North/scripts/nordic_lib.py + nordic_pipe.py: reuses B, M, mat, fbm, rock, bake/export helpers.
# Convention: island top at z=0, front corner of the island faces the camera at (+X, -Y);
# facades face -Y (front) and +X (right side).


# ---------------------------------------------------------------- materials
def emat_pattern(name, c1, c2, kind='stripes', scale=4.0, axis='X', rough=0.8):
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    nt.links.new(bsdf.outputs[0], out.inputs['Surface'])
    tc = nt.nodes.new('ShaderNodeTexCoord')
    if kind == 'checker':
        ck = nt.nodes.new('ShaderNodeTexChecker')
        ck.inputs['Scale'].default_value = scale
        ck.inputs['Color1'].default_value = (*s2l(hexc(c1)), 1)
        ck.inputs['Color2'].default_value = (*s2l(hexc(c2)), 1)
        nt.links.new(tc.outputs['Object'], ck.inputs['Vector'])
        col = ck.outputs['Color']
    else:
        wv = nt.nodes.new('ShaderNodeTexWave')
        wv.wave_type = 'BANDS'
        wv.bands_direction = axis
        wv.inputs['Scale'].default_value = scale
        wv.inputs['Distortion'].default_value = 0.0
        nt.links.new(tc.outputs['Object'], wv.inputs['Vector'])
        r = nt.nodes.new('ShaderNodeValToRGB')
        r.color_ramp.interpolation = 'CONSTANT'
        e = r.color_ramp.elements
        e[0].position = 0.0
        e[0].color = (*s2l(hexc(c1)), 1)
        e[1].position = 0.5
        e[1].color = (*s2l(hexc(c2)), 1)
        nt.links.new(wv.outputs['Fac'], r.inputs['Fac'])
        col = r.outputs['Color']
    nt.links.new(col, bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = rough
    MATS[name] = m
    return m


def set_mortar(m, hx, size=0.02):
    for n in m.node_tree.nodes:
        if n.type == 'TEX_BRICK':
            n.inputs['Mortar'].default_value = (*s2l(hexc(hx)), 1)
            n.inputs['Mortar Size'].default_value = size
    return m


def emat_cobble(name, c1, c2, grout, scale=2.0, rough=0.85):
    """Cobblestones: Voronoi cells with dark grout seams and per-stone colour variation."""
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    nt.links.new(bsdf.outputs[0], out.inputs['Surface'])
    tc = nt.nodes.new('ShaderNodeTexCoord')
    ve = nt.nodes.new('ShaderNodeTexVoronoi')
    ve.feature = 'DISTANCE_TO_EDGE'
    ve.inputs['Scale'].default_value = scale
    nt.links.new(tc.outputs['Object'], ve.inputs['Vector'])
    vc = nt.nodes.new('ShaderNodeTexVoronoi')
    vc.inputs['Scale'].default_value = scale
    nt.links.new(tc.outputs['Object'], vc.inputs['Vector'])
    sep = nt.nodes.new('ShaderNodeSeparateColor')
    nt.links.new(vc.outputs['Color'], sep.inputs[0])
    stone = _ramp(nt, c1, c2, sep.outputs[0], 0.0, 1.0)
    nz = nt.nodes.new('ShaderNodeTexNoise')
    nz.inputs['Scale'].default_value = scale * 4
    nt.links.new(tc.outputs['Object'], nz.inputs['Vector'])
    stone = _mix(nt, 0.25, stone, _ramp(nt, c1, c2, nz.outputs['Fac'], 0.3, 0.7))
    mr = nt.nodes.new('ShaderNodeMapRange')
    mr.inputs['From Min'].default_value = 0.03
    mr.inputs['From Max'].default_value = 0.09
    nt.links.new(ve.outputs['Distance'], mr.inputs['Value'])
    g = nt.nodes.new('ShaderNodeRGB')
    g.outputs[0].default_value = (*s2l(hexc(grout)), 1)
    col = _mix(nt, mr.outputs['Result'], g.outputs[0], stone)
    nt.links.new(col, bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = rough
    MATS[name] = m
    return m


def euro_palette():
    P = {}
    P['grass'] = mat('e_grass', '#4a9428', '#80c646', scale=1.1)
    P['grass_dark'] = mat('e_grass_dark', '#2f6a1c', '#4f9330', scale=2.2)
    P['dirt'] = mat('e_dirt', '#5a3820', '#9c6a3f', kind='rock', scale=0.8)
    P['root'] = mat('e_root', '#4a2e1a', '#6e4a2c', kind='log', scale=2.0, wave_dir='Z')
    P['rock_white'] = mat('e_rock_white', '#958f84', '#e6e2d8', kind='rock', scale=0.65)
    P['rock_warm'] = mat('e_rock_warm', '#64503d', '#b6936a', kind='rock', scale=0.7)
    P['boulder'] = mat('e_boulder', '#6f6c66', '#a9a69d', kind='rock', scale=1.6)
    P['cobble'] = emat_cobble('e_cobble', '#9a9488', '#d8d1c3', '#77716a', scale=3.2)
    P['cobble_warm'] = emat_cobble('e_cobble_warm', '#b88355', '#e8c18f', '#86603f', scale=2.6)
    P['curb'] = mat('e_curb', '#a39d92', '#d6d0c4', kind='rock', scale=3.0)
    P['gravel_white'] = mat('e_gravel_white', '#c2bcae', '#ebe7dd', kind='rock', scale=5.0)
    P['flagstone'] = mat('e_flagstone', '#6c7580', '#b6bec7', kind='rock', scale=1.5)
    P['limestone'] = set_mortar(mat('e_limestone', '#d2b78c', '#ecd8b2', kind='brick', scale=1.25), '#a58f6a')
    P['limestone_plain'] = mat('e_limestone_plain', '#dcc59c', '#f2e3c4', scale=1.5)
    P['plaster_cream'] = mat('e_plaster_cream', '#e8d3a1', '#f8ebc8', scale=1.4)
    P['plaster_ochre'] = mat('e_plaster_ochre', '#d0954e', '#ebbd7a', scale=1.3)
    P['whitewash'] = mat('e_whitewash', '#dedcd6', '#f6f5f1', scale=1.5)
    P['blue'] = mat('e_blue', '#1650bd', '#2a70e2', kind='flat')
    P['blue_dome'] = mat('e_blue_dome', '#1b5bd0', '#3f88f2', scale=1.0)
    P['zinc'] = mat('e_zinc', '#3567b4', '#6f9ae0', scale=1.6)
    P['zinc_dark'] = mat('e_zinc_dark', '#2a4f8c', '#3d6aae', kind='flat')
    P['tile'] = mat('e_tile', '#a3442a', '#e57d48', kind='wood', scale=2.4, wave_dir='X')
    P['tile_y'] = mat('e_tile_y', '#a3442a', '#e57d48', kind='wood', scale=2.4, wave_dir='Y')
    P['brick_red'] = mat('e_brick_red', '#8f4430', '#c0653e', kind='brick', scale=2.2)
    P['wood'] = mat('e_wood', '#6b4426', '#a3733f', kind='wood', scale=1.2, wave_dir='X')
    P['wood_v'] = mat('e_wood_v', '#6b4426', '#a3733f', kind='wood', scale=1.2, wave_dir='Z')
    P['wood_dark'] = mat('e_wood_dark', '#3a2416', '#5e3c24', kind='wood', scale=1.4, wave_dir='X')
    P['wood_red'] = mat('e_wood_red', '#5e1d14', '#8e3424', kind='wood', scale=1.4, wave_dir='Z')
    P['wicker'] = mat('e_wicker', '#9c6a36', '#d9a866', kind='wood', scale=6.0, wave_dir='Z')
    P['bark'] = mat('e_bark', '#4b3221', '#79573b', kind='log', scale=1.5, wave_dir='Z')
    P['leaves'] = mat('e_leaves', '#2e7a25', '#71c23f', scale=2.0)
    P['leaves_light'] = mat('e_leaves_light', '#4f9a2a', '#a0d65a', scale=2.4)
    P['olive_leaf'] = mat('e_olive_leaf', '#56744a', '#a2b98e', scale=2.6)
    P['cypress'] = mat('e_cypress', '#1f4d1d', '#407c35', scale=2.4)
    P['terracotta'] = mat('e_terracotta', '#ad5a33', '#e3915c', scale=2.0)
    P['iron'] = mat('e_iron', '#1c2023', '#3a4249', scale=4.0)
    P['green_iron'] = mat('e_green_iron', '#12473a', '#22705a', scale=3.0)
    P['eiffel'] = mat('e_eiffel', '#7a4322', '#b36c3a', scale=3.0)
    P['eiffel_dark'] = mat('e_eiffel_dark', '#55301a', '#7d4a2a', scale=3.0)
    P['glass'] = mat('e_glass', '#1c2c46', '#46699a', kind='flat')
    P['glass_lit'] = mat('e_glass_lit', '#f0b45a', '#ffd88a', kind='flat', emission='#ffb84a')
    P['frame_white'] = mat('e_frame_white', '#e7e4dc', '#ffffff', kind='flat')
    P['gold'] = mat('e_gold', '#c8962a', '#f2cc62', scale=4.0)
    P['teal'] = mat('e_teal', '#2a9a8c', '#5dcbb9', scale=3.0)
    P['steel'] = mat('e_steel', '#7d848c', '#c6ccd3', scale=3.0)
    P['rubber'] = mat('e_rubber', '#19191b', '#2c2c30', kind='flat')
    P['bulb'] = mat('e_bulb', '#ffd27a', '#fff0c0', kind='flat', emission='#ffd070')
    P['lamp_glow'] = mat('e_lamp_glow', '#ffe6a8', '#fff6dc', kind='flat', emission='#ffd88a')
    P['flower_pink'] = mat('e_flower_pink', '#b81a78', '#f255ac', scale=7.0)
    P['flower_red'] = mat('e_flower_red', '#bd1b22', '#f04a3a', scale=7.0)
    P['flower_white'] = mat('e_flower_white', '#e6e3ec', '#ffffff', scale=7.0)
    P['flower_yellow'] = mat('e_flower_yellow', '#e0a51c', '#ffd84a', scale=7.0)
    P['cloth_red'] = mat('e_cloth_red', '#b0161c', '#d8272a', kind='flat')
    P['cloth_white'] = mat('e_cloth_white', '#ececec', '#ffffff', kind='flat')
    P['cloth_blue'] = mat('e_cloth_blue', '#0c2f8a', '#1442b0', kind='flat')
    P['checker'] = emat_pattern('e_checker', '#c8202a', '#f6f2ee', 'checker', 9.0)
    P['awning'] = emat_pattern('e_awning', '#c4262a', '#f4efe6', 'stripes', 2.6, 'X')
    P['bread'] = mat('e_bread', '#b0702c', '#e2aa5a', scale=5.0)
    P['sign_green'] = mat('e_sign_green', '#1e5530', '#2e7a46', kind='flat')
    P['cream_paint'] = mat('e_cream_paint', '#efe5cc', '#fbf5e6', kind='flat')
    P['grape'] = mat('e_grape', '#55702a', '#93ad3e', scale=8.0)
    P['stone_step'] = mat('e_stone_step', '#a0978a', '#d2cabb', kind='rock', scale=2.0)
    P['marble'] = mat('e_marble', '#d8d1c3', '#f7f3ea', scale=2.2)
    P['marble_dark'] = mat('e_marble_dark', '#b3aa98', '#d3cbbb', scale=2.2)
    P['chalkboard'] = mat('e_chalkboard', '#1d2220', '#343a3b', kind='flat')
    P['bronze'] = mat('e_bronze', '#7a5a1e', '#c4952e', scale=4.0)
    P['pizza'] = mat('e_pizza', '#c8452a', '#f0b24a', scale=9.0)
    P['water'] = mat('e_water', '#3b98d4', '#8fd3f2', scale=2.0)
    return P


# ---------------------------------------------------------------- shapes
def se_r(a, n=4.0, rot=0.0):
    a2 = a - rot * D2R
    c, s = abs(math.cos(a2)), abs(math.sin(a2))
    return 1.0 / ((c ** n + s ** n) ** (1.0 / n))


def rrect(cx, cy, w, d, r, n=4):
    """Rounded rectangle outline (CCW) in XY."""
    r = min(r, w / 2 - 1e-3, d / 2 - 1e-3)
    pts = []
    for (qx, qy, a0) in ((1, -1, -90), (1, 1, 0), (-1, 1, 90), (-1, -1, 180)):
        ccx, ccy = cx + qx * (w / 2 - r), cy + qy * (d / 2 - r)
        for k in range(n + 1):
            a = (a0 + 90 * k / n) * D2R
            pts.append((ccx + math.cos(a) * r, ccy + math.sin(a) * r))
    return pts


def slab(b, mt, pts, z0, h, m=None, smooth=False):
    """Vertical prism from a CCW XY outline."""
    t = bmesh.new()
    lo = [t.verts.new((x, y, z0)) for (x, y) in pts]
    hi = [t.verts.new((x, y, z0 + h)) for (x, y) in pts]
    n = len(pts)
    t.faces.new(list(reversed(lo)))
    t.faces.new(hi)
    for i in range(n):
        t.faces.new((lo[i], lo[(i + 1) % n], hi[(i + 1) % n], hi[i]))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    bmesh.ops.triangulate(t, faces=[f for f in t.faces if len(f.verts) > 4])
    return b.add_bm(t, mt, smooth, m)


def frustum(b, mt, w0, d0, w1, d1, h, m=None, z0=0.0):
    t = bmesh.new()
    for (w, d, z) in ((w0, d0, z0), (w1, d1, z0 + h)):
        for sx in (-1, 1):
            for sy in (-1, 1):
                t.verts.new((sx * w / 2, sy * d / 2, z))
    bmesh.ops.convex_hull(t, input=t.verts)
    bmesh.ops.dissolve_limit(t, angle_limit=0.01, verts=t.verts, edges=t.edges)
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    return b.add_bm(t, mt, False, m)


def text_mesh(b, mt, s, size, m=None, depth=0.03):
    """Text in the local XZ plane, readable from -Y, centred at the origin."""
    cu = bpy.data.curves.new('tmp_txt', 'FONT')
    cu.body = s
    cu.size = size
    cu.extrude = depth
    cu.align_x = 'CENTER'
    cu.align_y = 'CENTER'
    cu.resolution_u = 2
    ob = bpy.data.objects.new('tmp_txt', cu)
    bpy.context.scene.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    t = bmesh.new()
    t.from_mesh(me)
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    bpy.data.meshes.remove(me)
    bmesh.ops.rotate(t, verts=t.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / 2, 4, 'X'))
    bmesh.ops.triangulate(t, faces=t.faces)
    return b.add_bm(t, mt, False, m)


def ring_tube(b, mt, r, rt, m=None, seg=16, tseg=5):
    pts = [(math.cos(2 * math.pi * k / seg) * r, 0, math.sin(2 * math.pi * k / seg) * r) for k in range(seg + 1)]
    b.push(m or Matrix.Identity(4))
    b.tube(mt, pts, rt, seg=tseg, caps=False)
    b.pop()


def blob_cluster(b, mt, center, r, n=6, seed=0, flat=0.8, spread=0.55, sub=2, nz=0.18):
    rnd = random.Random(seed)
    cx, cy, cz = center
    b.sphere(mt, r, m=M((cx, cy, cz), (0, 0, 0), (1, 1, flat)), sub=sub, nz=r * nz, nf=2.2 / max(r, 0.1), seed=seed)
    for i in range(n):
        a = 2 * math.pi * i / n + rnd.uniform(-0.3, 0.3)
        rr = r * rnd.uniform(0.5, 0.75)
        d = r * spread * rnd.uniform(0.8, 1.15)
        z = cz + r * rnd.uniform(-0.25, 0.35) * flat
        b.sphere(mt, rr, m=M((cx + math.cos(a) * d, cy + math.sin(a) * d, z), (0, 0, 0), (1, 1, flat)), sub=sub,
                 nz=rr * nz, nf=2.2 / max(rr, 0.1), seed=seed + i * 1.7)


# ---------------------------------------------------------------- island base
def euro_body(coll, mats, R=8.5, mt='dirt', seed=0.0, n=4.0, depth=4.6, chunky=0.45, name='Island_Body', seg=96):
    prof = [(1.0, -0.1), (0.995, -0.85), (1.0, -1.6), (0.955, -2.4), (0.86, -3.15), (0.67, -3.8), (0.4, -4.3), (0.0, -4.55)]
    k = depth / 4.55
    t = bmesh.new()
    rings = []
    for i, (f, z) in enumerate(prof):
        z *= k
        if f <= 1e-6:
            rings.append([t.verts.new((0, 0, z))])
            continue
        ring = []
        for j in range(seg):
            a = 2 * math.pi * j / seg
            layer = round(z / 0.8)
            chunk = fbm((math.cos(a) * 2.4, math.sin(a) * 2.4, layer * 0.61 + seed), 2)
            facet = 0.14 * noise.noise((round(a * 14) * 0.37, layer * 1.3, seed))
            off = (-chunky * abs(chunk) + facet) * (0.35 if i == 0 else 1.0)
            rr = R * f * se_r(a, n) + off
            ring.append(t.verts.new((math.cos(a) * rr, math.sin(a) * rr, z)))
        rings.append(ring)
    for i in range(len(rings) - 1):
        A, Bv = rings[i], rings[i + 1]
        if len(Bv) == 1:
            for j in range(seg):
                t.faces.new((A[j], Bv[0], A[(j + 1) % seg]))
        else:
            for j in range(seg):
                t.faces.new((A[j], A[(j + 1) % seg], Bv[(j + 1) % seg], Bv[j]))
    t.faces.new(list(reversed(rings[0])))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    b = B(name)
    b.add_bm(t, mats[mt], smooth=False)
    return b


def euro_top(b, mats, R=8.5, mt='grass', n=4.0, seed=0.0, drip=0.25, lip=0.45, seg=112):
    """Top surface at z=0 with a rounded lip that hangs over the cliff."""
    t = bmesh.new()
    prof = [(k / 7, 0.0) for k in range(1, 7)] + [(0.985, 0.0), (1.012, -0.05), (1.03, -0.2), (1.025, -0.36),
                                                  (1.0, -0.46), (0.975, -0.4)]
    center = t.verts.new((0, 0, 0.0))
    rings = []
    for i, (f, z) in enumerate(prof):
        ring = []
        for j in range(seg):
            a = 2 * math.pi * j / seg
            rr = R * f * se_r(a, n)
            zz = z * lip / 0.46
            if i >= 9:
                d = max(0.0, fbm((math.cos(a) * 3.1, math.sin(a) * 3.1, seed), 3)) * drip
                zz -= d
            ring.append(t.verts.new((math.cos(a) * rr, math.sin(a) * rr, zz)))
        rings.append(ring)
    for j in range(seg):
        t.faces.new((center, rings[0][j], rings[0][(j + 1) % seg]))
    for i in range(len(rings) - 1):
        for j in range(seg):
            t.faces.new((rings[i][j], rings[i + 1][j], rings[i + 1][(j + 1) % seg], rings[i][(j + 1) % seg]))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    b.add_bm(t, mats[mt], smooth=True)
    return b


def cliff_roots(b, mats, R, n=4.0, count=12, seed=3, zmin=-1.2, zmax=-3.6):
    rnd = random.Random(seed)
    for i in range(count):
        a = rnd.uniform(0, 2 * math.pi)
        z0 = rnd.uniform(zmin, zmin - 0.8)
        f = 0.99 if z0 > -1.7 else 0.95
        r0 = R * f * se_r(a, n) - 0.05
        pts = []
        L = rnd.uniform(1.2, 2.4)
        for k in range(6):
            t_ = k / 5
            rr = r0 + 0.12 * math.sin(t_ * 3 + i) + 0.05
            aa = a + 0.05 * math.sin(t_ * 5 + i)
            pts.append((math.cos(aa) * rr, math.sin(aa) * rr, z0 - L * t_))
        b.tube(mats['root'], pts, [0.07, 0.06, 0.05, 0.04, 0.03, 0.02], seg=5)


def embedded_rocks(b, mats, R, mt, n=4.0, count=14, seed=5, zr=(-0.8, -3.0), size=(0.35, 0.7)):
    rnd = random.Random(seed)
    for i in range(count):
        a = rnd.uniform(0, 2 * math.pi)
        z = rnd.uniform(*zr)
        rr = R * se_r(a, n) * (0.98 if z > -2.2 else 0.9)
        s = rnd.uniform(*size)
        rock(b, mats[mt], (math.cos(a) * rr, math.sin(a) * rr, z), (s, s * 0.8, s * 0.65),
             (0, 0, rnd.uniform(0, 180)), seed=i + seed)


# ---------------------------------------------------------------- facade parts (facade plane y=0, outward -Y)
def window(b, mats, w, h, m=None, frame='frame_white', glass='glass', shutters=None, sill='limestone_plain',
           arch=False, mull=True, flowerbox=None, fb_flower='flower_red', pediment=None):
    b.push(m or Matrix.Identity(4))
    b.box(mats[glass], (w, 0.05, h), m=M((0, -0.025, 0)), bevel=0.0)
    ft = 0.09
    b.box(mats[frame], (w + ft * 2, 0.08, ft), m=M((0, -0.05, -ft)), bevel=0.01)
    b.box(mats[frame], (w + ft * 2, 0.08, ft), m=M((0, -0.05, h)), bevel=0.01)
    for s in (-1, 1):
        b.box(mats[frame], (ft, 0.08, h), m=M((s * (w / 2 + ft / 2), -0.05, 0)), bevel=0.01)
    if mull:
        b.box(mats[frame], (0.05, 0.07, h), m=M((0, -0.05, 0)), bevel=0.0)
        b.box(mats[frame], (w, 0.07, 0.05), m=M((0, -0.05, h * 0.62)), bevel=0.0)
    if arch:
        semi = [(math.cos(math.pi * k / 10) * (w / 2 + ft), math.sin(math.pi * k / 10) * (w / 2 + ft) * 0.7) for k in range(11)]
        b.prism(mats[frame], semi, 0.08, m=M((0, -0.05, h + ft)))
        semi_g = [(math.cos(math.pi * k / 10) * w / 2, math.sin(math.pi * k / 10) * w / 2 * 0.7 - 0.0) for k in range(11)]
        b.prism(mats[glass], semi_g, 0.05, m=M((0, -0.09, h + ft)))
    if shutters:
        for s in (-1, 1):
            b.box(mats[shutters], (w * 0.5, 0.06, h + 0.05), m=M((s * (w * 0.78 + ft), -0.06, -0.02)), bevel=0.01)
            for k in range(5):
                b.box(mats[shutters], (w * 0.48, 0.03, 0.03), m=M((s * (w * 0.78 + ft), -0.1, h * (k + 0.5) / 5)), bevel=0.0)
    if sill:
        b.box(mats[sill], (w + 0.32, 0.2, 0.08), m=M((0, -0.1, -ft - 0.08)), bevel=0.02)
    if pediment:
        b.box(mats[pediment], (w + 0.36, 0.16, 0.12), m=M((0, -0.08, h + ft)), bevel=0.02)
    if flowerbox:
        b.box(mats[flowerbox], (w + 0.1, 0.24, 0.2), m=M((0, -0.2, -ft - 0.3)), bevel=0.02)
        for k in range(5):
            x = -w / 2 + (k + 0.5) * w / 5
            b.sphere(mats['leaves'], 0.13, m=M((x, -0.22, -ft - 0.05), (0, 0, 0), (1, 1, 0.8)), sub=1, nz=0.03, nf=8, seed=k)
            b.sphere(mats[fb_flower], 0.07, m=M((x + 0.04, -0.3, -ft + 0.02)), sub=1)
    b.pop()


def door(b, mats, w, h, m=None, mt='wood_dark', frame='limestone_plain', arch=False, glass_top=False):
    b.push(m or Matrix.Identity(4))
    b.box(mats[mt], (w, 0.08, h), m=M((0, -0.04, 0)), bevel=0.01)
    for s in (-1, 1):
        b.box(mats[mt], (w * 0.4, 0.05, h * 0.36), m=M((s * w * 0.22, -0.09, h * 0.12)), bevel=0.02)
        b.box(mats[mt], (w * 0.4, 0.05, h * 0.36), m=M((s * w * 0.22, -0.09, h * 0.54)), bevel=0.02)
    b.sphere(mats['gold'], 0.04, m=M((w * 0.36, -0.12, h * 0.48)), sub=1)
    if frame:
        b.box(mats[frame], (w + 0.24, 0.1, 0.12), m=M((0, -0.05, h)), bevel=0.02)
        for s in (-1, 1):
            b.box(mats[frame], (0.12, 0.1, h), m=M((s * (w / 2 + 0.06), -0.05, 0)), bevel=0.02)
    if arch:
        semi = [(math.cos(math.pi * k / 12) * w / 2, math.sin(math.pi * k / 12) * w / 2) for k in range(13)]
        b.prism(mats[mt], semi, 0.08, m=M((0, -0.04, h)))
    b.pop()


def rail(b, mats, length, h=0.85, m=None, mt='iron', step=0.16, ornate=True, r=0.022):
    """Railing along local X (centered), standing on z=0."""
    b.push(m or Matrix.Identity(4))
    b.tube(mats[mt], [(-length / 2, 0, h), (length / 2, 0, h)], 0.035, seg=5)
    b.tube(mats[mt], [(-length / 2, 0, 0.08), (length / 2, 0, 0.08)], r, seg=4)
    n = max(2, int(length / step))
    for i in range(n + 1):
        x = -length / 2 + length * i / n
        b.tube(mats[mt], [(x, 0, 0.0), (x, 0, h)], r, seg=4)
    if ornate:
        for i in range(n):
            x = -length / 2 + length * (i + 0.5) / n
            ring_tube(b, mats[mt], 0.06, 0.012, m=M((x, 0, h * 0.55)), seg=8, tseg=3)
    b.pop()


def balcony(b, mats, w, d, m=None, slab_mt='limestone_plain', rail_mt='iron', h=0.8):
    b.push(m or Matrix.Identity(4))
    b.box(mats[slab_mt], (w, d, 0.14), m=M((0, -d / 2, -0.14)), bevel=0.03)
    for k in range(3):
        x = -w / 2 + 0.2 + (w - 0.4) * k / 2
        b.box(mats[slab_mt], (0.12, d * 0.7, 0.22), m=M((x, -d * 0.4, -0.34)), bevel=0.02)
    rail(b, mats, w - 0.06, h, m=M((0, -d + 0.04, 0)), mt=rail_mt)
    for s in (-1, 1):
        rail(b, mats, d - 0.08, h, m=M((s * (w / 2 - 0.03), -d / 2, 0), (0, 0, 90)), mt=rail_mt, ornate=False)
    b.pop()


# ---------------------------------------------------------------- props
def round_tree(b, mats, loc, h=4.0, r=1.4, seed=0, leaf='leaves'):
    rnd = random.Random(seed)
    x, y, z = loc
    b.tube(mats['bark'], [(x, y, z - 0.1), (x + 0.08, y - 0.05, z + h * 0.35), (x - 0.05, y + 0.04, z + h * 0.6)],
           [0.2 * h / 4, 0.15 * h / 4, 0.1 * h / 4], seg=7)
    for (dx, dy, k) in ((0.6, 0.0, 0.5), (-0.55, 0.2, 0.55), (0.1, -0.5, 0.6)):
        b.tube(mats['bark'], [(x, y, z + h * 0.45), (x + dx * r * 0.6, y + dy * r * 0.6, z + h * (0.45 + k * 0.3))], 0.06 * h / 4, seg=5)
    cz = z + h - r * 0.9
    blob_cluster(b, mats[leaf], (x, y, cz), r, n=7, seed=seed, flat=0.85, spread=0.62)
    for i in range(4):
        a = rnd.uniform(0, 2 * math.pi)
        b.sphere(mats['leaves_light'], r * 0.42, m=M((x + math.cos(a) * r * 0.5, y + math.sin(a) * r * 0.5, cz + r * 0.55)),
                 sub=2, nz=0.06, nf=3, seed=seed + i)


def topiary_cone(b, mats, loc, h=1.4, r=0.42, pot=False):
    x, y, z = loc
    if pot:
        b.lathe(mats['terracotta'], [(0.24, 0), (0.32, 0.42), (0.34, 0.46), (0.3, 0.5)], seg=12, m=M((x, y, z)))
        z += 0.45
    b.cone(mats['grass_dark'], r, h, 10, m=M((x, y, z)), smooth=True, nz=0.035, nf=7, seed=x)


def bush(b, mats, loc, r=0.6, seed=0, mt='grass_dark', flowers=None):
    x, y, z = loc
    blob_cluster(b, mats[mt], (x, y, z + r * 0.55), r, n=5, seed=seed, flat=0.75, spread=0.6)
    if flowers:
        rnd = random.Random(seed + 11)
        for i in range(7):
            a = rnd.uniform(0, 2 * math.pi)
            b.sphere(mats[flowers], 0.09, m=M((x + math.cos(a) * r * 0.75, y + math.sin(a) * r * 0.75, z + r * rnd.uniform(0.5, 1.0))), sub=1)


def pot_plant(b, mats, loc, r=0.3, kind='bush', seed=0, flowers=None, h=None):
    x, y, z = loc
    hp = h or r * 1.3
    b.lathe(mats['terracotta'], [(r * 0.7, 0), (r, hp * 0.9), (r * 1.1, hp * 0.92), (r * 1.1, hp), (r * 0.95, hp)],
            seg=12, m=M((x, y, z)))
    b.cyl(mats['dirt'], r * 0.92, 0.04, 12, m=M((x, y, z + hp - 0.06)))
    z += hp
    if kind == 'bush':
        blob_cluster(b, mats['leaves'], (x, y, z + r * 0.75), r * 1.05, n=5, seed=seed, flat=0.8, spread=0.5, sub=1)
        if flowers:
            for i in range(6):
                a = i * 1.05 + seed
                b.sphere(mats[flowers], r * 0.2, m=M((x + math.cos(a) * r * 0.8, y + math.sin(a) * r * 0.8, z + r * 0.9)), sub=1)
    elif kind == 'ball':
        b.tube(mats['bark'], [(x, y, z - 0.05), (x, y, z + r * 1.4)], 0.04, seg=5)
        b.sphere(mats['grass_dark'], r * 1.1, m=M((x, y, z + r * 1.9)), sub=2, nz=0.04, nf=8, seed=seed)
    elif kind == 'olive':
        b.tube(mats['bark'], [(x, y, z - 0.05), (x + 0.05, y, z + r * 1.6)], 0.045, seg=5)
        blob_cluster(b, mats['olive_leaf'], (x, y, z + r * 2.2), r * 0.95, n=5, seed=seed, flat=0.8, spread=0.55, sub=1)
    elif kind == 'herb':
        for i in range(7):
            a = i * 0.9 + seed
            b.cone(mats['leaves'], 0.06, r * 1.3, 4, m=M((x + math.cos(a) * r * 0.45, y + math.sin(a) * r * 0.45, z - 0.04),
                                                         (math.cos(a) * 12, math.sin(a) * 12, 0)))
        b.sphere(mats['leaves'], r * 0.6, m=M((x, y, z + r * 0.4), (0, 0, 0), (1, 1, 0.7)), sub=1, nz=0.03, nf=10)


def cypress(b, mats, loc, h=5.0, r=0.7, seed=0, pot=False):
    x, y, z = loc
    if pot:
        b.lathe(mats['terracotta'], [(0.45, 0), (0.62, 0.55), (0.66, 0.62), (0.58, 0.7)], seg=14, m=M((x, y, z)))
        z += 0.62
    b.cyl(mats['bark'], 0.1, 0.4, 6, m=M((x, y, z - 0.05)))
    prof = [(0.0, 0.25), (r * 0.75, 0.5), (r, h * 0.28), (r * 0.92, h * 0.55), (r * 0.6, h * 0.8), (r * 0.25, h * 0.95), (0, h)]
    b.lathe(mats['cypress'], prof, seg=12, m=M((x, y, z)), smooth=True, nz=0.12, nf=2.5, seed=seed)


def street_lamp(b, mats, loc, h=3.4, mt='green_iron', double=False, rot=0.0):
    x, y, z = loc
    b.push(M((x, y, z), (0, 0, rot)))
    b.lathe(mats[mt], [(0.22, 0), (0.22, 0.25), (0.14, 0.4), (0.1, 0.6), (0.08, h * 0.5), (0.06, h - 0.25), (0.09, h - 0.2)],
            seg=10, smooth=True)
    for zz in (0.62, h * 0.5):
        b.cyl(mats[mt], 0.12, 0.08, 10, m=M((0, 0, zz)))
    heads = [(0, 0)] if not double else [(-0.45, 0), (0.45, 0)]
    if double:
        b.tube(mats[mt], [(-0.5, 0, h - 0.25), (-0.2, 0, h - 0.05), (0.2, 0, h - 0.05), (0.5, 0, h - 0.25)], 0.04, seg=5)
    for (hx, hy) in heads:
        zz = h - 0.2
        b.cyl(mats[mt], 0.14, 0.08, 8, m=M((hx, hy, zz)))
        b.cyl(mats['lamp_glow'], 0.15, 0.45, 8, r2=0.2, m=M((hx, hy, zz + 0.08)))
        for k in range(4):
            a = math.pi / 2 * k + math.pi / 4
            b.tube(mats[mt], [(hx + math.cos(a) * 0.15, hy + math.sin(a) * 0.15, zz + 0.08),
                              (hx + math.cos(a) * 0.205, hy + math.sin(a) * 0.205, zz + 0.53)], 0.02, seg=4)
        b.cone(mats[mt], 0.27, 0.3, 8, m=M((hx, hy, zz + 0.53)))
        b.sphere(mats[mt], 0.06, m=M((hx, hy, zz + 0.86)), sub=1)
    b.pop()


def bistro_chair(b, mats, loc, rot=0.0, seat='wicker', frame='wood', h=0.48):
    b.push(M(loc, (0, 0, rot)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.tube(mats[frame], [(sx * 0.2, sy * 0.2, 0), (sx * 0.17, sy * 0.17, h)], 0.025, seg=5)
    b.cyl(mats[seat], 0.24, 0.06, 12, m=M((0, 0, h)))
    pts = [(math.cos(a) * 0.22, math.sin(a) * 0.22 + 0.02, h + 0.06) for a in [math.pi * (0.15 + 0.7 * k / 8) for k in range(9)]]
    for k, dz in enumerate((0.22, 0.42)):
        b.tube(mats[frame], [(p[0], p[1], p[2] + dz) for p in pts], 0.025, seg=5)
    b.box(mats[seat], (0.38, 0.05, 0.36), m=M((0, 0.22, h + 0.08), (8, 0, 0)), bevel=0.02)
    for s in (-1, 1):
        b.tube(mats[frame], [(s * 0.2, 0.18, h), (s * 0.2, 0.22, h + 0.45)], 0.025, seg=5)
    b.pop()


def greek_chair(b, mats, loc, rot=0.0):
    b.push(M(loc, (0, 0, rot)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(mats['blue'], (0.05, 0.05, 0.46), m=M((sx * 0.19, sy * 0.19, 0)), bevel=0.01)
    b.box(mats['wicker'], (0.44, 0.44, 0.05), m=M((0, 0, 0.44)), bevel=0.01)
    for s in (-1, 1):
        b.box(mats['blue'], (0.05, 0.05, 0.52), m=M((s * 0.19, 0.19, 0.46)), bevel=0.01)
    for zz in (0.62, 0.8, 0.95):
        b.box(mats['blue'], (0.42, 0.04, 0.06), m=M((0, 0.19, zz)), bevel=0.01)
    b.pop()


def bistro_table(b, mats, loc, r=0.42, cloth='checker', h=0.74):
    x, y, z = loc
    b.push(M(loc))
    b.cyl(mats['iron'], 0.24, 0.04, 10)
    b.cyl(mats['iron'], 0.035, h, 6)
    b.cyl(mats[cloth], r + 0.08, 0.22, 16, r2=r + 0.02, m=M((0, 0, h - 0.18)), smooth=True)
    b.cyl(mats[cloth], r + 0.02, 0.04, 16, m=M((0, 0, h)))
    for (cx, cy) in ((0.12, -0.1), (-0.15, 0.05)):
        b.cyl(mats['cloth_white'], 0.08, 0.015, 10, m=M((cx, cy, h + 0.04)))
        b.cyl(mats['cloth_white'], 0.045, 0.07, 8, r2=0.05, m=M((cx, cy, h + 0.055)))
    b.pop()


def wheel(b, mats, r, m, tire=0.04, spokes=8):
    b.push(m)
    ring_tube(b, mats['rubber'], r, tire, seg=18, tseg=5)
    ring_tube(b, mats['steel'], r - tire, 0.015, seg=18, tseg=3)
    for k in range(spokes):
        a = math.pi * k / spokes
        b.tube(mats['steel'], [(-math.cos(a) * (r - tire), 0, -math.sin(a) * (r - tire)),
                               (math.cos(a) * (r - tire), 0, math.sin(a) * (r - tire))], 0.008, seg=3, caps=False)
    b.cyl(mats['steel'], 0.04, 0.08, 8, m=M((0, 0.04, 0), (90, 0, 0)))
    b.pop()


def bicycle(b, mats, loc, rot=0.0, frame='teal', basket=True):
    """Bicycle along local X, wheels r=0.36."""
    b.push(M(loc, (0, 0, rot)))
    r = 0.36
    rx, fx = -0.55, 0.55
    wheel(b, mats, r, M((rx, 0, r)))
    wheel(b, mats, r, M((fx, 0, r)))
    seat = (-0.25, 0, 0.88)
    head = (0.38, 0, 0.92)
    bb_ = (-0.05, 0, 0.38)
    for (p, q) in ((seat, bb_), (bb_, head), (seat, head), (bb_, (rx, 0, r)), (seat, (rx, 0, r)), (head, (fx, 0, r))):
        b.tube(mats[frame], [p, q], 0.028, seg=5)
    b.box(mats['wood_dark'], (0.22, 0.1, 0.05), m=M((-0.27, 0, 0.92)), bevel=0.02)
    b.tube(mats[frame], [(0.38, 0, 0.92), (0.36, 0, 1.08)], 0.025, seg=5)
    b.tube(mats['steel'], [(0.3, -0.24, 1.1), (0.36, 0, 1.08), (0.3, 0.24, 1.1)], 0.02, seg=5)
    b.tube(mats[frame], [(rx - 0.05, 0, r + 0.06), (rx + 0.25, 0, r + 0.42), (rx + 0.32, 0, r + 0.52)], 0.012, seg=3, caps=False)
    if basket:
        b.box(mats['wicker'], (0.32, 0.36, 0.24), m=M((0.62, 0, 0.9)), bevel=0.03)
        for k in range(3):
            b.cyl(mats['bread'], 0.04, 0.55, 6, m=M((0.6 + 0.04 * (k - 1), -0.08 + 0.08 * k, 0.95), (0, 25 - k * 10, 0)), smooth=True)
        b.sphere(mats['flower_red'], 0.06, m=M((0.66, 0.1, 1.14)), sub=1)
    b.pop()


def scooter(b, mats, loc, rot=0.0, body='teal'):
    """Vespa-like scooter along local X (front +X)."""
    b.push(M(loc, (0, 0, rot)))
    r = 0.22
    for wx in (-0.62, 0.58):
        b.push(M((wx, 0, r), (90, 0, 0)))
        b.cyl(mats['rubber'], r, 0.14, 14, base=False, smooth=True)
        b.cyl(mats['steel'], r * 0.55, 0.16, 10, base=False)
        b.pop()
    b.sphere(mats[body], 1.0, m=M((-0.45, 0, 0.55), (0, -8, 0), (0.48, 0.3, 0.3)), sub=2, smooth=True)
    b.box(mats[body], (0.55, 0.3, 0.08), m=M((0.05, 0, 0.3)), bevel=0.03)
    b.box(mats[body], (0.12, 0.34, 0.75), m=M((0.42, 0, 0.3), (0, -14, 0)), bevel=0.05)
    b.sphere(mats[body], 1.0, m=M((0.6, 0, 0.42), (0, 0, 0), (0.24, 0.14, 0.16)), sub=2, smooth=True)
    b.box(mats['rubber'], (0.55, 0.26, 0.1), m=M((-0.42, 0, 0.82)), bevel=0.05)
    b.tube(mats['steel'], [(0.5, 0, 0.98), (0.52, 0, 1.18)], 0.03, seg=5)
    b.tube(mats['steel'], [(0.5, -0.32, 1.2), (0.52, 0, 1.2), (0.5, 0.32, 1.2)], 0.025, seg=5)
    b.box(mats[body], (0.14, 0.28, 0.12), m=M((0.53, 0, 1.12)), bevel=0.04)
    b.sphere(mats['lamp_glow'], 0.07, m=M((0.6, 0, 1.2)), sub=1)
    b.box(mats['steel'], (0.06, 0.3, 0.12), m=M((-0.88, 0, 0.62)), bevel=0.02)
    b.pop()


def amphora(b, mats, loc, h=1.4, r=0.5, mt='terracotta'):
    prof = [(0.0, 0.0), (r * 0.35, 0.0), (r * 0.6, h * 0.15), (r * 0.95, h * 0.42), (r, h * 0.55), (r * 0.8, h * 0.75),
            (r * 0.38, h * 0.88), (r * 0.36, h * 0.95), (r * 0.48, h), (r * 0.36, h * 0.99)]
    b.lathe(mats[mt], prof, seg=18, m=M(loc), smooth=True)
    b.cyl(mats['terracotta'], r * 1.0, 0.05, 18, m=M((loc[0], loc[1], loc[2] + h * 0.6)))
    b.cyl(mats['terracotta'], r * 0.96, 0.04, 18, m=M((loc[0], loc[1], loc[2] + h * 0.47)))


def olive_tree(b, mats, loc, h=3.4, r=1.6, seed=0):
    rnd = random.Random(seed)
    x, y, z = loc
    trunk = [(x, y, z - 0.1), (x + 0.2, y + 0.1, z + h * 0.2), (x - 0.15, y - 0.05, z + h * 0.4), (x + 0.1, y + 0.1, z + h * 0.55)]
    b.tube(mats['bark'], trunk, [0.32, 0.24, 0.22, 0.16], seg=7, smooth=True)
    b.tube(mats['bark'], [(x + 0.05, y, z), (x + 0.3, y - 0.25, z + h * 0.25), (x + 0.15, y - 0.1, z + h * 0.5)],
           [0.18, 0.13, 0.1], seg=6)
    for i in range(3):
        b.tube(mats['bark'], [(x + 0.05 * i, y, z + 0.05), (x + math.cos(i * 2.1) * 0.6, y + math.sin(i * 2.1) * 0.6, z - 0.08)],
               [0.14, 0.06], seg=5)
    tips = []
    for i in range(5):
        a = 2 * math.pi * i / 5 + rnd.uniform(-0.3, 0.3)
        tip = (x + math.cos(a) * r * 0.65, y + math.sin(a) * r * 0.65, z + h * rnd.uniform(0.7, 0.85))
        b.tube(mats['bark'], [(x + 0.1, y + 0.1, z + h * 0.55), tip], [0.1, 0.05], seg=5)
        tips.append(tip)
    for i, tp in enumerate(tips):
        blob_cluster(b, mats['olive_leaf'], (tp[0], tp[1], tp[2] + 0.35), r * 0.55, n=4, seed=seed + i, flat=0.6, spread=0.7, sub=1)
    blob_cluster(b, mats['olive_leaf'], (x, y, z + h * 0.92), r * 0.6, n=5, seed=seed + 9, flat=0.6, spread=0.7, sub=1)


def string_lights(b, mats, p0, p1, sag=0.45, n=9):
    p0, p1 = Vector(p0), Vector(p1)
    pts = []
    for k in range(13):
        t_ = k / 12
        p = p0.lerp(p1, t_)
        p.z -= sag * 4 * t_ * (1 - t_)
        pts.append(tuple(p))
    b.tube(mats['iron'], pts, 0.012, seg=3)
    for k in range(1, n + 1):
        t_ = k / (n + 1)
        p = p0.lerp(p1, t_)
        p.z -= sag * 4 * t_ * (1 - t_) + 0.08
        b.sphere(mats['bulb'], 0.06, m=M(tuple(p)), sub=1)


def flag3(b, mats, cols, w, h, m=None, wave=0.08, nx=9, nz=6, depth=0.03):
    """Tricolour hanging from a pole along +X: vertical stripes, top-left at the origin."""
    b.push(m or Matrix.Identity(4))
    for s, mt in enumerate(cols):
        t = bmesh.new()
        grid = []
        for iz in range(nz + 1):
            row = []
            for ix in range(nx // 3 + 1):
                u = (s + ix / (nx // 3)) / 3
                v = iz / nz
                row.append(t.verts.new((u * w, math.sin(u * 6.0) * wave * u, -v * h + math.sin(u * 4.0) * wave * 0.6 * u)))
            grid.append(row)
        for iz in range(nz):
            for ix in range(nx // 3):
                t.faces.new((grid[iz][ix], grid[iz + 1][ix], grid[iz + 1][ix + 1], grid[iz][ix + 1]))
        bmesh.ops.solidify(t, geom=list(t.faces), thickness=depth)
        bmesh.ops.recalc_face_normals(t, faces=t.faces)
        b.add_bm(t, mats[mt], True)
    b.pop()


def steps(b, mt, n, w, run, rise, m=None, nose=0.03):
    """Stair going up along +Y from y=0, bottom at z=0."""
    b.push(m or Matrix.Identity(4))
    for i in range(n):
        b.box(mt, (w, run * (n - i) + nose, rise), m=M((0, run * i + run * (n - i) / 2 - nose / 2, rise * i)), bevel=0.03)
    b.pop()


# ---------------------------------------------------------------- bake (warm tint instead of the snowy cool tint)
def _bake_group_impl(objs, out_png, res=2048, ao_strength=0.55, margin=6, ao_samples=32, label='atlas'):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = 4
    _select(objs)
    for o in objs:
        me = o.data
        while me.uv_layers:
            me.uv_layers.remove(me.uv_layers[0])
        me.uv_layers.new(name='atlas')
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.002, correct_aspect=True,
                             scale_to_bounds=False)
    try:
        bpy.ops.uv.pack_islands(rotate=True, margin=0.0025)
    except Exception as e:
        print('pack_islands failed', e)
    bpy.ops.object.mode_set(mode='OBJECT')

    mats_used = []
    for o in objs:
        for m_ in o.data.materials:
            if m_ and m_ not in mats_used:
                mats_used.append(m_)

    def target(img):
        for m_ in mats_used:
            nt = m_.node_tree
            n = nt.nodes.get('BAKE')
            if n is None:
                n = nt.nodes.new('ShaderNodeTexImage')
                n.name = 'BAKE'
            n.image = img
            for x in nt.nodes:
                x.select = False
            n.select = True
            nt.nodes.active = n

    alb = bpy.data.images.new(label + '_alb', res, res, alpha=False)
    target(alb)
    bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, margin=margin, use_clear=True)
    emi = bpy.data.images.new(label + '_emi', res, res, alpha=False)
    target(emi)
    bpy.ops.object.bake(type='EMIT', margin=margin, use_clear=True)
    ao = bpy.data.images.new(label + '_ao', res, res, alpha=False)
    ao.colorspace_settings.name = 'Non-Color'
    target(ao)
    sc.cycles.samples = ao_samples
    sc.world = sc.world or bpy.data.worlds.new('W')
    bpy.ops.object.bake(type='AO', margin=margin, use_clear=True)

    n = res * res * 4
    A = np.empty(n, np.float32)
    alb.pixels.foreach_get(A)
    O = np.empty(n, np.float32)
    ao.pixels.foreach_get(O)
    E = np.empty(n, np.float32)
    emi.pixels.foreach_get(E)
    A = A.reshape(-1, 4)
    O = O.reshape(-1, 4)[:, :1]
    E = E.reshape(-1, 4)
    f = (1.0 - ao_strength) + ao_strength * np.clip(O, 0, 1) ** 0.8
    rgb = A[:, :3] * f
    rgb = rgb + (1.0 - f) * np.array([0.03, 0.01, 0.0], np.float32)
    rgb = np.maximum(rgb, E[:, :3] * 0.9)
    A[:, :3] = np.clip(rgb, 0, 1)
    A[:, 3] = 1.0
    final = bpy.data.images.new(label, res, res, alpha=False)
    final.pixels.foreach_set(A.ravel())
    final.filepath_raw = out_png
    final.file_format = 'PNG'
    final.save()
    for im in (alb, ao, emi):
        bpy.data.images.remove(im)
    return final


def palette():
    return euro_palette()


_nordic_preview = preview


def preview(out_png, **kw):
    """Europe islands are seen across their front corner (+X, -Y)."""
    kw.setdefault('azim', -50.0)
    kw.setdefault('elev', 30.0)
    kw.setdefault('dist', 44.0)
    kw.setdefault('target', (0, 0, 2.2))
    return _nordic_preview(out_png, **kw)


# ---------------------------------------------------------------- operator context (live Blender over MCP runs code from a timer)
def _ctx(objs):
    ctx = dict(active_object=objs[0], object=objs[0], selected_objects=list(objs), selected_editable_objects=list(objs),
               scene=bpy.context.scene, view_layer=bpy.context.view_layer)
    wm = bpy.context.window_manager
    for win in getattr(wm, 'windows', []):
        for area in win.screen.areas:
            if area.type == 'VIEW_3D':
                reg = next((r for r in area.regions if r.type == 'WINDOW'), None)
                ctx.update(window=win, screen=win.screen, area=area, region=reg)
                return ctx
    return ctx


def bake_group(objs, out_png, *a, **kw):
    with bpy.context.temp_override(**_ctx(objs)):
        return _bake_group_impl(objs, out_png, *a, **kw)


_nordic_export_fbx = export_fbx


def export_fbx(objs, path):
    with bpy.context.temp_override(**_ctx(objs)):
        return _nordic_export_fbx(objs, path)
