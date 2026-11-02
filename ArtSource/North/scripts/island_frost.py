# Island 2: Frost camp with roasting fire (ref: "Сделать без буйвола.jpg" - without the yak)
NAME = 'FrostCamp'


def spline(ctrl, n=16):
    P = [Vector(c) for c in ctrl]
    P = [P[0]] + P + [P[-1]]
    out = []
    segs = len(P) - 3
    for i in range(segs):
        p0, p1, p2, p3 = P[i], P[i + 1], P[i + 2], P[i + 3]
        k = max(2, n // segs)
        for j in range(k):
            t = j / k
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(P[-2])
    return out


def lerp_list(vals, n):
    out = []
    for i in range(n):
        t = i / (n - 1) * (len(vals) - 1)
        a = int(min(t, len(vals) - 2))
        f = t - a
        out.append(vals[a] * (1 - f) + vals[a + 1] * f)
    return out


def arc_band(r_out, r_in, z0, n=14, k=1.15, a0=0.0, a1=180.0):
    outer, inner = [], []
    for i in range(n + 1):
        t = math.radians(a0 + (a1 - a0) * i / n)
        outer.append((math.cos(t) * r_out, z0 + math.sin(t) * r_out * k))
        inner.append((math.cos(t) * r_in, z0 + math.sin(t) * r_in * k))
    return outer + list(reversed(inner))


def mitten(b, mt, loc, rot=0):
    pts = [(-0.12, 0), (0.12, 0), (0.13, -0.3), (0.2, -0.32), (0.2, -0.42), (0.1, -0.44), (0.1, -0.5), (-0.13, -0.52),
           (-0.15, -0.3)]
    b.prism(mt, pts, 0.09, m=M(loc, (0, rot, 0)))
    b.box(mats_ref['cloth_white'], (0.27, 0.11, 0.07), m=M((loc[0], loc[1], loc[2] - 0.04), (0, rot, 0)), base=False,
          bevel=0.02)


def sock(b, mt, loc, rot=0):
    pts = [(-0.08, 0), (0.08, 0), (0.08, -0.42), (0.28, -0.46), (0.28, -0.58), (-0.08, -0.58)]
    b.prism(mt, pts, 0.09, m=M(loc, (0, rot, 0)))


def build(mats, cb, ck):
    global mats_ref
    mats_ref = mats
    rnd = random.Random(11)
    base = [island_body(cb, mats, 10.0, 'rock', seed=4.2), island_snow(cb, mats, 10.0, seed=6.3, drip=0.25, lip=0.5)]
    bm = B('Mountains')
    for (x, y, r, h, sd) in ((2.4, 6.7, 3.0, 6.6, 1.0), (5.3, 4.9, 3.3, 8.6, 2.0), (7.6, 2.0, 2.2, 5.4, 3.0),
                             (-0.7, 7.7, 2.1, 4.4, 4.0), (3.9, 7.4, 1.9, 4.8, 5.0)):
        mountain(bm, mats, (x, y, -0.3), r, h, seed=sd, rot=sd * 40)
    for i, (a, d, s) in enumerate(((-128, 9.3, 0.9), (-118, 9.5, 0.6), (-62, 9.2, 0.75), (-48, 9.4, 1.0), (-30, 9.3, 0.7),
                                   (-148, 9.2, 0.8), (205, 9.3, 0.7), (-88, 9.6, 0.5))):
        ar = a * D2R
        rock(bm, mats['stone'], (math.cos(ar) * d, math.sin(ar) * d, 0.1), (s * 1.3, s, s * 0.7), (0, 0, a), seed=i * 3.1)
    base.append(bm.build(cb, pivot='origin'))
    bld = []

    # ---- hut
    HM = M((-4.3, 3.3, 0), (0, 0, -14))
    W, L, WH = 4.0, 4.4, 1.9
    yf = -L / 2
    b = B('S01_Hut', HM)
    b.box(mats['wood_v'], (W, L, WH), m=M((0, 0, -0.1)), bevel=0.05)
    b.prism(mats['wood_v'], arc_band(W / 2, 0.01, WH - 0.1, n=12, k=0.95), 0.2, m=M((0, yf + 0.12, 0)))
    b.box(mats['wood_dark'], (1.15, 0.14, 1.9), m=M((0, yf - 0.02, 0)), bevel=0.03)
    for s in (-1, 1):
        b.box(mats['wood'], (0.16, 0.2, 2.05), m=M((s * 0.66, yf - 0.04, 0)), bevel=0.03)
    b.box(mats['wood'], (1.5, 0.2, 0.18), m=M((0, yf - 0.04, 1.95)), bevel=0.03)
    for z in (0.45, 1.35):
        b.box(mats['iron'], (1.05, 0.06, 0.08), m=M((0, yf - 0.1, z)), bevel=0.01)
    b.box(mats['snow'], (1.6, 0.6, 0.18), m=M((0, yf - 0.35, 0)), bevel=0.08, smooth=True, nz=0.04, nf=3)
    bld.append(b.build(ck))

    b = B('S02_HutRoof', HM)
    b.prism(mats['wood_dark'], arc_band(W / 2 + 0.35, W / 2 + 0.05, WH - 0.25, n=16, k=1.0, a0=-8, a1=188), L + 0.6)
    b.prism(mats['snow'], arc_band(W / 2 + 0.75, W / 2 + 0.3, WH - 0.15, n=16, k=0.98, a0=18, a1=162), L + 0.75)
    for s in (-1, 1):
        a = math.radians(90 - s * 72)
        ex, ez = math.cos(a) * (W / 2 + 0.55), WH - 0.15 + math.sin(a) * (W / 2 + 0.55) * 0.98
        for i in range(9):
            y = -L / 2 - 0.3 + (i + 0.5) * (L + 0.6) / 9
            ln = 1 + 0.8 * abs(math.sin(i * 1.9 + s))
            b.sphere(mats['snow'], 0.22, m=M((ex + s * 0.05, y, ez - 0.1 * ln), (0, 0, 0), (1, 0.9, 0.7 * ln)), sub=1)
    # crossed front boards + tall curved arm with lantern
    for s in (-1, 1):
        b.tube(mats['wood'], [(s * 2.35, yf - 0.45, 1.3), (-s * 0.55, yf - 0.45, 4.75)], 0.13, seg=6, smooth=False)
    arm = spline([(0.45, yf - 0.45, 4.5), (1.0, yf - 0.55, 5.4), (1.9, yf - 0.6, 5.85), (2.5, yf - 0.6, 5.7)], 14)
    b.tube(mats['wood'], arm, lerp_list([0.14, 0.12, 0.1, 0.08], len(arm)), seg=6)
    b.tube(mats['iron'], [(2.45, yf - 0.6, 5.7), (2.45, yf - 0.6, 5.1)], 0.025, seg=4)
    b.box(mats['wood_dark'], (0.34, 0.34, 0.06), m=M((2.45, yf - 0.6, 4.62)), bevel=0.01)
    b.box(mats['glow'], (0.24, 0.24, 0.38), m=M((2.45, yf - 0.6, 4.68)), bevel=0.02)
    b.cone(mats['wood_dark'], 0.24, 0.2, 4, m=M((2.45, yf - 0.6, 5.06), (0, 0, 45)))
    b.sphere(mats['snow'], 1.0, m=M((-0.0, yf - 0.45, 4.85), (0, 0, 0), (0.35, 0.25, 0.2)), sub=1)
    bld.append(b.build(ck))

    b = B('S03_HutHorns', HM)
    for s in (-1, 1):
        ctrl = [(s * 1.75, yf - 0.1, 2.6), (s * 2.75, yf - 0.3, 2.85), (s * 3.45, yf - 0.3, 3.6), (s * 3.45, yf - 0.2, 4.5),
                (s * 2.95, yf - 0.1, 5.05)]
        pts = spline(ctrl, 18)
        b.tube(mats['bone'], pts, lerp_list([0.42, 0.36, 0.27, 0.16, 0.04], len(pts)), seg=9)
        b.tube(mats['wood_dark'], [(s * 1.6, yf - 0.05, 2.55), (s * 1.85, yf - 0.08, 2.6)], 0.45, seg=9)
    bld.append(b.build(ck))

    # ---- cart
    b = B('S04_Cart', M((-2.8, -0.3, 0), (0, 0, 18)))
    b.box(mats['wood'], (2.4, 1.3, 0.12), m=M((0, 0, 0.7)), bevel=0.02)
    for s in (-1, 1):
        b.box(mats['wood'], (2.4, 0.08, 0.5), m=M((0, s * 0.62, 0.8)), bevel=0.02)
        b.box(mats['wood'], (0.08, 1.3, 0.5), m=M((s * 1.17, 0, 0.8)), bevel=0.02)
        for x in (-1.15, 0, 1.15):
            b.box(mats['wood_dark'], (0.1, 0.12, 0.62), m=M((x, s * 0.65, 0.7)), bevel=0.02)
    b.box(mats['snow'], (2.2, 1.1, 0.25), m=M((0, 0, 0.8)), bevel=0.1, smooth=True, nz=0.05, nf=3)
    for x in (-0.7, 0.75):
        for s in (-1, 1):
            b.push(M((x, s * 0.78, 0.42), (90, 0, 0)))
            b.cyl(mats['iron'], 0.43, 0.1, 14, base=False)
            b.cyl(mats['wood'], 0.37, 0.12, 14, base=False)
            b.cyl(mats['wood_dark'], 0.1, 0.22, 8, base=False)
            for k in range(6):
                b.box(mats['wood'], (0.06, 0.66, 0.06), m=M((0, 0, 0), (0, 0, k * 30)), base=False, bevel=0.0)
            b.pop()
    for s in (-1, 1):
        b.tube(mats['wood'], [(1.1, s * 0.42, 0.75), (2.7, s * 0.38, 0.45)], 0.06, seg=6)
    bld.append(b.build(ck))

    # ---- barrels
    for i, (x, y, h) in enumerate(((-7.3, -2.4, 1.1), (-6.2, -3.1, 1.05), (-6.7, -1.2, 1.15))):
        b = B(f'S05_Barrel_{i}')
        barrel(b, mats, (x, y, -0.02), h=h * 1.25, r=0.62, rot=(0, 0, rnd.uniform(0, 90)))
        bld.append(b.build(ck))

    # ---- campfire
    FX = (2.2, -2.4, 0)
    b = B('S06_Campfire', M(FX))
    for i in range(12):
        a = 2 * math.pi * i / 12
        rock(b, mats['rock_plain'], (math.cos(a) * 0.85, math.sin(a) * 0.85, 0.08), (0.25, 0.2, 0.17), seed=i * 1.3)
    b.cyl(mats['ember'], 0.72, 0.05, 12)
    for i in range(5):
        a = 2 * math.pi * i / 5
        b.tube(mats['bark'], [(math.cos(a) * 0.7, math.sin(a) * 0.7, 0.05), (-math.cos(a) * 0.05, -math.sin(a) * 0.05, 0.55)],
               0.09, seg=6)
    for (x, y, r, h, tilt) in ((0, 0, 0.36, 1.1, 0), (0.22, 0.1, 0.24, 0.8, 14), (-0.22, -0.08, 0.24, 0.75, -16),
                               (0.05, -0.25, 0.2, 0.6, 8), (-0.1, 0.25, 0.2, 0.65, -8)):
        b.cone(mats['fire'], r, h, 7, m=M((x, y, 0.1), (tilt, tilt * 0.5, 0)))
    bld.append(b.build(ck))

    b = B('S07_Spit', M(FX))
    for s in (-1, 1):
        b.tube(mats['wood'], [(s * 1.4, 0, 0), (s * 1.4, 0, 1.25)], 0.07, seg=6)
        b.tube(mats['wood'], [(s * 1.4, 0, 1.05), (s * 1.55, 0, 1.5)], 0.05, seg=5)
        b.tube(mats['wood'], [(s * 1.4, 0, 1.05), (s * 1.25, 0, 1.5)], 0.05, seg=5)
    b.tube(mats['iron'], [(-1.6, 0, 1.42), (1.75, 0, 1.42), (1.75, 0, 1.1), (2.05, 0, 1.1)], 0.04, seg=6)
    b.sphere(mats['meat'], 1.0, m=M((0.05, 0, 1.42), (0, 0, 0), (0.78, 0.48, 0.46)), sub=2, nz=0.06, nf=3)
    for s in (-1, 1):
        b.sphere(mats['bone'], 0.11, m=M((s * 0.82, 0, 1.42)), sub=1)
    bld.append(b.build(ck))

    # ---- benches (snow covered stone blocks)
    for i, (x, y, rz, ln) in enumerate(((-0.1, -3.2, 12, 1.6), (4.5, -2.1, 18, 1.5), (5.9, -4.3, 30, 1.2))):
        b = B(f'S08_Bench_{i}', M((x, y, 0), (0, 0, rz)))
        b.box(mats['stone'], (ln, 0.62, 0.5), m=M((0, 0, -0.05)), bevel=0.08, nz=0.04, nf=2, seed=i)
        b.box(mats['snow'], (ln + 0.1, 0.72, 0.18), m=M((0, 0, 0.42)), bevel=0.08, smooth=True, nz=0.03, nf=3, seed=i)
        bld.append(b.build(ck))

    # ---- clothesline
    b = B('S09_Clothesline')
    p0, p1 = Vector((4.5, 1.3, 0)), Vector((8.0, -1.0, 0))
    for p in (p0, p1):
        b.cyl(mats['log'], 0.11, 2.15, 7, m=M((p.x, p.y, -0.1), (rnd.uniform(-3, 3), rnd.uniform(-3, 3), 0)))
        snow_blob(b, mats, (p.x, p.y, 2.08), 0.13, 0.6)
    rope = []
    for k in range(13):
        t = k / 12
        q = p0.lerp(p1, t)
        rope.append((q.x, q.y, 1.95 - 0.38 * 4 * t * (1 - t)))
    b.tube(mats['rope'], rope, 0.025, seg=4)
    rot = math.degrees(math.atan2(p1.y - p0.y, p1.x - p0.x))
    items = (('m', 0.12, 'cloth_brown'), ('s', 0.24, 'cloth_red'), ('m', 0.36, 'cloth_red'), ('s', 0.5, 'cloth_brown'),
             ('m', 0.63, 'cloth_brown'), ('scarf', 0.78, 'cloth_red'), ('s', 0.9, 'cloth_red'))
    for kind, t, mn in items:
        q = p0.lerp(p1, t)
        z = 1.95 - 0.38 * 4 * t * (1 - t) - 0.03
        b.push(M((q.x, q.y, z), (0, 0, rot)))
        if kind == 'm':
            mitten(b, mats[mn], (0, 0, 0))
        elif kind == 's':
            sock(b, mats[mn], (0, 0, 0))
        else:
            for k in range(5):
                b.box(mats['cloth_red' if k % 2 == 0 else 'cloth_white'], (0.22, 0.05, 0.18), m=M((0, 0, -0.18 * (k + 1))),
                      bevel=0.01)
            b.box(mats['cloth_red'], (0.22, 0.05, 0.12), m=M((0, 0, -0.1)), bevel=0.01)
        b.pop()
    bld.append(b.build(ck))

    # ---- pine trees
    trees = [(-8.2, 1.4, 6.8), (-7.5, 4.1, 5.0), (-5.6, 6.5, 6.0), (-2.0, 6.6, 4.0), (0.6, 5.2, 3.3), (8.6, -2.9, 3.8),
             (-8.4, -1.0, 4.4)]
    for i, (x, y, h) in enumerate(trees):
        b = B(f'S10_Pine_{i}')
        pine(b, mats, (x, y, -0.05), h=h, r=h * 0.3, tiers=5 if h > 4.5 else 4, rot=rnd.uniform(0, 60), seed=i + 20)
        bld.append(b.build(ck))
    return base, bld
