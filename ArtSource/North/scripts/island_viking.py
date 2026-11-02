# Island 1: Viking camp (ref: photo_2026-03-18_18-22-49.jpg)
NAME = 'VikingCamp'


def axe(b, mats, flip=1):
    b.tube(mats['wood_dark'], [(0, 0, -0.9), (0, 0, 0.85)], 0.055, seg=6)
    blade = [(0.0, -0.12), (0.18, -0.16), (0.45, -0.38), (0.6, -0.22), (0.63, 0.1), (0.58, 0.38), (0.42, 0.52),
             (0.18, 0.26), (0.0, 0.18)]
    b.prism(mats['steel'], [(flip * x, z) for (x, z) in blade], 0.07, m=M((0, 0, 0.55)))
    b.box(mats['iron'], (0.16, 0.12, 0.34), m=M((0, 0, 0.45)), bevel=0.02)


def tent(b, mats, loc, rot, cloth=False):
    b.push(M(loc, (0, 0, rot)))
    b.prism(mats['canvas'], [(-1.05, 0), (1.05, 0), (0.0, 1.6)], 2.3)
    b.prism(mats['emblem'], [(-0.42, 0), (0.42, 0), (0.0, 1.05)], 0.04, m=M((0, -1.16, 0)))
    for y in (-1.2, 1.2):
        b.tube(mats['log'], [(-1.12, y, 0), (0.32, y, 1.95)], 0.06, seg=6)
        b.tube(mats['log'], [(1.12, y, 0), (-0.32, y, 1.95)], 0.06, seg=6)
    b.sphere(mats['snow'], 1.0, m=M((0, 0, 1.55), (0, 0, 0), (0.28, 1.3, 0.2)), sub=2, nz=0.05, nf=3)
    for s in (-1, 1):
        b.sphere(mats['snow'], 1.0, m=M((s * 0.5, 0, 0.85), (0, s * 33, 0), (0.12, 1.1, 0.55)), sub=2, nz=0.04, nf=3)
    if cloth:
        b.cloth(mats['cloth_grey'], 0.55, 1.05, m=M((0.32, -1.25, 1.85)), nx=4, nz=6, wave=0.05, depth=0.03)
    b.pop()


def dummy(b, mats, loc, rot=0):
    b.push(M(loc, (0, 0, rot)))
    b.cyl(mats['log'], 0.08, 1.9, 7)
    b.cyl(mats['straw'], 0.27, 0.8, 9, r2=0.24, m=M((0, 0, 0.75)), smooth=True, nz=0.03, nf=6)
    for z in (0.9, 1.35):
        b.cyl(mats['rope'], 0.285, 0.06, 9, m=M((0, 0, z)))
    b.tube(mats['straw'], [(-0.62, 0, 1.35), (0.62, 0, 1.35)], [0.1, 0.1], seg=7)
    b.sphere(mats['straw'], 0.24, m=M((0, 0, 1.82)), sub=2, nz=0.02, nf=6)
    snow_blob(b, mats, (0, 0, 2.02), 0.17, 0.5)
    snow_blob(b, mats, (0, 0, 1.56), 0.26, 0.4)
    b.pop()


def fire_pit(b, mats, loc):
    b.push(M(loc))
    for i in range(9):
        a = 2 * math.pi * i / 9
        rock(b, mats['rock_plain'], (math.cos(a) * 0.48, math.sin(a) * 0.48, 0.06), (0.17, 0.15, 0.12), seed=i + loc[0])
    b.cyl(mats['ember'], 0.38, 0.04, 10)
    for i in range(3):
        a = 2 * math.pi * i / 3 + 0.4
        b.tube(mats['bark'], [(math.cos(a) * 0.4, math.sin(a) * 0.4, 0.05), (-math.cos(a) * 0.05, -math.sin(a) * 0.05, 0.32)],
               0.06, seg=6)
    b.cone(mats['fire'], 0.2, 0.62, 6, m=M((0, 0, 0.08)))
    b.cone(mats['fire'], 0.13, 0.45, 6, m=M((0.12, 0.05, 0.08), (0, 12, 0)))
    b.cone(mats['fire'], 0.12, 0.4, 6, m=M((-0.1, -0.06, 0.08), (0, -14, 0)))
    b.pop()


def build(mats, cb, ck):
    rnd = random.Random(7)
    base = [island_body(cb, mats, 10.0, 'dirt', seed=1.3), island_snow(cb, mats, 10.0, seed=2.1, drip=0.32)]
    bld = []

    # ---- S01 palisade (10 arc pieces)
    R = 8.9
    gA = -78.0
    gap = 9.5
    a0, a1 = gA + gap, gA + 360 - gap
    span = a1 - a0
    n_logs = int((span * D2R * R) / 0.43)
    pieces = 10
    per = n_logs // pieces + 1
    for p in range(pieces):
        b = B(f'S01_Palisade_{p:02d}')
        idx = range(p * per, min(n_logs, (p + 1) * per))
        idx = list(idx)
        if not idx:
            continue
        for i in idx:
            a = (a0 + span * (i + 0.5) / n_logs) * D2R
            x, y = math.cos(a) * R, math.sin(a) * R
            h = 1.75 + rnd.uniform(-0.2, 0.35)
            r = 0.21 + rnd.uniform(-0.01, 0.03)
            b.push(M((x, y, -0.15), (rnd.uniform(-3, 3), rnd.uniform(-3, 3), rnd.uniform(0, 90))))
            b.cyl(mats['log'], r, h, 8)
            b.cone(mats['log'], r, 0.38, 8, m=M((0, 0, h)))
            snow_blob(b, mats, (0, 0, h + 0.17), r * 0.95, 0.65, seed=i)
            b.pop()
        # inner rail
        aa = [(a0 + span * (i + 0.5) / n_logs) * D2R for i in (idx[0], idx[-1])]
        pts = []
        for k in range(6):
            a = aa[0] + (aa[1] - aa[0]) * k / 5
            pts.append((math.cos(a) * (R - 0.27), math.sin(a) * (R - 0.27), 1.05))
        b.tube(mats['wood'], pts, 0.08, seg=6)
        bld.append(b.build(ck))

    # ---- S02 gate + crossed axes
    b = B('S02_Gate')
    gx, gy = math.cos(gA * D2R) * R, math.sin(gA * D2R) * R
    b.push(M((gx, gy, -0.15), (0, 0, gA + 90)))
    for s in (-1, 1):
        b.cyl(mats['log'], 0.3, 2.95, 10, m=M((s * 1.38, 0, 0)))
        b.cone(mats['log'], 0.3, 0.42, 10, m=M((s * 1.38, 0, 2.95)))
        snow_blob(b, mats, (s * 1.38, 0, 3.15), 0.3, 0.7, seed=s)
    b.box(mats['wood'], (3.0, 0.24, 0.26), m=M((0, 0, 2.45)), bevel=0.04)
    b.box(mats['snow'], (2.9, 0.3, 0.14), m=M((0, 0, 2.7)), bevel=0.06, smooth=True, nz=0.03, nf=4)
    for s in (-1, 1):
        b.push(M((s * 1.12, 0.05, 0.12), (0, 0, s * 24)))
        for k in range(5):
            x = -s * (0.13 + k * 0.225)
            b.box(mats['wood_v'], (0.21, 0.12, 2.05 + 0.05 * (k % 2)), m=M((x, 0, 0)), bevel=0.025)
        for z in (0.35, 1.55):
            b.box(mats['iron'], (1.12, 0.15, 0.1), m=M((-s * 0.57, 0, z)), bevel=0.02)
        b.box(mats['wood_dark'], (1.25, 0.06, 0.12), m=M((-s * 0.57, -0.08, 0.95), (0, s * 52, 0)), base=False,
              bevel=0.02)
        b.pop()
    for s in (-1, 1):
        b.push(M((0, -0.05, 3.15), (0, s * 38, 0)))
        axe(b, mats, flip=s)
        b.pop()
    b.pop()
    bld.append(b.build(ck))

    # ---- S03 longhouse walls / S04 roof
    LH = M((-3.3, 2.8, 0), (0, 0, -22))
    W, L, EZ, AZ = 6.6, 5.6, 0.35, 5.7
    b = B('S03_Longhouse', LH)
    for yy in (-L / 2 + 0.4, L / 2 - 0.4):
        b.prism(mats['wood_v'], [(-2.95, 0), (2.95, 0), (0.0, AZ - 0.55)], 0.24, m=M((0, yy, 0)))
    for s in (-1, 1):
        b.box(mats['wood'], (0.3, L - 0.6, 0.7), m=M((s * 2.85, 0, 0)), bevel=0.04)
    fy = -L / 2 + 0.25
    for s in (-1, 1):
        b.box(mats['wood_dark'], (0.22, 0.2, 2.35), m=M((s * 0.78, fy, 0)), bevel=0.03)
        # carved band along gable edges
        ang = math.degrees(math.atan2(AZ - 0.55, 2.95))
        ln = math.hypot(2.95, AZ - 0.55)
        b.box(mats['wood_dark'], (ln * 0.86, 0.12, 0.32),
              m=M((s * 1.47 * 0.98, fy - 0.03, (AZ - 0.55) / 2 - 0.12), (0, s * ang, 0)), base=False, bevel=0.03)
    b.box(mats['wood_dark'], (1.9, 0.22, 0.26), m=M((0, fy, 2.3)), bevel=0.03)
    b.box(mats['wood_dark'], (1.38, 0.1, 2.25), m=M((0, fy + 0.02, 0)), bevel=0.02)
    b.box(mats['wood'], (1.2, 0.06, 0.1), m=M((0, fy - 0.06, 0.7)), bevel=0.01)
    b.box(mats['wood'], (1.2, 0.06, 0.1), m=M((0, fy - 0.06, 1.6)), bevel=0.01)
    knot = [(math.cos(t * 2 * math.pi / 16) * 0.42, fy - 0.08, 3.25 + math.sin(t * 2 * math.pi / 16) * 0.42) for t in range(17)]
    b.tube(mats['wood_dark'], knot, 0.06, seg=6)
    b.tube(mats['wood_dark'], [(-0.3, fy - 0.08, 2.95), (0.3, fy - 0.08, 3.55)], 0.05, seg=6)
    b.tube(mats['wood_dark'], [(0.3, fy - 0.08, 2.95), (-0.3, fy - 0.08, 3.55)], 0.05, seg=6)
    b.box(mats['snow'], (1.9, 0.5, 0.22), m=M((0, fy - 0.3, 0)), bevel=0.1, smooth=True, nz=0.05, nf=3)
    bld.append(b.build(ck))

    b = B('S04_LonghouseRoof', LH)
    ang = gable_roof(b, mats, W, L, EZ, AZ, thick=0.5, over=0.45, snow_tiers=3, roof_mat='thatch', seed=3.0, snow_cover=0.7, fringe=True)
    half = W / 2
    slope = math.hypot(half, AZ - EZ)
    for yy in (-L / 2 - 0.5, L / 2 + 0.5):
        for s in (-1, 1):
            nrm = Vector((s * math.sin(ang * D2R), 0, math.cos(ang * D2R)))
            ext = 1.15
            # centre of plank: midpoint between eave point and point beyond apex
            p0 = Vector((s * (half + 0.15), yy, EZ - 0.1))
            dirv = Vector((-s * half, 0, AZ - EZ)).normalized()
            p1 = Vector((0, yy, AZ)) + dirv * ext
            c = (p0 + p1) / 2 + nrm * 0.32
            b.box(mats['wood'], ((p1 - p0).length, 0.22, 0.4), m=M(c, (0, s * ang, 0)), base=False, bevel=0.05)
        if yy < 0:
            # wolf head on the front right gable tip
            hx, hz = 0.72, AZ + 1.05
            b.push(M((hx, yy - 0.05, hz), (0, -10, 0), 1.55))
            b.box(mats['wood'], (0.55, 0.6, 0.55), base=False, bevel=0.12, smooth=False)
            b.box(mats['wood'], (0.32, 0.5, 0.28), m=M((0, -0.45, -0.1)), base=False, bevel=0.08)
            b.box(mats['wood_dark'], (0.12, 0.08, 0.08), m=M((0, -0.72, -0.05)), base=False, bevel=0.02)
            for s in (-1, 1):
                b.cone(mats['wood'], 0.12, 0.32, 5, m=M((s * 0.18, 0.05, 0.25)))
                b.box(mats['emblem'], (0.07, 0.04, 0.07), m=M((s * 0.15, -0.31, 0.08)), base=False, bevel=0.0)
            snow_blob(b, mats, (0, 0.05, 0.3), 0.3, 0.45)
            b.pop()
    bld.append(b.build(ck))

    # ---- S05 tents
    for i, (loc, rot, cl) in enumerate([((-7.0, -1.3, 0), 100, False), ((-6.7, -4.0, 0), 78, False),
                                        ((-4.9, -6.4, 0), 48, True)]):
        b = B(f'S05_Tent_{i}')
        tent(b, mats, loc, rot, cl)
        bld.append(b.build(ck))

    # ---- S06 watchtower
    b = B('S06_Watchtower', M((6.1, 2.6, 0), (0, 0, -18)))
    P = 0.95
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cyl(mats['log'], 0.15, 4.35, 8, m=M((sx * P, sy * P, -0.1)))
    b.box(mats['wood'], (2.45, 2.45, 0.2), m=M((0, 0, 2.45)), bevel=0.04)
    for (dx, dy, rz) in ((0, -1, 0), (0, 1, 0), (-1, 0, 90)):
        b.box(mats['wood_v'], (2.15, 0.1, 0.95), m=M((dx * P, dy * P, 2.65), (0, 0, rz)), bevel=0.02)
        b.box(mats['wood'], (2.3, 0.18, 0.12), m=M((dx * P, dy * P, 3.58), (0, 0, rz)), bevel=0.02)
    b.box(mats['wood'], (0.12, 2.3, 0.12), m=M((P, 0, 3.58)), bevel=0.02)
    for (dy, rz) in ((-1, 0), (1, 0)):
        b.tube(mats['log'], [(-P, dy * P, 0.2), (P, dy * P, 2.35)], 0.07, seg=6)
        b.tube(mats['log'], [(P, dy * P, 0.2), (-P, dy * P, 2.35)], 0.07, seg=6)
    gable_roof(b, mats, 2.9, 2.6, 4.05, 5.05, thick=0.16, over=0.25, snow_tiers=1, roof_mat='wood_dark', seed=5,
               droop_lumps=True)
    for yy in (-1.55, 1.55):
        for s in (-1, 1):
            b.box(mats['wood'], (1.95, 0.12, 0.2), m=M((s * 0.55, yy, 4.6), (0, s * 34, 0)), base=False, bevel=0.03)
    # ladder on the +X side
    for yy in (-0.32, 0.32):
        b.tube(mats['wood'], [(1.95, yy, 0), (1.15, yy, 2.6)], 0.055, seg=6)
    for k in range(7):
        t = (k + 0.6) / 7.5
        b.tube(mats['wood'], [(1.95 - 0.8 * t, -0.34, 2.6 * t), (1.95 - 0.8 * t, 0.34, 2.6 * t)], 0.04, seg=5)
    bld.append(b.build(ck))

    # ---- S07 banner
    b = B('S07_Banner', M((1.5, 6.4, 0), (0, 0, -8)))
    b.cyl(mats['log'], 0.13, 7.3, 8, m=M((0, 0, -0.1)))
    b.cone(mats['iron'], 0.12, 0.35, 6, m=M((0, 0, 7.2)))
    b.tube(mats['log'], [(-1.25, 0, 6.65), (1.25, 0, 6.65)], 0.09, seg=6)
    for s in (-1, 1):
        b.sphere(mats['iron'], 0.11, m=M((s * 1.3, 0, 6.65)), sub=1)
        b.tube(mats['rope'], [(s * 1.0, -0.05, 6.62), (s * 0.95, -0.12, 6.45)], 0.025, seg=4)
    b.box(mats['snow'], (2.3, 0.22, 0.1), m=M((0, 0, 6.72)), bevel=0.05, smooth=True, nz=0.02, nf=5)
    b.cloth(mats['cloth_grey'], 2.05, 3.1, m=M((0, -0.17, 6.55)), notch=0.65, nx=10, nz=12, wave=0.05, depth=0.05)
    ey = -0.3
    b.push(M((0, ey, 0)))
    b.box(mats['emblem'], (0.12, 0.04, 1.35), m=M((0, 0, 4.25)), bevel=0.0)
    for (x, z, r, ln) in ((-0.22, 5.35, 38, 0.6), (0.22, 5.35, -38, 0.6), (-0.18, 5.0, 55, 0.5), (0.18, 5.0, -55, 0.5),
                          (-0.15, 4.35, -50, 0.4), (0.15, 4.35, 50, 0.4), (0, 5.55, 0, 0.35)):
        b.box(mats['emblem'], (0.07, 0.04, ln), m=M((x, 0, z - ln / 2), (0, r, 0)), bevel=0.0)
    bird = [(-0.22, 0), (-0.05, 0.05), (0.08, 0.22), (0.12, 0.08), (0.24, 0.02), (0.08, -0.06), (-0.02, -0.18)]
    for s in (-1, 1):
        b.prism(mats['emblem'], [(s * x, z) for (x, z) in bird], 0.04, m=M((s * 0.62, 0, 5.6)))
    b.pop()
    bld.append(b.build(ck))

    # ---- S08 training ground
    b = B('S08_TrainingRing')
    ring_decal(b, mats['snow_trampled'], (1.4, -0.9), 2.0, 2.85, z=0.03, seed=1.0)
    bld.append(b.build(ck))
    for i, (loc, rot) in enumerate([((0.9, 1.0, 0), 10), ((2.6, 1.25, 0), -15)]):
        b = B(f'S08_Dummy_{i}')
        dummy(b, mats, loc, rot)
        bld.append(b.build(ck))
    for i, loc in enumerate([(-0.4, -4.3, 0), (4.6, -3.2, 0), (5.3, 0.1, 0), (-0.8, 3.6, 0), (3.9, -5.4, 0)]):
        b = B(f'S08_Post_{i}')
        b.push(M(loc, (rnd.uniform(-4, 4), rnd.uniform(-4, 4), 0)))
        b.cyl(mats['log'], 0.13, 1.05, 8, m=M((0, 0, -0.1)))
        b.cyl(mats['rope'], 0.15, 0.08, 8, m=M((0, 0, 0.6)))
        snow_blob(b, mats, (0, 0, 0.98), 0.15, 0.6, seed=i)
        b.pop()
        bld.append(b.build(ck))

    # ---- S09 fire pits
    for i, loc in enumerate([(-2.5, -1.7, 0), (-3.1, -4.7, 0)]):
        b = B(f'S09_FirePit_{i}')
        fire_pit(b, mats, loc)
        bld.append(b.build(ck))

    # ---- S10 pine trees
    trees = [(-7.4, 2.0, 4.2), (-6.4, 5.0, 4.8), (-3.8, 6.9, 5.4), (-0.8, 7.5, 4.4), (3.6, 7.0, 5.1), (6.4, 5.6, 4.3),
             (-1.6, 7.9, 3.6), (7.4, 3.9, 3.8), (-5.4, 6.6, 5.9), (5.0, 6.6, 3.9)]
    for i, (x, y, h) in enumerate(trees):
        b = B(f'S10_Pine_{i}')
        pine(b, mats, (x, y, -0.05), h=h, r=h * 0.3, tiers=5, rot=rnd.uniform(0, 60), seed=i)
        bld.append(b.build(ck))
    return base, bld
