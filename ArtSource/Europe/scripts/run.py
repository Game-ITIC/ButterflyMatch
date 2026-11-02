# Builds one Europe island. Two ways to run:
#   live Blender over MCP:  set JOB = {'island': 'island_paris', 'bake': True, 'preview': True} first, then exec this file
#   headless:               blender -b --factory-startup --python run.py -- island_paris [nobake] [noprev]
import os, sys, time

SCRIPTS = 'D:/GitHub/JigsawMatch2/ArtSource/Europe/scripts'
NORDIC = 'D:/GitHub/JigsawMatch2/ArtSource/North/scripts'
OUT = 'D:/GitHub/JigsawMatch2/Assets/Content/Models/Regions/Europe'
SRC = 'D:/GitHub/JigsawMatch2/ArtSource/Europe'

try:
    JOB
except NameError:
    _a = sys.argv[sys.argv.index('--') + 1:]
    JOB = {'island': _a[0], 'bake': 'nobake' not in _a, 'preview': 'noprev' not in _a}

_g = {'__name__': '__main__'}
for _f in [os.path.join(NORDIC, 'nordic_lib.py'), os.path.join(NORDIC, 'nordic_pipe.py'),
           os.path.join(SCRIPTS, 'euro_lib.py'), os.path.join(SCRIPTS, JOB['island'] + '.py')]:
    exec(compile(open(_f, encoding='utf-8').read(), _f, 'exec'), _g)
_src = SRC if JOB['bake'] else JOB.get('preview_dir', os.path.join(SRC, 'preview_nobake'))
_t = time.time()
_stats = _g['run_island'](_g['build'], _g['NAME'], OUT, _src, bake=JOB['bake'], do_preview=JOB['preview'])
_stats['time'] = round(time.time() - _t, 1)
result = _stats
print('RESULT', result)
