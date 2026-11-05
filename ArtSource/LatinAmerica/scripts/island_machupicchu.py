# Island: Peru / Machu Picchu - terraces, stone citadel, Sun Temple, Huayna Picchu silhouette
# (concept: ArtSource/RegionConcepts/LatinAmerica/MachuPicchu_IslandConcept.png)
NAME = 'MachuPicchu'
R_ISL = 7.8

TC = (-1.3, 1.2)            # terrace stack centre
TW, TD, TR = 10.6, 8.4, 1.6  # lowest terrace size and corner radius
T_STEP, T_H = 1.15, 0.55     # inset and height of every terrace
PEAK = (4.3, 4.6)            # Huayna Picchu


def terrace(b, mats, k):
    w, d = TW - 2 * T_STEP * k, TD - 2 * T_STEP * k
    z0 = T_H * k
    cy = TC[1] + 0.35 * k    # terraces climb towards the back
    slab(b, mats['inca'], rrect(TC[0], cy, w, d, max(0.3, TR - 0.2 * k), n=5), z0 - 0.02, T_H + 0.02)
    slab(b, mats['terrace_grass'], rrect(TC[0], cy, w - 0.24, d - 0.24, max(0.25, TR - 0.25 - 0.2 * k), n=5), z0 + T_H - 0.02, 0.06)
    return z0 + T_H, cy


def top_level():
    k = 4
    return T_H * (k + 1), TC[1] + 0.35 * k


def stone_house(b, mats, loc, w=2.0, d=1.6, h=1.4, rot=0.0, gable_h=0.9):
    """Inca stone house: thick ashlar walls, trapezoid doorway and niches, stone gables (roof is a later step)."""
    b.push(M(loc, (0, 0, rot)))
    b.box(mats['inca'], (w, d, h), bevel=0.05)
    b.box(mats['terrace_grass'], (w - 0.36, d - 0.36, 0.05), m=M((0, 0, h - 0.02)), bevel=0.0)
    for s in (-1, 1):
        b.prism(mats['inca'], [(-d / 2, 0.0), (d / 2, 0.0), (0.0, gable_h)], 0.3, m=M((s * (w / 2 - 0.15), 0, h), (0, 0, 90)))
    b.push(M((0, -d / 2, 0)))
    b.prism(mats['doorway'], [(-0.32, 0.0), (0.32, 0.0), (0.22, 1.0), (-0.22, 1.0)], 0.06, m=M((0, -0.03, 0.0)))
    b.box(mats['inca_plain'], (0.7, 0.1, 0.12), m=M((0, -0.05, 1.0)), bevel=0.02)
    for x in (-0.65, 0.65):
        b.prism(mats['doorway'], [(-0.12, 0.0), (0.12, 0.0), (0.08, 0.36), (-0.08, 0.36)], 0.06, m=M((x, -0.03, 0.62)))
    b.pop()
    b.pop()


def llama(b, mats, loc, rot=0.0, coat='cloth_white', seed=0):
    b.push(M(loc, (0, 0, rot)))
    b.sphere(mats[coat], 1.0, m=M((0, 0, 0.75), (0, 0, 0), (0.55, 0.3, 0.3)), sub=2, nz=0.03, nf=6, seed=seed)
    for (x, y) in ((-0.35, -0.15), (-0.35, 0.15), (0.35, -0.15), (0.35, 0.15)):
        b.cyl(mats[coat], 0.07, 0.62, 6, r2=0.06, m=M((x, y, 0.0)))
    b.tube(mats[coat], [(0.42, 0, 0.85), (0.52, 0, 1.25), (0.56, 0, 1.55)], [0.13, 0.11, 0.1], seg=7)
    b.box(mats[coat], (0.34, 0.2, 0.2), m=M((0.64, 0, 1.5)), bevel=0.08)
    for s in (-1, 1):
        b.cone(mats[coat], 0.05, 0.22, 5, m=M((0.54, s * 0.07, 1.65), (s * 10, -10, 0)))
        b.sphere(mats['doorway'], 0.03, m=M((0.72, s * 0.09, 1.58)), sub=1)
    b.box(mats['textile'], (0.5, 0.42, 0.06), m=M((0, 0, 1.03)), bevel=0.02)
    b.sphere(mats[coat], 0.1, m=M((-0.56, 0, 0.88)), sub=1)
    b.pop()


def build(mats, cb, ck):
    rnd = random.Random(83)
    body = euro_body(cb, mats, R_ISL, 'granite_grey', seed=5.5, depth=5.4, chunky=0.75)
    embedded_rocks(body, mats, R_ISL, 'jungle_rock', count=14, seed=16, zr=(-0.9, -3.4), size=(0.45, 0.9))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'moss', seed=7.2, drip=0.3, lip=0.45)
    for i in range(18):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.96, 1.01)
        bush(top, mats, (math.cos(a) * rr, math.sin(a) * rr, -0.2), rnd.uniform(0.3, 0.55), seed=i, mt='leaves')
    for i in range(8):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.84, 0.94)
        rock(top, mats['granite_grey'], (math.cos(a) * rr, math.sin(a) * rr, 0.0), (0.35, 0.3, 0.25), (0, 0, a * 40), seed=i + 5)
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 / S02 agricultural terraces
    b = B('S01_LowerTerraces')
    for k in range(3):
        terrace(b, mats, k)
    bld.append(b.build(ck))
    b = B('S02_UpperTerraces')
    for k in range(3, 5):
        terrace(b, mats, k)
    bld.append(b.build(ck))
    ztop, ytop = top_level()

    # ---------------------------------------------------------- S03 Huayna Picchu peak behind the citadel
    b = B('S03_HuaynaPicchu')
    prof = [(1.0, 0.0), (0.82, 0.18), (0.62, 0.38), (0.45, 0.58), (0.3, 0.78), (0.14, 0.94), (0.0, 1.0)]
    def rfn(a, z, i):
        return 0.09 * fbm((math.cos(a) * 2.0, math.sin(a) * 2.0, z * 3.0 + 1.3), 2) * (0.2 if i == len(prof) - 1 else 1.0)
    b.lathe(mats['moss'], prof, seg=16, m=M((PEAK[0], PEAK[1], 0.0), (0, 0, 0), (2.8, 2.4, 8.2)), smooth=False, rfn=rfn)
    def rfn2(a, z, i):
        return 0.05 * fbm((math.cos(a) * 2.0, math.sin(a) * 2.0, z * 3.0 + 4.1), 2)
    # bare granite summit cap, slightly proud of the green slope
    b.lathe(mats['granite_grey'], [(0.5, 0.6), (0.35, 0.77), (0.2, 0.93), (0.02, 1.01)], seg=16,
            m=M((PEAK[0], PEAK[1], 0.0), (0, 0, 0), (2.8, 2.4, 8.2)), smooth=False, rfn=rfn2)

    def prof_r(zn):
        for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
            if z0 <= zn <= z1:
                return r0 + (r1 - r0) * (zn - z0) / (z1 - z0)
        return 0.0
    for k in range(4):   # terraces cut into the peak's camera-facing flank
        z = 1.2 + k * 0.95
        r = prof_r(z / 8.2)
        a = -1.9 + 0.1 * k   # towards the camera / -Y side
        x, y = PEAK[0] + math.cos(a) * r * 2.8 * 0.93, PEAK[1] + math.sin(a) * r * 2.4 * 0.93
        b.box(mats['inca'], (r * 3.0, 0.55, 0.42), m=M((x, y, z - 0.3), (0, 0, a / D2R + 90)), bevel=0.04)
        b.box(mats['terrace_grass'], (r * 2.9, 0.45, 0.06), m=M((x, y, z + 0.1), (0, 0, a / D2R + 90)), bevel=0.0)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 stone houses on the top terrace
    houses = [((-3.6, ytop + 1.2, ztop), 0), ((-1.0, ytop + 1.6, ztop), 0), ((1.4, ytop + 0.6, ztop), -10)]
    b = B('S04_StoneHouses')
    for (loc, rot) in houses:
        stone_house(b, mats, loc, rot=rot)
    b.box(mats['inca'], (5.4, 0.35, 0.5), m=M((-2.2, ytop - 0.6, ztop)), bevel=0.04)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 thatched roofs
    b = B('S05_ThatchedRoofs')
    for (loc, rot) in houses:
        b.push(M(loc, (0, 0, rot + 90)))
        gable_roof(b, mats, 1.6 + 0.4, 2.0 + 0.1, 1.35, 1.4 + 0.95, thick=0.22, over=0.12, snow_tiers=0, roof_mat='thatch', droop_lumps=False,
                   fringe=True)
        b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 Sun Temple (Torreon): curved tower on the third terrace
    z3 = T_H * 3
    st = (0.9, TC[1] + 0.35 * 2 - (TD - 2 * T_STEP * 2) / 2 + 1.4)
    b = B('S06_SunTemple', M((st[0], st[1], z3)))
    b.lathe(mats['inca'], [(1.25, 0.0), (1.2, 1.7), (1.1, 1.85), (0.0, 1.85)], seg=20, smooth=False)
    b.box(mats['inca'], (2.2, 1.6, 1.3), m=M((-1.0, 0.6, 0.0)), bevel=0.05)
    b.cyl(mats['inca_plain'], 1.3, 0.14, 20, m=M((0, 0, 1.85)))
    for k in range(3):
        a = (-120 + k * 35) * D2R
        b.prism(mats['doorway'], [(-0.14, 0.0), (0.14, 0.0), (0.1, 0.42), (-0.1, 0.42)], 0.08,
                m=M((math.cos(a) * 1.23, math.sin(a) * 1.23, 0.85), (0, 0, a / D2R + 90)))
    b.cyl(mats['gold'], 0.32, 0.05, 16, m=M((0, -1.26, 1.25), (90, 0, 0)))
    for k in range(8):
        a = k * math.pi / 4
        b.box(mats['gold'], (0.07, 0.03, 0.2), m=M((math.cos(a) * 0.4, -1.3, 1.25 + math.sin(a) * 0.4), (0, -a / D2R + 90, 0)), bevel=0.0)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 stone stairs up the terraces
    b = B('S07_StoneStairs')
    for k in range(5):
        w, d = TW - 2 * T_STEP * k, TD - 2 * T_STEP * k
        cy = TC[1] + 0.35 * k
        y_front = cy - d / 2
        steps(b, mats['inca_plain'], 3, 1.1, T_STEP / 3, T_H / 3, m=M((-1.9, y_front - T_STEP + 0.05 if k > 0 else y_front - 1.15, T_H * k - (0 if k > 0 else 0))))
    path_slabs(b, mats['inca_plain'], [(-1.0, -6.9), (-1.9, -5.0)], w=1.1, step=0.55, seed=4)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 water channel and fountain basins
    b = B('S08_WaterChannel')
    for k in range(4):
        w, d = TW - 2 * T_STEP * k, TD - 2 * T_STEP * k
        cy = TC[1] + 0.35 * k
        x = TC[0] - w / 2 + 0.9
        z = T_H * (k + 1)
        b.box(mats['inca_plain'], (0.55, 0.55, 0.3), m=M((x, cy - 0.4, z - 0.02)), bevel=0.04)
        b.box(mats['water_pool'], (0.36, 0.36, 0.05), m=M((x, cy - 0.4, z + 0.26)), bevel=0.0)
        b.box(mats['water_pool'], (0.14, 0.06, T_H), m=M((x, cy - 0.7, z - T_H + 0.05)), bevel=0.0)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 textile stall: blanket, pots, rolled weavings
    b = B('S09_TextileStall')
    canopy_stall(b, mats, (4.3, -2.2, 0.0), w=2.0, d=1.3, h=1.9, rot=-20, cloth='textile', table='wood')
    b.box(mats['rug'], (1.6, 1.1, 0.04), m=M((2.6, -3.9, 0.0), (0, 0, -20)), bevel=0.0)
    for (x, y, r, h, mt) in ((5.6, -2.9, 0.24, 0.55, 'clay'), (5.6, -3.5, 0.18, 0.42, 'terracotta'), (3.0, -2.4, 0.2, 0.45, 'clay')):
        pot(b, mats, (x, y, 0.0), r, h, mt=mt)
    for k in range(3):
        b.cyl(mats['textile' if k != 1 else 'rug'], 0.13, 0.9, 10, m=M((2.3 + 0.3 * k, -3.6 - 0.1 * k, 0.17), (0, 90, -20)), smooth=True)
    for (x, y) in ((5.4, -1.6), (2.2, -4.6)):
        b.lathe(mats['flower_red'], [(0.0, 0.0), (0.12, 0.0), (0.16, 0.5), (0.0, 0.62)], seg=6, m=M((x, y, 0.0)))
        bush(b, mats, (x, y, 0.0), 0.35, seed=int(x * 7), mt='leaves', flowers='flower_red')
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 llamas grazing
    for k, (x, y, rot, coat) in enumerate(((5.2, -4.6, 150, 'cloth_white'), (-5.4, -5.2, 30, 'bread'))):
        b = B(f'S10_Llama_{k}')
        llama(b, mats, (x, y, 0.0), rot, coat=coat, seed=k)
        bld.append(b.build(ck))
    return base, bld
