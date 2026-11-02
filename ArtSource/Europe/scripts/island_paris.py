# Island: France / Paris café with the Eiffel Tower (ref: Downloads/Evropa/photo_2026-04-07_18-22-32.jpg)
NAME = 'ParisCafe'
R_ISL = 8.6


def rot_pts(pts, c, ang):
    ca, sa = math.cos(ang * D2R), math.sin(ang * D2R)
    return [(c[0] + (x - c[0]) * ca - (y - c[1]) * sa, c[1] + (x - c[0]) * sa + (y - c[1]) * ca) for (x, y) in pts]


def eiffel_hw(z, H, base=2.15):
    t = min(1.0, z / (H * 0.9))
    return base * (1 - t) ** 2.4 + 0.17


def eiffel_face_braces(b, mats, H, z_a, z_b, n, r=0.045):
    for k in range(n):
        za = z_a + (z_b - z_a) * k / n
        zb = z_a + (z_b - z_a) * (k + 1) / n
        ha, hb = eiffel_hw(za, H), eiffel_hw(zb, H)
        b.tube(mats['eiffel_dark'], [(-ha, -ha, za), (hb, -hb, zb)], r, seg=4)
        b.tube(mats['eiffel_dark'], [(ha, -ha, za), (-hb, -hb, zb)], r, seg=4)
        b.tube(mats['eiffel'], [(-hb, -hb, zb), (hb, -hb, zb)], r * 1.3, seg=4)


def eiffel_legs(b, mats, H, z_a, z_b, n=7):
    for sx in (-1, 1):
        for sy in (-1, 1):
            pts, rad = [], []
            for k in range(n):
                z = z_a + (z_b - z_a) * k / (n - 1)
                hw = eiffel_hw(z, H)
                pts.append((sx * hw, sy * hw, z))
                rad.append(0.1 + 0.2 * (1 - z / H) ** 1.5)
            b.tube(mats['eiffel'], pts, rad, seg=4, smooth=False)


def eiffel_platform(b, mats, H, z, extra=0.55, th=0.26):
    hw = eiffel_hw(z, H) + extra
    b.box(mats['eiffel'], (hw * 2, hw * 2, th), m=M((0, 0, z - th / 2)), bevel=0.04)
    b.box(mats['eiffel_dark'], (hw * 2 + 0.12, hw * 2 + 0.12, 0.08), m=M((0, 0, z - th / 2 - 0.04)), bevel=0.02)
    for r_ in range(4):
        b.push(M((0, 0, 0), (0, 0, 90 * r_)))
        b.box(mats['eiffel_dark'], (hw * 2, 0.06, 0.32), m=M((0, -hw + 0.03, z + th / 2)), bevel=0.0)
        b.pop()


def build(mats, cb, ck):
    rnd = random.Random(11)
    # ---------------------------------------------------------- island base
    body = euro_body(cb, mats, R_ISL, 'dirt', seed=2.2, depth=5.0, chunky=0.4)
    cliff_roots(body, mats, R_ISL, count=16, seed=3)
    embedded_rocks(body, mats, R_ISL, 'boulder', count=12, seed=4, zr=(-0.9, -2.8))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'grass', seed=1.4, drip=0.22)
    for (x, y, s) in ((-6.7, -3.4, 0.55), (-6.1, -4.1, 0.38), (-5.6, -3.2, 0.3), (6.6, -2.2, 0.4)):
        rock(top, mats['boulder'], (x, y, 0.05), (s, s * 0.85, s * 0.7), (0, 0, x * 20), seed=x)
    for i in range(26):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.9, 0.98)
        top.cone(mats['grass_dark'], 0.12, rnd.uniform(0.18, 0.3), 4, m=M((math.cos(a) * rr, math.sin(a) * rr, -0.02)))
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    HX = M((1.8, 3.4, 0))
    W, D = 5.2, 4.4
    FRONT = M((0, -D / 2, 0))
    RIGHT = M((W / 2, 0, 0), (0, 0, 90))

    # ---------------------------------------------------------- S01 cobbled paths
    b = B('S01_Paths')
    slab(b, mats['cobble'], rrect(-4.2, 0.9, 5.8, 5.8, 0.9), -0.05, 0.1)
    slab(b, mats['cobble'], rrect(1.9, 0.25, 6.8, 2.3, 0.5), -0.05, 0.1)
    slab(b, mats['cobble'], rrect(5.3, 3.3, 1.9, 6.0, 0.5), -0.05, 0.1)
    slab(b, mats['cobble'], rot_pts(rrect(3.9, -3.3, 1.7, 5.6, 0.5), (3.9, -3.3), 45), -0.05, 0.1)
    slab(b, mats['cobble'], rot_pts(rrect(-2.2, -3.2, 1.3, 4.0, 0.45), (-2.2, -3.2), -40), -0.05, 0.1)
    for (cx, cy, w, d) in ((-4.2, 0.9, 6.1, 6.1),):
        slab(b, mats['curb'], rrect(cx, cy, w, d, 1.0), -0.06, 0.08)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 café ground floor
    b = B('S02_Cafe', HX)
    b.box(mats['curb'], (W + 0.12, D + 0.12, 0.25), bevel=0.03)
    b.box(mats['limestone'], (W, D, 2.7), bevel=0.02)
    for z in (0.75, 1.2, 1.65, 2.1):
        b.box(mats['limestone_plain'], (W + 0.05, D + 0.05, 0.05), m=M((0, 0, z)), bevel=0.0)
    b.push(FRONT)
    b.box(mats['wood_red'], (3.3, 0.14, 2.45), m=M((-0.7, -0.05, 0.12)), bevel=0.03)
    for wx in (-1.78, 0.38):
        b.box(mats['glass_lit'], (0.92, 0.05, 1.3), m=M((wx, -0.13, 0.62)), bevel=0.0)
        b.box(mats['wood_red'], (0.05, 0.06, 1.3), m=M((wx, -0.16, 0.62)), bevel=0.0)
        b.box(mats['wood_red'], (0.92, 0.06, 0.05), m=M((wx, -0.16, 1.2)), bevel=0.0)
        b.box(mats['wood_dark'], (1.05, 0.2, 0.08), m=M((wx, -0.17, 0.54)), bevel=0.02)
    b.box(mats['glass_lit'], (0.78, 0.05, 1.05), m=M((-0.7, -0.13, 1.0)), bevel=0.0)
    b.box(mats['wood_dark'], (0.86, 0.08, 1.0), m=M((-0.7, -0.14, 0.0)), bevel=0.02)
    b.sphere(mats['gold'], 0.045, m=M((-0.42, -0.2, 1.0)), sub=1)
    b.box(mats['wood_red'], (3.4, 0.2, 0.42), m=M((-0.7, -0.12, 2.2)), bevel=0.04)
    text_mesh(b, mats['gold'], 'CAFÉ', 0.34, m=M((-0.7, -0.24, 2.42)), depth=0.025)
    window(b, mats, 0.78, 1.45, m=M((1.75, 0, 0.8)), pediment='limestone_plain')
    b.box(mats['blue'], (0.5, 0.04, 0.3), m=M((2.25, -0.06, 2.05)), bevel=0.01)
    b.box(mats['frame_white'], (0.54, 0.03, 0.34), m=M((2.25, -0.03, 2.03)), bevel=0.0)
    b.pop()
    b.push(RIGHT)
    for wx in (-1.2, 1.0):
        window(b, mats, 0.78, 1.45, m=M((wx, 0, 0.8)), pediment='limestone_plain')
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S03 upper floors with balconies
    b = B('S03_UpperFloors', HX)
    b.box(mats['limestone'], (W, D, 4.7), m=M((0, 0, 2.7)), bevel=0.02)
    for z in (2.7, 5.05):
        b.box(mats['limestone_plain'], (W + 0.14, D + 0.14, 0.16), m=M((0, 0, z)), bevel=0.03)
    b.box(mats['limestone_plain'], (W + 0.36, D + 0.36, 0.3), m=M((0, 0, 7.38)), bevel=0.07)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(mats['limestone_plain'], (0.2, 0.2, 4.6), m=M((sx * (W / 2 - 0.06), sy * (D / 2 - 0.06), 2.8)), bevel=0.02)
    b.push(FRONT)
    for wx in (-1.65, 0.0, 1.65):
        for wz in (3.15, 5.5):
            window(b, mats, 0.78, 1.5, m=M((wx, 0, wz)), pediment='limestone_plain', sill=None)
        balcony(b, mats, 1.15, 0.38, m=M((wx, 0, 5.45)), h=0.62)
    balcony(b, mats, W - 0.2, 0.55, m=M((0, 0, 3.1)), h=0.7)
    b.pop()
    b.push(RIGHT)
    for wx in (-1.3, 0.0, 1.3):
        for wz in (3.15, 5.5):
            window(b, mats, 0.72, 1.5, m=M((wx, 0, wz)), pediment='limestone_plain', sill=None)
        balcony(b, mats, 1.05, 0.36, m=M((wx, 0, 3.1)), h=0.62)
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 mansard roof
    b = B('S04_MansardRoof', HX)
    z0 = 7.66
    b.box(mats['zinc_dark'], (W + 0.24, D + 0.24, 0.1), m=M((0, 0, z0 - 0.02)), bevel=0.02)
    frustum(b, mats['zinc'], W + 0.12, D + 0.12, W - 1.3, D - 1.3, 2.2, z0=z0)
    frustum(b, mats['zinc_dark'], W - 1.28, D - 1.28, W - 1.7, D - 1.7, 0.32, z0=z0 + 2.2)
    for (face, xs) in ((FRONT, (-1.65, 0.0, 1.65)), (RIGHT, (-1.25, 1.25))):
        b.push(face)
        for wx in xs:
            b.box(mats['frame_white'], (0.86, 1.0, 1.0), m=M((wx, 0.42, z0 + 0.3)), bevel=0.03)
            b.push(M((wx, 0.42, z0 + 1.3), (90, 0, 0)))
            b.cyl(mats['zinc_dark'], 0.5, 1.08, 10, base=False)
            b.pop()
            window(b, mats, 0.5, 0.62, m=M((wx, -0.09, z0 + 0.45)), arch=True, sill=None, mull=True)
            for s in (-1, 1):
                b.box(mats['limestone_plain'], (0.1, 0.1, 1.0), m=M((wx + s * 0.4, -0.1, z0 + 0.3)), bevel=0.02)
        b.pop()
    for cx in (-1.25, 1.25):
        b.box(mats['limestone_plain'], (0.8, 0.45, 1.25), m=M((cx, 0.55, z0 + 1.9)), bevel=0.03)
        b.box(mats['limestone_plain'], (0.92, 0.56, 0.1), m=M((cx, 0.55, z0 + 3.12)), bevel=0.02)
        for k in range(3):
            b.cyl(mats['terracotta'], 0.08, 0.3, 8, r2=0.065, m=M((cx - 0.24 + 0.24 * k, 0.55, z0 + 3.2)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 / S06 Eiffel tower
    H = 12.2
    EX = M((-4.2, 0.9, 0))
    z1, z2, z3 = H * 0.2, H * 0.46, H * 0.82
    b = B('S05_TowerBase', EX)
    for sx in (-1, 1):
        for sy in (-1, 1):
            hw = eiffel_hw(0, H)
            b.box(mats['curb'], (0.85, 0.85, 0.35), m=M((sx * hw, sy * hw, -0.05)), bevel=0.05)
    eiffel_legs(b, mats, H, 0.0, z2 + 0.15)
    for r_ in range(4):
        b.push(M((0, 0, 0), (0, 0, 90 * r_)))
        aw, ah, az0 = eiffel_hw(0.9, H) * 0.86, z1 - 1.25, 0.75
        pts = []
        for k in range(13):
            ang = math.pi * k / 12
            z = az0 + math.sin(ang) * ah
            pts.append((math.cos(ang) * aw, -eiffel_hw(z, H) - 0.02, z))
        b.tube(mats['eiffel_dark'], pts, 0.08, seg=4)
        eiffel_face_braces(b, mats, H, z1, z2, 3)
        b.pop()
    eiffel_platform(b, mats, H, z1, extra=0.6)
    eiffel_platform(b, mats, H, z2, extra=0.4, th=0.22)
    bld.append(b.build(ck))

    b = B('S06_TowerTop', EX)
    eiffel_legs(b, mats, H, z2 + 0.1, z3 + 0.4, n=6)
    for r_ in range(4):
        b.push(M((0, 0, 0), (0, 0, 90 * r_)))
        eiffel_face_braces(b, mats, H, z2 + 0.1, z3, 5, r=0.035)
        b.pop()
    b.box(mats['eiffel'], (0.75, 0.75, 0.2), m=M((0, 0, z3)), bevel=0.03)
    b.box(mats['eiffel_dark'], (0.55, 0.55, 0.5), m=M((0, 0, z3 + 0.2)), bevel=0.03)
    b.cyl(mats['eiffel'], 0.2, H - z3 - 0.2, 6, r2=0.08, m=M((0, 0, z3 + 0.7)))
    b.cyl(mats['steel'], 0.035, 1.7, 5, m=M((0, 0, H + 0.4)))
    flag3(b, mats, ('cloth_blue', 'cloth_white', 'cloth_red'), 1.05, 0.68, m=M((0.03, 0, H + 2.05)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 café terrace
    b = B('S07_Terrace')
    bistro_table(b, mats, (1.05, -0.15, 0.05))
    bistro_chair(b, mats, (0.4, -0.15, 0.05), rot=90)
    bistro_chair(b, mats, (1.7, -0.25, 0.05), rot=-95)
    bistro_table(b, mats, (3.2, 0.05, 0.05), r=0.36)
    bistro_chair(b, mats, (3.2, -0.6, 0.05), rot=180)
    pot_plant(b, mats, (-0.55, 0.85, 0.05), 0.24, kind='ball', seed=1)
    pot_plant(b, mats, (2.85, 0.85, 0.05), 0.24, kind='ball', seed=2)
    b.push(M((4.25, 0.3, 0.05), (0, 0, -20)))
    for s in (-1, 1):
        b.tube(mats['wood'], [(s * 0.28, -0.25, 0), (s * 0.22, 0.0, 1.1)], 0.03, seg=4)
    b.tube(mats['wood'], [(0, 0.3, 0), (0, 0.02, 1.05)], 0.03, seg=4)
    b.box(mats['chalkboard'], (0.52, 0.04, 0.7), m=M((0, -0.12, 0.35), (-12, 0, 0)), bevel=0.0)
    b.box(mats['wood'], (0.6, 0.06, 0.8), m=M((0, -0.1, 0.3), (-12, 0, 0)), bevel=0.02)
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 street lamps
    for i, (loc, h, dbl) in enumerate([((6.0, 5.6, 0.05), 4.4, True), ((-1.2, -2.4, 0.05), 3.1, False),
                                      ((-6.5, -1.9, 0.05), 3.1, False), ((2.0, -5.3, 0.05), 3.1, False)]):
        b = B(f'S08_Lamp_{i}')
        street_lamp(b, mats, loc, h, double=dbl, rot=45)
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 bicycle
    b = B('S09_Bicycle')
    bicycle(b, mats, (4.55, -0.15, 0.05), rot=78)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 trees and bushes
    for i, (x, y, h, r) in enumerate([(-1.0, 6.3, 4.6, 1.55), (-6.0, 5.0, 4.1, 1.35), (5.7, 7.0, 3.6, 1.15),
                                      (-6.8, 2.6, 3.4, 1.1)]):
        b = B(f'S10_Tree_{i}')
        round_tree(b, mats, (x, y, 0.0), h, r, seed=i + 3)
        bld.append(b.build(ck))
    for i, (x, y, h) in enumerate([(-1.3, -1.6, 1.4), (-2.4, -1.9, 1.0), (-7.0, -0.4, 1.2)]):
        b = B(f'S10_Topiary_{i}')
        topiary_cone(b, mats, (x, y, 0.0), h, h * 0.3)
        bld.append(b.build(ck))
    for i, (x, y, r, fl) in enumerate([(-0.3, -2.5, 0.55, None), (-5.2, -5.2, 0.6, 'flower_red'),
                                       (6.4, 0.2, 0.55, 'flower_pink'), (2.6, -6.6, 0.5, None)]):
        b = B(f'S10_Bush_{i}')
        bush(b, mats, (x, y, 0.0), r, seed=i * 3, flowers=fl)
        bld.append(b.build(ck))
    b = B('S10_FlowerBed')
    bed = rot_pts(rrect(-2.6, -5.4, 3.6, 1.5, 0.7), (-2.6, -5.4), 45)
    slab(b, mats['curb'], bed, -0.02, 0.22)
    slab(b, mats['dirt'], rot_pts(rrect(-2.6, -5.4, 3.3, 1.2, 0.55), (-2.6, -5.4), 45), 0.0, 0.26)
    for k in range(9):
        t_ = (k + 0.5) / 9 - 0.5
        x, y = -2.6 + t_ * 3.0 * math.cos(45 * D2R), -5.4 + t_ * 3.0 * math.sin(45 * D2R)
        fl = ('flower_red', 'flower_pink', 'flower_yellow', 'flower_white')[k % 4]
        bush(b, mats, (x, y, 0.2), 0.3, seed=40 + k, mt='leaves', flowers=fl)
    bld.append(b.build(ck))
    return base, bld
