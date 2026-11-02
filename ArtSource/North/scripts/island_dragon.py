# Island 3: Frost keep (ref: "сделать без драконов.jpg" - without the dragons)
NAME = 'FrostKeep'


def star_pts(r_out, r_in, n=5, rot=90.0):
    pts = []
    for i in range(n * 2):
        a = math.radians(rot + 180.0 * i / n)
        r = r_out if i % 2 == 0 else r_in
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return pts


def arch_door(b, mats, x, y, w, h, fill='glow', frame='stone', depth=0.12):
    rr = w / 2
    b.box(mats[fill], (w, depth, h - rr), m=M((x, y, 0)), bevel=0.0)
    b.cyl(mats[fill], rr, depth, 12, m=M((x, y, h - rr), (90, 0, 0)), base=False)
    pts = [(x - rr - 0.12, y - 0.05, 0.0), (x - rr - 0.12, y - 0.05, h - rr)]
    for k in range(1, 12):
        a = math.pi - math.pi * k / 12
        pts.append((x + math.cos(a) * (rr + 0.12), y - 0.05, h - rr + math.sin(a) * (rr + 0.12)))
    pts += [(x + rr + 0.12, y - 0.05, h - rr), (x + rr + 0.12, y - 0.05, 0.0)]
    b.tube(mats[frame], pts, 0.15, seg=6, smooth=False)


def hang_banner(b, mats, mt, w, h, star=True, notch=0.3, phase=0.0):
    b.tube(mats['wood_dark'], [(-w / 2 - 0.12, 0, 0.05), (w / 2 + 0.12, 0, 0.05)], 0.05, seg=6)
    b.cloth(mats[mt], w, h, notch=notch, nx=6, nz=10, wave=0.04, depth=0.04, phase=phase)
    if star:
        b.prism(mats['emblem_white'], star_pts(w * 0.3, w * 0.12, 4, 90), 0.03, m=M((0, -0.07, -h * 0.42)))
    for k in range(5):
        b.cone(mats['ice'], 0.035, 0.18 + 0.1 * (k % 2), 4, m=M((-w / 2 + (k + 0.5) * w / 5, 0, -h + notch * (1 - abs((k + 0.5) / 5 - 0.5) * 2) + 0.02), (180, 0, 0)))


def build(mats, cb, ck):
    rnd = random.Random(23)
    base = [island_body(cb, mats, 10.0, 'rock', seed=8.7), island_snow(cb, mats, 10.0, seed=3.3, drip=0.3, lip=0.5)]
    bm = B('Crags')
    for (x, y, r, h, sd) in ((2.9, 6.4, 2.6, 5.6, 1.5), (5.8, 4.4, 2.3, 4.4, 2.5), (-4.9, 5.8, 2.3, 4.0, 3.5),
                             (0.0, 7.6, 2.3, 4.2, 4.5), (-7.3, 3.6, 1.7, 2.6, 5.5), (7.4, 2.2, 1.6, 2.4, 6.5)):
        mountain(bm, mats, (x, y, -0.3), r, h, seed=sd, rot=sd * 33, mt='crag')
    base.append(bm.build(cb, pivot='origin'))
    bld = []

    # ---- hall: stone floor, timber floor, roof, banner
    HM = M((-0.8, 2.0, 0), (0, 0, -12))
    W, L = 5.4, 4.6
    yf = -L / 2
    b = B('S01_HallStone', HM)
    b.box(mats['brick'], (W, L, 3.0), m=M((0, 0, -0.1)), bevel=0.04)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(mats['stone'], (0.5, 0.5, 3.05), m=M((sx * (W / 2 - 0.15), sy * (L / 2 - 0.15), -0.1)), bevel=0.06)
    arch_door(b, mats, -1.45, yf - 0.02, 1.0, 2.0)
    arch_door(b, mats, 0.75, yf - 0.02, 1.45, 2.55)
    b.box(mats['stone'], (2.0, 0.6, 0.18), m=M((0.75, yf - 0.35, -0.05)), bevel=0.04)
    b.box(mats['snow'], (2.0, 0.6, 0.1), m=M((0.75, yf - 0.4, 0.12)), bevel=0.04, smooth=True, nz=0.03, nf=4)
    bld.append(b.build(ck))

    b = B('S02_HallTimber', HM)
    b.box(mats['wood_v'], (W + 0.2, L + 0.2, 2.2), m=M((0, 0, 2.9)), bevel=0.04)
    b.box(mats['wood_dark'], (W + 0.4, L + 0.4, 0.3), m=M((0, 0, 2.8)), bevel=0.04)
    b.box(mats['snow'], (W + 0.5, L + 0.5, 0.12), m=M((0, 0, 3.08)), bevel=0.05, smooth=True, nz=0.03, nf=4)
    for x in (-W / 2 - 0.05, -0.9, 0.9, W / 2 + 0.05):
        b.box(mats['wood_dark'], (0.25, 0.25, 2.25), m=M((x, yf - 0.12, 2.9)), bevel=0.03)
    for x in (-1.8, 1.8):
        b.box(mats['glow'], (0.5, 0.06, 0.7), m=M((x, yf - 0.1, 3.75)), bevel=0.0)
        b.box(mats['wood_dark'], (0.7, 0.12, 0.1), m=M((x, yf - 0.14, 3.7)), bevel=0.01)
        b.box(mats['wood_dark'], (0.08, 0.1, 0.72), m=M((x, yf - 0.14, 3.75)), bevel=0.0)
    bld.append(b.build(ck))

    b = B('S03_HallRoof', HM)
    b.prism(mats['wood_v'], [(-W / 2 - 0.1, 5.05), (W / 2 + 0.1, 5.05), (0, 8.2)], 0.25, m=M((0, yf + 0.15, 0)))
    b.prism(mats['wood_v'], [(-W / 2 - 0.1, 5.05), (W / 2 + 0.1, 5.05), (0, 8.2)], 0.25, m=M((0, -yf - 0.15, 0)))
    ang = gable_roof(b, mats, W + 1.3, L, 5.0, 8.7, thick=0.3, over=0.55, snow_tiers=2, roof_mat='wood_dark',
                     icicles=True, seed=2.0, snow_cover=0.93)
    half = (W + 1.3) / 2
    for yy in (yf - 0.6, -yf + 0.6):
        for s in (-1, 1):
            nrm = Vector((s * math.sin(ang * D2R), 0, math.cos(ang * D2R)))
            p0 = Vector((s * (half + 0.1), yy, 4.9))
            dirv = Vector((-s * half, 0, 8.7 - 5.0)).normalized()
            p1 = Vector((0, yy, 8.7)) + dirv * 0.95
            c = (p0 + p1) / 2 + nrm * 0.28
            b.box(mats['wood'], ((p1 - p0).length, 0.2, 0.34), m=M(c, (0, s * ang, 0)), base=False, bevel=0.05)
    # carved head on the front apex
    b.push(M((0, yf - 0.65, 9.35), (0, 0, 0), 1.2))
    b.box(mats['wood'], (0.45, 0.6, 0.45), base=False, bevel=0.1)
    b.box(mats['wood'], (0.3, 0.55, 0.25), m=M((0, -0.45, -0.08)), base=False, bevel=0.06)
    for s in (-1, 1):
        b.tube(mats['wood_dark'], [(s * 0.15, 0.1, 0.2), (s * 0.3, 0.35, 0.5), (s * 0.28, 0.6, 0.62)], [0.07, 0.05, 0.02], seg=5)
    snow_blob(b, mats, (0, 0.05, 0.27), 0.3, 0.5)
    b.pop()
    bld.append(b.build(ck))

    b = B('S04_HallBanner', HM)
    b.push(M((0, yf - 0.35, 7.25)))
    hang_banner(b, mats, 'cloth_blue', 1.05, 2.2, notch=0.35)
    b.pop()
    bld.append(b.build(ck))

    # ---- side house
    b = B('S05_SideHouse', M((4.3, 0.1, 0), (0, 0, -20)))
    b.box(mats['stone'], (3.9, 3.4, 0.7), m=M((0, 0, -0.1)), bevel=0.06)
    b.box(mats['wood_v'], (3.6, 3.1, 2.1), m=M((0, 0, 0.55)), bevel=0.04)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(mats['wood_dark'], (0.22, 0.22, 2.15), m=M((sx * 1.78, sy * 1.53, 0.55)), bevel=0.03)
    arch_door(b, mats, 0.35, -1.6, 0.95, 1.85, fill='wood_dark', frame='stone')
    b.box(mats['iron'], (0.08, 0.06, 0.08), m=M((0.62, -1.7, 1.0)), bevel=0.0)
    b.box(mats['glow'], (0.5, 0.06, 0.55), m=M((-1.05, -1.58, 1.4)), bevel=0.0)
    b.box(mats['wood_dark'], (0.66, 0.1, 0.1), m=M((-1.05, -1.62, 1.35)), bevel=0.01)
    b.push(M((0, 0, 0), (0, 0, 90)))
    b.prism(mats['wood_v'], [(-1.6, 2.6), (1.6, 2.6), (0, 4.25)], 0.2, m=M((0, -1.75, 0)))
    b.prism(mats['wood_v'], [(-1.6, 2.6), (1.6, 2.6), (0, 4.25)], 0.2, m=M((0, 1.75, 0)))
    gable_roof(b, mats, 4.3, 3.6, 2.5, 4.6, thick=0.25, over=0.4, snow_tiers=2, roof_mat='wood_dark', icicles=True,
               seed=7.0, snow_cover=0.93)
    b.pop()
    b.cyl(mats['stone'], 0.3, 1.2, 6, m=M((1.0, 0.8, 4.0)))
    snow_blob(b, mats, (1.0, 0.8, 5.25), 0.35, 0.5)
    bld.append(b.build(ck))

    # ---- boardwalk + railing along the front-left rim
    a0, a1 = 196.0, 286.0
    b = B('S06_Boardwalk')
    n = 34
    for i in range(n):
        a = (a0 + (a1 - a0) * (i + 0.5) / n) * D2R
        rr = 8.55
        b.box(mats['wood'], (1.05, 0.36, 0.09), m=M((math.cos(a) * rr, math.sin(a) * rr, 0.1),
                                                    (rnd.uniform(-2, 2), rnd.uniform(-2, 2), math.degrees(a) + rnd.uniform(-3, 3))),
              base=False, bevel=0.015)
    for rr in (8.15, 8.95):
        pts = [(math.cos((a0 + (a1 - a0) * k / 12) * D2R) * rr, math.sin((a0 + (a1 - a0) * k / 12) * D2R) * rr, 0.0)
               for k in range(13)]
        b.tube(mats['wood_dark'], pts, 0.07, seg=5)
    bld.append(b.build(ck))

    b = B('S07_Railing')
    posts = []
    for k in range(9):
        a = (a0 + (a1 - a0) * k / 8) * D2R
        p = Vector((math.cos(a) * 9.35, math.sin(a) * 9.35, 0))
        posts.append(p)
        b.cyl(mats['log'], 0.12, 1.15, 7, m=M((p.x, p.y, -0.1), (rnd.uniform(-4, 4), rnd.uniform(-4, 4), 0)))
        snow_blob(b, mats, (p.x, p.y, 1.08), 0.14, 0.6, seed=k)
    for k in range(8):
        p, q = posts[k], posts[k + 1]
        pts = []
        for j in range(7):
            t = j / 6
            r = p.lerp(q, t)
            pts.append((r.x, r.y, 0.92 - 0.22 * 4 * t * (1 - t)))
        b.tube(mats['rope'], pts, 0.035, seg=4)
    bld.append(b.build(ck))

    # ---- banners hanging over the rim
    for i, (a, mt) in enumerate(((212, 'cloth_blue'), (252, 'cloth_green'), (300, 'cloth_green'), (336, 'cloth_blue'))):
        ar = a * D2R
        b = B(f'S08_RimBanner_{i}', M((math.cos(ar) * 10.0, math.sin(ar) * 10.0, 0.25), (0, 0, a + 90)))
        b.tube(mats['wood_dark'], [(0, 0.9, 0.0), (0, -0.25, 0.02)], 0.06, seg=5)
        b.box(mats['snow'], (0.25, 0.9, 0.08), m=M((0, 0.4, 0.04)), bevel=0.03, smooth=True)
        b.push(M((0, -0.18, -0.05)))
        hang_banner(b, mats, mt, 0.85, 2.0, notch=0.3, phase=i)
        b.pop()
        bld.append(b.build(ck))

    # ---- flag poles with pennants
    for i, (x, y, h, mt) in enumerate(((8.3, 1.4, 7.0, 'cloth_green'), (-2.9, 5.5, 6.6, 'cloth_blue'),
                                       (2.5, 5.0, 6.2, 'cloth_green'))):
        b = B(f'S09_FlagPole_{i}', M((x, y, 0), (0, 0, rnd.uniform(-20, 10))))
        b.cyl(mats['wood_dark'], 0.11, h, 7, m=M((0, 0, -0.1)))
        b.sphere(mats['iron'], 0.13, m=M((0, 0, h)), sub=1)
        b.prism(mats[mt], [(0.0, 0.0), (2.0, -0.3), (2.0, -0.55), (0.0, -0.95)], 0.05, m=M((0.08, 0, h - 0.2)),
                bend=lambda x, z: 0.18 * math.sin(x * 2.4))
        b.prism(mats['emblem_white'], star_pts(0.22, 0.09, 4, 90), 0.03, m=M((0.75, -0.08, h - 0.65)))
        for k in range(4):
            b.cone(mats['ice'], 0.03, 0.15, 4, m=M((0.3 + k * 0.45, 0, h - 1.15 + k * 0.12), (180, 0, 0)))
        snow_blob(b, mats, (0, 0, h + 0.1), 0.12, 0.6)
        bld.append(b.build(ck))

    # ---- boulders, rope fence, tall weathered post
    for i, (x, y, s) in enumerate(((-6.6, -1.6, 1.15), (-7.5, 0.5, 0.95), (-5.5, -3.5, 0.8), (-7.7, -2.8, 0.75),
                                   (-4.4, -5.6, 0.55), (2.4, -3.8, 0.45), (5.8, -2.9, 0.55), (-6.0, 1.9, 0.7))):
        b = B(f'S10_Boulder_{i}')
        rock(b, mats['rock_moss'], (x, y, s * 0.35), (s * 1.2, s, s * 0.9), (0, 0, rnd.uniform(0, 90)), seed=i * 2.7)
        snow_blob(b, mats, (x, y, s * 0.95), s * 0.85, 0.45, seed=i)
        bld.append(b.build(ck))
    b = B('S10_RopeFence')
    fp = [Vector(p) for p in ((-6.4, 3.0, 0), (-5.1, 4.1, 0), (-3.7, 4.9, 0))]
    for k, p in enumerate(fp):
        b.cyl(mats['log'], 0.11, 1.1, 7, m=M((p.x, p.y, -0.1)))
        snow_blob(b, mats, (p.x, p.y, 1.03), 0.13, 0.6, seed=k)
    for k in range(len(fp) - 1):
        pts = []
        for j in range(7):
            t = j / 6
            r = fp[k].lerp(fp[k + 1], t)
            pts.append((r.x, r.y, 0.85 - 0.2 * 4 * t * (1 - t)))
        b.tube(mats['rope'], pts, 0.035, seg=4)
    bld.append(b.build(ck))
    b = B('S10_TallPost', M((-8.4, 1.6, 0), (3, -4, 0)))
    b.cyl(mats['log'], 0.34, 4.6, 9, r2=0.3, m=M((0, 0, -0.1)), nz=0.04, nf=2)
    snow_blob(b, mats, (0, 0, 4.55), 0.36, 0.55)
    bld.append(b.build(ck))
    return base, bld
