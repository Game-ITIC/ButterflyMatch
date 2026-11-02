# Island: Italy / Pisa tower and pizzeria (ref: Downloads/Evropa/photo_2026-11-02_18-44-03.jpg)
NAME = 'PisaPlaza'
R_ISL = 7.8
LEAN = M((-3.4, 2.1, 0), (4.6, -4.6, 0))
TIER_H = 1.12
GROUND_H = 2.3


def tower_tier(b, mats, z, r=1.38, h=TIER_H, cols=16):
    b.cyl(mats['marble_dark'], r + 0.36, 0.14, 24, m=M((0, 0, z)))
    b.cyl(mats['marble'], r, h, 20, m=M((0, 0, z + 0.14)), smooth=True)
    for k in range(cols):
        a = 2 * math.pi * k / cols
        b.cyl(mats['marble'], 0.065, h - 0.26, 6, m=M((math.cos(a) * (r + 0.24), math.sin(a) * (r + 0.24), z + 0.14)))
        a2 = a + math.pi / cols
        b.box(mats['chalkboard'], (0.26, 0.05, h - 0.42), m=M((math.cos(a2) * (r + 0.005), math.sin(a2) * (r + 0.005), z + 0.24),
                                                              (0, 0, a2 / D2R + 90)), bevel=0.0)
    b.cyl(mats['marble'], r + 0.32, 0.14, 24, m=M((0, 0, z + h - 0.12)))
    for k in range(cols):
        a = 2 * math.pi * (k + 0.5) / cols
        b.sphere(mats['marble'], 0.12, m=M((math.cos(a) * (r + 0.3), math.sin(a) * (r + 0.3), z + h - 0.12), (0, 0, a / D2R), (0.4, 1.4, 0.7)),
                 sub=1)


def build(mats, cb, ck):
    rnd = random.Random(31)
    body = euro_body(cb, mats, R_ISL, 'rock_warm', seed=6.3, depth=5.0, chunky=0.55)
    embedded_rocks(body, mats, R_ISL, 'rock_warm', count=10, seed=8, zr=(-0.9, -3.0), size=(0.4, 0.8))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'cobble_warm', seed=3.3, drip=0.04, lip=0.25)
    for i in range(18):
        a = math.pi * 0.55 + rnd.uniform(0, math.pi * 0.85)
        rr = R_ISL * se_r(a) * rnd.uniform(0.97, 1.02)
        bush(top, mats, (math.cos(a) * rr, math.sin(a) * rr, -0.25), rnd.uniform(0.3, 0.55), seed=i, mt='leaves')
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 plaza wall + bench
    b = B('S01_PlazaWall')
    b.box(mats['curb'], (4.4, 0.45, 0.62), m=M((-4.4, -2.6, 0)), bevel=0.05)
    b.box(mats['curb'], (0.45, 1.8, 0.62), m=M((-2.4, -3.3, 0)), bevel=0.05)
    for (x, y, w, d) in ((-4.4, -2.6, 4.5, 0.55), (-2.4, -3.3, 0.55, 1.9)):
        b.box(mats['stone_step'], (w, d, 0.1), m=M((x, y, 0.62)), bevel=0.03)
    b.push(M((-4.4, -3.25, 0)))
    for x in (-0.75, 0.75):
        b.box(mats['iron'], (0.08, 0.45, 0.42), m=M((x, 0, 0)), bevel=0.01)
    for k in range(3):
        b.box(mats['wood'], (1.8, 0.13, 0.06), m=M((0, -0.15 + 0.15 * k, 0.42)), bevel=0.01)
    for k in range(2):
        b.box(mats['wood'], (1.8, 0.05, 0.12), m=M((0, 0.25, 0.62 + 0.18 * k), (-10, 0, 0)), bevel=0.01)
    b.pop()
    bld.append(b.build(ck))
    # small fountain on the open front of the plaza
    b = B('S01_Fountain', M((0.6, -4.2, 0)))
    b.cyl(mats['stone_step'], 1.25, 0.12, 20)
    b.lathe(mats['marble_dark'], [(1.05, 0.1), (1.15, 0.12), (1.12, 0.55), (0.98, 0.58), (0.92, 0.2), (0.0, 0.2)], seg=24, smooth=True)
    b.cyl(mats['water'], 0.95, 0.04, 20, m=M((0, 0, 0.42)))
    b.cyl(mats['marble'], 0.16, 1.0, 10, m=M((0, 0, 0.2)), smooth=True)
    b.lathe(mats['marble'], [(0.0, 0.0), (0.5, 0.0), (0.42, 0.12), (0.12, 0.16), (0.0, 0.16)], seg=16, m=M((0, 0, 1.15)), smooth=True)
    b.sphere(mats['marble'], 0.14, m=M((0, 0, 1.42)), sub=2)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 / S03 leaning tower
    b = B('S02_TowerBase')
    for k, (r, h) in enumerate(((2.3, 0.2), (2.05, 0.2))):
        b.cyl(mats['marble_dark'], r, h, 28, m=M((-3.4, 2.1, -0.1 + 0.2 * k)))
    b.push(LEAN)
    b.cyl(mats['marble'], 1.6, GROUND_H + 0.2, 24, m=M((0, 0, 0.0)), smooth=True)
    for k in range(14):
        a = 2 * math.pi * k / 14
        b.cyl(mats['marble'], 0.1, GROUND_H - 0.5, 6, m=M((math.cos(a) * 1.62, math.sin(a) * 1.62, 0.25)))
        a2 = a + math.pi / 14
        b.box(mats['marble_dark'], (0.42, 0.05, 1.4), m=M((math.cos(a2) * 1.6, math.sin(a2) * 1.6, 0.5), (0, 0, a2 / D2R + 90)), bevel=0.0)
    b.box(mats['wood_dark'], (0.7, 0.12, 1.3), m=M((0, -1.6, 0.25)), bevel=0.02)
    b.prism(mats['wood_dark'], [(math.cos(math.pi * k / 10) * 0.35, math.sin(math.pi * k / 10) * 0.35) for k in range(11)], 0.12,
            m=M((0, -1.6, 1.55)))
    for i in range(3):
        tower_tier(b, mats, 0.2 + GROUND_H + i * TIER_H)
    b.pop()
    bld.append(b.build(ck))

    b = B('S03_TowerTop', LEAN)
    for i in range(3, 6):
        tower_tier(b, mats, 0.2 + GROUND_H + i * TIER_H)
    zb = 0.2 + GROUND_H + 6 * TIER_H
    b.cyl(mats['marble_dark'], 1.55, 0.14, 24, m=M((0, 0, zb)))
    b.cyl(mats['marble'], 1.0, 1.15, 16, m=M((0, 0, zb + 0.14)), smooth=True)
    for k in range(10):
        a = 2 * math.pi * k / 10
        b.cyl(mats['marble'], 0.06, 0.95, 6, m=M((math.cos(a) * 1.12, math.sin(a) * 1.12, zb + 0.14)))
    b.cyl(mats['marble'], 1.22, 0.16, 20, m=M((0, 0, zb + 1.1)))
    b.cyl(mats['marble_dark'], 1.05, 0.18, 20, m=M((0, 0, zb + 1.26)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 pizzeria walls
    PX = M((2.4, 2.75, 0))
    W, D, H = 4.8, 3.8, 5.0
    FRONT = M((0, -D / 2, 0))
    RIGHT = M((W / 2, 0, 0), (0, 0, 90))
    b = B('S04_Pizzeria', PX)
    b.box(mats['curb'], (W + 0.1, D + 0.1, 0.22), bevel=0.03)
    b.box(mats['plaster_cream'], (W, D, H), bevel=0.04)
    b.box(mats['plaster_ochre'], (0.6, D - 0.4, H - 0.3), m=M((-W / 2 - 0.02, 0.2, 0.0)), bevel=0.02)
    b.box(mats['limestone_plain'], (W + 0.12, D + 0.12, 0.14), m=M((0, 0, 2.55)), bevel=0.03)
    for sx in (-1, 1):
        for sy in (-1, 1):
            for k in range(9):
                big = k % 2 == 0
                b.box(mats['limestone_plain'], (0.34 if big else 0.22, 0.34 if not big else 0.22, 0.42),
                      m=M((sx * (W / 2 - 0.1), sy * (D / 2 - 0.1), 0.25 + k * 0.52)), bevel=0.03)
    b.push(FRONT)
    door(b, mats, 0.9, 1.95, m=M((-1.4, 0, 0.22)), mt='wood', frame='limestone_plain', arch=True)
    b.box(mats['glass_lit'], (1.6, 0.05, 1.25), m=M((0.75, -0.03, 0.75)), bevel=0.0)
    b.cyl(mats['pizza'], 0.32, 0.04, 14, m=M((0.75, -0.08, 1.1), (90, 0, 0)))
    for (x, z, w, h) in ((0.75, 0.62, 1.84, 0.13), (0.75, 2.0, 1.84, 0.12)):
        b.box(mats['wood'], (w, 0.12, h), m=M((x, -0.06, z)), bevel=0.02)
    for x in (-0.12, 1.62, 0.75):
        b.box(mats['wood'], (0.1, 0.12, 1.4), m=M((x, -0.06, 0.66)), bevel=0.01)
    for wx in (-1.15, 1.0):
        window(b, mats, 0.72, 1.0, m=M((wx, 0, 3.62)), frame='wood_dark', shutters='sign_green', sill='limestone_plain',
               flowerbox='terracotta', fb_flower='flower_red' if wx < 0 else 'flower_white', mull=True)
    b.tube(mats['steel'], [(W / 2 - 0.35, -0.08, 0.2), (W / 2 - 0.35, -0.08, H + 0.1)], 0.06, seg=6)
    b.pop()
    b.push(RIGHT)
    window(b, mats, 0.72, 1.05, m=M((-0.6, 0, 3.25)), frame='wood_dark', shutters='sign_green', sill=None)
    balcony(b, mats, 1.6, 0.6, m=M((-0.6, 0, 3.1)), slab_mt='limestone_plain', h=0.65)
    window(b, mats, 0.7, 1.0, m=M((0.6, 0, 0.95)), frame='wood_dark', shutters='sign_green', sill='limestone_plain')
    b.pop()
    for k in range(3):
        pot_plant(b, mats, (W / 2 + 0.25, -0.6 - 0.45 + k * 0.45, 3.12), 0.12, flowers=('flower_red', 'flower_pink', 'flower_white')[k], seed=k)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 tile roof
    b = B('S05_TileRoof', PX)
    Wr, Dr, hr, z0 = W + 0.55, D + 0.55, 1.75, H
    b.box(mats['plaster_cream'], (W + 0.3, D + 0.3, 0.16), m=M((0, 0, z0 - 0.04)), bevel=0.04)
    frustum(b, mats['tile'], Wr, Dr, Wr * 0.42, 0.06, hr, z0=z0 + 0.1)
    ang = math.degrees(math.atan2(hr, Dr / 2))
    for s in (-1, 1):
        for k in range(6):
            t_ = (k + 0.5) / 6.5
            w = Wr - (Wr - Wr * 0.42) * t_ - 0.25
            y = s * Dr / 2 * (1 - t_)
            z = z0 + 0.1 + hr * t_
            b.box(mats['tile'], (w, 0.12, 0.08), m=M((0, y + s * 0.0, z + 0.05), (s * ang, 0, 0)), base=False, bevel=0.03)
    b.cyl(mats['tile'], 0.13, Wr * 0.42 + 0.2, 8, m=M((0, 0, z0 + 0.1 + hr), (0, 90, 0)), base=False, smooth=True)
    b.box(mats['plaster_ochre'], (0.55, 0.45, 1.1), m=M((1.2, 0.6, z0 + 1.1)), bevel=0.03)
    b.box(mats['tile'], (0.7, 0.6, 0.12), m=M((1.2, 0.6, z0 + 2.2)), bevel=0.03)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 awning + sign + menu board
    b = B('S06_Awning', PX)
    b.push(FRONT)
    aw_w, aw_d = 3.2, 1.05
    b.box(mats['awning'], (aw_w, aw_d, 0.06), m=M((-0.25, -aw_d / 2 * 0.94, 2.35), (-22, 0, 0)), base=False, bevel=0.0)
    for k in range(9):
        x = -0.25 - aw_w / 2 + (k + 0.5) * aw_w / 9
        b.cyl(mats['awning'], 0.19, 0.07, 3, m=M((x, -aw_d * 0.95, 2.13), (90, 0, 0)), base=False)
    b.box(mats['iron'], (aw_w + 0.05, 0.05, 0.05), m=M((-0.25, -aw_d * 0.94, 2.14)), bevel=0.0)
    b.box(mats['sign_green'], (3.0, 0.14, 0.56), m=M((-0.25, -0.08, 2.62)), bevel=0.04)
    b.box(mats['gold'], (3.1, 0.12, 0.06), m=M((-0.25, -0.08, 2.6)), bevel=0.01)
    b.box(mats['gold'], (3.1, 0.12, 0.06), m=M((-0.25, -0.08, 3.15)), bevel=0.01)
    text_mesh(b, mats['cream_paint'], 'PIZZERIA', 0.34, m=M((-0.25, -0.17, 2.9)), depth=0.025)
    b.pop()
    b.push(M((-0.45, -D / 2 - 1.4, 0), (0, 0, 15)))
    for s in (-1, 1):
        b.tube(mats['wood'], [(s * 0.28, -0.25, 0), (s * 0.22, 0.0, 1.1)], 0.03, seg=4)
        b.tube(mats['wood'], [(s * 0.28, 0.25, 0), (s * 0.22, 0.0, 1.1)], 0.03, seg=4)
    b.box(mats['wood'], (0.6, 0.06, 0.8), m=M((0, -0.12, 0.3), (-12, 0, 0)), bevel=0.02)
    b.box(mats['chalkboard'], (0.5, 0.04, 0.68), m=M((0, -0.15, 0.36), (-12, 0, 0)), bevel=0.0)
    b.pop()
    b.box(mats['cloth_red'], (0.9, 0.5, 0.03), m=M((-1.4, -D / 2 - 0.4, 0.01)), bevel=0.0)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 lamp post + string lights
    b = B('S07_Lights')
    street_lamp(b, mats, (-1.1, -0.9, 0.0), 3.4, mt='iron')
    top_tower = LEAN @ Vector((0.9, -1.4, 5.2))
    corner = Vector((2.4 - W / 2 + 0.05, 2.75 - D / 2 - 0.05, 4.4))
    string_lights(b, mats, tuple(top_tower), tuple(corner), sag=0.7, n=10)
    string_lights(b, mats, tuple(LEAN @ Vector((0.6, -1.5, 3.9))), (-1.1, -0.9, 3.25), sag=0.4, n=6)
    string_lights(b, mats, (-1.1, -0.9, 3.25), tuple(corner - Vector((0, 0, 1.0))), sag=0.35, n=6)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 scooter
    b = B('S08_Scooter')
    scooter(b, mats, (2.9, -1.35, 0.0), rot=25)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 cypress
    b = B('S09_Cypress')
    cypress(b, mats, (5.6, 4.6, 0.0), h=4.6, r=0.75, seed=2, pot=True)
    bld.append(b.build(ck))
    b = B('S09_CypressSmall')
    cypress(b, mats, (-6.1, 4.0, 0.0), h=3.6, r=0.6, seed=5)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 planters
    for i, (x, y, r, kind, fl) in enumerate([(5.4, 1.4, 0.42, 'olive', None), (4.7, -0.6, 0.3, 'herb', None),
                                             (5.5, -0.3, 0.26, 'bush', 'flower_red'), (0.1, 0.25, 0.26, 'bush', 'flower_red'),
                                             (-1.5, 0.6, 0.3, 'herb', None), (-6.2, 0.6, 0.32, 'bush', 'flower_pink'),
                                             (-5.4, -1.6, 0.3, 'ball', None)]):
        b = B(f'S10_Planter_{i}')
        pot_plant(b, mats, (x, y, 0.0), r, kind=kind, flowers=fl, seed=i)
        bld.append(b.build(ck))
    return base, bld
