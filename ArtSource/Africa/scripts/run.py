# Builds one Africa island in the live Blender over MCP (the production path for this region):
#   set JOB = {'island': 'island_giza', 'bake': True, 'preview': True} first, then exec this file.
# Kits: North nordic_lib.py + nordic_pipe.py, Europe euro_lib.py, then the warm-region kit africa_lib.py.
# Outputs: FBX + baked BaseMaps -> Assets/Content/Models/Regions/Africa/<Id>/, .blend + preview -> ArtSource/Africa/.
import os, sys, time

ROOT = 'D:/GitHub/JigsawMatch2'
SCRIPTS = ROOT + '/ArtSource/Africa/scripts'
KITS = [ROOT + '/ArtSource/North/scripts/nordic_lib.py', ROOT + '/ArtSource/North/scripts/nordic_pipe.py',
        ROOT + '/ArtSource/Europe/scripts/euro_lib.py', ROOT + '/ArtSource/Africa/scripts/africa_lib.py']
OUT = ROOT + '/Assets/Content/Models/Regions/Africa'
SRC = ROOT + '/ArtSource/Africa'

try:
    JOB
except NameError:
    _a = sys.argv[sys.argv.index('--') + 1:]
    JOB = {'island': _a[0], 'bake': 'nobake' not in _a, 'preview': 'noprev' not in _a}

_g = {'__name__': '__main__'}
for _f in KITS + [os.path.join(JOB.get('scripts', SCRIPTS), JOB['island'] + '.py')]:
    exec(compile(open(_f, encoding='utf-8').read(), _f, 'exec'), _g)
_out = JOB.get('out', OUT)
_src = JOB.get('src', SRC) if JOB['bake'] else JOB.get('preview_dir', os.path.join(JOB.get('src', SRC), 'preview_nobake'))
_t = time.time()
_stats = _g['run_island'](_g['build'], _g['NAME'], _out, _src, bake=JOB['bake'], do_preview=JOB['preview'])
_stats['time'] = round(time.time() - _t, 1)
result = _stats
print('RESULT', result)
