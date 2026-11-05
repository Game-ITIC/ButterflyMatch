# Island: Morocco / Marrakech - Koutoubia-inspired minaret, medina gate, tiled fountain
# (concept: ArtSource/RegionConcepts/Africa/MarrakechMedina_IslandConcept.png)
NAME = 'MarrakechMedina'
R_ISL = 7.7

GATE = (-3.1, 1.3)          # medina gate (faces -Y)
WALL_Y = 1.3
WALL_H = 2.5
MIN = (3.4, 3.5)            # minaret
MIN_W, MIN_H = 2.0, 7.4
FOUNT = (0.5, -2.5)


def horseshoe(w, h, n=14):
    """Horseshoe arch outline in local XZ (bottom at z=0), CCW."""
    r = w / 2 * 1.12
    cz = h - r
    pts = [(w / 2, 0.0)]
    a0, a1 = -25.0, 205.0
    for k in range(n + 1):
        a = (a0 + (a1 - a0) * k / n) * D2R
        pts.append((math.cos(a) * r, cz + math.sin(a) * r))
    pts.append((-w / 2, 0.0))
    return pts


def merlons(b, mt, p0, p1, z, size=0.32, gap=0.24, depth=0.4):
    p0, p1 = Vector((p0[0], p0[1], 0)), Vector((p1[0], p1[1], 0))
    d = p1 - p0
    L = d.length
    ang = math.degrees(math.atan2(d.y, d.x))
    n = max(1, int(L / (size + gap)))
    for k in range(n):
        c = p0 + d * ((k + 0.5) / n)
        b.box(mt, (size, depth, size * 1.1), m=M((c.x, c.y, z), (0, 0, ang)), bevel=0.03)
        b.cone(mt, size * 0.5, size * 0.55, 4, m=M((c.x, c.y, z + size * 1.1), (0, 0, ang + 45)))


def wall(b, mats, p0, p1, h=WALL_H, t=0.6):
    p0v, p1v = Vector((p0[0], p0[1], 0)), Vector((p1[0], p1[1], 0))
    d = p1v - p0v
    c = (p0v + p1v) / 2
    ang = math.degrees(math.atan2(d.y, d.x))
    b.box(mats['medina'], (d.length + t, t, h), m=M((c.x, c.y, 0), (0, 0, ang)), bevel=0.06)
    b.box(mats['medina_dark'], (d.length + t + 0.06, t + 0.06, 0.25), m=M((c.x, c.y, 0), (0, 0, ang)), bevel=0.04)
    merlons(b, mats['medina'], p0, p1, h)


def arch_panel(b, mats, w, h, m, mt='doorway', frame='medina_dark', depth=0.06):
    b.push(m)
    b.prism(mats[frame], horseshoe(w + 0.18, h + 0.12), depth, m=M((0, -depth / 2 - 0.005, -0.06)))
    b.prism(mats[mt], horseshoe(w, h), depth, m=M((0, -depth / 2 - 0.035, 0)))
    b.pop()


def build(mats, cb, ck):
    rnd = random.Random(53)
    body = euro_body(cb, mats, R_ISL, 'red_earth', seed=3.9, depth=5.0, chunky=0.6)
    embedded_rocks(body, mats, R_ISL, 'medina_dark', count=12, seed=9, zr=(-0.9, -3.2), size=(0.4, 0.8))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'cobble_warm', seed=1.7, drip=0.05, lip=0.28)
    for i in range(20):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.96, 1.01)
        bush(top, mats, (math.cos(a) * rr, math.sin(a) * rr, -0.22), rnd.uniform(0.3, 0.5), seed=i, mt='leaves')
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 tiled courtyard
    b = B('S01_Courtyard')
    slab(b, mats['medina_dark'], rrect(FOUNT[0], FOUNT[1], 7.4, 5.0, 0.4), -0.02, 0.1)
    slab(b, mats['zellige'], rrect(FOUNT[0], FOUNT[1], 6.9, 4.5, 0.3), 0.0, 0.12)
    slab(b, mats['cobble_warm'], rrect(GATE[0], (GATE[1] + FOUNT[1] + 2.4) / 2, 2.0, GATE[1] - FOUNT[1] - 2.2, 0.2), -0.02, 0.1)
    for (x, y) in ((FOUNT[0] - 3.45, FOUNT[1] - 2.25), (FOUNT[0] + 3.45, FOUNT[1] - 2.25), (FOUNT[0] + 3.45, FOUNT[1] + 2.25)):
        b.box(mats['medina'], (0.42, 0.42, 0.5), m=M((x, y, 0)), bevel=0.05)
        b.sphere(mats['gold'], 0.1, m=M((x, y, 0.58)), sub=1)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 crenellated medina walls with corner towers
    b = B('S02_MedinaWalls')
    gx0, gx1 = GATE[0] - 1.85, GATE[0] + 1.85
    wall(b, mats, (-6.4, WALL_Y), (gx0, WALL_Y))
    wall(b, mats, (gx1, WALL_Y), (1.7, WALL_Y))
    wall(b, mats, (-6.4, WALL_Y), (-6.4, 5.2))
    for (x, y) in ((-6.4, WALL_Y), (1.7, WALL_Y), (-6.4, 5.2)):
        b.box(mats['medina'], (1.15, 1.15, WALL_H + 0.7), m=M((x, y, 0)), bevel=0.08)
        merlons(b, mats['medina'], (x - 0.45, y - 0.5), (x + 0.45, y - 0.5), WALL_H + 0.7, size=0.26, gap=0.12)
        merlons(b, mats['medina'], (x + 0.5, y - 0.45), (x + 0.5, y + 0.45), WALL_H + 0.7, size=0.26, gap=0.12)
        arch_panel(b, mats, 0.3, 0.55, M((x, y - 0.58, 1.6)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S03 medina gate (Bab) with zellige frame
    b = B('S03_MedinaGate', M((GATE[0], GATE[1], 0)))
    GW, GD, GH = 3.7, 1.5, 4.0
    b.box(mats['medina'], (GW, GD, GH), bevel=0.08)
    b.box(mats['medina_dark'], (GW + 0.08, GD + 0.08, 0.3), bevel=0.04)
    b.box(mats['medina'], (GW + 0.2, GD + 0.2, 0.22), m=M((0, 0, GH - 0.2)), bevel=0.05)
    merlons(b, mats['medina'], (-GW / 2, -GD / 2 + 0.2), (GW / 2, -GD / 2 + 0.2), GH, size=0.36, gap=0.2)
    b.push(M((0, -GD / 2, 0)))
    b.box(mats['zellige'], (2.5, 0.06, 3.15), m=M((0, -0.02, 0.0)), bevel=0.0)
    b.box(mats['tile_green'], (2.7, 0.1, 0.18), m=M((0, -0.04, 3.15)), bevel=0.02)
    for s in (-1, 1):
        b.box(mats['tile_green'], (0.16, 0.1, 3.15), m=M((s * 1.28, -0.04, 0.0)), bevel=0.01)
    b.prism(mats['medina'], horseshoe(1.85, 2.85), 0.08, m=M((0, -0.07, 0.0)))
    b.prism(mats['doorway'], horseshoe(1.5, 2.55), 0.08, m=M((0, -0.11, 0.0)))
    b.box(mats['wood_dark'], (1.3, 0.06, 1.7), m=M((0, -0.16, 0.0)), bevel=0.01)
    for k in range(6):
        b.sphere(mats['gold'], 0.035, m=M((-0.5 + 0.2 * k, -0.2, 0.9)), sub=1)
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 / S05 Koutoubia-inspired minaret
    b = B('S04_Minaret', M((MIN[0], MIN[1], 0)))
    b.box(mats['medina_dark'], (MIN_W + 0.4, MIN_W + 0.4, 0.35), bevel=0.05)
    b.box(mats['medina'], (MIN_W, MIN_W, MIN_H), bevel=0.06)
    for face in (M((0, -MIN_W / 2, 0)), M((MIN_W / 2, 0, 0), (0, 0, 90))):
        b.push(face)
        arch_panel(b, mats, 0.75, 1.5, M((0, 0, 0.35)), mt='wood_dark')
        for (z, w, h, n) in ((2.6, 0.42, 0.95, 2), (4.2, 0.3, 0.8, 3), (5.45, 0.52, 0.6, 1)):
            for k in range(n):
                x = (k - (n - 1) / 2) * 0.62
                arch_panel(b, mats, w, h, M((x, 0, z)))
        b.box(mats['zellige'], (MIN_W - 0.2, 0.06, 0.55), m=M((0, -0.02, 6.35)), bevel=0.0)
        b.box(mats['medina_dark'], (MIN_W - 0.1, 0.1, 0.08), m=M((0, -0.04, 6.3)), bevel=0.0)
        b.box(mats['medina_dark'], (MIN_W - 0.1, 0.1, 0.08), m=M((0, -0.04, 6.92)), bevel=0.0)
        b.pop()
    b.box(mats['medina'], (MIN_W + 0.2, MIN_W + 0.2, 0.2), m=M((0, 0, MIN_H - 0.05)), bevel=0.04)
    hw = MIN_W / 2 + 0.05
    for (p0, p1) in (((-hw, -hw), (hw, -hw)), ((hw, -hw), (hw, hw)), ((hw, hw), (-hw, hw)), ((-hw, hw), (-hw, -hw))):
        merlons(b, mats['medina'], p0, p1, MIN_H + 0.15, size=0.3, gap=0.18, depth=0.3)
    bld.append(b.build(ck))

    b = B('S05_MinaretTop', M((MIN[0], MIN[1], MIN_H + 0.15)))
    b.box(mats['medina'], (0.95, 0.95, 1.5), bevel=0.05)
    for face in (M((0, -0.475, 0)), M((0.475, 0, 0), (0, 0, 90))):
        arch_panel(b, mats, 0.36, 0.85, face @ M((0, 0, 0.3)))
        b.push(face)
        b.box(mats['zellige'], (0.8, 0.05, 0.22), m=M((0, -0.01, 1.2)), bevel=0.0)
        b.pop()
    b.box(mats['medina'], (1.1, 1.1, 0.14), m=M((0, 0, 1.5)), bevel=0.03)
    b.lathe(mats['tile_green'], [(0.55, 0.0), (0.5, 0.18), (0.36, 0.38), (0.0, 0.52)], seg=12, m=M((0, 0, 1.64)), smooth=True)
    b.cyl(mats['gold'], 0.03, 1.0, 6, m=M((0, 0, 2.1)))
    for (z, r) in ((2.35, 0.17), (2.62, 0.14), (2.86, 0.1)):
        b.sphere(mats['gold'], r, m=M((0, 0, z)), sub=2)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 tiled octagonal fountain
    b = B('S06_TiledFountain', M((FOUNT[0], FOUNT[1], 0)))
    oct8 = [(math.cos(math.pi / 8 + k * math.pi / 4) * 1.55, math.sin(math.pi / 8 + k * math.pi / 4) * 1.55) for k in range(8)]
    oct_in = [(x * 0.82, y * 0.82) for (x, y) in oct8]
    slab(b, mats['zellige_blue'], oct8, 0.0, 0.62)
    ring_slab(b, mats['medina'], [(x * 1.06, y * 1.06) for (x, y) in oct8], oct8, 0.62, 0.12)
    slab(b, mats['water_pool'], oct_in, 0.0, 0.66)
    b.cyl(mats['zellige'], 0.28, 0.75, 8, m=M((0, 0, 0.6)))
    b.lathe(mats['medina'], [(0.0, 0.0), (0.7, 0.0), (0.62, 0.12), (0.2, 0.2), (0.0, 0.2)], seg=16, m=M((0, 0, 1.3)), smooth=True)
    b.cyl(mats['water_pool'], 0.58, 0.04, 16, m=M((0, 0, 1.42)))
    b.cyl(mats['zellige'], 0.12, 0.4, 8, m=M((0, 0, 1.45)))
    b.sphere(mats['gold'], 0.13, m=M((0, 0, 1.92)), sub=2)
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        b.tube(mats['water_pool'], [(math.cos(a) * 0.25, math.sin(a) * 0.25, 1.85), (math.cos(a) * 0.6, math.sin(a) * 0.6, 1.75),
                                    (math.cos(a) * 0.75, math.sin(a) * 0.75, 1.45)], 0.04, seg=4)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 riad house behind the walls
    b = B('S07_RiadHouse', M((-3.6, 3.7, 0)))
    W, D, H = 3.4, 2.6, 4.4
    b.box(mats['medina'], (W, D, H), bevel=0.06)
    b.box(mats['medina'], (W + 0.2, D + 0.2, 0.18), m=M((0, 0, H - 0.05)), bevel=0.04)
    frustum(b, mats['tile_green'], W + 0.5, D + 0.5, W * 0.35, 0.05, 1.2, z0=H + 0.12)
    b.push(M((0, -D / 2, 0)))
    for x in (-0.85, 0.85):
        arch_panel(b, mats, 0.5, 0.9, M((x, 0, 3.0)), mt='tile_green')
        b.box(mats['wood_dark'], (0.75, 0.25, 0.08), m=M((x, -0.1, 2.95)), bevel=0.02)
    b.pop()
    b.push(M((W / 2, 0, 0), (0, 0, 90)))
    arch_panel(b, mats, 0.5, 0.9, M((0, 0, 3.0)), mt='tile_green')
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 souk stalls
    b = B('S08_MarketStalls')
    canopy_stall(b, mats, (4.9, -1.2, 0.0), w=2.3, d=1.5, h=2.1, rot=-30, cloth='market_a')
    canopy_stall(b, mats, (-4.4, -3.4, 0.0), w=2.3, d=1.5, h=2.1, rot=12, cloth='market_b')
    for (x, y, f) in ((4.3, -1.7, 'spice_red'), (4.85, -2.0, 'spice_yellow'), (5.4, -2.3, 'spice_green'),
                      (-5.0, -3.5, 'spice_yellow'), (-4.4, -3.6, 'spice_red'), (-3.8, -3.7, 'spice_green')):
        basket(b, mats, (x, y, 0.8), 0.24, 0.16, fill=f)
    b.box(mats['rug'], (1.5, 1.0, 0.03), m=M((3.6, -3.4, 0.0), (0, 0, -30)), bevel=0.0)
    for (x, y, r, h) in ((6.0, -2.4, 0.22, 0.55), (-5.8, -4.3, 0.24, 0.6), (-3.0, -4.6, 0.18, 0.45)):
        pot(b, mats, (x, y, 0.0), r, h)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 palm trees
    for k, (x, y, h, lx, ly) in enumerate(((6.0, 1.4, 4.6, 0.45, -0.1), (-6.0, -1.0, 4.0, -0.35, -0.3),
                                            (-0.4, 4.6, 5.0, -0.2, 0.3), (6.7, -3.7, 3.4, 0.4, -0.3))):
        b = B(f'S09_Palm_{k}')
        palm(b, mats, (x, y, 0.0), h=h, lean=(lx, ly), seed=k * 5 + 2, fronds=8, L=1.9 + 0.1 * (h - 3.4))
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 bougainvillea + flower pots
    b = B('S10_FlowerPots')
    for (x, y, r) in ((-5.3, 0.8, 0.6), (0.6, 0.8, 0.55), (1.3, 0.75, 0.45)):
        bush(b, mats, (x, y, WALL_H - 0.55), r, seed=int(x * 9), mt='leaves', flowers='flower_pink')
    for (x, y) in ((-1.0, 0.4), (-5.2, 0.4)):
        b.tube(mats['bark'], [(x, y, 0.0), (x + 0.1, y + 0.1, 1.4), (x, y + 0.3, WALL_H - 0.4)], 0.07, seg=5)
    for (x, y, fl) in ((FOUNT[0] - 2.2, FOUNT[1] + 1.3, 'flower_red'), (FOUNT[0] + 2.2, FOUNT[1] + 1.3, 'flower_pink'),
                       (FOUNT[0] + 2.4, FOUNT[1] - 1.3, 'flower_yellow'), (GATE[0] - 1.4, GATE[1] - 1.2, 'flower_pink'),
                       (GATE[0] + 1.4, GATE[1] - 1.2, 'flower_red')):
        pot_plant(b, mats, (x, y, 0.0), 0.3, kind='bush', flowers=fl, seed=int(x * 3))
    bld.append(b.build(ck))
    return base, bld
