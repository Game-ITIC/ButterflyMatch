# Procedural stylized-snow modelling kit for the "North" region islands (Blender 5.x, headless).
import bpy, bmesh, math, random, os
import numpy as np
from mathutils import Matrix, Vector, Euler, noise

D2R = math.pi / 180.0


# ---------------------------------------------------------------- scene
def reset():
    bpy.ops.wm.read_homefile(use_empty=True)
    sc = bpy.context.scene
    sc.unit_settings.system = 'METRIC'
    sc.unit_settings.scale_length = 1.0
    return sc


def s2l(c):
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c)


def hexc(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


# ---------------------------------------------------------------- materials
MATS = {}

SNOW_A = '#c9d6ea'
SNOW_B = '#f7f9fd'


def _ramp(nt, c1, c2, fac_socket, p0=0.3, p1=0.7):
    r = nt.nodes.new('ShaderNodeValToRGB')
    e = r.color_ramp.elements
    e[0].position = p0
    e[0].color = (*s2l(hexc(c1)), 1)
    e[1].position = p1
    e[1].color = (*s2l(hexc(c2)), 1)
    nt.links.new(fac_socket, r.inputs['Fac'])
    return r.outputs['Color']


def _mix(nt, fac, a, b):
    m = nt.nodes.new('ShaderNodeMix')
    m.data_type = 'RGBA'
    nt.links.new(fac, m.inputs[0]) if not isinstance(fac, float) else None
    if isinstance(fac, float):
        m.inputs[0].default_value = fac
    nt.links.new(a, m.inputs[6])
    nt.links.new(b, m.inputs[7])
    return m.outputs[2]


def mat(name, c1, c2=None, kind='noise', scale=2.0, snow=False, snow_lo=0.55, snow_hi=0.75,
        wave_dir='Z', emission=None):
    """kind: noise | wood | log | rock | brick | flat"""
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    nt.links.new(bsdf.outputs[0], out.inputs['Surface'])
    tc = nt.nodes.new('ShaderNodeTexCoord')
    obj_co = tc.outputs['Object']
    c2 = c2 or c1

    nz = nt.nodes.new('ShaderNodeTexNoise')
    nz.inputs['Scale'].default_value = scale
    nz.inputs['Detail'].default_value = 5.0
    nz.inputs['Roughness'].default_value = 0.55
    nt.links.new(obj_co, nz.inputs['Vector'])

    if kind == 'flat':
        col = _ramp(nt, c1, c2, nz.outputs['Fac'], 0.35, 0.65)
    elif kind in ('wood', 'log'):
        wv = nt.nodes.new('ShaderNodeTexWave')
        wv.wave_type = 'BANDS'
        wv.bands_direction = wave_dir
        wv.inputs['Scale'].default_value = scale * (1.6 if kind == 'wood' else 0.8)
        wv.inputs['Distortion'].default_value = 6.0
        wv.inputs['Detail'].default_value = 3.0
        nt.links.new(obj_co, wv.inputs['Vector'])
        mth = nt.nodes.new('ShaderNodeMath')
        mth.operation = 'MULTIPLY_ADD'
        nt.links.new(wv.outputs['Fac'], mth.inputs[0])
        mth.inputs[1].default_value = 0.55
        nt.links.new(nz.outputs['Fac'], mth.inputs[2])
        col = _ramp(nt, c1, c2, mth.outputs[0], 0.35, 0.95)
    elif kind == 'rock':
        vo = nt.nodes.new('ShaderNodeTexVoronoi')
        vo.inputs['Scale'].default_value = scale * 1.4
        nt.links.new(obj_co, vo.inputs['Vector'])
        mth = nt.nodes.new('ShaderNodeMath')
        mth.operation = 'MULTIPLY_ADD'
        nt.links.new(vo.outputs['Distance'], mth.inputs[0])
        mth.inputs[1].default_value = 0.7
        nt.links.new(nz.outputs['Fac'], mth.inputs[2])
        col = _ramp(nt, c1, c2, mth.outputs[0], 0.45, 1.05)
    elif kind == 'brick':
        br = nt.nodes.new('ShaderNodeTexBrick')
        br.inputs['Scale'].default_value = scale
        br.inputs['Mortar Size'].default_value = 0.025
        br.inputs['Color1'].default_value = (*s2l(hexc(c1)), 1)
        br.inputs['Color2'].default_value = (*s2l(hexc(c2)), 1)
        br.inputs['Mortar'].default_value = (*s2l(hexc('#3c3f46')), 1)
        sp = nt.nodes.new('ShaderNodeSeparateXYZ')
        nt.links.new(obj_co, sp.inputs[0])
        ad = nt.nodes.new('ShaderNodeMath')
        ad.operation = 'ADD'
        nt.links.new(sp.outputs['X'], ad.inputs[0])
        nt.links.new(sp.outputs['Y'], ad.inputs[1])
        cb = nt.nodes.new('ShaderNodeCombineXYZ')
        nt.links.new(ad.outputs[0], cb.inputs['X'])
        nt.links.new(sp.outputs['Z'], cb.inputs['Y'])
        nt.links.new(cb.outputs[0], br.inputs['Vector'])
        mx = nt.nodes.new('ShaderNodeMath')
        mx.operation = 'MULTIPLY_ADD'
        nt.links.new(nz.outputs['Fac'], mx.inputs[0])
        mx.inputs[1].default_value = 0.5
        mx.inputs[2].default_value = 0.75
        mc = nt.nodes.new('ShaderNodeMix')
        mc.data_type = 'RGBA'
        mc.blend_type = 'MULTIPLY'
        mc.inputs[0].default_value = 1.0
        nt.links.new(br.outputs['Color'], mc.inputs[6])
        nt.links.new(mx.outputs[0], mc.inputs[7])
        col = mc.outputs[2]
    else:
        col = _ramp(nt, c1, c2, nz.outputs['Fac'])

    if snow:
        geo = nt.nodes.new('ShaderNodeNewGeometry')
        sep = nt.nodes.new('ShaderNodeSeparateXYZ')
        nt.links.new(geo.outputs['Normal'], sep.inputs[0])
        n2 = nt.nodes.new('ShaderNodeTexNoise')
        n2.inputs['Scale'].default_value = 3.5
        n2.inputs['Detail'].default_value = 4.0
        nt.links.new(obj_co, n2.inputs['Vector'])
        add = nt.nodes.new('ShaderNodeMath')
        add.operation = 'MULTIPLY_ADD'
        nt.links.new(n2.outputs['Fac'], add.inputs[0])
        add.inputs[1].default_value = 0.35
        nt.links.new(sep.outputs['Z'], add.inputs[2])
        mr = nt.nodes.new('ShaderNodeMapRange')
        mr.inputs['From Min'].default_value = snow_lo + 0.17
        mr.inputs['From Max'].default_value = snow_hi + 0.17
        nt.links.new(add.outputs[0], mr.inputs['Value'])
        snowcol = _ramp(nt, SNOW_A, SNOW_B, n2.outputs['Fac'], 0.3, 0.7)
        col = _mix(nt, mr.outputs['Result'], col, snowcol)

    nt.links.new(col, bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.9
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*s2l(hexc(emission)), 1)
        bsdf.inputs['Emission Strength'].default_value = 2.0
    MATS[name] = m
    return m


def palette():
    P = {}
    P['snow'] = mat('snow', SNOW_A, SNOW_B, scale=1.6)
    P['snow_trampled'] = mat('snow_trampled', '#9e9ca2', '#c8c9d0', scale=3.0)
    P['dirt'] = mat('dirt', '#5a3a26', '#8a5c3c', kind='rock', scale=0.9)
    P['rock'] = mat('rock', '#5d6470', '#a3abb8', kind='rock', scale=0.7, snow=True, snow_lo=0.3, snow_hi=0.5)
    P['rock_plain'] = mat('rock_plain', '#474d57', '#8c94a1', kind='rock', scale=0.7)
    P['rock_moss'] = mat('rock_moss', '#4b5a52', '#8d9a8f', kind='rock', scale=1.0, snow=True, snow_lo=0.5, snow_hi=0.7)
    P['stone'] = mat('stone', '#535a64', '#8f97a3', kind='rock', scale=1.8, snow=True, snow_lo=0.78, snow_hi=0.92)
    P['brick'] = mat('brick', '#5f6670', '#8a919c', kind='brick', scale=1.6)
    P['crag'] = mat('crag', '#4a515b', '#959dab', kind='rock', scale=0.8, snow=True, snow_lo=0.5, snow_hi=0.68)
    P['wood'] = mat('wood', '#4a2c1a', '#7d5233', kind='wood', scale=1.2, snow=True, wave_dir='X')
    P['wood_v'] = mat('wood_v', '#4a2c1a', '#7d5233', kind='wood', scale=1.2, snow=True, wave_dir='Z')
    P['wood_dark'] = mat('wood_dark', '#2e1c12', '#4f3220', kind='wood', scale=1.4, snow=True, wave_dir='X')
    P['log'] = mat('log', '#5b3820', '#8c5d38', kind='log', scale=1.0, snow=True, wave_dir='Z')
    P['bark'] = mat('bark', '#3d2718', '#5e3e28', kind='log', scale=1.5, wave_dir='Z')
    P['thatch'] = mat('thatch', '#8a6a3c', '#c9a46a', kind='wood', scale=2.5, wave_dir='Y')
    P['straw'] = mat('straw', '#a98a4f', '#d9bd7e', kind='wood', scale=3.0, wave_dir='Z', snow=True)
    P['canvas'] = mat('canvas', '#a89a80', '#d9cdb3', scale=2.5, snow=True, snow_lo=0.7, snow_hi=0.9)
    P['cloth_grey'] = mat('cloth_grey', '#8f9097', '#c3c5cc', scale=2.0)
    P['cloth_blue'] = mat('cloth_blue', '#1f2c52', '#3a4f86', scale=2.0)
    P['cloth_green'] = mat('cloth_green', '#1c3a33', '#335f52', scale=2.0)
    P['cloth_red'] = mat('cloth_red', '#7a1f1a', '#b83a2c', scale=3.0)
    P['cloth_brown'] = mat('cloth_brown', '#6b4630', '#9a6a48', scale=3.0)
    P['cloth_white'] = mat('cloth_white', '#d8d8dc', '#f4f4f6', scale=3.0)
    P['emblem'] = mat('emblem', '#1b1b20', '#2f3036', kind='flat')
    P['emblem_white'] = mat('emblem_white', '#e9edf5', '#ffffff', kind='flat')
    P['iron'] = mat('iron', '#2f3237', '#5a5f68', scale=4.0)
    P['steel'] = mat('steel', '#6f7680', '#b6bdc7', scale=3.0)
    P['pine'] = mat('pine', '#163a32', '#2f5f4e', scale=2.2)
    P['bone'] = mat('bone', '#b8a487', '#ece2cc', scale=2.0)
    P['rope'] = mat('rope', '#7d6545', '#a88c62', scale=6.0)
    P['fire'] = mat('fire', '#ff7a1a', '#ffd25a', kind='flat', emission='#ff8a2a')
    P['ember'] = mat('ember', '#2a1208', '#c2410f', kind='flat')
    P['glow'] = mat('glow', '#e8621a', '#ffb048', kind='flat', emission='#ff8530')
    P['meat'] = mat('meat', '#6a2a12', '#b4582a', scale=4.0)
    P['ice'] = mat('ice', '#a9cbe8', '#e6f3ff', kind='flat')
    return P


# ---------------------------------------------------------------- noise
def fbm(p, oct=3):
    v, a, f = 0.0, 1.0, 1.0
    for _ in range(oct):
        v += a * noise.noise(Vector(p) * f)
        a *= 0.5
        f *= 2.03
    return v


def M(loc=(0, 0, 0), rot=(0, 0, 0), scl=(1, 1, 1)):
    if isinstance(scl, (int, float)):
        scl = (scl, scl, scl)
    return (Matrix.Translation(Vector(loc)) @ Euler([r * D2R for r in rot]).to_matrix().to_4x4()
            @ Matrix.Diagonal((scl[0], scl[1], scl[2], 1.0)))


# ---------------------------------------------------------------- builder
class B:
    """Accumulates primitives into one mesh object (one animated part)."""

    def __init__(self, name, xf=None):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []
        self.xf = [xf or Matrix.Identity(4)]

    def push(self, m):
        self.xf.append(self.xf[-1] @ m)
        return self

    def pop(self):
        self.xf.pop()
        return self

    def _slot(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def add_bm(self, t, mt, smooth=False, m=None, nz=0.0, nf=1.0, nseed=0.0):
        mm = self.xf[-1] @ (m or Matrix.Identity(4))
        bmesh.ops.transform(t, matrix=mm, verts=t.verts)
        if nz:
            t.normal_update()
            for v in t.verts:
                v.co += v.normal * nz * fbm((v.co * nf) + Vector((nseed, nseed * 1.7, 0)))
        idx = self._slot(mt)
        vm = {}
        for v in t.verts:
            vm[v] = self.bm.verts.new(v.co)
        for f in t.faces:
            try:
                nfc = self.bm.faces.new([vm[v] for v in f.verts])
            except ValueError:
                continue
            nfc.material_index = idx
            nfc.smooth = smooth
        t.free()
        return self

    # --- primitives (all local, then transformed by m and the push stack)
    def box(self, mt, size, m=None, bevel=0.03, base=True, smooth=False, nz=0.0, nf=1.0, seed=0.0):
        t = bmesh.new()
        bmesh.ops.create_cube(t, size=1.0)
        bmesh.ops.scale(t, vec=Vector(size), verts=t.verts)
        if base:
            bmesh.ops.translate(t, vec=Vector((0, 0, size[2] / 2)), verts=t.verts)
        if bevel > 0:
            bmesh.ops.bevel(t, geom=list(t.edges), offset=min(bevel, min(size) * 0.45), segments=1,
                            affect='EDGES', profile=0.5)
        return self.add_bm(t, mt, smooth, m, nz, nf, seed)

    def cyl(self, mt, r, h, seg=10, r2=None, m=None, base=True, smooth=False, nz=0.0, nf=1.0, seed=0.0, caps=True):
        t = bmesh.new()
        bmesh.ops.create_cone(t, cap_ends=caps, cap_tris=False, segments=seg, radius1=r,
                              radius2=r if r2 is None else r2, depth=h)
        if base:
            bmesh.ops.translate(t, vec=Vector((0, 0, h / 2)), verts=t.verts)
        return self.add_bm(t, mt, smooth, m, nz, nf, seed)

    def cone(self, mt, r, h, seg=10, m=None, smooth=False, nz=0.0, nf=1.0, seed=0.0):
        return self.cyl(mt, r, h, seg, r2=0.0, m=m, smooth=smooth, nz=nz, nf=nf, seed=seed)

    def sphere(self, mt, r, m=None, sub=2, smooth=True, nz=0.0, nf=1.0, seed=0.0):
        t = bmesh.new()
        bmesh.ops.create_icosphere(t, subdivisions=sub, radius=r)
        return self.add_bm(t, mt, smooth, m, nz, nf, seed)

    def lathe(self, mt, prof, seg=16, m=None, smooth=True, rfn=None, nz=0.0, nf=1.0, seed=0.0):
        """prof: list of (r, z) bottom->top. r==0 at an end makes a pole. rfn(theta, z, i) -> radius offset."""
        t = bmesh.new()
        rings = []
        for i, (r, z) in enumerate(prof):
            if r <= 1e-6:
                rings.append([t.verts.new((0, 0, z))])
                continue
            ring = []
            for j in range(seg):
                a = 2 * math.pi * j / seg
                rr = r + (rfn(a, z, i) if rfn else 0.0)
                ring.append(t.verts.new((math.cos(a) * rr, math.sin(a) * rr, z)))
            rings.append(ring)
        for i in range(len(rings) - 1):
            A, Bv = rings[i], rings[i + 1]
            if len(A) == 1 and len(Bv) == 1:
                continue
            if len(A) == 1:
                for j in range(seg):
                    t.faces.new((A[0], Bv[j], Bv[(j + 1) % seg]))
            elif len(Bv) == 1:
                for j in range(seg):
                    t.faces.new((A[j], Bv[0], A[(j + 1) % seg]))
            else:
                for j in range(seg):
                    t.faces.new((A[j], A[(j + 1) % seg], Bv[(j + 1) % seg], Bv[j]))
        if len(rings[0]) > 1:
            t.faces.new(list(reversed(rings[0])))
        if len(rings[-1]) > 1:
            t.faces.new(rings[-1])
        bmesh.ops.recalc_face_normals(t, faces=t.faces)
        return self.add_bm(t, mt, smooth, m, nz, nf, seed)

    def tube(self, mt, pts, radii, seg=8, m=None, smooth=True, caps=True):
        t = bmesh.new()
        pts = [Vector(p) for p in pts]
        if isinstance(radii, (int, float)):
            radii = [radii] * len(pts)
        rings = []
        prev_n = None
        for i, p in enumerate(pts):
            if i == 0:
                tg = pts[1] - pts[0]
            elif i == len(pts) - 1:
                tg = pts[-1] - pts[-2]
            else:
                tg = (pts[i + 1] - pts[i - 1])
            tg.normalize()
            if prev_n is None:
                up = Vector((0, 0, 1)) if abs(tg.z) < 0.9 else Vector((1, 0, 0))
                n1 = tg.cross(up).normalized()
            else:
                n1 = (prev_n - tg * prev_n.dot(tg)).normalized()
            prev_n = n1
            n2 = tg.cross(n1).normalized()
            ring = []
            for j in range(seg):
                a = 2 * math.pi * j / seg
                ring.append(t.verts.new(p + (n1 * math.cos(a) + n2 * math.sin(a)) * radii[i]))
            rings.append(ring)
        for i in range(len(rings) - 1):
            for j in range(seg):
                t.faces.new((rings[i][j], rings[i][(j + 1) % seg], rings[i + 1][(j + 1) % seg], rings[i + 1][j]))
        if caps:
            t.faces.new(list(reversed(rings[0])))
            t.faces.new(rings[-1])
        bmesh.ops.recalc_face_normals(t, faces=t.faces)
        return self.add_bm(t, mt, smooth, m)

    def prism(self, mt, pts2, depth, m=None, smooth=False, bend=None):
        """2D polygon in local XZ (x right, z up), extruded along Y (centered). bend(x,z)->dy."""
        t = bmesh.new()
        F, Bk = [], []
        for (x, z) in pts2:
            dy = bend(x, z) if bend else 0.0
            F.append(t.verts.new((x, -depth / 2 + dy, z)))
            Bk.append(t.verts.new((x, depth / 2 + dy, z)))
        n = len(pts2)
        t.faces.new(F)
        t.faces.new(list(reversed(Bk)))
        for i in range(n):
            t.faces.new((F[i], Bk[i], Bk[(i + 1) % n], F[(i + 1) % n]))
        bmesh.ops.recalc_face_normals(t, faces=t.faces)
        bmesh.ops.triangulate(t, faces=[f for f in t.faces if len(f.verts) > 4])
        return self.add_bm(t, mt, smooth, m)

    def cloth(self, mt, w, h, m=None, notch=0.0, nx=8, nz=10, wave=0.12, depth=0.05, phase=0.0):
        """Hanging banner: top edge at z=0, hangs down to -h, in XZ plane, gentle wave in Y."""
        t = bmesh.new()
        grid = []
        for iz in range(nz + 1):
            row = []
            for ix in range(nx + 1):
                u = ix / nx
                v = iz / nz
                x = (u - 0.5) * w
                z = -v * h
                if notch > 0 and iz == nz:
                    z += notch * (1 - abs(u - 0.5) * 2)
                y = math.sin(u * 5.0 + phase) * wave * v + math.sin(v * 3.0 + phase) * wave * 0.4
                row.append(t.verts.new((x, y, z)))
            grid.append(row)
        for iz in range(nz):
            for ix in range(nx):
                t.faces.new((grid[iz][ix], grid[iz + 1][ix], grid[iz + 1][ix + 1], grid[iz][ix + 1]))
        # thickness
        ret = bmesh.ops.solidify(t, geom=list(t.faces), thickness=depth)
        bmesh.ops.recalc_face_normals(t, faces=t.faces)
        return self.add_bm(t, mt, True, m)

    def star_cone(self, mt, r_out, h, pts=7, r_in_f=0.72, droop=0.18, m=None, smooth=False, hollow=0.25):
        t = bmesh.new()
        apex = t.verts.new((0, 0, h))
        ring = []
        for j in range(pts * 2):
            a = math.pi * j / pts
            r = r_out if j % 2 == 0 else r_out * r_in_f
            z = -droop if j % 2 == 0 else 0.0
            ring.append(t.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
        bot = t.verts.new((0, 0, h * hollow))
        n = len(ring)
        for j in range(n):
            t.faces.new((apex, ring[j], ring[(j + 1) % n]))
            t.faces.new((bot, ring[(j + 1) % n], ring[j]))
        bmesh.ops.recalc_face_normals(t, faces=t.faces)
        return self.add_bm(t, mt, smooth, m)

    # --- finish
    def build(self, coll, pivot='bottom'):
        me = bpy.data.meshes.new(self.name)
        bm = self.bm
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(me)
        bm.free()
        for m_ in self.mats:
            me.materials.append(m_)
        co = np.empty(len(me.vertices) * 3)
        me.vertices.foreach_get('co', co)
        co = co.reshape(-1, 3)
        mn, mx = co.min(0), co.max(0)
        if pivot == 'bottom':
            pv = Vector(((mn[0] + mx[0]) / 2, (mn[1] + mx[1]) / 2, mn[2]))
        elif pivot == 'origin':
            pv = Vector((0, 0, 0))
        else:
            pv = Vector(pivot)
        me.transform(Matrix.Translation(-pv))
        me.update()
        ob = bpy.data.objects.new(self.name, me)
        ob.location = pv
        coll.objects.link(ob)
        return ob


# ---------------------------------------------------------------- reusable props
def pine(b, mats, loc, h=4.5, r=1.4, tiers=5, rot=0.0, seed=0):
    rnd = random.Random(seed)
    b.push(M(loc, (0, 0, rot)))
    b.cyl(mats['bark'], 0.16 * h / 4.5, h * 0.3, 7, r2=0.1 * h / 4.5)
    for i in range(tiers):
        t = i / (tiers - 1)
        z = h * (0.16 + 0.68 * t)
        rr = r * (1.0 - 0.75 * t)
        hh = h * 0.34 * (1.0 - 0.35 * t)
        a = rnd.uniform(0, 60)
        b.star_cone(mats['pine'], rr, hh, pts=7, droop=0.22 * rr, m=M((0, 0, z), (0, 0, a)))
        # snow layer sits on top of each tier, leaving the green rim visible
        b.star_cone(mats['snow'], rr * 0.8, hh * 0.78, pts=7, droop=0.16 * rr,
                    m=M((0, 0, z + hh * 0.26), (0, 0, a + 8)), smooth=True)
    b.cone(mats['snow'], r * 0.16, h * 0.16, 7, m=M((0, 0, h * 0.95)), smooth=True)
    b.pop()


def snow_blob(b, mats, loc, r=0.2, sz=0.55, seed=0.0, scl=None):
    b.sphere(mats['snow'], r, m=M(loc, (0, 0, 0), scl or (1.0, 1.0, sz)), sub=2, nz=r * 0.25, nf=4.0 / max(r, 0.05),
             seed=seed)


def rock(b, mt, loc, size=(1, 1, 0.8), rot=(0, 0, 0), seed=0.0, sub=2):
    b.sphere(mt, 1.0, m=M(loc, rot, size), sub=sub, smooth=False, nz=0.28, nf=1.1, seed=seed)


def barrel(b, mats, loc, h=1.1, r=0.45, rot=(0, 0, 0)):
    b.push(M(loc, rot))
    prof = [(0, 0), (r * 0.86, 0), (r, h * 0.5), (r * 0.86, h), (0, h)]
    b.lathe(mats['wood_v'], [(r * 0.86, 0), (r * 0.95, h * 0.25), (r, h * 0.5), (r * 0.95, h * 0.75),
                             (r * 0.86, h)], seg=14, smooth=True)
    for zf in (0.12, 0.88):
        rr = r * (0.9 if zf < 0.5 else 0.9)
        b.cyl(mats['iron'], rr + 0.025, 0.07, 14, m=M((0, 0, h * zf - 0.035)))
    for zf in (0.38, 0.62):
        b.cyl(mats['iron'], r * 0.99 + 0.025, 0.06, 14, m=M((0, 0, h * zf - 0.03)))
    b.cyl(mats['wood_dark'], r * 0.8, 0.04, 14, m=M((0, 0, h - 0.06)))
    snow_blob(b, mats, (0, 0, h + 0.02), r * 0.82, 0.32, seed=loc[0])
    b.pop()


def gable_roof(b, mats, w, length, eave_z, apex_z, thick=0.3, over=0.4, snow_tiers=2, roof_mat='wood_dark',
               icicles=False, seed=0.0, droop_lumps=True, snow_cover=0.8, fringe=False):
    """Roof ridge along local Y, centered at origin. Slopes go to +-w/2 at eave_z."""
    half = w / 2
    run, rise = half, apex_z - eave_z
    slope = math.sqrt(run * run + rise * rise)
    ang = math.degrees(math.atan2(rise, run))
    L = length + over * 2
    for s in (-1, 1):
        cx = s * half / 2
        cz = eave_z + rise / 2
        rot = (0, s * -ang, 0) if s > 0 else (0, ang, 0)
        # roof slab
        b.box(mats[roof_mat], (slope + 0.25, L, thick), m=M((cx, 0, cz), (0, s * ang, 0)), base=False, bevel=0.05)
        # snow layer(s)
        nrm = Vector((s * math.sin(ang * D2R), 0, math.cos(ang * D2R)))
        dn = Vector((s * half, 0, -rise)).normalized()  # down-slope direction
        for k in range(snow_tiers):
            f0 = k / snow_tiers
            f1 = (k + 1) / snow_tiers
            cov = snow_cover if k < snow_tiers - 1 else min(1.0, snow_cover + 0.08)
            fe = f0 + (f1 - f0) * cov
            fc = (f0 + fe) / 2
            seg_len = slope * (fe - f0) + 0.12
            px = s * half * fc
            pz = apex_z - rise * fc
            off = nrm * (thick / 2 + 0.1 + 0.07 * k)
            b.box(mats['snow'], (seg_len, L + 0.12, 0.3), m=M((px + off.x, 0, pz + off.z), (0, s * ang, 0)),
                  base=False, bevel=0.12, smooth=True, nz=0.06, nf=2.0, seed=seed + k + s)
            if droop_lumps:
                # hanging drips along the lower edge of this snow tier
                ep = Vector((s * half * fe, 0, apex_z - rise * fe)) + off * 0.85
                n = max(4, int(L / 0.42))
                for i in range(n):
                    y = -L / 2 + (i + 0.5) * L / n
                    rr = 0.16 + 0.07 * math.sin(i * 2.3 + seed + k)
                    ln = 1.0 + 0.9 * abs(math.sin(i * 1.37 + seed * 0.7 + k * 2.1))
                    c = ep + dn * 0.05 + Vector((0, y, -0.06 * ln))
                    b.sphere(mats['snow'], rr, m=M(c, (0, s * ang * 0.5, 0), (1.0, 0.85, 0.7 * ln)), sub=1, nz=0.03,
                             nf=5)
        if fringe:
            # straw/board fringe under the eave
            n = max(4, int(L / 0.3))
            for i in range(n):
                y = -L / 2 + (i + 0.5) * L / n
                c = Vector((s * (half + 0.08), y, eave_z - 0.12))
                b.cone(mats[roof_mat], 0.13, 0.28 + 0.12 * abs(math.sin(i * 2.1)), 5, m=M(c, (180, 0, 0)))
        if icicles:
            n = int(L / 0.35)
            for i in range(n):
                y = -L / 2 + (i + 0.5) * L / n
                ln = 0.25 + 0.35 * abs(math.sin(i * 1.7 + seed))
                b.cone(mats['ice'], 0.07, ln, 5, m=M((s * (half + 0.1), y, eave_z - 0.05), (180, 0, 0)))
    return ang
