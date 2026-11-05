# Island: Egypt / Giza - Great Pyramid, Great Sphinx, small oasis
# (concept: ArtSource/RegionConcepts/Africa/EgyptGiza_IslandConcept.png)
NAME = 'GizaPyramids'
R_ISL = 7.8

PYR = (-0.2, 3.3)          # pyramid centre
PYR_W, PYR_H, PYR_N = 8.0, 6.4, 15
SPHINX = M((-4.5, -3.3, 0), (0, 0, -10), 1.25)   # faces -Y (towards the camera side)
GATE = (-0.2, -0.95)       # temple gate in front of the pyramid face
OASIS = (5.2, -1.6)
PATH = [(3.9, -6.6), (2.8, -4.6), (1.2, -3.1), (-0.2, -1.95)]


def pyramid_course(b, mats, i0, i1):
    # courses i0..i1-1 of the stepped Great Pyramid; each course is a frustum with a small ledge
    hh = PYR_H / PYR_N
    for i in range(i0, i1):
        w0 = PYR_W * (1 - i / PYR_N)
        w1 = PYR_W * (1 - (i + 1) / PYR_N)
        wt = w0 - (w0 - w1) * 0.72
        frustum(b, mats['pyramid'], w0, w0, wt, wt, hh, m=M((PYR[0], PYR[1], 0)), z0=hh * i)


def build(mats, cb, ck):
    rnd = random.Random(41)
    body = euro_body(cb, mats, R_ISL, 'sandstone', seed=7.7, depth=5.0, chunky=0.6)
    embedded_rocks(body, mats, R_ISL, 'red_earth', count=12, seed=4, zr=(-0.9, -3.2), size=(0.4, 0.85))
    top = B('Island_Top')
    euro_top(top, mats, R_ISL, 'sand', seed=5.1, drip=0.12, lip=0.35)
    # low dunes and stones on the open sand
    for (x, y, rx, ry, rz) in ((-5.6, 5.4, 1.6, 1.1, 0.35), (5.6, 5.2, 1.4, 1.0, 0.3), (-6.2, -5.4, 1.2, 0.9, 0.25),
                               (6.1, -4.9, 1.1, 0.8, 0.22), (-6.4, 1.0, 0.9, 1.6, 0.28)):
        top.sphere(mats['sand_dark'], 1.0, m=M((x, y, -0.05), (0, 0, rnd.uniform(0, 90)), (rx, ry, rz)), sub=2, nz=0.05, nf=2, seed=x)
    for i in range(12):
        a = rnd.uniform(0, 2 * math.pi)
        rr = R_ISL * se_r(a) * rnd.uniform(0.84, 0.95)
        rock(top, mats['sandstone_light'], (math.cos(a) * rr, math.sin(a) * rr, 0.0), (0.32, 0.26, 0.2), (0, 0, a * 40), seed=i)
    for (x, y, r) in ((-6.6, -2.6, 0.38), (6.5, 2.6, 0.34), (2.2, 6.6, 0.3), (-3.0, -6.4, 0.32)):
        bush(top, mats, (x, y, 0.0), r, seed=int(x * 7), mt='dry_bush')
    base = [body.build(cb, pivot='origin'), top.build(cb, pivot='origin')]
    bld = []

    # ---------------------------------------------------------- S01 stone causeway
    b = B('S01_StonePath')
    path_slabs(b, mats['sandstone_light'], PATH, w=1.5, step=0.66, seed=3)
    for (p, q) in zip(PATH, PATH[1:]):
        d = Vector((q[0] - p[0], q[1] - p[1], 0))
        n = Vector((-d.y, d.x, 0)).normalized()
        ang = math.degrees(math.atan2(d.y, d.x))
        for s in (-1, 1):
            c = Vector(((p[0] + q[0]) / 2, (p[1] + q[1]) / 2, 0)) + n * s * 0.92
            b.box(mats['sandstone'], (d.length + 0.1, 0.22, 0.2), m=M((c.x, c.y, 0), (0, 0, ang)), bevel=0.04)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S02 / S03 Great Pyramid
    b = B('S02_PyramidBase')
    b.box(mats['sandstone_light'], (PYR_W + 0.6, PYR_W + 0.6, 0.12), m=M((PYR[0], PYR[1], -0.06)), bevel=0.04)
    pyramid_course(b, mats, 0, 8)
    bld.append(b.build(ck))

    b = B('S03_PyramidTop')
    pyramid_course(b, mats, 8, PYR_N)
    zc = PYR_H / PYR_N * PYR_N
    wc = PYR_W / PYR_N * 1.15
    frustum(b, mats['gold'], wc, wc, 0.04, 0.04, 0.62, m=M((PYR[0], PYR[1], 0)), z0=zc - 0.02)
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S04 temple gate (pylon portal in front of the pyramid)
    b = B('S04_TempleGate', M((GATE[0], GATE[1], 0)))
    b.box(mats['sandstone_light'], (3.2, 1.3, 0.22), bevel=0.04)
    for s in (-1, 1):
        frustum(b, mats['sphinx'], 0.95, 0.95, 0.78, 0.8, 2.3, m=M((s * 0.95, 0, 0)), z0=0.22)
        b.box(mats['sandstone_light'], (0.95, 0.98, 0.16), m=M((s * 0.95, 0, 2.5)), bevel=0.03)
    b.box(mats['sphinx'], (2.9, 0.92, 0.45), m=M((0, 0, 2.05)), bevel=0.04)
    b.box(mats['sandstone_light'], (3.1, 1.05, 0.14), m=M((0, -0.02, 2.5)), bevel=0.03)
    b.box(mats['nemes'], (2.4, 0.06, 0.26), m=M((0, -0.47, 2.14)), bevel=0.0)
    b.sphere(mats['gold'], 0.16, m=M((0, -0.5, 2.27), (0, 0, 0), (2.2, 0.4, 0.8)), sub=1)
    b.box(mats['doorway'], (0.95, 0.08, 1.85), m=M((0, -0.42, 0.22)), bevel=0.0)
    for s in (-1, 1):
        # papyrus columns flanking the doorway
        b.cyl(mats['sandstone_light'], 0.15, 1.55, 8, m=M((s * 0.62, -0.62, 0.22)), smooth=True)
        b.cyl(mats['palm_leaf'], 0.15, 0.24, 8, r2=0.27, m=M((s * 0.62, -0.62, 1.75)))
        b.box(mats['sandstone_light'], (0.4, 0.4, 0.1), m=M((s * 0.62, -0.62, 1.99)), bevel=0.02)
    b.box(mats['sandstone_light'], (1.7, 0.42, 0.14), m=M((0, -0.62, 2.07)), bevel=0.02)
    steps(b, mats['sandstone_light'], 2, 1.6, 0.3, 0.11, m=M((0, -1.25, 0)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S05 / S06 Great Sphinx
    b = B('S05_SphinxBody', SPHINX)
    b.box(mats['sandstone_light'], (2.5, 4.9, 0.32), m=M((0, 0.15, 0)), bevel=0.06)
    b.box(mats['sphinx'], (1.55, 2.9, 1.05), m=M((0, 0.75, 0.3)), bevel=0.32, smooth=True)
    b.sphere(mats['sphinx'], 1.0, m=M((0, 1.95, 0.95), (0, 0, 0), (0.85, 0.7, 0.68)), sub=2, smooth=True, nz=0.03, nf=2)
    b.box(mats['sphinx'], (1.35, 1.0, 1.25), m=M((0, -0.55, 0.3)), bevel=0.3, smooth=True)
    for s in (-1, 1):
        b.box(mats['sphinx'], (0.46, 1.75, 0.42), m=M((s * 0.47, -1.35, 0.3)), bevel=0.15, smooth=True)
        b.sphere(mats['sphinx'], 0.27, m=M((s * 0.47, -2.2, 0.48), (0, 0, 0), (1.0, 1.15, 0.7)), sub=2)
        # rear legs tucked along the flank
        b.box(mats['sphinx'], (0.3, 1.2, 0.36), m=M((s * 0.82, 1.7, 0.3)), bevel=0.12, smooth=True)
    b.tube(mats['sphinx'], [(0.75, 2.4, 0.42), (0.95, 1.6, 0.36), (0.92, 0.9, 0.36)], [0.1, 0.08, 0.06], seg=6)
    bld.append(b.build(ck))

    b = B('S06_SphinxHead', SPHINX)
    b.box(mats['sphinx'], (0.95, 0.8, 0.55), m=M((0, -0.65, 1.42)), bevel=0.2, smooth=True)
    # nemes headdress: wide trapezoid behind the face + lappets over the chest
    b.push(M((0, -0.72, 1.85)))
    b.prism(mats['nemes'], [(-0.72, -0.15), (0.72, -0.15), (0.5, 0.75), (0.22, 0.98), (-0.22, 0.98), (-0.5, 0.75)], 0.78,
            m=M((0, 0.08, 0)))
    for s in (-1, 1):
        b.box(mats['nemes'], (0.26, 0.2, 0.95), m=M((s * 0.5, -0.33, -0.55)), bevel=0.05)
    # face
    b.box(mats['sphinx'], (0.62, 0.5, 0.78), m=M((0, -0.3, -0.05)), bevel=0.14, smooth=True)
    b.box(mats['sphinx'], (0.12, 0.14, 0.22), m=M((0, -0.58, 0.22)), bevel=0.04)
    for s in (-1, 1):
        b.box(mats['doorway'], (0.14, 0.04, 0.06), m=M((s * 0.15, -0.56, 0.47)), bevel=0.0)
        b.box(mats['sphinx'], (0.2, 0.05, 0.05), m=M((s * 0.15, -0.56, 0.56)), bevel=0.0)
    b.box(mats['sphinx'], (0.22, 0.05, 0.05), m=M((0, -0.56, 0.1)), bevel=0.0)
    b.box(mats['nemes'], (0.66, 0.54, 0.14), m=M((0, -0.3, 0.72)), bevel=0.04)
    b.box(mats['sphinx'], (0.12, 0.12, 0.34), m=M((0, -0.52, -0.38)), bevel=0.03)   # beard
    b.pop()
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S07 obelisks along the causeway
    for k, (x, y) in enumerate(((2.25, -2.4), (0.35, -4.1))):
        b = B(f'S07_Obelisk_{k}', M((x, y, 0)))
        b.box(mats['sandstone_light'], (0.95, 0.95, 0.35), bevel=0.05)
        b.box(mats['sandstone'], (0.75, 0.75, 0.15), m=M((0, 0, 0.35)), bevel=0.03)
        frustum(b, mats['granite_red'], 0.5, 0.5, 0.32, 0.32, 3.3, z0=0.5)
        frustum(b, mats['gold'], 0.32, 0.32, 0.02, 0.02, 0.4, z0=3.8)
        for zz in (1.2, 1.9, 2.6):
            b.box(mats['doorway'], (0.18, 0.04, 0.22), m=M((0, -0.22 - (zz - 1.2) * 0.03, zz)), bevel=0.0)
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S08 oasis pool
    b = B('S08_OasisPool')
    inner = pool(b, mats, OASIS[0], OASIS[1], 1.55, 1.15, seed=2.2, rim='sandstone_light', rot=-20)
    ring_decal(b, mats['grass'], OASIS, 1.7, 2.25, z=0.015, seed=1.3)
    for (x, y) in ((OASIS[0] - 1.5, OASIS[1] + 0.9), (OASIS[0] + 1.75, OASIS[1] - 0.4), (OASIS[0] + 0.3, OASIS[1] - 1.55)):
        reeds(b, mats, (x, y, 0.0), n=7, h=0.9, seed=int(x * 10))
    for k in range(3):
        b.cyl(mats['flower_white'], 0.16, 0.03, 8, m=M((OASIS[0] - 0.6 + 0.6 * k, OASIS[1] + 0.25 - 0.3 * k, 0.17)))
    bld.append(b.build(ck))

    # ---------------------------------------------------------- S09 palm trees around the oasis
    for k, (x, y, h, lx, ly) in enumerate(((-6.7, -0.9, 3.5, -0.35, -0.3),
                                            (6.9, -0.3, 4.4, 0.45, 0.25),
                                            (6.3, -3.4, 3.6, 0.4, -0.45),
                                            (-6.2, 3.4, 4.0, -0.4, 0.2))):
        b = B(f'S09_Palm_{k}')
        palm(b, mats, (x, y, 0.0), h=h, lean=(lx, ly), seed=k * 3 + 1, fronds=8, L=1.8 + 0.1 * (h - 3.3))
        bld.append(b.build(ck))

    # ---------------------------------------------------------- S10 caravan market tent + pots
    b = B('S10_MarketTent')
    canopy_stall(b, mats, (5.7, 3.9, 0.0), w=2.2, d=1.5, h=2.1, rot=-10, cloth='tent')
    b.box(mats['rug'], (1.6, 1.0, 0.03), m=M((5.5, 2.3, 0.0), (0, 0, -10)), bevel=0.0)
    for (x, y, r, h) in ((4.4, 2.9, 0.24, 0.6), (4.3, 2.3, 0.18, 0.45), (7.0, 3.3, 0.26, 0.68), (7.0, 2.6, 0.18, 0.44)):
        pot(b, mats, (x, y, 0.0), r, h)
    for (x, y, f) in ((5.1, 3.7, 'spice_red'), (5.7, 3.6, 'spice_yellow'), (6.3, 3.5, 'spice_green')):
        basket(b, mats, (x, y, 0.8), 0.22, 0.16, fill=f)
    bld.append(b.build(ck))
    return base, bld
