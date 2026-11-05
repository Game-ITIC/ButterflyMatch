# Island: Brazil / Rio de Janeiro - Christ the Redeemer on Corcovado, Sugarloaf with cable car,
# curved bay with beach and wave-pattern boardwalk, colorful hillside houses
# (concept: ArtSource/RegionConcepts/LatinAmerica/Rio_IslandConcept.png)
NAME = 'RioDeJaneiro'
R_ISL = 7.8

BAY = (2.7, -3.5)             # bay centre (front-right corner)
BRX, BRY, BROT, BSEED = 3.3, 2.2, -20.0, 3.3
HILL = (-3.4, 3.4)            # Corcovado
HILL_S = (3.0, 2.6, 4.6)
SUGAR = (4.9, 3.5)            # Sugarloaf
SUGAR_S = (1.55, 1.4, 6.0)
URCA = (1.9, 2.0)             # Morro da Urca (first cable car station)
URCA_S = (1.5, 1.3, 2.1)
LEDGE1 = (-4.3, -0.5, 4.2, 2.6)   # hillside ledges the houses sit on (cx, cy, w, d)
LEDGE2 = (-4.7, 0.6, 3.4, 1.9)
L_H = 0.6


def bay_pt(a, s):
    """Point on the bay shoreline (same wobble as blob_outline) scaled by s from the bay centre."""
    f = 1.0 + 0.12 * fbm((math.cos(a) * 1.7, math.sin(a) * 1.7, BSEED), 2)
    x, y = math.cos(a) * BRX * f * s, math.sin(a) * BRY * f * s
    c, sn = math.cos(BROT * D2R), math.sin(BROT * D2R)
    return (BAY[0] + x * c - y * sn, BAY[1] + x * sn + y * c)


def band(a0, a1, s0, s1, n=18):
    outer = [bay_pt(a0 + (a1 - a0) * k / n, s1) for k in range(n + 1)]
    inner = [bay_pt(a1 - (a1 - a0) * k / n, s0) for k in range(n + 1)]
    return outer + inner


def rocky_lathe(b, mt, loc, prof, scl, seed, amp=0.08, seg=20):
    def rfn(a, z, i):
        return amp * fbm((math.cos(a) * 2.2, math.sin(a) * 2.2, z * 3.0 + seed), 2) * (0.2 if i == len(prof) - 1 else 1.0)
    b.lathe(mt, prof, seg=seg, m=M((loc[0], loc[1], 0.0), (0, 0, 0), scl), smooth=False, rfn=rfn)


def favela_house(b, mats, loc, w, d, h, col, rot=0.0, tank=False, seed=0):
    rnd = random.Random(seed)
    b.push(M(loc, (0, 0, rot)))
    b.box(mats[col], (w, d, h), bevel=0.03)
    b.box(mats['inca_plain'], (w + 0.08, d + 0.08, 0.08), m=M((0, 0, h)), bevel=0.02)
    # door or window on the front (-Y) face, a window on the side (+X) face
    if rnd.random() < 0.5 and h > 0.75:
        b.box(mats['doorway'], (0.24, 0.05, 0.5), m=M((rnd.uniform(-0.2, 0.2) * w, -d / 2 - 0.01, 0.0)), bevel=0.0)
    else:
        b.box(mats['doorway'], (0.24, 0.05, 0.24), m=M((rnd.uniform(-0.2, 0.2) * w, -d / 2 - 0.01, h * 0.45)), bevel=0.0)
    b.box(mats['doorway'], (0.05, 0.22, 0.22), m=M((w / 2 + 0.01, rnd.uniform(-0.15, 0.15) * d, h * 0.45)), bevel=0.0)
    if tank:
        b.cyl(mats['house_blue'], 0.16, 0.24, 8, m=M((w * 0.22, d * 0.15, h + 0.08)))
    b.pop()


def umbrella(b, mats, loc, cloth, tilt=(8, 0)):
    b.push(M(loc, (tilt[0], tilt[1], 0)))
    b.tube(mats['wood'], [(0, 0, 0), (0, 0, 1.25)], 0.03, seg=5)
    b.cone(mats[cloth], 0.62, 0.3, 8, m=M((0, 0, 1.05)))
    b.pop()


def build(mats, cb, ck):
    rnd = random.Random(97)
    body = euro_body(cb, mats, R_ISL, 'rock_warm', seed=6.1, depth=5.2, chunky=0.6)
    embedded_rocks(body, mats, R_ISL, 'granite_grey', count=12, seed=19, zr=(-0.9, -3.4), size=(0.4, 0.85))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'grass_trop', seed=8.8, drip=0.25, lip=0.45)
    # the bay: sea water with a thin sandy rim; the beach crescent is a build step
    inner = blob_outline(BAY[0], BAY[1], BRX, BRY, seed=BSEED, rot=BROT)
    outer = blob_outline(BAY[0], BAY[1], BRX + 0.35, BRY + 0.35, seed=BSEED, rot=BROT)
    ring_slab(top, mats['beach'], outer, inner, -0.02, 0.06)
    slab(top, mats['water_sea'], inner, -0.12, 0.13)
    for i in range(7):
        a = rnd.uniform(math.pi * 1.15, math.pi * 1.95)   # rocks on the seaward rim only
        x, y = bay_pt(a, 1.12)
        rock(top, mats['granite_grey'], (x, y, 0.0), (0.4, 0.32, 0.28), (0, 0, a * 30), seed=i)
    for (x, y, r) in ((-6.6, -4.2, 0.5), (-1.0, -6.4, 0.45), (6.7, 0.4, 0.45), (-6.9, 3.6, 0.5)):
        bush(top, mats, (x, y, 0.0), r, seed=int(x * 5), mt='leaves')
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 beach crescent with umbrellas and towels
    b = B('S01_Beach')
    a0, a1 = 0.15 * math.pi, 1.12 * math.pi
    slab(b, mats['beach'], band(a0, a1, 1.05, 1.42), -0.02, 0.1)
    for k, (t, s, cl) in enumerate(((0.35, 1.25, 'cloth_red'), (0.55, 1.22, 'house_yellow'), (0.78, 1.27, 'cloth_blue'), (0.95, 1.22, 'house_green'))):
        x, y = bay_pt(t * math.pi, s)
        umbrella(b, mats, (x, y, 0.06), cl, tilt=(6, -6 + 4 * k))
        tx, ty = bay_pt(t * math.pi + 0.12, s - 0.04)
        b.box(mats['textile' if k % 2 else 'rug'], (0.75, 0.42, 0.03), m=M((tx, ty, 0.08), (0, 0, t * 180 - 90 + BROT)), bevel=0.0)
    lx, ly = bay_pt(0.66 * math.pi, 1.18)
    b.push(M((lx, ly, 0.06), (0, 0, 30)))   # lifeguard post
    for (x, y) in ((-0.25, -0.25), (0.25, -0.25), (-0.25, 0.25), (0.25, 0.25)):
        b.tube(mats['wood'], [(x, y, 0), (x * 0.7, y * 0.7, 1.0)], 0.035, seg=5)
    b.box(mats['cloth_white'], (0.55, 0.55, 0.45), m=M((0, 0, 1.0)), bevel=0.03)
    b.box(mats['cloth_red'], (0.7, 0.7, 0.07), m=M((0, 0, 1.45)), bevel=0.02)
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 hillside: Corcovado and the ledges for houses
    b = B('S02_Hillside')
    rocky_lathe(b, mats['grass_trop'], HILL, [(1.0, 0.0), (0.86, 0.22), (0.66, 0.5), (0.44, 0.76), (0.26, 0.93), (0.2, 1.0), (0.0, 1.0)],
                HILL_S, 2.0, amp=0.09)
    rocky_lathe(b, mats['granite_grey'], (HILL[0] + 0.15, HILL[1] - 0.15),
                [(0.47, 0.7), (0.38, 0.8), (0.28, 0.92), (0.24, 0.98), (0.0, 0.98)], HILL_S, 5.0, amp=0.06)
    for (cx, cy, w, d), z0 in ((LEDGE1, 0.0), (LEDGE2, L_H)):
        slab(b, mats['jungle_rock'], rrect(cx, cy, w, d, 0.5, n=4), z0 - 0.02, L_H + 0.02)
        slab(b, mats['grass_trop'], rrect(cx, cy, w - 0.2, d - 0.2, 0.42, n=4), z0 + L_H - 0.02, 0.05)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S03 statue pedestal and viewing terrace on the summit
    zt = HILL_S[2]
    b = B('S03_StatuePedestal', M((HILL[0], HILL[1], zt - 0.05)))
    b.cyl(mats['inca_plain'], 0.95, 0.22, 10)
    b.cyl(mats['limestone_plain'], 0.85, 0.06, 10, m=M((0, 0, 0.22)))
    b.box(mats['redeemer'], (0.7, 0.7, 0.85), m=M((0, 0, 0.28)), bevel=0.04)
    b.box(mats['redeemer'], (0.8, 0.8, 0.1), m=M((0, 0, 1.13)), bevel=0.03)
    for k in range(10):
        a = k * 2 * math.pi / 10
        b.cyl(mats['iron'], 0.02, 0.32, 4, m=M((math.cos(a) * 0.9, math.sin(a) * 0.9, 0.22)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 Christ the Redeemer: robe, open arms, head (faces the camera)
    zs = zt - 0.05 + 1.23
    b = B('S04_RedeemerStatue', M((HILL[0], HILL[1], zs), (0, 0, 45)))
    frustum(b, mats['redeemer'], 0.62, 0.46, 0.4, 0.3, 1.55)
    b.box(mats['redeemer'], (0.5, 0.36, 0.42), m=M((0, 0, 1.4)), bevel=0.08)
    b.prism(mats['redeemer'], [(-1.15, 0.0), (1.15, 0.0), (1.15, 0.2), (0.25, 0.28), (-0.25, 0.28), (-1.15, 0.2)], 0.24,
            m=M((0, 0.12, 1.5), (90, 0, 0)))
    for s in (-1, 1):
        b.sphere(mats['redeemer'], 0.1, m=M((s * 1.18, 0, 1.6)), sub=1)
    b.box(mats['redeemer'], (0.14, 0.14, 0.12), m=M((0, 0, 1.82)), bevel=0.03)
    b.sphere(mats['redeemer'], 0.17, m=M((0, 0, 2.06), (0, 0, 0), (0.95, 0.95, 1.1)), sub=2)
    for k in range(4):   # robe folds
        b.box(mats['redeemer'], (0.05, 0.05, 1.3), m=M((-0.18 + 0.12 * k, -0.2 + 0.015 * abs(k - 1.5), 0.05), (4, 0, 0)), bevel=0.0)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 Sugarloaf and Morro da Urca
    b = B('S05_Sugarloaf')
    rocky_lathe(b, mats['granite_grey'], SUGAR, [(1.0, 0.0), (0.99, 0.3), (0.92, 0.55), (0.76, 0.76), (0.5, 0.92), (0.22, 0.99), (0.0, 1.0)],
                SUGAR_S, 1.0, amp=0.05, seg=22)
    rocky_lathe(b, mats['leaves'], SUGAR, [(1.12, 0.0), (1.0, 0.12), (0.88, 0.2), (0.0, 0.2)], SUGAR_S, 3.0, amp=0.1, seg=22)
    rocky_lathe(b, mats['grass_trop'], URCA, [(1.0, 0.0), (0.84, 0.35), (0.62, 0.72), (0.42, 0.95), (0.36, 1.0), (0.0, 1.0)],
                URCA_S, 6.0, amp=0.08, seg=16)
    rocky_lathe(b, mats['granite_grey'], (URCA[0] + 0.12, URCA[1] - 0.12), [(0.86, 0.3), (0.66, 0.66), (0.0, 0.7)], URCA_S, 8.0, amp=0.06, seg=16)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 cable car: two stations, cables and a yellow cabin
    b = B('S06_CableCar')
    st_lo = (URCA[0], URCA[1], URCA_S[2])
    st_hi = (SUGAR[0], SUGAR[1], SUGAR_S[2])
    for (x, y, z), rot in ((st_lo, 25), (st_hi, 25)):
        b.push(M((x, y, z - 0.1), (0, 0, rot)))
        b.box(mats['cream_paint'], (0.9, 0.7, 0.55), bevel=0.04)
        b.box(mats['house_blue'], (1.05, 0.85, 0.1), m=M((0, 0, 0.55)), bevel=0.03)
        b.box(mats['glass'], (0.6, 0.04, 0.25), m=M((0, -0.36, 0.2)), bevel=0.0)
        b.pop()
    p0 = (st_lo[0] + 0.25, st_lo[1] + 0.15, st_lo[2] + 0.35)
    p1 = (st_hi[0] - 0.3, st_hi[1] - 0.15, st_hi[2] + 0.35)
    for off in (-0.09, 0.09):
        sag = [(p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t + off, p0[2] + (p1[2] - p0[2]) * t - 0.5 * math.sin(math.pi * t))
               for t in (0.0, 0.2, 0.4, 0.6, 0.8, 1.0)]
        b.tube(mats['cable'], sag, 0.02, seg=4)
    t = 0.45
    cx, cy = p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t
    cz = p0[2] + (p1[2] - p0[2]) * t - 0.5 * math.sin(math.pi * t)
    b.tube(mats['cable'], [(cx, cy, cz), (cx, cy, cz - 0.35)], 0.025, seg=4)
    b.box(mats['cablecar'], (0.55, 0.42, 0.42), m=M((cx, cy, cz - 0.8), (0, 0, 25)), bevel=0.06)
    b.box(mats['glass'], (0.58, 0.44, 0.16), m=M((cx, cy, cz - 0.62), (0, 0, 25)), bevel=0.02)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 colorful houses stacked on the hillside ledges
    cols = ['house_yellow', 'house_pink', 'house_blue', 'house_green', 'house_orange']
    rows = [   # (x, y, z, w, d, h)
        (-5.9, -1.3, L_H, 1.0, 0.9, 0.85), (-4.8, -1.4, L_H, 0.9, 0.85, 1.0), (-3.75, -1.25, L_H, 1.0, 0.9, 0.8), (-2.75, -1.35, L_H, 0.85, 0.85, 0.95),
        (-5.9, 0.4, 2 * L_H, 0.95, 0.9, 0.9), (-4.85, 0.3, 2 * L_H, 1.0, 0.9, 0.8), (-3.8, 0.35, 2 * L_H, 0.9, 0.85, 1.0),
        (-4.8, -1.4, L_H + 1.08, 0.8, 0.75, 0.7), (-3.8, 0.35, 2 * L_H + 1.08, 0.75, 0.7, 0.65), (-4.6, -2.6, 0.0, 1.0, 0.9, 0.85),
    ]
    b = B('S07_ColorfulHouses')
    for k, (x, y, z, w, d, h) in enumerate(rows):
        favela_house(b, mats, (x, y, z), w, d, h, cols[(k * 3) % len(cols)], rot=rnd.uniform(-6, 6), tank=(k % 3 == 1), seed=k)
    steps(b, mats['stone_step'], 3, 0.6, 0.22, L_H / 3, m=M((-2.4, -0.2, 0.0), (0, 0, 90)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 boardwalk: wave-pattern promenade, kiosk and lamps
    b = B('S08_Boardwalk')
    slab(b, mats['cloth_white'], band(a0, a1, 1.42, 1.66), -0.02, 0.1)
    for (ws, ph) in ((1.5, 0.0), (1.58, math.pi)):
        pts = []
        for k in range(43):
            a = a0 + (a1 - a0) * k / 42
            s = ws + 0.03 * math.sin(a * 26 + ph)
            x, y = bay_pt(a, s)
            pts.append((x, y, 0.09))
        b.tube(mats['iron'], pts, 0.035, seg=4)
    kx, ky = bay_pt(0.5 * math.pi, 1.82)
    b.push(M((kx, ky, 0.0), (0, 0, BROT)))
    b.cyl(mats['house_green'], 0.42, 0.9, 8)
    b.cone(mats['cloth_white'], 0.75, 0.3, 8, m=M((0, 0, 0.9)))
    b.cyl(mats['house_yellow'], 0.44, 0.1, 8, m=M((0, 0, 0.62)))
    b.pop()
    for t in (0.28, 0.72, 1.02):
        x, y = bay_pt(t * math.pi, 1.7)
        street_lamp(b, mats, (x, y, 0.06), h=2.4, mt='iron')
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 palms along the promenade
    for k, t in enumerate((0.1, 0.28, 0.46, 0.64, 0.82)):
        x, y = bay_pt(t * math.pi, 1.86)
        b = B(f'S09_Palm_{k}')
        palm(b, mats, (x, y, 0.0), h=3.4 + 0.35 * (k % 2), lean=(0.3 * math.cos(t * math.pi), -0.3), seed=k * 5 + 2, fronds=8, L=1.7)
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 jungle trees on the slopes
    for k, (x, y, h, r) in enumerate(((-0.9, 2.2, 2.8, 1.1), (-6.3, 2.0, 3.0, 1.2), (0.0, 4.9, 3.2, 1.2), (-1.6, 6.2, 2.6, 1.0),
                                      (2.9, 5.6, 2.4, 0.9), (-6.6, -2.6, 2.4, 0.9))):
        b = B(f'S10_JungleTree_{k}')
        jungle_tree(b, mats, (x, y, 0.0), h=h, r=r, seed=k * 9 + 4, leaf='leaves' if k % 2 else 'leaves_light')
        bld.append(b.build(ck))
    return base, bld
