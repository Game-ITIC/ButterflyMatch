# Island: South Africa / Cape Town - Table Mountain, Cape Dutch waterfront, small harbor
# (concept: ArtSource/RegionConcepts/Africa/CapeTown_IslandConcept.png)
NAME = 'CapeTownHarbor'
R_ISL = 7.8

HARBOR = (3.5, -3.4)        # harbor basin centre (front-right corner)
HOUSE = (-2.7, 0.2)         # Cape Dutch homestead (faces -Y)
MESA = (-0.6, 4.9)          # Table Mountain
LION = (5.3, 3.4)           # Lion's Head


def rocky_lathe(b, mt, loc, prof, scl, seed, amp=0.08, seg=20):
    def rfn(a, z, i):
        return amp * fbm((math.cos(a) * 2.2, math.sin(a) * 2.2, z * 3.0 + seed), 2) * (0.2 if i == len(prof) - 1 else 1.0)
    b.lathe(mt, prof, seg=seg, m=M((loc[0], loc[1], 0.0), (0, 0, 0), scl), smooth=False, rfn=rfn)


def cape_gable(w, h, n=8):
    """Ornate Cape Dutch front gable outline in local XZ: curved shoulders, scrolls and a rounded crown."""
    right = [(w / 2, 0.0)]
    for k in range(1, n + 1):        # concave shoulder
        t = k / n
        right.append((w / 2 - w * 0.22 * t, h * 0.55 * t + 0.08 * math.sin(math.pi * t)))
    for k in range(1, n + 1):        # convex scroll
        t = k / n
        right.append((w * 0.28 - w * 0.12 * t, h * 0.55 + h * 0.3 * math.sin(math.pi / 2 * t)))
    for k in range(1, n // 2 + 1):   # rounded crown up to the top centre
        a = math.pi / 2 * k / (n // 2)
        right.append((math.cos(a) * w * 0.16, h * 0.85 + math.sin(a) * h * 0.15))
    left = [(-x, z) for (x, z) in reversed(right[:-1])]
    return right + left


def build(mats, cb, ck):
    rnd = random.Random(61)
    body = euro_body(cb, mats, R_ISL, 'rock_warm', seed=2.4, depth=5.2, chunky=0.6)
    embedded_rocks(body, mats, R_ISL, 'granite_grey', count=12, seed=12, zr=(-0.9, -3.4), size=(0.4, 0.85))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'grass', seed=4.4, drip=0.2, lip=0.4)
    # harbor basin cut into the front-right corner: sea water with a sandy beach ring and boulders
    inner = blob_outline(HARBOR[0], HARBOR[1], 3.0, 2.3, seed=1.1, rot=-35)
    outer = blob_outline(HARBOR[0], HARBOR[1], 3.55, 2.85, seed=1.1, rot=-35)
    ring_slab(top, mats['beach'], outer, inner, -0.02, 0.06)
    slab(top, mats['water_sea'], inner, -0.12, 0.13)
    for i in range(10):
        a = rnd.uniform(0, 2 * math.pi)
        x = HARBOR[0] + math.cos(a) * 3.4
        y = HARBOR[1] + math.sin(a) * 2.7
        if x * x + y * y < (R_ISL * 0.95) ** 2:
            rock(top, mats['granite_grey'], (x, y, 0.0), (0.42, 0.34, 0.3), (0, 0, a * 30), seed=i)
    for (x, y, r) in ((-6.4, -4.4, 0.5), (-6.8, 1.6, 0.45), (0.6, -6.4, 0.4), (-3.4, -6.2, 0.45)):
        bush(top, mats, (x, y, 0.0), r, seed=int(x * 5), mt='fynbos')
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 harbor pier: stone quay, wooden jetty, cobble path
    b = B('S01_HarborPier')
    qa = [(HARBOR[0] - 3.1, HARBOR[1] + 1.7), (HARBOR[0] - 1.2, HARBOR[1] + 2.5), (HARBOR[0] + 1.3, HARBOR[1] + 2.6)]
    for (p, q) in zip(qa, qa[1:]):
        d = Vector((q[0] - p[0], q[1] - p[1], 0))
        c = ((p[0] + q[0]) / 2, (p[1] + q[1]) / 2)
        b.box(mats['curb'], (d.length + 0.4, 0.9, 0.55), m=M((c[0], c[1], -0.25), (0, 0, math.degrees(math.atan2(d.y, d.x)))), bevel=0.06)
    b.push(M((HARBOR[0] - 0.4, HARBOR[1] + 2.0, 0.0), (0, 0, -55)))
    b.box(mats['wood'], (0.9, 3.4, 0.12), m=M((0, -1.7, 0.12)), bevel=0.02)
    for k in range(5):
        for s in (-1, 1):
            b.cyl(mats['wood_dark'], 0.07, 0.75, 6, m=M((s * 0.42, -0.2 - 0.75 * k, -0.45)))
    b.pop()
    path_slabs(b, mats['cobble'], [(HOUSE[0] + 0.2, HOUSE[1] - 1.8), (-1.0, -2.6), (0.4, -1.6)], w=1.1, step=0.55, seed=6)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 Table Mountain with its tablecloth cloud
    b = B('S02_TableMountain')
    MS = (7.0, 2.5, 5.4)
    rocky_lathe(b, mats['fynbos'], MESA, [(1.0, 0.0), (0.88, 0.2), (0.78, 0.4), (0.0, 0.5)], MS, 3.0, amp=0.08)
    rocky_lathe(b, mats['mountain'], MESA, [(0.82, 0.3), (0.72, 0.5), (0.68, 0.66), (0.655, 0.9), (0.62, 1.0), (0.0, 1.0)],
                MS, 5.0, amp=0.09, seg=24)
    for k in range(9):
        x = MESA[0] - 3.8 + k * 0.95
        b.sphere(mats['cloth_white'], 0.6, m=M((x, MESA[1] + 0.1, MS[2] + 0.05 + 0.08 * math.sin(k * 1.7)), (0, 0, 0), (1.5, 1.6, 0.45)),
                 sub=2, nz=0.04, nf=3, seed=k)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S03 Lion's Head peak
    b = B('S03_LionsHead')
    rocky_lathe(b, mats['fynbos'], LION, [(1.0, 0.0), (0.78, 0.3), (0.0, 0.34)], (2.0, 1.7, 4.6), 7.0, amp=0.07, seg=14)
    rocky_lathe(b, mats['mountain'], LION, [(0.72, 0.25), (0.6, 0.5), (0.46, 0.75), (0.3, 0.92), (0.0, 1.0)], (2.0, 1.7, 4.6), 9.0, amp=0.07, seg=14)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 Cape Dutch house walls + ornate gable
    W, D, H = 4.6, 2.5, 2.3
    b = B('S04_CapeDutchHouse', M((HOUSE[0], HOUSE[1], 0)))
    b.box(mats['curb'], (W + 0.2, D + 0.2, 0.2), bevel=0.04)
    b.box(mats['capedutch'], (W, D, H), m=M((0, 0, 0.18)), bevel=0.05)
    b.push(M((0, -D / 2, 0.18)))
    b.prism(mats['capedutch'], cape_gable(1.9, 2.0), 0.28, m=M((0, -0.04, H - 0.05)))
    window(b, mats, 0.4, 0.55, m=M((0, -0.12, H + 0.55)), frame='frame_white', glass='glass', shutters=None, sill=None, mull=True)
    door(b, mats, 0.85, 1.6, m=M((0, 0, 0.0)), mt='shutter_green', frame='capedutch', glass_top=True)
    for x in (-1.55, -0.85, 0.85, 1.55):
        window(b, mats, 0.42, 0.85, m=M((x, 0, 0.75)), frame='frame_white', shutters='shutter_green', sill='capedutch', mull=True)
    b.pop()
    b.push(M((W / 2, 0, 0.18), (0, 0, 90)))
    window(b, mats, 0.42, 0.85, m=M((0.0, 0, 0.75)), frame='frame_white', shutters='shutter_green', sill='capedutch', mull=True)
    b.pop()
    for s in (-1, 1):
        b.prism(mats['capedutch'], [(-D / 2, 0.0), (D / 2, 0.0), (D / 2 - 0.05, 0.1), (0.0, 1.35), (-D / 2 + 0.05, 0.1)], 0.24,
                m=M((s * (W / 2 - 0.12), 0, H + 0.18), (0, 0, 90)))
        b.box(mats['capedutch'], (0.45, 0.45, 0.55), m=M((s * (W / 2 - 0.12), 0, H + 1.35)), bevel=0.04)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 thatched roof
    b = B('S05_ThatchedRoof', M((HOUSE[0], HOUSE[1], 0.18), (0, 0, 90)))
    gable_roof(b, mats, D + 0.5, W - 0.3, H - 0.05, H + 1.35, thick=0.32, over=0.0, snow_tiers=0, roof_mat='thatch',
               droop_lumps=False)
    b.box(mats['thatch_x'], (0.34, W - 0.3, 0.2), m=M((0, 0, H + 1.3)), base=False, bevel=0.08)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 lighthouse at the harbor mouth
    b = B('S06_Lighthouse', M((6.5, -0.9, 0)))
    rock(b, mats['granite_grey'], (0, 0, 0.0), (1.0, 0.9, 0.45), (0, 0, 20), seed=3)
    for k in range(5):
        b.cyl(mats['cloth_red' if k % 2 == 0 else 'cloth_white'], 0.5 - 0.04 * k, 0.62, 14, r2=0.46 - 0.04 * k, m=M((0, 0, 0.3 + 0.62 * k)), smooth=True)
    b.cyl(mats['iron'], 0.55, 0.1, 14, m=M((0, 0, 3.4)))
    b.cyl(mats['lamp_glow'], 0.3, 0.55, 10, m=M((0, 0, 3.5)))
    for k in range(6):
        a = 2 * math.pi * k / 6
        b.cyl(mats['iron'], 0.025, 0.55, 4, m=M((math.cos(a) * 0.32, math.sin(a) * 0.32, 3.5)))
    b.cone(mats['cloth_red'], 0.45, 0.5, 12, m=M((0, 0, 4.05)), smooth=True)
    b.sphere(mats['iron'], 0.07, m=M((0, 0, 4.58)), sub=1)
    rail(b, mats, 1.0, 0.3, m=M((0, -0.5, 3.5)), mt='iron', step=0.2, ornate=False)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 sailboat in the harbor
    b = B('S07_Sailboat', M((HARBOR[0] + 0.4, HARBOR[1] - 0.6, 0.02), (0, 0, 20)))
    hull = [(-1.25, 0.0), (1.25, 0.0), (1.55, 0.55), (-1.45, 0.5)]
    b.prism(mats['hull_white'], hull, 0.85, m=M((0, 0, -0.18)))
    b.prism(mats['hull_blue'], [(-1.28, 0.25), (1.38, 0.25), (1.48, 0.42), (-1.38, 0.4)], 0.9, m=M((0, 0, -0.18)))
    b.box(mats['wood'], (2.6, 0.72, 0.06), m=M((0.05, 0, 0.32)), bevel=0.02)
    b.box(mats['hull_white'], (0.8, 0.55, 0.32), m=M((-0.4, 0, 0.36)), bevel=0.06)
    b.cyl(mats['wood_dark'], 0.05, 3.3, 6, m=M((0.2, 0, 0.36)))
    b.prism(mats['sail'], [(0.25, 0.75), (1.6, 0.75), (0.25, 3.45)], 0.04, m=M((0, 0, 0.0)))
    b.prism(mats['sail'], [(0.15, 0.8), (-1.2, 0.8), (0.15, 3.0)], 0.04, m=M((0, 0.02, 0.0)))
    b.tube(mats['wood_dark'], [(0.2, 0, 0.78), (-1.3, 0, 0.78)], 0.03, seg=4)
    flag3(b, mats, ('cloth_red', 'cloth_white', 'cloth_blue'), 0.35, 0.2, m=M((0.2, 0, 3.65)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 umbrella pines
    for k, (x, y, h, r) in enumerate(((-5.7, -1.7, 4.0, 1.6), (3.5, 1.7, 4.2, 1.6), (-6.2, 3.0, 3.6, 1.4))):
        b = B(f'S08_UmbrellaTree_{k}')
        jungle_tree(b, mats, (x, y, 0.0), h=h, r=r, seed=k * 4 + 1, leaf='cypress')
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 protea garden in front of the house
    b = B('S09_ProteaGarden')
    for i, (x, y) in enumerate(((-4.9, -2.3), (-4.1, -3.3), (-3.0, -2.6), (-1.6, -3.6), (-5.2, -4.4), (-0.5, -4.9))):
        bush(b, mats, (x, y, 0.0), 0.5, seed=i + 3, mt='fynbos')
        for j in range(2):
            a = i * 1.3 + j * 2.4
            px, py, pz = x + math.cos(a) * 0.28, y + math.sin(a) * 0.28, 0.75 + 0.12 * j
            b.cone(mats['protea'], 0.32, 0.5, 7, m=M((px, py, pz - 0.12), (180, 0, 0)))
            b.sphere(mats['protea'], 0.28, m=M((px, py, pz), (0, 0, 0), (1, 1, 0.9)), sub=1, nz=0.03, nf=8, seed=a)
            b.sphere(mats['flower_white'], 0.14, m=M((px, py, pz + 0.16)), sub=1)
    for (x, y) in ((-2.0, -1.4), (-3.6, -1.4)):
        b.cone(mats['agave'], 0.1, 0.7, 4, m=M((x, y, 0.0), (20, 0, 0)))
        agave(b, mats, (x, y, 0.0), r=0.45, seed=int(x * 10), mt='agave')
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 harbor lamps, benches and bollards
    b = B('S10_HarborLamps')
    for (x, y) in ((HARBOR[0] - 2.4, HARBOR[1] + 2.9), (HARBOR[0] + 0.5, HARBOR[1] + 3.3), (-0.6, -1.0)):
        street_lamp(b, mats, (x, y, 0.0), 2.6, mt='iron')
    for (x, y) in ((HARBOR[0] - 1.6, HARBOR[1] + 2.25), (HARBOR[0] - 0.2, HARBOR[1] + 2.5), (HARBOR[0] + 1.0, HARBOR[1] + 2.55)):
        b.cyl(mats['iron'], 0.12, 0.35, 8, m=M((x, y, 0.0)))
        b.sphere(mats['iron'], 0.13, m=M((x, y, 0.35)), sub=1)
    b.push(M((HARBOR[0] - 1.0, HARBOR[1] + 3.3, 0.0), (0, 0, 8)))
    for x in (-0.7, 0.7):
        b.box(mats['iron'], (0.08, 0.45, 0.42), m=M((x, 0, 0)), bevel=0.01)
    for k in range(3):
        b.box(mats['wood'], (1.6, 0.13, 0.06), m=M((0, -0.15 + 0.15 * k, 0.42)), bevel=0.01)
    b.box(mats['wood'], (1.6, 0.05, 0.3), m=M((0, 0.25, 0.55), (-10, 0, 0)), bevel=0.01)
    b.pop()
    for (x, y, s) in ((HARBOR[0] + 1.9, HARBOR[1] + 2.9, 0.5), (HARBOR[0] + 2.3, HARBOR[1] + 2.6, 0.4)):
        b.box(mats['wood'], (s, s, s), m=M((x, y, 0.0), (0, 0, 15)), bevel=0.03)
    bld.append(b.build(ck))
    return base, bld
