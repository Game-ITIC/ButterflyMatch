# Island base, bake, export and preview helpers. Concatenated after nordic_lib.py.
import sys


def island_body(coll, mats, R=10.0, kind='rock', seed=0.0, name='Island_Body'):
    b = B(name)
    if kind == 'dirt':
        prof = [(R - 0.15, -0.15), (R - 0.05, -0.7), (R + 0.05, -1.4), (R * 0.985, -2.1), (R * 0.93, -2.7),
                (R * 0.8, -3.2), (R * 0.5, -3.55), (0, -3.65)]

        def rfn(a, z, i):
            return 0.18 * fbm((math.cos(a) * 3, math.sin(a) * 3, z * 0.5 + seed), 2) - (0.1 if i == 0 else 0)

        b.lathe(mats['dirt'], list(reversed(prof)), seg=72, smooth=True, rfn=rfn)
    else:
        prof = [(R - 0.2, -0.15), (R - 0.15, -0.9), (R + 0.05, -1.7), (R * 0.97, -2.6), (R * 0.9, -3.4),
                (R * 0.74, -4.0), (R * 0.45, -4.35), (0, -4.45)]

        def rfn(a, z, i):
            layer = round(z / 0.85)
            chunk = fbm((math.cos(a) * 2.4, math.sin(a) * 2.4, layer * 0.61 + seed), 2)
            facet = 0.12 * noise.noise((round(a * 9) * 0.37, layer * 1.3, seed))
            v = -0.42 * abs(chunk) + facet
            return v * (0.3 if i == len(prof) - 1 else 1.0)

        b.lathe(mats['rock_plain'], list(reversed(prof)), seg=60, smooth=False, rfn=rfn)
    return b.build(coll, pivot='origin')


def island_snow(coll, mats, R=10.0, seed=0.0, drip=0.45, name='Island_Snow', lip=1.0):
    t = bmesh.new()
    seg = 112
    prof = [(R * k / 7, 0.0) for k in range(1, 7)] + [(R - 0.25, -0.02), (R + 0.15, -0.14), (R + 0.32, -0.42),
                                                       (R + 0.3, -0.78), (R + 0.12, -0.98), (R - 0.35, -0.9)]
    center = t.verts.new((0, 0, 0.02))
    rings = []
    for i, (r, z) in enumerate(prof):
        ring = []
        for j in range(seg):
            a = 2 * math.pi * j / seg
            d = max(0.0, fbm((math.cos(a) * 3.1, math.sin(a) * 3.1, seed), 3)) * drip + \
                0.09 * (math.sin(a * 37 + seed) + 1) + 0.05 * (math.sin(a * 13 + seed * 2) + 1)
            zz = z * (lip if i >= 7 else 1.0)
            rr = r
            if i < 6:
                zz += 0.05 * fbm((math.cos(a) * r * 0.25, math.sin(a) * r * 0.25, 4 + seed), 2)
            elif i >= 8:
                zz -= d * (0.6 if i == 8 else 1.0) * lip
                rr += 0.05 * fbm((math.cos(a) * 6, math.sin(a) * 6, seed + 9), 2)
            ring.append(t.verts.new((math.cos(a) * rr, math.sin(a) * rr, zz)))
        rings.append(ring)
    for j in range(seg):
        t.faces.new((center, rings[0][j], rings[0][(j + 1) % seg]))
    for i in range(len(rings) - 1):
        for j in range(seg):
            t.faces.new((rings[i][j], rings[i + 1][j], rings[i + 1][(j + 1) % seg], rings[i][(j + 1) % seg]))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    b = B(name)
    b.add_bm(t, mats['snow'], smooth=True)
    return b.build(coll, pivot='origin')


def mountain(b, mats, loc, r=3.0, h=7.0, seed=0.0, rot=0.0, mt='rock'):
    prof = [(r, 0), (r * 0.8, h * 0.25), (r * 0.55, h * 0.5), (r * 0.3, h * 0.75), (r * 0.08, h * 0.95), (0, h)]

    def rfn(a, z, i):
        return (0.55 * fbm((math.cos(a) * 1.6, math.sin(a) * 1.6, z * 0.35 + seed), 3)) * (1 - z / h * 0.6)

    b.push(M(loc, (0, 0, rot)))
    b.lathe(mats[mt], prof, seg=11, smooth=False, rfn=rfn, nz=0.35, nf=0.6, seed=seed)
    b.pop()


def ring_decal(b, mt, center, r0, r1, z=0.03, seg=48, seed=0.0):
    t = bmesh.new()
    A, Bv = [], []
    for j in range(seg):
        a = 2 * math.pi * j / seg
        w0 = r0 + 0.25 * fbm((math.cos(a) * 2, math.sin(a) * 2, seed), 2)
        w1 = r1 + 0.3 * fbm((math.cos(a) * 2, math.sin(a) * 2, seed + 3), 2)
        A.append(t.verts.new((center[0] + math.cos(a) * w0, center[1] + math.sin(a) * w0, z)))
        Bv.append(t.verts.new((center[0] + math.cos(a) * w1, center[1] + math.sin(a) * w1, z)))
    for j in range(seg):
        t.faces.new((A[j], Bv[j], Bv[(j + 1) % seg], A[(j + 1) % seg]))
    bmesh.ops.recalc_face_normals(t, faces=t.faces)
    for f in t.faces:
        if f.normal.z < 0:
            f.normal_flip()
    b.add_bm(t, mt, smooth=True)


# ---------------------------------------------------------------- bake + export
def _select(objs):
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    vl.objects.active = objs[0]


def bake_group(objs, out_png, res=2048, ao_strength=0.6, margin=6, ao_samples=32, label='atlas'):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = 4
    _select(objs)
    for o in objs:
        me = o.data
        while me.uv_layers:
            me.uv_layers.remove(me.uv_layers[0])
        me.uv_layers.new(name='atlas')
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.002, correct_aspect=True,
                             scale_to_bounds=False)
    try:
        bpy.ops.uv.pack_islands(rotate=True, margin=0.0025)
    except Exception as e:
        print('pack_islands failed', e)
    bpy.ops.object.mode_set(mode='OBJECT')

    mats_used = []
    for o in objs:
        for m_ in o.data.materials:
            if m_ and m_ not in mats_used:
                mats_used.append(m_)

    def target(img):
        for m_ in mats_used:
            nt = m_.node_tree
            n = nt.nodes.get('BAKE')
            if n is None:
                n = nt.nodes.new('ShaderNodeTexImage')
                n.name = 'BAKE'
            n.image = img
            for x in nt.nodes:
                x.select = False
            n.select = True
            nt.nodes.active = n

    alb = bpy.data.images.new(label + '_alb', res, res, alpha=False)
    target(alb)
    sc.cycles.samples = 4
    bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, margin=margin, use_clear=True)
    emi = bpy.data.images.new(label + '_emi', res, res, alpha=False)
    target(emi)
    bpy.ops.object.bake(type='EMIT', margin=margin, use_clear=True)
    ao = bpy.data.images.new(label + '_ao', res, res, alpha=False)
    ao.colorspace_settings.name = 'Non-Color'
    target(ao)
    sc.cycles.samples = ao_samples
    sc.world = sc.world or bpy.data.worlds.new('W')
    bpy.ops.object.bake(type='AO', margin=margin, use_clear=True)

    n = res * res * 4
    A = np.empty(n, np.float32)
    alb.pixels.foreach_get(A)
    O = np.empty(n, np.float32)
    ao.pixels.foreach_get(O)
    E = np.empty(n, np.float32)
    emi.pixels.foreach_get(E)
    A = A.reshape(-1, 4)
    O = O.reshape(-1, 4)[:, :1]
    E = E.reshape(-1, 4)
    f = (1.0 - ao_strength) + ao_strength * np.clip(O, 0, 1) ** 0.8
    rgb = A[:, :3] * f
    # cool tint in occluded areas -> snowy painterly feel
    rgb = rgb + (1.0 - f) * np.array([0.0, 0.02, 0.06], np.float32)
    rgb = np.maximum(rgb, E[:, :3] * 0.9)
    A[:, :3] = np.clip(rgb, 0, 1)
    A[:, 3] = 1.0
    final = bpy.data.images.new(label, res, res, alpha=False)
    final.pixels.foreach_set(A.ravel())
    final.filepath_raw = out_png
    final.file_format = 'PNG'
    final.save()
    for im in (alb, ao, emi):
        bpy.data.images.remove(im)
    return final


def finalize_material(objs, img, name):
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    nt = m.node_tree
    bsdf = nt.nodes.get('Principled BSDF')
    tx = nt.nodes.new('ShaderNodeTexImage')
    tx.image = img
    nt.links.new(tx.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.9
    for o in objs:
        me = o.data
        me.materials.clear()
        me.materials.append(m)
        for p in me.polygons:
            p.material_index = 0
    return m


def export_fbx(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    _select(objs)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, mesh_smooth_type='FACE', use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False,
                             use_triangles=True)


def preview(out_png, target=(0, 0, 1.6), dist=38.0, elev=32.0, azim=-78.0, res=(1100, 900), lens=50, engine=None):
    sc = bpy.context.scene
    cam_d = bpy.data.cameras.new('PrevCam')
    cam_d.lens = lens
    cam = bpy.data.objects.new('PrevCam', cam_d)
    sc.collection.objects.link(cam)
    a, e = math.radians(azim), math.radians(elev)
    cam.location = Vector(target) + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * dist
    d = Vector(target) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.camera = cam
    sun_d = bpy.data.lights.new('Sun', 'SUN')
    sun_d.energy = 3.2
    sun_d.color = (1.0, 0.96, 0.9)
    sun_d.angle = math.radians(8)
    sun = bpy.data.objects.new('Sun', sun_d)
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    sc.collection.objects.link(sun)
    w = sc.world or bpy.data.worlds.new('W')
    sc.world = w
    try:
        w.use_nodes = True
    except Exception:
        pass
    bg = w.node_tree.nodes.get('Background')
    bg.inputs[0].default_value = (0.86, 0.9, 0.96, 1)
    bg.inputs[1].default_value = 0.9
    sc.render.engine = engine or 'BLENDER_EEVEE'
    if sc.render.engine == 'CYCLES':
        sc.cycles.samples = 24
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    sc.view_settings.view_transform = 'Standard'
    sc.render.filepath = out_png
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)
    bpy.data.objects.remove(sun)


def run_island(build_fn, name, out_root, src_root, res_base=2048, res_bld=2048, do_preview=True, bake=True):
    sc = reset()
    random.seed(1)
    mats = palette()
    coll_base = bpy.data.collections.new(name + '_Island')
    coll_bld = bpy.data.collections.new(name + '_Buildings')
    sc.collection.children.link(coll_base)
    sc.collection.children.link(coll_bld)
    base_objs, bld_objs = build_fn(mats, coll_base, coll_bld)
    stats = {'base_tris': sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in base_objs),
             'bld_tris': sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in bld_objs),
             'parts': [o.name for o in bld_objs]}
    out_dir = os.path.join(out_root, name)
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(src_root, exist_ok=True)
    if bake:
        # buildings appear step by step in game: keep their occlusion out of the island bake
        for o in bld_objs:
            o.hide_render = True
        img_b = bake_group(base_objs, os.path.join(out_dir, name + '_Island_BaseMap.png'), res_base, label=name + '_Island_BaseMap')
        for o in bld_objs:
            o.hide_render = False
        img_k = bake_group(bld_objs, os.path.join(out_dir, name + '_Buildings_BaseMap.png'), res_bld, label=name + '_Buildings_BaseMap')
        finalize_material(base_objs, img_b, name + '_Island_Mat')
        finalize_material(bld_objs, img_k, name + '_Buildings_Mat')
        export_fbx(base_objs, os.path.join(out_dir, name + '_Island.fbx'))
        export_fbx(bld_objs, os.path.join(out_dir, name + '_Buildings.fbx'))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(src_root, name + '.blend'))
    if do_preview:
        preview(os.path.join(src_root, name + '_preview.png'))
    print('RESULT', name, stats)
    return stats
