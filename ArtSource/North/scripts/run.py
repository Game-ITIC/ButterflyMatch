import sys, os, time
d = os.path.dirname(os.path.abspath(__file__))
args = sys.argv[sys.argv.index('--') + 1:]
g = {'__name__': '__main__'}
for f in ['nordic_lib.py', 'nordic_pipe.py', args[0] + '.py']:
    exec(compile(open(os.path.join(d, f), encoding='utf-8').read(), f, 'exec'), g)
flags = args[1:]
OUT = 'D:/GitHub/JigsawMatch2/Assets/Content/Models/Regions/North'
SRC = 'D:/GitHub/JigsawMatch2/ArtSource/North' if 'nobake' not in flags else os.path.join(d, '..', 'preview_nobake')
t = time.time()
g['run_island'](g['build'], g['NAME'], OUT, SRC, bake='nobake' not in flags, do_preview='noprev' not in flags)
print('TIME', time.time() - t)
