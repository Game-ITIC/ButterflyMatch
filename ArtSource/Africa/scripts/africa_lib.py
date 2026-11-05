# Warm-region kit shared by the Africa (Egypt / Morocco / South Africa) and Latin America
# (Mexico / Peru / Brazil) islands. Concatenated after ArtSource/North/scripts/nordic_lib.py + nordic_pipe.py
# and ArtSource/Europe/scripts/euro_lib.py: reuses B, M, mat, fbm, rock, euro_body/euro_top, bake/export helpers.
# Same conventions as Europe: island top at z=0, the front corner faces the camera at (+X, -Y),
# facades face -Y (front) and +X (right side); preview azimuth -50 (Unity yaw 16).


# ---------------------------------------------------------------- materials
def warm_palette():
    P = euro_palette()
    # ground / cliffs
    P['sand'] = mat('a_sand', '#d9a457', '#f1cf87', scale=1.1)
    P['sand_dark'] = mat('a_sand_dark', '#bf8540', '#ddae67', scale=2.0)
    P['sandstone'] = mat('a_sandstone', '#7e4527', '#c7834c', kind='rock', scale=0.7)
    P['sandstone_light'] = mat('a_sandstone_light', '#c4955a', '#ebc68a', kind='rock', scale=1.4)
    P['red_earth'] = mat('a_red_earth', '#8a3b22', '#c8673d', kind='rock', scale=0.75)
    P['jungle_rock'] = mat('a_jungle_rock', '#6a5a48', '#a89478', kind='rock', scale=0.75)
    P['granite_grey'] = mat('a_granite_grey', '#6c6862', '#aaa59c', kind='rock', scale=1.2)
    P['mountain'] = mat('a_mountain', '#5f5a55', '#9b928a', kind='rock', scale=0.5)
    P['grass_trop'] = mat('a_grass_trop', '#3f9a2a', '#86d04a', scale=1.2)
    P['moss'] = mat('a_moss', '#3c7a26', '#7bb23e', scale=2.4)
    # Egypt
    P['pyramid'] = set_mortar(mat('a_pyramid', '#d09c55', '#ebc684', kind='brick', scale=0.85), '#a2733c')
    P['sphinx'] = mat('a_sphinx', '#b5733f', '#dca468', kind='rock', scale=1.6)
    P['nemes'] = emat_pattern('a_nemes', '#1f4f9c', '#e4b94c', 'stripes', 9.0, 'Z')
    P['granite_red'] = mat('a_granite_red', '#7a3b2e', '#b2655a', kind='rock', scale=3.0)
    P['doorway'] = mat('a_doorway', '#2a1a12', '#4a3020', kind='flat')
    # Morocco
    P['medina'] = mat('a_medina', '#b9563a', '#de8460', scale=1.5)
    P['medina_dark'] = mat('a_medina_dark', '#8f3e2a', '#b65a3e', kind='rock', scale=2.0)
    P['zellige'] = emat_pattern('a_zellige', '#13806e', '#f1ead8', 'checker', 4.5)
    P['zellige_blue'] = emat_pattern('a_zellige_blue', '#1a4f9e', '#f1ead8', 'checker', 4.5)
    P['tile_green'] = mat('a_tile_green', '#11604e', '#1f8a6c', kind='wood', scale=2.4, wave_dir='X')
    P['market_a'] = emat_pattern('a_market_a', '#c22a2e', '#f1e6cf', 'stripes', 3.2, 'X')
    P['market_b'] = emat_pattern('a_market_b', '#178a78', '#f1e6cf', 'stripes', 3.2, 'X')
    P['spice_red'] = mat('a_spice_red', '#a8261a', '#d8482a', scale=8.0)
    P['spice_yellow'] = mat('a_spice_yellow', '#d39a12', '#f2c53a', scale=8.0)
    P['spice_green'] = mat('a_spice_green', '#5a7a1e', '#8eae3a', scale=8.0)
    # South Africa
    P['capedutch'] = mat('a_capedutch', '#e9e6dc', '#fbfaf5', scale=1.5)
    P['thatch'] = mat('a_thatch', '#7b5a2c', '#b48c52', kind='wood', scale=2.6, wave_dir='Y')
    P['thatch_x'] = mat('a_thatch_x', '#7b5a2c', '#b48c52', kind='wood', scale=2.6, wave_dir='X')
    P['shutter_green'] = mat('a_shutter_green', '#1d4a2c', '#2d6b40', kind='flat')
    P['protea'] = mat('a_protea', '#c23a5e', '#f087a2', scale=6.0)
    P['fynbos'] = mat('a_fynbos', '#5f7a3a', '#a2b066', scale=3.0)
    P['hull_white'] = mat('a_hull_white', '#dfe2e6', '#ffffff', kind='flat')
    P['hull_blue'] = mat('a_hull_blue', '#14427e', '#2a63ad', kind='flat')
    P['sail'] = mat('a_sail', '#e6e1d4', '#fbf8f0', kind='flat')
    P['water_sea'] = mat('a_water_sea', '#1683b8', '#4cc6e2', scale=1.2)
    P['foam'] = mat('a_foam', '#d8f0f6', '#ffffff', kind='flat')
    # Latin America
    P['maya'] = set_mortar(mat('a_maya', '#b49a78', '#ddc8a4', kind='brick', scale=1.1), '#857254')
    P['maya_plain'] = mat('a_maya_plain', '#bda482', '#e2d0ae', kind='rock', scale=1.6)
    P['maya_red'] = mat('a_maya_red', '#a2442a', '#cf6a42', kind='flat')
    P['agave'] = mat('a_agave', '#4f8a7a', '#8cc2ac', scale=3.0)
    P['cenote'] = mat('a_cenote', '#0f8f86', '#3fd2b8', scale=1.4)
    P['inca'] = set_mortar(mat('a_inca', '#8f8a80', '#c5bfb2', kind='brick', scale=1.6), '#5f5a52')
    P['inca_plain'] = mat('a_inca_plain', '#9a948a', '#cfc9bc', kind='rock', scale=1.8)
    P['terrace_grass'] = mat('a_terrace_grass', '#4a9a2e', '#8fd24e', scale=1.6)
    P['textile'] = emat_pattern('a_textile', '#b3202e', '#f0a43a', 'stripes', 7.0, 'X')
    P['redeemer'] = mat('a_redeemer', '#d8d3c6', '#f4f1e8', scale=2.0)
    P['house_yellow'] = mat('a_house_yellow', '#e2a91c', '#f6cf52', kind='flat')
    P['house_pink'] = mat('a_house_pink', '#d9567a', '#f28aa6', kind='flat')
    P['house_blue'] = mat('a_house_blue', '#2a86c8', '#56b2ea', kind='flat')
    P['house_green'] = mat('a_house_green', '#2f9a5c', '#5fc786', kind='flat')
    P['house_orange'] = mat('a_house_orange', '#df6a1e', '#f69a4a', kind='flat')
    P['beach'] = mat('a_beach', '#e6cf96', '#faeac0', scale=1.6)
    P['cable'] = mat('a_cable', '#2a2c30', '#44474d', kind='flat')
    P['cablecar'] = mat('a_cablecar', '#e8b21c', '#f6d65a', kind='flat')
    # shared props
    P['palm_leaf'] = mat('a_palm_leaf', '#2c7a22', '#7cc43c', scale=3.0)
    P['palm_trunk'] = mat('a_palm_trunk', '#6a4628', '#a87c4c', kind='wood', scale=3.4, wave_dir='Z')
    P['coconut'] = mat('a_coconut', '#4a3016', '#6e4a22', kind='flat')
    P['water_pool'] = mat('a_water_pool', '#178fba', '#5fd2e4', scale=1.5)
    P['reed'] = mat('a_reed', '#5a8a28', '#9cc44a', scale=4.0)
    P['clay'] = mat('a_clay', '#9a552e', '#d08a52', scale=3.0)
    P['rug'] = emat_pattern('a_rug', '#a32228', '#e2a43a', 'stripes', 6.0, 'X')
    P['tent'] = emat_pattern('a_tent', '#ece2c8', '#b4402a', 'stripes', 3.0, 'X')
    P['dry_bush'] = mat('a_dry_bush', '#6f7a34', '#a8ae58', scale=3.0)
    return P


def palette():
    return warm_palette()


# ---------------------------------------------------------------- outlines and slabs
def blob_outline(cx, cy, rx, ry, seed=0.0, n=28, wob=0.12, rot=0.0):
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n
        f = 1.0 + wob * fbm((math.cos(a) * 1.7, math.sin(a) * 1.7, seed), 2)
        x, y = math.cos(a) * rx * f, math.sin(a) * ry * f
        c, s = math.cos(rot * D2R), math.sin(rot * D2R)
        pts.append((cx + x * c - y * s, cy + x * s + y * c))
    return pts


def ring_slab(b, mt, outer, inner, z0, h, smooth=False):
    """Vertical ring prism between two CCW outlines with the same point count (rim of a pool, wall loop)."""
    t = bmesh.new()
    n = len(outer)
    ol = [t.verts.new((x, y, z0)) for (x, y) in outer]
    oh = [t.verts.new((x, y, z0 + h)) for (x, y) in outer]
    il = [t.verts.new((x, y, z0)) for (x, y) in inner]
    ih = [t.verts.new((x, y, z0 + h)) for (x, y) in inner]
    for i in range(n):
        j = (i + 1) % n
        t.faces.new((ol[i], ol[j], oh[j], oh[i]))
        t.faces.new((ih[i], ih[j], il[j], il[i]))
        t.faces.new((oh[i], oh[j], ih[j], ih[i]))
        t.faces.new((il[i], il[j], ol[j], ol[i]))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    return b.add_bm(t, mt, smooth)


def pool(b, mats, cx, cy, rx, ry, seed=0.0, rim='sandstone_light', water='water_pool', rim_w=0.32, h=0.26, rot=0.0):
    inner = blob_outline(cx, cy, rx, ry, seed, rot=rot)
    outer = blob_outline(cx, cy, rx + rim_w, ry + rim_w, seed, rot=rot)
    ring_slab(b, mats[rim], outer, inner, -0.02, h)
    slab(b, mats[water], inner, -0.02, h - 0.1)
    return inner


def stepped(b, mt, cx, cy, w0, d0, w1, d1, z0, h, n, ledge=0.35, m=None):
    """Stepped pyramid / terrace stack: n courses from (w0, d0) at z0 to (w1, d1) at z0 + h, with ledges."""
    b.push(m or Matrix.Identity(4))
    hh = h / n
    for i in range(n):
        t0, t1 = i / n, (i + 1) / n
        wa, da = w0 + (w1 - w0) * t0, d0 + (d1 - d0) * t0
        wb, db = w0 + (w1 - w0) * t1, d0 + (d1 - d0) * t1
        # each course leans in a little; the next one starts further in -> visible ledge
        wt = wa - (wa - wb) * (1.0 - ledge)
        dt = da - (da - db) * (1.0 - ledge)
        frustum(b, mt, wa, da, wt, dt, hh, m=M((cx, cy, 0)), z0=z0 + hh * i)
    b.pop()


def path_slabs(b, mt, pts, w=1.3, step=0.62, gap=0.08, h=0.07, seed=1, z=0.0):
    """Paving slabs following a polyline (each slab rotated along the path)."""
    rnd = random.Random(seed)
    P = [Vector((p[0], p[1], 0)) for p in pts]
    for (p0, p1) in zip(P, P[1:]):
        d = p1 - p0
        L = d.length
        ang = math.degrees(math.atan2(d.y, d.x))
        n = max(1, int(L / step))
        for k in range(n):
            c = p0 + d * ((k + 0.5) / n)
            for s in (-1, 1):
                wj = w / 2 - 0.04
                off = Vector((-d.y, d.x, 0)).normalized() * (s * w / 4)
                b.box(mt, (L / n - gap, wj, h + rnd.uniform(0, 0.03)),
                      m=M((c.x + off.x + rnd.uniform(-0.03, 0.03), c.y + off.y, z), (0, 0, ang + rnd.uniform(-3, 3))), bevel=0.03)


# ---------------------------------------------------------------- vegetation
def palm(b, mats, loc, h=4.2, lean=(0.5, 0.0), seed=0, fronds=8, L=1.9, coconuts=True):
    rnd = random.Random(seed)
    x, y, z = loc
    lx, ly = lean
    pts, rad = [], []
    for k in range(7):
        t_ = k / 6
        bend = t_ ** 1.6
        pts.append((x + lx * bend, y + ly * bend, z - 0.1 + h * t_))
        rad.append(0.2 - 0.09 * t_)
    b.tube(mats['palm_trunk'], pts, rad, seg=7)
    # trunk rings
    for k in range(1, 6):
        t_ = k / 6
        bend = t_ ** 1.6
        b.cyl(mats['palm_trunk'], rad[k] + 0.035, 0.07, 7, m=M((x + lx * bend, y + ly * bend, z - 0.1 + h * t_ - 0.03)))
    top = Vector(pts[-1])
    b.sphere(mats['palm_trunk'], 0.2, m=M(tuple(top)), sub=1)
    if coconuts:
        for k in range(3):
            a = 2 * math.pi * k / 3 + seed
            b.sphere(mats['coconut'], 0.11, m=M((top.x + math.cos(a) * 0.15, top.y + math.sin(a) * 0.15, top.z - 0.14)), sub=1)
    for i in range(fronds):
        a = 2 * math.pi * i / fronds + rnd.uniform(-0.2, 0.2)
        LL = L * rnd.uniform(0.85, 1.1)
        frond(b, mats['palm_leaf'], top + Vector((0, 0, 0.05)), a, LL, lift=rnd.uniform(0.25, 0.45), seed=seed + i)


def frond(b, mt, base, ang, L, lift=0.35, w=0.42, seg=7, seed=0):
    """Drooping palm frond: a folded serrated ribbon with thickness."""
    t = bmesh.new()
    dx, dy = math.cos(ang), math.sin(ang)
    px, py = -dy, dx
    rows = []
    for k in range(seg + 1):
        t_ = k / seg
        cx = base.x + dx * L * t_
        cy = base.y + dy * L * t_
        cz = base.z + lift * L * math.sin(math.pi * t_ * 0.75) - 0.7 * L * t_ * t_
        ww = w * math.sin(math.pi * min(1.0, 0.12 + t_ * 0.95)) * (1.0 if k % 2 == 0 else 0.7)
        fold = 0.07 * ww / w
        rows.append((t.verts.new((cx - px * ww, cy - py * ww, cz - 0.06 * t_)),
                     t.verts.new((cx, cy, cz + fold)),
                     t.verts.new((cx + px * ww, cy + py * ww, cz - 0.06 * t_))))
    for k in range(seg):
        a, c = rows[k], rows[k + 1]
        t.faces.new((a[0], c[0], c[1], a[1]))
        t.faces.new((a[1], c[1], c[2], a[2]))
    bmesh.ops.solidify(t, geom=list(t.faces), thickness=0.035)
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    b.add_bm(t, mt, True)


def agave(b, mats, loc, r=0.6, n=11, seed=0, mt='agave'):
    rnd = random.Random(seed)
    x, y, z = loc
    for i in range(n):
        a = 2 * math.pi * i / n + rnd.uniform(-0.15, 0.15)
        tilt = rnd.uniform(35, 60) if i % 2 == 0 else rnd.uniform(15, 35)
        ln = r * rnd.uniform(0.85, 1.25) * (1.0 if i % 2 == 0 else 0.8)
        b.cone(mats[mt], 0.11 * r / 0.6, ln, 4, m=M((x, y, z - 0.02), (0, tilt, a / D2R), (1.0, 0.45, 1.0)))
    b.cone(mats[mt], 0.14 * r / 0.6, r * 0.9, 5, m=M((x, y, z - 0.02)))


def reeds(b, mats, loc, n=7, h=0.9, seed=0):
    rnd = random.Random(seed)
    x, y, z = loc
    for i in range(n):
        a = rnd.uniform(0, 2 * math.pi)
        d = rnd.uniform(0.0, 0.25)
        b.cone(mats['reed'], 0.05, h * rnd.uniform(0.7, 1.2), 4,
               m=M((x + math.cos(a) * d, y + math.sin(a) * d, z - 0.02), (rnd.uniform(-12, 12), rnd.uniform(-12, 12), 0)))


def jungle_tree(b, mats, loc, h=4.2, r=1.7, seed=0, leaf='leaves'):
    """Broad canopy tree (umbrella pine / tropical tree) with a few layered blobs."""
    rnd = random.Random(seed)
    x, y, z = loc
    b.tube(mats['bark'], [(x, y, z - 0.1), (x + 0.1, y, z + h * 0.45), (x - 0.05, y + 0.05, z + h * 0.75)],
           [0.24 * h / 4, 0.17 * h / 4, 0.11 * h / 4], seg=7)
    for (dx, dy) in ((0.7, 0.2), (-0.6, 0.3), (0.0, -0.7)):
        b.tube(mats['bark'], [(x, y, z + h * 0.55), (x + dx * r * 0.55, y + dy * r * 0.55, z + h * 0.82)], 0.07 * h / 4, seg=5)
    blob_cluster(b, mats[leaf], (x, y, z + h * 0.9), r, n=7, seed=seed, flat=0.55, spread=0.75)
    for i in range(3):
        a = rnd.uniform(0, 2 * math.pi)
        b.sphere(mats['leaves_light'], r * 0.45, m=M((x + math.cos(a) * r * 0.45, y + math.sin(a) * r * 0.45, z + h * 0.9 + r * 0.35), (0, 0, 0), (1, 1, 0.6)),
                 sub=2, nz=0.05, nf=3, seed=seed + i)


def fern(b, mats, loc, r=0.55, n=7, seed=0, mt='leaves'):
    rnd = random.Random(seed)
    x, y, z = loc
    for i in range(n):
        a = 2 * math.pi * i / n + rnd.uniform(-0.2, 0.2)
        frond(b, mats[mt], Vector((x, y, z + 0.05)), a, r * rnd.uniform(0.8, 1.15), lift=0.6, w=0.16, seg=5, seed=seed + i)


# ---------------------------------------------------------------- small props
def pot(b, mats, loc, r=0.22, h=0.5, mt='clay'):
    b.lathe(mats[mt], [(0.0, 0.0), (r * 0.55, 0.0), (r, h * 0.45), (r * 0.85, h * 0.8), (r * 0.5, h * 0.92), (r * 0.6, h), (r * 0.45, h * 0.98)],
            seg=12, m=M(loc), smooth=True)


def basket(b, mats, loc, r=0.28, h=0.22, fill='spice_red'):
    b.cyl(mats['wicker'], r, h, 12, r2=r * 1.12, m=M(loc))
    x, y, z = loc
    b.sphere(mats[fill], r * 0.98, m=M((x, y, z + h - 0.02), (0, 0, 0), (1, 1, 0.45)), sub=2)


def canopy_stall(b, mats, loc, w=2.2, d=1.5, h=2.0, rot=0.0, cloth='tent', table='wood'):
    """Market stall: four poles, sloped striped canopy facing -Y, a counter."""
    b.push(M(loc, (0, 0, rot)))
    for sx in (-1, 1):
        for sy in (-1, 1):
            hh = h if sy > 0 else h - 0.3
            b.tube(mats['wood'], [(sx * w / 2, sy * d / 2, 0), (sx * w / 2, sy * d / 2, hh)], 0.05, seg=5)
    ang = math.degrees(math.atan2(0.3, d))
    b.box(mats[cloth], (w + 0.3, d + 0.5, 0.06), m=M((0, -0.1, h - 0.15), (-ang, 0, 0)), base=False, bevel=0.0)
    for k in range(7):
        xx = -w / 2 - 0.15 + (k + 0.5) * (w + 0.3) / 7
        b.cyl(mats[cloth], 0.17, 0.06, 3, m=M((xx, -d / 2 - 0.36, h - 0.42), (90, 0, 0)), base=False)
    b.box(mats[table], (w * 0.9, d * 0.45, 0.8), m=M((0, -d * 0.2, 0)), bevel=0.04)
    b.pop()
