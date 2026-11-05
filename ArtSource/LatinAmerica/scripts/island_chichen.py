# Island: Mexico / Chichen Itza - El Castillo, stelae, agave, cenote
# (concept: ArtSource/RegionConcepts/LatinAmerica/ChichenItza_IslandConcept.png)
NAME = 'ChichenItza'
R_ISL = 7.8

PYR = (-0.9, 2.0)           # El Castillo centre
PW0, PW1, PH, PN = 7.6, 2.9, 4.7, 9
CENOTE = (4.6, -2.7)


def castillo_courses(b, mats, i0, i1):
    hh = PH / PN
    for i in range(i0, i1):
        w = PW0 - (PW0 - PW1) * i / PN
        frustum(b, mats['maya'], w, w, w - 0.22, w - 0.22, hh * 0.86, m=M((PYR[0], PYR[1], 0)), z0=hh * i)
        frustum(b, mats['maya_plain'], w - 0.22, w - 0.22, w - 0.3, w - 0.3, hh * 0.14, m=M((PYR[0], PYR[1], 0)), z0=hh * i + hh * 0.86)


def stairway(b, mats, m):
    """One El Castillo stairway on a face: steps climbing local +Y from the plaza to the top platform, balustrades."""
    b.push(m)
    run = (PW0 - PW1) / 2
    n = 18
    steps(b, mats['maya_plain'], n, 1.7, run / n, PH / n, m=M((0, 0, 0)), nose=0.02)
    ang = math.degrees(math.atan2(PH, run))
    L = math.hypot(PH, run)
    for s in (-1, 1):
        b.box(mats['maya'], (0.32, L + 0.2, 0.42), m=M((s * 1.0, run / 2, PH / 2), (ang, 0, 0)), base=False, bevel=0.04)
        # feathered-serpent head at the foot of each balustrade
        b.box(mats['maya_plain'], (0.5, 0.75, 0.55), m=M((s * 1.0, -0.25, 0.0)), bevel=0.1)
        b.box(mats['maya_red'], (0.42, 0.06, 0.12), m=M((s * 1.0, -0.64, 0.12)), bevel=0.0)
        for e in (-1, 1):
            b.box(mats['doorway'], (0.08, 0.05, 0.08), m=M((s * 1.0 + e * 0.13, -0.64, 0.36)), bevel=0.0)
    b.pop()


def stela(b, mats, loc, rot=0.0, h=1.9):
    b.push(M(loc, (0, 0, rot)))
    b.box(mats['jungle_rock'], (1.0, 0.7, 0.18), bevel=0.04)
    b.box(mats['maya_plain'], (0.78, 0.38, h), m=M((0, 0, 0.18)), bevel=0.06)
    b.cyl(mats['maya_plain'], 0.39, 0.38, 10, m=M((0, 0.19, 0.18 + h), (90, 0, 0)), smooth=True)
    b.push(M((0, -0.19, 0.18)))
    b.box(mats['jungle_rock'], (0.6, 0.04, h * 0.78), m=M((0, -0.01, 0.2)), bevel=0.0)
    b.box(mats['maya_plain'], (0.42, 0.06, 0.42), m=M((0, -0.03, h * 0.55)), bevel=0.04)   # face
    for e in (-1, 1):
        b.box(mats['doorway'], (0.09, 0.04, 0.07), m=M((e * 0.1, -0.07, h * 0.55 + 0.25)), bevel=0.0)
    b.box(mats['doorway'], (0.18, 0.04, 0.05), m=M((0, -0.07, h * 0.55 + 0.08)), bevel=0.0)
    b.box(mats['maya_plain'], (0.5, 0.06, 0.12), m=M((0, -0.03, h * 0.55 + 0.45)), bevel=0.02)
    for k in range(3):
        b.box(mats['maya_plain'], (0.14, 0.05, 0.14), m=M((-0.18 + 0.18 * k, -0.03, h * 0.25)), bevel=0.02)
    b.pop()
    b.pop()


def build(mats, cb, ck):
    rnd = random.Random(71)
    body = euro_body(cb, mats, R_ISL, 'jungle_rock', seed=8.8, depth=5.0, chunky=0.6)
    embedded_rocks(body, mats, R_ISL, 'granite_grey', count=12, seed=14, zr=(-0.9, -3.2), size=(0.4, 0.85))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'grass_trop', seed=6.1, drip=0.25, lip=0.42)
    for i in range(22):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.96, 1.01)
        bush(top, mats, (math.cos(a) * rr, math.sin(a) * rr, -0.2), rnd.uniform(0.32, 0.55), seed=i, mt='leaves')
    # cenote hole in the ground: dark rock walls, turquoise water (the stone rim is a build step)
    inner = blob_outline(CENOTE[0], CENOTE[1], 1.85, 1.45, seed=2.7, rot=20)
    slab(top, mats['jungle_rock'], blob_outline(CENOTE[0], CENOTE[1], 2.1, 1.7, seed=2.7, rot=20), -0.04, 0.06)
    slab(top, mats['cenote'], inner, -0.04, 0.08)
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 limestone plaza + sacbe path
    b = B('S01_Plaza')
    slab(b, mats['maya_plain'], rrect(PYR[0] + 0.4, PYR[1] - 0.5, PW0 + 2.2, PW0 + 1.2, 0.5), -0.02, 0.1)
    path_slabs(b, mats['maya_plain'], [(3.2, -6.6), (2.0, -4.4), (0.4, -3.2), (-0.9, -2.6)], w=1.6, step=0.7, seed=9)
    for (x, y) in ((PYR[0] + 0.4 - (PW0 + 2.2) / 2, PYR[1] - 0.5 - (PW0 + 1.2) / 2), (PYR[0] + 0.4 + (PW0 + 2.2) / 2, PYR[1] - 0.5 - (PW0 + 1.2) / 2)):
        b.box(mats['maya'], (0.5, 0.5, 0.35), m=M((x, y, 0)), bevel=0.05)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 / S03 El Castillo terraces
    b = B('S02_PyramidBase')
    castillo_courses(b, mats, 0, 5)
    bld.append(b.build(ck))
    b = B('S03_PyramidTop')
    castillo_courses(b, mats, 5, PN)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 grand stairways on the front and right faces
    b = B('S04_GrandStairway')
    stairway(b, mats, M((PYR[0], PYR[1] - PW0 / 2 - 0.15, 0)))
    stairway(b, mats, M((PYR[0] + PW0 / 2 + 0.15, PYR[1], 0), (0, 0, 90)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 temple on the top platform
    b = B('S05_Temple', M((PYR[0], PYR[1], PH)))
    TW = PW1 - 0.6
    b.box(mats['maya_plain'], (TW, TW, 1.35), bevel=0.05)
    b.box(mats['maya_red'], (TW + 0.08, TW + 0.08, 0.32), m=M((0, 0, 1.0)), bevel=0.02)
    for k in range(5):
        for (fx, fy, rz) in ((0, -1, 0), (1, 0, 90)):
            x = (k - 2) * TW / 5.5
            b.box(mats['maya_plain'], (0.2, 0.06, 0.2), m=M((fx * (TW / 2 + 0.05) + (x if fy else 0), fy * (TW / 2 + 0.05) + (0 if fy else x), 1.06), (0, 0, rz)), bevel=0.02)
    b.box(mats['maya_plain'], (TW + 0.3, TW + 0.3, 0.18), m=M((0, 0, 1.35)), bevel=0.04)
    b.box(mats['maya'], (TW - 0.5, TW - 0.5, 0.3), m=M((0, 0, 1.53)), bevel=0.04)
    for (face) in (M((0, -TW / 2, 0)), M((TW / 2, 0, 0), (0, 0, 90))):
        b.push(face)
        for x in (-0.55, 0.0, 0.55):
            b.box(mats['doorway'], (0.36, 0.06, 0.78), m=M((x, -0.02, 0.0)), bevel=0.0)
        for x in (-0.3, 0.3):
            b.box(mats['maya_plain'], (0.16, 0.16, 0.8), m=M((x, -0.06, 0.0)), bevel=0.02)
        b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S06 carved stelae
    for k, (x, y, rot) in enumerate(((-4.7, -2.5, 12), (-2.6, -4.3, -8), (2.4, -0.9, -25))):
        b = B(f'S06_Stela_{k}')
        stela(b, mats, (x, y, 0.0), rot, h=1.9 if k != 1 else 1.6)
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 cenote rim: boulders, ferns and hanging vines
    b = B('S07_Cenote')
    for i in range(14):
        a = 2 * math.pi * i / 14 + rnd.uniform(-0.1, 0.1)
        x = CENOTE[0] + math.cos(a + 20 * D2R) * 2.2
        y = CENOTE[1] + math.sin(a + 20 * D2R) * 1.8
        s = rnd.uniform(0.28, 0.42)
        rock(b, mats['jungle_rock'], (x, y, -0.05), (s, s * 0.85, s * 0.7), (0, 0, a / D2R), seed=i + 30)
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.4
        x = CENOTE[0] + math.cos(a) * 1.55
        y = CENOTE[1] + math.sin(a) * 1.25
        b.tube(mats['leaves'], [(x, y, 0.3), (x * 0.98 + CENOTE[0] * 0.02, y, -0.1), (x * 0.95 + CENOTE[0] * 0.05, y * 0.95 + CENOTE[1] * 0.05, -0.04)],
               [0.07, 0.05, 0.03], seg=4)
    for (x, y) in ((CENOTE[0] - 1.9, CENOTE[1] + 1.2), (CENOTE[0] + 1.8, CENOTE[1] + 1.0), (CENOTE[0] + 0.3, CENOTE[1] - 1.8)):
        fern(b, mats, (x, y, 0.1), r=0.7, n=7, seed=int(x * 10))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 agave garden
    b = B('S08_AgaveGarden')
    for i, (x, y, r) in enumerate(((-5.6, -0.4, 0.75), (-4.0, -5.3, 0.6), (0.9, -5.6, 0.55), (5.8, 1.6, 0.7),
                                   (-1.2, -4.8, 0.45), (2.0, -6.5, 0.5), (-6.0, -3.8, 0.5))):
        agave(b, mats, (x, y, 0.0), r=r * 1.3, n=11, seed=i * 3 + 1)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 jungle trees
    for k, (x, y, h, r, leaf) in enumerate(((-5.8, 4.6, 4.6, 1.9, 'leaves'), (4.4, 4.8, 4.2, 1.8, 'leaves'),
                                             (-6.2, 1.6, 3.6, 1.4, 'grass_dark'), (6.5, -0.5, 3.4, 1.3, 'leaves'))):
        b = B(f'S09_JungleTree_{k}')
        jungle_tree(b, mats, (x, y, 0.0), h=h, r=r, seed=k * 7 + 3, leaf=leaf)
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 marigold bushes and palms
    b = B('S10_FlowerBushes')
    for i, (x, y, fl) in enumerate(((-3.6, -1.6, 'flower_yellow'), (2.6, -2.4, 'flower_red'), (-5.0, -4.6, 'flower_yellow'),
                                    (3.4, -5.6, 'flower_pink'), (6.0, -2.2, 'flower_yellow'), (2.9, 1.5, 'flower_red'))):
        bush(b, mats, (x, y, 0.0), 0.5, seed=i + 40, mt='leaves', flowers=fl)
    palm(b, mats, (5.4, 2.9, 0.0), h=4.0, lean=(0.4, 0.2), seed=8, fronds=8, L=1.9)
    bld.append(b.build(ck))
    return base, bld
