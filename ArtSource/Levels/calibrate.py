"""Turns AutoPlayBot measure runs (results.csv) into calibration.json for gen_levels.py.

Measure mode gives goal levels 60 moves (240 s when timed) and no score gate, and lets score levels
run 30 moves (90 s); the bot logs the moves it needed and the score after every move.

Usage: uv run --no-project python ArtSource/Levels/calibrate.py <results.csv> [<results.csv> ...]
Levels the bot never finished are listed as UNBEATABLE and get no calibration entry.
"""
import csv
import json
import os
import statistics
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CALIBRATION = os.path.join(HERE, 'calibration.json')


def main(paths):
    rows = {}
    for p in paths:
        with open(p, encoding='utf-8') as fh:
            for r in csv.DictReader(fh):
                rows.setdefault(int(r['level']), []).append(r)

    cal = {}
    if os.path.exists(CALIBRATION):
        with open(CALIBRATION, encoding='utf-8') as fh:
            cal = json.load(fh)

    bad = []
    for level, rs in sorted(rows.items()):
        mode = rs[0]['mode']
        if mode == 'SCORE':
            trails = [[int(v) for v in r['scoreTrail'].split('|') if v] for r in rs if r['scoreTrail']]
            trails = [t for t in trails if len(t) >= 5]
            if not trails:
                bad.append((level, mode, [r['result'] for r in rs]))
                continue
            n = min(len(t) for t in trails)
            trail = [int(statistics.median(t[k] for t in trails)) for k in range(n)]
            entry = {'trail': trail}
            used = [int(r['used']) for r in rs]
            bot_moves = [max(1, int(r['botMoves'])) for r in rs]
            entry['secs_per_move'] = round(statistics.median(u / m for u, m in zip(used, bot_moves)), 2)
            cal[str(level)] = entry
            continue

        wins = [r for r in rs if r['result'] == 'win']
        if not wins:
            bad.append((level, mode, [r['result'] for r in rs]))
            cal.pop(str(level), None)
            continue
        need = statistics.median(int(r['used']) for r in wins)
        # a lost run still tells the goal needs more than its moves
        if len(wins) < len(rs):
            need *= 1.15
        cal[str(level)] = {
            'need': round(need, 1),
            'moves_needed': round(statistics.median(int(r['botMoves']) for r in wins), 1),
            'score': int(statistics.median(int(r['score']) for r in wins)),
            'win_rate': round(len(wins) / len(rs), 2),
        }

    with open(CALIBRATION, 'w', encoding='utf-8') as fh:
        json.dump(cal, fh, indent=1, sort_keys=True)
    print(f'calibrated {len(cal)} levels')
    for level, mode, results in bad:
        print(f'UNBEATABLE {level} {mode} {results}')


if __name__ == '__main__':
    main(sys.argv[1:])
