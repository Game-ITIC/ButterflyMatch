# Island: Greece / Santorini chapel (ref: Downloads/Evropa/photo_2026-04-07_18-22-33.jpg)
NAME = 'SantoriniChapel'
R_ISL = 7.4
DEPTH = 5.4


def cliff_f(z):
    """Approximate cliff radius factor of euro_body at height z (no noise)."""
    prof = [(1.0, -0.1), (0.995, -0.85), (1.0, -1.6), (0.955, -2.4), (0.86, -3.15), (0.67, -3.8)]
    k = DEPTH / 4.55
    for (f0, z0), (f1, z1) in zip(prof, prof[1:]):
        z0, z1 = z0 * k, z1 * k
        if z1 <= z <= z0:
            return f0 + (f1 - f0) * (z - z0) / (z1 - z0)
    return 1.0


def build(mats, cb, ck):
    rnd = random.Random(21)
    body = euro_body(cb, mats, R_ISL, 'rock_white', seed=4.1, depth=DEPTH, chunky=0.75)
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'gravel_white', seed=2.6, drip=0.05, lip=0.22)
    for i in range(14):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.86, 0.95)
        rock(top, mats['rock_white'], (math.cos(a) * rr, math.sin(a) * rr, 0.0), (0.3, 0.26, 0.18), (0, 0, a * 50), seed=i)
    for (x, y, r) in ((-5.9, 5.2, 0.45), (6.0, 3.6, 0.4), (-2.4, 6.4, 0.35), (5.4, -5.0, 0.35), (-4.6, -5.6, 0.3)):
        bush(top, mats, (x, y, 0.0), r, seed=int(x * 10), mt='olive_leaf')
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    CX = M((1.9, 2.8, 0))
    W, D, H, P = 5.0, 4.2, 3.7, 0.4
    FRONT = M((0, -D / 2, 0))
    RIGHT = M((W / 2, 0, 0), (0, 0, 90))
    TX0, TX1, TY0, TY1, TZ = -6.3, -0.9, -3.4, 3.8, 0.5
    tcx, tcy, tw, td = (TX0 + TX1) / 2, (TY0 + TY1) / 2, TX1 - TX0, TY1 - TY0

    # ---------------------------------------------------------- S01 terrace
    b = B('S01_Terrace')
    slab(b, mats['blue'], rrect(tcx, tcy, tw + 0.08, td + 0.08, 0.35), -0.02, 0.16)
    slab(b, mats['whitewash'], rrect(tcx, tcy, tw, td, 0.32), 0.0, TZ)
    slab(b, mats['flagstone'], rrect(tcx, tcy, tw - 0.5, td - 0.5, 0.2), TZ - 0.02, 0.05)
    b.box(mats['whitewash'], (1.9, 0.3, 0.75), m=M((TX0 + 0.95, TY0 + 0.15, TZ)), bevel=0.08)
    b.box(mats['whitewash'], (1.5, 0.3, 0.75), m=M((TX1 - 0.75, TY0 + 0.15, TZ)), bevel=0.08)
    rail(b, mats, tw - 3.4, 0.62, m=M((tcx + 0.2, TY0 + 0.15, TZ)), step=0.2)
    b.box(mats['whitewash'], (0.3, td, 0.75), m=M((TX0 + 0.15, tcy, TZ)), bevel=0.08)
    b.box(mats['whitewash'], (tw, 0.3, 0.75), m=M((tcx, TY1 - 0.15, TZ)), bevel=0.08)
    for (x, y) in ((TX0 + 0.15, TY0 + 0.15), (TX1 - 0.15, TY0 + 0.15)):
        b.box(mats['whitewash'], (0.42, 0.42, 1.0), m=M((x, y, TZ)), bevel=0.08)
    steps(b, mats['whitewash'], 3, 1.2, 0.32, TZ / 3, m=M((TX1 + 0.98, -1.2, 0), (0, 0, 90)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 chapel
    b = B('S02_Chapel', CX)
    b.box(mats['whitewash'], (W + 0.5, D + 0.5, P), bevel=0.08)
    b.box(mats['blue'], (W + 0.56, D + 0.56, 0.1), bevel=0.02)
    b.box(mats['whitewash'], (W, D, H), m=M((0, 0, P)), bevel=0.12)
    b.box(mats['whitewash'], (W + 0.14, D + 0.14, 0.22), m=M((0, 0, P + H - 0.05)), bevel=0.08)
    b.push(FRONT)
    door(b, mats, 0.92, 1.55, m=M((-0.25, 0, P)), mt='blue', frame='whitewash', arch=True)
    window(b, mats, 0.55, 0.72, m=M((1.35, 0, P + 1.2)), frame='whitewash', glass='glass', shutters='blue', sill='whitewash')
    b.box(mats['iron'], (0.16, 0.16, 0.26), m=M((0.55, -0.12, P + 1.65)), bevel=0.02)
    b.box(mats['lamp_glow'], (0.11, 0.11, 0.18), m=M((0.55, -0.12, P + 1.69)), bevel=0.0)
    b.pop()
    b.push(RIGHT)
    window(b, mats, 0.55, 0.72, m=M((-0.95, 0, P + 1.2)), frame='whitewash', glass='glass', shutters='blue', sill='whitewash')
    door(b, mats, 0.78, 1.45, m=M((0.75, 0, P + 0.2)), mt='blue', frame='whitewash')
    b.pop()
    steps(b, mats['whitewash'], 3, 1.5, 0.3, P / 3, m=M((-0.25, -D / 2 - 0.95, 0)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S03 dome
    b = B('S03_Dome', CX)
    zt = P + H + 0.17
    b.cyl(mats['whitewash'], 1.32, 0.5, 24, m=M((-0.3, 0.15, zt)), smooth=True)
    b.cyl(mats['whitewash'], 1.4, 0.1, 24, m=M((-0.3, 0.15, zt + 0.48)), smooth=True)
    prof = [(1.34, 0), (1.3, 0.35), (1.18, 0.7), (0.98, 0.98), (0.7, 1.2), (0.38, 1.33), (0.0, 1.37)]
    b.lathe(mats['blue_dome'], prof, seg=28, m=M((-0.3, 0.15, zt + 0.56)), smooth=True)
    b.cyl(mats['whitewash'], 0.14, 0.18, 10, m=M((-0.3, 0.15, zt + 1.9)))
    b.box(mats['whitewash'], (0.09, 0.09, 0.7), m=M((-0.3, 0.15, zt + 2.05)), bevel=0.01)
    b.box(mats['whitewash'], (0.42, 0.09, 0.09), m=M((-0.3, 0.15, zt + 2.5)), bevel=0.01)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 bell tower
    b = B('S04_BellTower', CX)
    bx, by, bz = 1.35, -D / 2 + 0.3, P + H + 0.15
    for s in (-1, 1):
        b.box(mats['whitewash'], (0.32, 0.5, 1.05), m=M((bx + s * 0.48, by, bz)), bevel=0.06)
    b.box(mats['whitewash'], (1.3, 0.5, 0.42), m=M((bx, by, bz + 1.05)), bevel=0.06)
    b.prism(mats['whitewash'], [(-0.65, 0), (0.65, 0), (0.0, 0.5)], 0.5, m=M((bx, by, bz + 1.47)))
    b.lathe(mats['bronze'], [(0.0, 0.0), (0.24, 0.0), (0.22, 0.08), (0.15, 0.3), (0.13, 0.45), (0.0, 0.5)], seg=12,
            m=M((bx, by, bz + 0.35)))
    b.cyl(mats['iron'], 0.02, 0.3, 4, m=M((bx, by, bz + 0.82)))
    b.box(mats['whitewash'], (0.08, 0.08, 0.6), m=M((bx, by, bz + 1.9)), bevel=0.01)
    b.box(mats['whitewash'], (0.36, 0.08, 0.08), m=M((bx, by, bz + 2.28)), bevel=0.01)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 cliff stairs
    b = B('S05_CliffStairs')
    b.box(mats['stone_step'], (1.4, 1.2, 0.12), m=M((-1.0, -(R_ISL - 0.6), -0.04)), bevel=0.04)
    for k in range(8):
        z = -0.32 - 0.34 * k
        x = -0.15 + 0.5 * k
        y = -(R_ISL * cliff_f(z)) + 0.05
        b.box(mats['stone_step'], (0.55, 1.15, 0.26), m=M((x, y - 0.25, z)), bevel=0.05)
    # stepping stones from the stairs to the chapel door
    p0, p1 = Vector((-0.9, -(R_ISL - 1.5), 0)), Vector((1.6, -0.6, 0))
    for k in range(7):
        p = p0.lerp(p1, (k + 0.5) / 7)
        b.box(mats['stone_step'], (0.75 + 0.1 * (k % 2), 0.6, 0.08), m=M((p.x + 0.12 * math.sin(k * 2.0), p.y, -0.03), (0, 0, 30 + 9 * k)),
              bevel=0.03)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 pergola with vines
    b = B('S06_Pergola')
    px0, px1, py0, py1 = TX0 + 0.45, TX1 - 0.45, TY0 + 0.55, TY1 - 0.5
    ph = 2.6
    for (x, y) in ((px0, py0), (px1, py0), (px0, py1), (px1, py1), ((px0 + px1) / 2, py0)):
        b.box(mats['wood_v'], (0.2, 0.2, ph), m=M((x, y, TZ)), bevel=0.03)
    for y in (py0, py1):
        b.box(mats['wood'], (px1 - px0 + 0.6, 0.18, 0.22), m=M(((px0 + px1) / 2, y, TZ + ph)), bevel=0.03)
    n = 8
    for k in range(n + 1):
        x = px0 + (px1 - px0) * k / n
        b.box(mats['wood'], (0.14, py1 - py0 + 0.7, 0.16), m=M((x, (py0 + py1) / 2, TZ + ph + 0.22)), bevel=0.02)
    for i in range(14):
        x = px0 + (px1 - px0) * rnd.uniform(0.0, 1.0)
        y = py0 + (py1 - py0) * rnd.uniform(0.25, 1.0)
        blob_cluster(b, mats['leaves'], (x, y, TZ + ph + 0.45), rnd.uniform(0.55, 0.8), n=4, seed=i, flat=0.45, spread=0.8, sub=1)
    for i in range(6):
        x = px0 + (px1 - px0) * rnd.uniform(0.0, 1.0)
        blob_cluster(b, mats['leaves_light'], (x, py0 + rnd.uniform(0, 0.6), TZ + ph + 0.3), 0.5, n=3, seed=i + 30, flat=0.5, sub=1)
        b.sphere(mats['grape'], 0.13, m=M((x + 0.2, py0 + 0.2, TZ + ph - 0.1), (0, 0, 0), (1, 1, 1.5)), sub=1, nz=0.03, nf=12)
    for (x, y) in ((px0, py0), (px1, py0), ((px0 + px1) / 2, py0)):
        pts = [(x + 0.13 * math.cos(t * 1.7), y - 0.13 * math.sin(t * 1.7) - 0.02, TZ + t * ph / 6) for t in range(7)]
        b.tube(mats['bark'], pts, 0.05, seg=4)
        for k in range(3):
            b.sphere(mats['leaves'], 0.22, m=M((x, y - 0.12, TZ + 0.6 + k * 0.7), (0, 0, 0), (1, 0.7, 1.2)), sub=1, nz=0.04, nf=6)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 dining
    b = B('S07_Dining')
    tx, ty = tcx + 0.2, tcy + 0.3
    b.box(mats['wood'], (1.7, 0.95, 0.08), m=M((tx, ty, TZ + 0.72)), bevel=0.02)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(mats['wood_v'], (0.08, 0.08, 0.72), m=M((tx + sx * 0.72, ty + sy * 0.38, TZ)), bevel=0.01)
    for (x, y, r_) in ((tx - 0.45, ty - 0.75, 0), (tx + 0.45, ty - 0.75, 0), (tx - 0.45, ty + 0.75, 180), (tx + 0.45, ty + 0.75, 180),
                       (tx - 1.15, ty, 90)):
        greek_chair(b, mats, (x, y, TZ), rot=r_ + 180)
    for (x, y) in ((tx - 0.4, ty), (tx + 0.4, ty)):
        b.cyl(mats['cloth_white'], 0.16, 0.02, 12, m=M((x, y, TZ + 0.8)))
    b.lathe(mats['terracotta'], [(0.0, 0), (0.07, 0), (0.1, 0.12), (0.05, 0.25), (0.06, 0.3), (0.0, 0.3)], seg=10, m=M((tx, ty, TZ + 0.8)))
    for (x, y) in ((TX0 + 0.15, TY0 + 0.15), (TX1 - 0.15, TY0 + 0.15)):
        b.box(mats['iron'], (0.22, 0.22, 0.34), m=M((x, y, TZ + 1.0)), bevel=0.02)
        b.box(mats['lamp_glow'], (0.16, 0.16, 0.26), m=M((x, y, TZ + 1.04)), bevel=0.0)
        b.cone(mats['iron'], 0.18, 0.14, 4, m=M((x, y, TZ + 1.34), (0, 0, 45)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 balcony (right side of the chapel)
    b = B('S08_Balcony')
    bx0, bx1, by0, by1, bzz = 4.55, 5.75, 2.4, 4.4, 0.62
    b.box(mats['wood'], (bx1 - bx0, by1 - by0, 0.12), m=M(((bx0 + bx1) / 2, (by0 + by1) / 2, bzz)), bevel=0.02)
    for (x, y) in ((bx1 - 0.08, by0 + 0.08), (bx1 - 0.08, by1 - 0.08), (bx0 + 0.15, by0 + 0.08)):
        b.box(mats['wood_v'], (0.12, 0.12, bzz), m=M((x, y, 0)), bevel=0.02)
    rail(b, mats, by1 - by0, 0.7, m=M((bx1 - 0.04, (by0 + by1) / 2, bzz + 0.12), (0, 0, 90)), mt='blue', step=0.22, ornate=False, r=0.03)
    rail(b, mats, bx1 - bx0, 0.7, m=M(((bx0 + bx1) / 2, by0 + 0.04, bzz + 0.12)), mt='blue', step=0.22, ornate=False, r=0.03)
    pot_plant(b, mats, (bx1 - 0.35, by1 - 0.4, bzz + 0.12), 0.18, flowers='flower_red', seed=5)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 bougainvillea + flower pots
    for i, (x, y, top_z, d) in enumerate([(-0.75, 0.55, 4.4, (0.0, -0.25)), (4.55, 0.55, 4.1, (0.25, -0.2))]):
        b = B(f'S09_Bougainvillea_{i}')
        pts = [(x + d[0] * math.sin(k * 1.3) * 0.5, y + d[1] * 0.4, k * top_z / 6) for k in range(7)]
        b.tube(mats['bark'], pts, [0.08, 0.07, 0.06, 0.05, 0.05, 0.04, 0.03], seg=5)
        for k in range(9):
            z = 0.9 + k * (top_z - 0.6) / 9
            ox = rnd.uniform(-0.45, 0.45)
            blob_cluster(b, mats['leaves'], (x + ox, y + d[1], z), 0.26, n=3, seed=k + i * 10, flat=0.9, spread=0.6, sub=1)
            for j in range(3):
                b.sphere(mats['flower_pink'], rnd.uniform(0.12, 0.17),
                         m=M((x + ox + rnd.uniform(-0.3, 0.3), y + d[1] - 0.12, z + rnd.uniform(-0.15, 0.2))), sub=1, nz=0.03, nf=10)
        bld.append(b.build(ck))
    for i, (x, y, r, fl) in enumerate([(0.4, -0.3, 0.26, 'flower_pink'), (-0.6, -3.75, 0.3, 'flower_pink'),
                                       (3.2, -0.45, 0.22, 'flower_red')]):
        b = B(f'S09_Pot_{i}')
        pot_plant(b, mats, (x, y, 0.0), r, flowers=fl, seed=i)
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 olive tree + amphora
    b = B('S10_OliveTree')
    for k in range(10):
        a = 2 * math.pi * k / 10
        rock(b, mats['stone_step'], (5.0 + math.cos(a) * 0.95, -1.6 + math.sin(a) * 0.95, 0.05), (0.22, 0.18, 0.14), seed=k)
    olive_tree(b, mats, (5.0, -1.6, 0.0), h=3.8, r=1.8, seed=4)
    bld.append(b.build(ck))
    b = B('S10_Amphora')
    amphora(b, mats, (3.0, -2.7, 0.0), h=1.35, r=0.5)
    bld.append(b.build(ck))
    return base, bld
