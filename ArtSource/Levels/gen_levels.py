"""Generates link-match levels 46-300 in the JuiceFresh text format
(Assets/JuiceFresh/Resources/Levels/<n>.txt, same format the LevelMakerEditor writes).
Levels 1-45 are the hand-made opening chapter and are never touched.

Cell = two digits: <block><obstacle>
  block:    0 hole, 1 normal, 2 jelly (BLOCK), 6 double jelly (DOUBLEBLOCK)
  obstacle: 0 none, 3 cage (WIREBLOCK), 4 stone (SOLIDBLOCK), 5 double stone,
            7 wall (UNDESTROYABLE), 8 spreading moss (THRIVING)
MODE: 0 score, 1 bring ingredients down, 2 collect colours, 3 clear jelly, 4 break cages, 5 defuse bombs
LIMIT <type>/<n>: type 0 = moves, 1 = seconds.

Difficulty is a 10-level saw-tooth (easy -> hard -> breather -> boss) on top of a slow global ramp,
and every mechanic is introduced on an easy level before it is mixed with others.

Moves, seconds and star scores come from bot measurements (calibration.json, written by calibrate.py
from AutoPlayBot runs). Without calibration the script falls back to formula estimates.
Layouts depend only on the level number, so re-running after calibration only changes the numbers.

Run:  uv run --no-project python ArtSource/Levels/gen_levels.py
"""
import json
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'Assets', 'JuiceFresh', 'Resources', 'Levels')
CALIBRATION = os.path.join(HERE, 'calibration.json')
FIRST, LAST = 46, 300

# ---------------------------------------------------------------- shapes
SHAPES = {
    'shield': [".#####.", "#######", "#######", "#######", "#######", "#######", ".#####.", "..###.."],
    'pine': ["..###..", ".#####.", "#######", ".#####.", "#######", "#######", "#######", "..###.."],
    'longhouse': ["...#...", "..###..", ".#####.", "#######", "#######", "#######", "#######", "#######"],
    'fjord': ["#######", "#######", ".#####.", "#######", "#######", ".#####.", "#######", "#######"],
    'rune': ["#.###.#", "#######", "##.#.##", "#######", "##.#.##", "#######", "#.###.#"],
    'cross': ["..####..", "..####..", "########", "########", "########", "########", "..####..", "..####.."],
    'snowflake': ["#.####.#", ".######.", "##.##.##", "########", "########", "##.##.##", ".######.", "#.####.#"],
    'full7': ["#######"] * 7,
    'full78': ["#######"] * 8,
    'full8': ["########"] * 8,
    'tall6': ["######"] * 8,
    'gate': ["##...##", "##...##", "#######", "#######", "#######", "#######", "#######", "#######"],
    'iceberg': ["...##...", "..####..", ".######.", "########", "########", "########", "########", "########"],
    'heart': [".##.##.", "#######", "#######", "#######", "#######", ".#####.", "..###..", "...#..."],
    'diamond': ["...##...", "..####..", ".######.", "########", "########", ".######.", "..####..", "...##..."],
    'hourglass': ["#######", "#######", ".#####.", "..###..", "..###..", ".#####.", "#######", "#######"],
    'arch': ["#######", "#######", "#######", "#######", "##...##", "##...##", "##...##", "##...##"],
    'towers': ["##.#.##", "##.#.##", "#######", "#######", "#######", "#######", "##.#.##", "##.#.##"],
    'ring': ["########", "########", "##....##", "##....##", "##....##", "##....##", "########", "########"],
    'stairs': ["#.......", "##......", "###.....", "####....", "#####...", "######..", "#######.", "########"],
    'xcross': ["##....##", "###..###", ".######.", "..####..", "..####..", ".######.", "###..###", "##....##"],
    'temple': ["...#...", "..###..", "#######", "#.#.#.#", "#######", "#######", "#######", "#######"],
    'wave': ["##..##..", "########", "########", "..##..##", "########", "########", "##..##..", "########"],
    'frame': ["#######", "#.....#", "#.###.#", "##.#.##", "#.###.#", "#.....#", "#######", "#######"],
    'comb': ["#.#.#.#", "#.#.#.#", "#######", "#######", "#######", "#######", "#######", "#######"],
    'bowl': ["#.....#", "##...##", "###.###", "#######", "#######", "#######", ".#####.", "..###.."],
    'kite': ["..###..", ".#####.", "#######", "#######", ".#####.", "..###..", "..###..", "...#..."],
    'lantern': ["..###..", "#######", "#######", "##.#.##", "#######", "#######", "#######", "..###.."],
    'butterfly': ["##...##", "###.###", "#######", ".#####.", ".#####.", "#######", "###.###", "##...##"],
}


def procedural_shape(rnd):
    """Random symmetric board: corner cuts, notches and inner holes, always one connected area."""
    for _ in range(200):
        cols = rnd.choice([7, 7, 8])
        rows = rnd.choice([7, 8, 8])
        g = [[1] * cols for _ in range(rows)]
        half = (cols + 1) // 2

        def carve(x, y):
            g[y][x] = 0
            g[y][cols - 1 - x] = 0

        # corner cuts (triangles) top and/or bottom
        for corner_row, step in ((0, 1), (rows - 1, -1)):
            if rnd.random() < 0.55:
                depth = rnd.randint(1, 2)
                for d in range(depth):
                    for x in range(depth - d):
                        carve(x, corner_row + d * step)
        # inner holes / notches
        for _ in range(rnd.randint(0, 3)):
            x = rnd.randrange(0, half)
            y = rnd.randrange(1, rows - 1)
            carve(x, y)
            if rnd.random() < 0.4 and y + 1 < rows - 1:
                carve(x, y + 1)
        cells = [(x, y) for y in range(rows) for x in range(cols) if g[y][x]]
        if len(cells) < 38 or not connected(g, cols, rows):
            continue
        if any(not any(g[y][x] for y in range(rows)) for x in range(cols)):
            continue
        return [''.join('#' if v else '.' for v in r) for r in g]
    return SHAPES['full78']


def straight_columns(spec):
    for x in range(len(spec[0])):
        col = ''.join(r[x] for r in spec)
        if col.strip('.') != col.lstrip('.') or '.' in col.lstrip('.'):
            return False
    return True


def connected(g, cols, rows):
    cells = [(x, y) for y in range(rows) for x in range(cols) if g[y][x]]
    if not cells:
        return False
    seen = {cells[0]}
    stack = [cells[0]]
    while stack:
        x, y = stack.pop()
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                nx, ny = x + dx, y + dy
                if 0 <= nx < cols and 0 <= ny < rows and g[ny][nx] and (nx, ny) not in seen:
                    seen.add((nx, ny))
                    stack.append((nx, ny))
    return len(seen) == len(cells)


class Board:
    def __init__(self, rows_spec):
        self.rows = len(rows_spec)
        self.cols = len(rows_spec[0])
        self.b = [[1 if ch == '#' else 0 for ch in r] for r in rows_spec]
        self.o = [[0] * self.cols for _ in range(self.rows)]

    def cells(self):
        return [(x, y) for y in range(self.rows) for x in range(self.cols) if self.b[y][x] > 0]

    def entry(self, x, y):
        # the top cell of a column segment is where new pieces drop in: never block it
        return y == 0 or self.b[y - 1][x] == 0

    def free(self, x, y):
        # obstacles only on plain cells: jelly under a wall can never be cleared (unwinnable level)
        return self.b[y][x] == 1 and self.o[y][x] == 0 and not self.entry(x, y)

    def text(self):
        return '\n'.join(' '.join(f'{self.b[y][x]}{self.o[y][x]}' for x in range(self.cols))
                         for y in range(self.rows))


def mirror(bd, x, y):
    return {(x, y), (bd.cols - 1 - x, y)}


def wall_ok(bd, x, y):
    """A wall never breaks, so it only goes inside the board: all 8 neighbours playable and wall-free,
    otherwise it cuts off pockets that refill poorly (level 79 of the first pass was unwinnable)."""
    if y < 2:
        return False
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            nx, ny = x + dx, y + dy
            if (dx or dy) and not (0 <= nx < bd.cols and 0 <= ny < bd.rows and bd.b[ny][nx] > 0 and bd.o[ny][nx] != 7):
                return False
    return True


def cage_ok(bd, x, y):
    """A cage breaks only when a chain runs through it: it needs at least 3 playable neighbours
    (level 282 had corner cages with one neighbour that the bot could not open in 40 moves)."""
    open_nb = 0
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            nx, ny = x + dx, y + dy
            if (dx or dy) and 0 <= nx < bd.cols and 0 <= ny < bd.rows and bd.b[ny][nx] > 0                     and bd.o[ny][nx] not in (4, 5, 7, 8):
                open_nb += 1
    return open_nb >= 3


def open_area_connected(bd):
    """Cells that pieces can occupy (no wall) must form one 8-connected area."""
    g = [[1 if bd.b[y][x] > 0 and bd.o[y][x] != 7 else 0 for x in range(bd.cols)] for y in range(bd.rows)]
    return connected(g, bd.cols, bd.rows)


def place(bd, cells, code, limit):
    """Put obstacle `code` on mirrored pairs from `cells` (in order) until `limit` cells are used."""
    put = 0
    for (x, y) in cells:
        if put >= limit:
            break
        grp = mirror(bd, x, y)
        if all(bd.free(gx, gy) and (code != 7 or wall_ok(bd, gx, gy))
               and (code != 3 or cage_ok(bd, gx, gy)) for gx, gy in grp):
            for gx, gy in grp:
                bd.o[gy][gx] = code
            # a blocker must not wall in a cage that is already there (level 275: stones next to edge cages)
            cages_near = [(gx + dx, gy + dy) for gx, gy in grp for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                          if 0 <= gx + dx < bd.cols and 0 <= gy + dy < bd.rows and bd.o[gy + dy][gx + dx] == 3]
            if (code == 7 and not open_area_connected(bd)) or not all(cage_ok(bd, cx, cy) for cx, cy in cages_near):
                for gx, gy in grp:
                    bd.o[gy][gx] = 0
                continue
            put += len(grp)
    return put


def pattern_cells(bd, rnd, pattern, rows=None):
    """Candidate cells for an obstacle pattern, ordered so that `place` builds the pattern."""
    rows = rows if rows is not None else range(1, bd.rows - 1)
    cells = [(x, y) for (x, y) in bd.cells() if y in rows]
    cx = (bd.cols - 1) / 2
    if pattern == 'wall':
        y0 = rnd.choice(list(rows)) if rows else 2
        return sorted(cells, key=lambda c: (abs(c[1] - y0), abs(c[0] - cx)))
    if pattern == 'center':
        cy = (bd.rows - 1) / 2
        return sorted(cells, key=lambda c: abs(c[0] - cx) + abs(c[1] - cy) + rnd.random() * 0.3)
    if pattern == 'sides':
        return sorted(cells, key=lambda c: -abs(c[0] - cx) + rnd.random() * 0.5)
    if pattern == 'checker':
        return [c for c in cells if (c[0] + c[1]) % 2 == 0][::2] + cells
    if pattern == 'diagonal':
        return sorted(cells, key=lambda c: abs((c[0] if c[0] <= cx else bd.cols - 1 - c[0]) - (c[1] - 1)) + rnd.random() * 0.2)
    rnd.shuffle(cells)
    return cells


# ---------------------------------------------------------------- level plan
# position in the 10-level cycle -> tier
CYCLE = ['easy', 'normal', 'normal', 'hard', 'breather', 'normal', 'normal', 'hard', 'normal', 'boss']
# moves = bot need * HUMAN * factor (lower = harder). The bot links 10-14 pieces, a player 6-10,
# so a player needs ~1.3x the bot's moves; the tier factor then sets the margin on top of that.
HUMAN = 1.3
FACTOR = {'breather': 1.4, 'easy': 1.3, 'normal': 1.12, 'hard': 1.0, 'boss': 0.93}

# Extra margin for levels the bot still lost at the calibrated limit (verify2: won 0-1 of 3 runs).
# Two or three measure runs miss the spread of random refills on these layouts.
TUNE = {159: 1.5, 165: 1.2, 225: 1.1, 235: 1.2, 282: 1.2}  # 225 re-measured with 6 runs; 165 after the free-move fix

# mechanic -> first level it appears (on an easy level, alone)
INTRO = {
    'stone2': 48,      # double stone
    'moss': 53,        # spreading moss
    'bombs': 56,       # mode 5
    'time': 66,        # timed level
    'wall': 74,        # undestroyable walls
    'cage_obstacle': 85,
    'items3': 95,      # three colours to collect
}

MODE_CYCLE = [3, 2, 4, 1, 0, 3, 5, 2, 1, 4, 3, 0, 2, 5, 1, 3, 4, 2, 0, 1]


def level_plan(n):
    i = n - FIRST
    tier = CYCLE[i % 10]
    t = i / (LAST - FIRST)
    rnd = random.Random(7919 * n + 13)
    mode = MODE_CYCLE[i % len(MODE_CYCLE)]
    if mode == 5 and n < INTRO['bombs']:
        mode = 2
    if n == INTRO['bombs']:
        mode, tier = 5, 'easy'
    timed = (n >= INTRO['time'] and mode in (0, 2) and (i % 3 == 1 or n == INTRO['time']))
    if n == INTRO['time']:
        mode, tier, timed = 2, 'easy', True
    if n in (INTRO['wall'], INTRO['moss'], INTRO['stone2']):
        # a new blocker is shown on an easy jelly / colour level, never with ingredients
        mode, tier, timed = (3 if n != INTRO['moss'] else 2), 'easy', False
    if n == INTRO['cage_obstacle']:
        mode, tier, timed = 3, 'normal', False
    return i, t, tier, mode, timed, rnd


def unlocked(n, key):
    return n >= INTRO[key]


def colours_for(tier, t, rnd):
    if tier in ('breather', 'easy') and t < 0.75:
        return 3
    if tier == 'normal' and t < 0.25 and rnd.random() < 0.3:
        return 3
    return 4


def make_level(n, cal):
    i, t, tier, mode, timed, rnd = level_plan(n)
    # shape: 50% library, 50% procedural; boss levels use the tighter library shapes
    if tier == 'boss' or rnd.random() < 0.5:
        shape_name = rnd.choice(sorted(SHAPES))
        spec = SHAPES[shape_name]
    else:
        shape_name = 'proc'
        spec = procedural_shape(rnd)
    if mode == 1:
        # ingredients fall straight down and leave through the bottom row: every column must run without
        # gaps from its first cell to the bottom (a piece above a hole could never get out)
        while not straight_columns(spec):
            shape_name = rnd.choice(sorted(SHAPES))
            spec = SHAPES[shape_name]
    bd = Board(spec)
    colours = colours_for(tier, t, rnd)
    hard = {'breather': 0.0, 'easy': 0.25, 'normal': 0.5, 'hard': 0.8, 'boss': 1.0}[tier]
    level = 0.35 * t + 0.65 * hard * (0.4 + 0.6 * t)          # 0..1 obstacle density driver
    extra = []
    patterns = ['wall', 'center', 'sides', 'checker', 'diagonal', 'random']

    def obstacles(rows=None, avoid_bottom=True):
        rows = rows if rows is not None else range(1, bd.rows - (1 if avoid_bottom else 0))
        budget = round(len(bd.cells()) * (0.04 + 0.16 * level))
        kinds = [4]
        if unlocked(n, 'stone2'):
            kinds.append(5)
        if unlocked(n, 'moss') and mode != 4:
            kinds.append(8)
        if unlocked(n, 'wall') and mode not in (1,):
            kinds.append(7)
        if n == INTRO['stone2']:
            kinds = [5]
        if n == INTRO['moss']:
            kinds = [8]
        if n == INTRO['wall']:
            kinds = [7]
        rnd.shuffle(kinds)
        for k in kinds[:2]:
            cnt = max(2, budget // 2) if k != 7 else min(4, max(2, budget // 3))
            if k == 8:
                cnt = min(cnt, 4)
            place(bd, pattern_cells(bd, rnd, rnd.choice(patterns), rows), k, cnt)

    if mode == 0:  # score
        obstacles()
        extra.append(f'GETSTARS {1 if tier in ("easy", "breather") else 2}')
    elif mode == 1:  # ingredients
        for x in range(bd.cols):
            bd.b[bd.rows - 1][x] = 1
        two = t > 0.15 and rnd.random() < 0.7
        a = 2 + round(2 * level)
        b = 1 + round(2 * level)
        obstacles(rows=range(2, bd.rows - 2))
        if two:
            extra += ['COLLECT ITEMS 0/1/', f'COLLECT COUNT {a}/{b}/']
        else:
            extra += ['COLLECT ITEMS 0/', f'COLLECT COUNT {a + 1}/']
    elif mode == 2:  # colours
        k = 1 if tier in ('breather', 'easy') else 2
        if unlocked(n, 'items3') and tier in ('hard', 'boss'):
            k = 3
        k = min(k, colours)
        cols = sorted(rnd.sample(range(colours), k))
        cnt = 12 + round(14 * level) - 3 * (k - 1)
        obstacles()
        extra += ['COLLECT ITEMS ' + '/'.join(str(c) for c in cols),
                  'COLLECT COUNT ' + '/'.join(str(cnt) for _ in cols)]
    elif mode == 3:  # jelly
        jp = rnd.choice(['bottom', 'full', 'ring', 'checker', 'center', 'top', 'columns'])
        cx, cy = (bd.cols - 1) / 2, (bd.rows - 1) / 2
        for (x, y) in bd.cells():
            on = {
                'bottom': y >= bd.rows - (3 + round(3 * level)),
                'full': True,
                'ring': x in (0, bd.cols - 1) or y in (0, bd.rows - 1) or abs(x - cx) + abs(y - cy) < 2,
                'checker': (x + y) % 2 == 0,
                'center': abs(x - cx) + abs(y - cy) <= 2 + round(2 * level),
                'top': y <= 2 + round(2 * level),
                'columns': (x if x <= cx else bd.cols - 1 - x) % 2 == 0,
            }[jp]
            if on:
                bd.b[y][x] = 2
        # double jelly core
        core = round(10 * level)
        cand = sorted([c for c in bd.cells() if bd.b[c[1]][c[0]] == 2],
                      key=lambda c: abs(c[0] - cx) + abs(c[1] - cy) + rnd.random())
        for (x, y) in cand[:core]:
            bd.b[y][x] = 6
            bd.b[y][bd.cols - 1 - x] = 6 if bd.b[y][bd.cols - 1 - x] == 2 else bd.b[y][bd.cols - 1 - x]
        if level > 0.2:
            obstacles()
        extra.append('COLLECT ITEMS 0/0/0/0/')
    elif mode == 4:  # cages
        n_cage = 2 + round(4 * level)
        place(bd, pattern_cells(bd, rnd, rnd.choice(patterns)), 3, n_cage)
        hp = 6 + round(10 * level)
        if level > 0.25:
            obstacles()
        extra.append(f'CAGE {hp}')
    else:  # bombs
        count = 3 + round(3 * level)
        timer = max(7, 14 - round(5 * level))
        if level > 0.15:
            obstacles()
        extra += ['GETSTARS 1', f'BOMBS {count}/{timer}']

    # cages as an extra blocker on goal levels (need a CAGE hp line, so only where none is set yet)
    if unlocked(n, 'cage_obstacle') and mode in (0, 2, 3) and tier in ('hard', 'boss', 'normal') and rnd.random() < 0.35:
        place(bd, pattern_cells(bd, rnd, 'random'), 3, 2)
        extra.append(f'CAGE {4 + round(4 * level)}')

    limit_type = 1 if timed else 0
    limit, stars = numbers(n, mode, tier, timed, level, bd, cal)
    lines = [f'MODE {mode}', f'SIZE {bd.cols}/{bd.rows}', f'LIMIT {limit_type}/{limit}',
             f'COLOR LIMIT {colours}', f'STARS {stars[0]}/{stars[1]}/{stars[2]}'] + extra
    meta = dict(n=n, mode=mode, tier=tier, timed=timed, shape=shape_name, colours=colours, limit=limit)
    return '\n'.join(lines) + '\n' + bd.text(), bd, meta


def r100(v):
    return max(100, int(round(v / 100.0)) * 100)


def numbers(n, mode, tier, timed, level, bd, cal):
    """Moves (or seconds) and star scores. Calibrated from bot runs when available."""
    f = FACTOR[tier] * HUMAN
    c = cal.get(str(n))
    if c is None:
        # formula estimate (first pass, before the bot measured anything)
        moves = {0: 18, 1: 18, 2: 18, 3: 22, 4: 18, 5: 16}[mode] + round(6 * level)
        if timed:
            moves = 60 + round(30 * level)
        s1 = 250 * moves
        return moves, (r100(s1), r100(s1 * 1.6), r100(s1 * 2.2))

    if mode == 0:
        trail = c['trail']                       # median score after k moves
        moves = c.get('moves') or (16 + round(6 * level))
        k = max(3, min(len(trail), round(moves / f)))
        s_target = trail[k - 1]
        s2 = trail[min(len(trail), round(k * 1.2)) - 1] * 1.05
        s3 = trail[min(len(trail), moves) - 1] * 1.1
        s1 = s_target
        if tier not in ('easy', 'breather'):
            # GETSTARS 2: the second star is the goal
            s1, s2 = s_target * 0.6, s_target
            s3 = max(s3, s2 * 1.3)
        if timed:
            secs = c['secs_per_move'] * k
            moves = round(secs * f + 1.6 * k)  # +1.6 s per move: a player looks for the chain, the bot does not
        return moves, (r100(s1), r100(max(s2, s1 * 1.15)), r100(max(s3, s2 * 1.15)))

    need = c['need'] * TUNE.get(n, 1.0)           # median moves (or seconds) the bot needed
    if timed:
        moves = min(180, max(30, round(need * f + 1.6 * c["moves_needed"])))
    else:
        # floor 10: with 8 moves a short level was lost on one unlucky refill (55, 99, 159 in verify1)
        moves = min(40, max(10, round(need * f + 0.5)))
    score = c['score']                            # median score when the goal was done
    return moves, (r100(score * 0.3), r100(score * 0.85), r100(score * 1.3))


def validate(n, bd, meta):
    errs = []
    cells = bd.cells()
    mode = meta['mode']
    if len(cells) < 30:
        errs.append(f'only {len(cells)} playable cells')
    for x in range(bd.cols):
        if not any(bd.b[y][x] > 0 for y in range(bd.rows)):
            errs.append(f'column {x} empty')
    for x in range(bd.cols):
        for y in range(bd.rows):
            if bd.b[y][x] > 0 and bd.entry(x, y) and bd.o[y][x] in (4, 5, 7, 8):
                errs.append(f'entry cell {x},{y} blocked')
    if mode == 1 and not straight_columns([''.join('#' if bd.b[y][x] else '.' for x in range(bd.cols)) for y in range(bd.rows)]):
        errs.append('ingredient level has a column with a gap')
    if mode == 1 and any(bd.b[bd.rows - 1][x] == 0 or bd.o[bd.rows - 1][x] != 0 for x in range(bd.cols)):
        errs.append('ingredient level needs an open bottom row')
    if mode == 3 and not any(bd.b[y][x] in (2, 6) for (x, y) in cells):
        errs.append('no jelly')
    if any(bd.o[y][x] == 3 and not cage_ok(bd, x, y) for (x, y) in cells):
        errs.append('cage with fewer than 3 open neighbours')
    if mode == 4 and not any(bd.o[y][x] == 3 for (x, y) in cells):
        errs.append('no cages')
    blocked = sum(1 for (x, y) in cells if bd.o[y][x] in (4, 5, 7, 8))
    if blocked > len(cells) * 0.28:
        errs.append(f'too many obstacles ({blocked})')
    walls = sum(1 for (x, y) in cells if bd.o[y][x] == 7)
    if walls > 6:
        errs.append(f'too many walls ({walls})')
    if not open_area_connected(bd):
        errs.append('walls split the board')
    lim = meta['limit']
    if meta['timed'] and not 30 <= lim <= 180:
        errs.append(f'seconds {lim}')
    if not meta['timed'] and not 6 <= lim <= 45:
        errs.append(f'moves {lim}')
    return errs


def main():
    cal = {}
    if os.path.exists(CALIBRATION):
        with open(CALIBRATION, encoding='utf-8') as fh:
            cal = json.load(fh)
    ok = True
    summary = []
    for n in range(FIRST, LAST + 1):
        txt, bd, meta = make_level(n, cal)
        errs = validate(n, bd, meta)
        if errs:
            ok = False
            print(f'{n}: INVALID {errs}')
        with open(os.path.join(OUT, f'{n}.txt'), 'w', newline='\n', encoding='utf-8') as fh:
            fh.write(txt)
        summary.append(meta)
        print(f"{n}: mode {meta['mode']} {meta['tier']:<8} {meta['shape']:<9} {bd.cols}x{bd.rows} "
              f"c{meta['colours']} {'T' if meta['timed'] else 'M'}{meta['limit']}")
    with open(os.path.join(HERE, 'plan.json'), 'w', encoding='utf-8') as fh:
        json.dump(summary, fh, indent=0)
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
