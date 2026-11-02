"""Generates match-3 levels 46-90 ("North" chapter) in the JuiceFresh text format used by
Assets/JuiceFresh/Resources/Levels/<n>.txt (same format the LevelMakerEditor writes).

Cell = two digits: <block><obstacle>
  block:    0 hole, 1 normal, 2 jelly (BLOCK), 6 double jelly (DOUBLEBLOCK)
  obstacle: 0 none, 3 cage (WIREBLOCK), 4 stone (SOLIDBLOCK), 5 double stone, 8 thriving
MODE: 0 score, 1 collect ingredients, 2 collect coloured items, 3 clear jelly, 4 break cages
Run:  uv run --no-project python ArtSource/Levels/gen_north_levels.py
"""
import os
import random
import sys

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'JuiceFresh', 'Resources', 'Levels')
FIRST, LAST = 46, 90

SHAPES = {
    'shield': [".#####.",
               "#######",
               "#######",
               "#######",
               "#######",
               "#######",
               ".#####.",
               "..###.."],
    'pine': ["..###..",
             ".#####.",
             "#######",
             ".#####.",
             "#######",
             "#######",
             "#######",
             "..###.."],
    'longhouse': ["...#...",
                  "..###..",
                  ".#####.",
                  "#######",
                  "#######",
                  "#######",
                  "#######",
                  "#######"],
    'fjord': ["#######",
              "#######",
              ".#####.",
              "#######",
              "#######",
              ".#####.",
              "#######",
              "#######"],
    'rune': ["#.###.#",
             "#######",
             "##.#.##",
             "#######",
             "##.#.##",
             "#######",
             "#.###.#"],
    'cross': ["..####..",
              "..####..",
              "########",
              "########",
              "########",
              "########",
              "..####..",
              "..####.."],
    'snowflake': ["#.####.#",
                  ".######.",
                  "##.##.##",
                  "########",
                  "########",
                  "##.##.##",
                  ".######.",
                  "#.####.#"],
    'full7': ["#######"] * 7,
    'full78': ["#######"] * 8,
    'full8': ["########"] * 8,
    'tall6': ["######"] * 8,
    'gate': ["##...##",
             "##...##",
             "#######",
             "#######",
             "#######",
             "#######",
             "#######",
             "#######"],
    'iceberg': ["...##...",
                "..####..",
                ".######.",
                "########",
                "########",
                "########",
                "########",
                "########"],
}

# (mode, shape) cycle for the chapter: varied, every 5th level is a "hard" one, the next one a breather.
PLAN = [
    (0, 'shield'), (3, 'full7'), (1, 'longhouse'), (2, 'fjord'), (4, 'rune'),
    (0, 'pine'), (3, 'snowflake'), (2, 'shield'), (1, 'full78'), (3, 'cross'),
    (2, 'iceberg'), (4, 'shield'), (0, 'snowflake'), (1, 'gate'), (3, 'longhouse'),
    (2, 'full8'), (4, 'fjord'), (3, 'pine'), (1, 'shield'), (0, 'cross'),
    (2, 'rune'), (3, 'iceberg'), (4, 'full78'), (1, 'snowflake'), (3, 'gate'),
    (0, 'fjord'), (2, 'longhouse'), (4, 'cross'), (3, 'tall6'), (1, 'iceberg'),
    (2, 'pine'), (0, 'rune'), (3, 'shield'), (4, 'snowflake'), (1, 'fjord'),
    (3, 'full8'), (2, 'gate'), (0, 'longhouse'), (4, 'iceberg'), (3, 'rune'),
    (1, 'cross'), (2, 'snowflake'), (4, 'gate'), (0, 'full8'), (3, 'iceberg'),
]
assert len(PLAN) == LAST - FIRST + 1


def mirror_set(cols, x, y):
    return {(x, y), (cols - 1 - x, y)}


class Board:
    def __init__(self, shape):
        rows = SHAPES[shape]
        self.rows = len(rows)
        self.cols = len(rows[0])
        self.b = [[1 if ch == '#' else 0 for ch in r] for r in rows]
        self.o = [[0] * self.cols for _ in range(self.rows)]

    def cells(self):
        return [(x, y) for y in range(self.rows) for x in range(self.cols) if self.b[y][x] > 0]

    def top(self, x):
        return next((y for y in range(self.rows) if self.b[y][x] > 0), -1)

    def free(self, x, y):
        # never block the entry cell of a column segment (top cell or cell right under a hole)
        return self.b[y][x] > 0 and self.o[y][x] == 0 and y > 0 and self.b[y - 1][x] > 0

    def text(self):
        return '\n'.join(' '.join(f'{self.b[y][x]}{self.o[y][x]}' for x in range(self.cols)) for y in range(self.rows))


def sprinkle(bd, rnd, code, count, rows=None, avoid_bottom=True, mirror=True):
    placed = 0
    cand = [c for c in bd.cells() if (rows is None or c[1] in rows) and (not avoid_bottom or c[1] < bd.rows - 1)]
    rnd.shuffle(cand)
    for (x, y) in cand:
        if placed >= count:
            break
        group = mirror_set(bd.cols, x, y) if mirror else {(x, y)}
        if all(bd.free(gx, gy) and bd.b[gy][gx] == 1 for gx, gy in group):
            for gx, gy in group:
                bd.o[gy][gx] = code
            placed += len(group)
    return placed


def round100(v):
    return int(round(v / 100.0)) * 100


def make_level(n, mode, shape, rnd):
    i = n - FIRST
    t = i / (LAST - FIRST)              # 0..1 across the chapter
    hard = (i % 5 == 4)
    breather = (i % 5 == 0 and i > 0)
    bd = Board(shape)
    lines = [f'MODE {mode}', f'SIZE {bd.cols}/{bd.rows}']
    extra = []
    n_cells = len(bd.cells())

    if mode == 0:  # score
        moves = 22 - round(4 * t) - (3 if hard else 0) + (3 if breather else 0)
        sprinkle(bd, rnd, 8, 2 + round(3 * t), rows=range(1, bd.rows - 1))
        if t > 0.3:
            sprinkle(bd, rnd, 4, 2 + round(2 * t), rows=range(2, bd.rows - 1))
        getstars = 2 if (hard or t > 0.55) else 1
        s1 = round100(290 * moves)
        extra.append(f'GETSTARS {getstars}')
    elif mode == 1:  # ingredients
        a = 2 + round(2 * t)
        b = 2 + round(1.5 * t) + (1 if hard else 0)
        moves = 14 + a + b - (2 if hard else 0) + (2 if breather else 0)
        for x in range(bd.cols):  # ingredients leave through the bottom row: keep it open
            bd.b[bd.rows - 1][x] = 1
        sprinkle(bd, rnd, 4, 2 + round(2 * t), rows=range(2, bd.rows - 2))
        if t > 0.4:
            sprinkle(bd, rnd, 5, 2, rows=range(2, bd.rows - 2))
        s1 = round100(260 * moves)
        extra += ['COLLECT ITEMS 0/1/', f'COLLECT COUNT {a}/{b}/']
    elif mode == 2:  # coloured items
        colours = sorted(rnd.sample(range(4), 2))
        cnt = 10 + round(6 * t) + (2 if hard else 0) - (2 if breather else 0)
        moves = 12 + round(len(colours) * cnt / 6.5) - (1 if hard else 0)
        sprinkle(bd, rnd, 4, 2 + round(2 * t), rows=range(1, bd.rows - 1))
        if t > 0.35:
            sprinkle(bd, rnd, 8, 2, rows=range(bd.rows - 2, bd.rows), avoid_bottom=False)
        s1 = round100(250 * moves)
        extra += ['COLLECT ITEMS ' + '/'.join(str(c) for c in colours),
                  'COLLECT COUNT ' + '/'.join(str(cnt) for _ in colours)]
    elif mode == 3:  # jelly
        cells = bd.cells()
        jelly_rows = max(3, min(bd.rows, 3 + round(3 * t) + (1 if hard else 0) - (1 if breather else 0)))
        for (x, y) in cells:
            if y >= bd.rows - jelly_rows:
                bd.b[y][x] = 2
        # double jelly core, symmetric
        core = round(2 + 6 * t) + (2 if hard else 0)
        cand = [(x, y) for (x, y) in cells if bd.b[y][x] == 2 and abs(x - (bd.cols - 1) / 2) <= 1.5]
        rnd.shuffle(cand)
        put = 0
        for (x, y) in cand:
            if put >= core:
                break
            for gx, gy in mirror_set(bd.cols, x, y):
                if bd.b[gy][gx] == 2:
                    bd.b[gy][gx] = 6
                    put += 1
        if t > 0.25:
            sprinkle(bd, rnd, 4, 2 + round(2 * t), rows=range(0, bd.rows - jelly_rows))
        jelly = sum(1 if bd.b[y][x] == 2 else 2 for (x, y) in bd.cells() if bd.b[y][x] in (2, 6))
        moves = min(30, max(16, round(jelly * 0.52) + 4 - (2 if hard else 0) + (2 if breather else 0)))
        s1 = round100(270 * moves)
        extra.append('COLLECT ITEMS 0/0/0/0/')
    else:  # cages
        n_cage = 2 + round(2 * t) + (1 if hard else 0)
        cand = [(x, y) for (x, y) in bd.cells() if 1 <= y <= bd.rows - 2]
        rnd.shuffle(cand)
        put = 0
        for (x, y) in cand:
            if put >= n_cage:
                break
            grp = mirror_set(bd.cols, x, y)
            if all(bd.free(gx, gy) for gx, gy in grp):
                for gx, gy in grp:
                    bd.o[gy][gx] = 3
                put += len(grp)
        hp = 7 + round(6 * t) + (2 if hard else 0) - (2 if breather else 0)
        hits = put * hp
        moves = min(28, max(12, round(hits / 2.8) + 4))
        sprinkle(bd, rnd, 8, 2 + round(2 * t), rows=range(1, bd.rows - 1))
        s1 = round100(270 * moves)
        extra.append(f'CAGE {hp}')

    s2, s3 = round100(s1 * 1.5), round100(s1 * 2.0)
    lines += [f'LIMIT 0/{moves}', 'COLOR LIMIT 4', f'STARS {s1}/{s2}/{s3}'] + extra
    return '\n'.join(lines) + '\n' + bd.text(), bd, mode, moves


def validate(n, bd, mode, moves):
    errs = []
    cells = bd.cells()
    if len(cells) < 30:
        errs.append(f'only {len(cells)} playable cells')
    for x in range(bd.cols):
        if not any(bd.b[y][x] > 0 for y in range(bd.rows)):
            errs.append(f'column {x} empty')
    if mode == 1 and any(bd.b[bd.rows - 1][x] == 0 or bd.o[bd.rows - 1][x] != 0 for x in range(bd.cols)):
        errs.append('collect level needs an open bottom row')
    if mode == 3 and not any(bd.b[y][x] in (2, 6) for (x, y) in cells):
        errs.append('no jelly')
    if mode == 4 and not any(bd.o[y][x] == 3 for (x, y) in cells):
        errs.append('no cages')
    blocked = sum(1 for (x, y) in cells if bd.o[y][x] in (4, 5, 8))
    if blocked > len(cells) * 0.25:
        errs.append(f'too many obstacles ({blocked})')
    if not 8 <= moves <= 40:
        errs.append(f'moves {moves}')
    return errs


def main():
    rnd = random.Random(2026)
    ok = True
    for idx, n in enumerate(range(FIRST, LAST + 1)):
        mode, shape = PLAN[idx]
        txt, bd, m, moves = make_level(n, mode, shape, rnd)
        errs = validate(n, bd, m, moves)
        if errs:
            ok = False
            print(f'{n}: INVALID {errs}')
        with open(os.path.join(OUT, f'{n}.txt'), 'w', newline='\n', encoding='utf-8') as f:
            f.write(txt)
        print(f'{n}: mode {m} {shape:<9} {bd.cols}x{bd.rows} moves {moves}')
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
