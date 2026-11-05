#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using JuiceFresh;
using JuiceFresh.Scripts;
using JuiceFresh.States;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Utils.DebugTools
{
    /// <summary>
    /// Editor-only play-test bot: plays levels in the real game scene through the same PlayingState methods
    /// a finger drag uses, and writes one CSV row per attempt (win/lose/stuck, stars, moves left, errors).
    /// Started from Tools/JigsawMatch2/AutoPlay (settings live in EditorPrefs), does nothing otherwise.
    /// </summary>
    public class AutoPlayBot : MonoBehaviour
    {
        public const string KeyEnabled = "AutoPlayBot.Enabled";
        public const string KeyLevels = "AutoPlayBot.Levels";       // "46-300" or "3,7,10-12"
        public const string KeyRuns = "AutoPlayBot.Runs";
        public const string KeyTimeScale = "AutoPlayBot.TimeScale";
        public const string KeyOut = "AutoPlayBot.Out";             // absolute folder for results.csv / errors.log
        // Measure mode: goal levels get MeasureMoves moves and no score gate, score levels never finish early;
        // the CSV then tells how many moves the bot needed / how the score grows per move.
        public const string KeyMeasure = "AutoPlayBot.Measure";
        private const int MeasureMoves = 60;
        private const int MeasureScoreMoves = 30;

        private const float RunTimeout = 240f;     // real seconds per attempt
        private const float StuckTimeout = 25f;    // real seconds of DragBlocked/no move while Playing

        private readonly List<int> _levels = new();
        private readonly List<string> _runErrors = new();
        private readonly System.Random _rnd = new();
        private string _outDir;
        private int _runs;
        private float _timeScale;
        private bool _measure;
        private bool _debug;
        private object _sceneLoader;
        private object _health;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!EditorPrefs.GetBool(KeyEnabled, false))
            {
                return;
            }

            var go = new GameObject("AutoPlayBot");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoPlayBot>();
        }

        private void Awake()
        {
            _levels.AddRange(ParseLevels(EditorPrefs.GetString(KeyLevels, "1-10")));
            _runs = Mathf.Max(1, EditorPrefs.GetInt(KeyRuns, 1));
            _timeScale = Mathf.Max(1f, EditorPrefs.GetFloat(KeyTimeScale, 4f));
            _measure = EditorPrefs.GetBool(KeyMeasure, false);
            _debug = EditorPrefs.GetBool("AutoPlayBot.Debug", false);
            _outDir = EditorPrefs.GetString(KeyOut, Path.Combine(Application.dataPath, "..", "Temp", "AutoPlay"));
            Directory.CreateDirectory(_outDir);
            Application.logMessageReceived += OnLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            var frames = (stackTrace ?? string.Empty).Split('\n');
            var sb = new StringBuilder();
            for (var i = 0; i < frames.Length && i < 8; i++)
            {
                if (frames[i].Trim().Length > 0)
                {
                    sb.Append("\n    ").Append(frames[i].Trim());
                }
            }

            _runErrors.Add($"{type}: {condition.Split('\n')[0]}{sb}");
        }

        private IEnumerator Start()
        {
            var csv = Path.Combine(_outDir, "results.csv");
            if (!File.Exists(csv))
            {
                File.WriteAllText(csv, "level,run,result,stars,limitLeft,limit,used,botMoves,score,seconds,errors,mode,scoreTrail,goalLeft\n");
            }

            // Wait for the menu (DI ready) before the first scene switch.
            float wait = 0f;
            while ((_sceneLoader = Resolve("Itic.Scopes.SceneLoader")) == null && wait < 60f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            _health = Resolve("Systems.HealthSystem");
            yield return new WaitForSecondsRealtime(3f);

            foreach (var level in _levels)
            {
                for (var run = 1; run <= _runs; run++)
                {
                    _runErrors.Clear();
                    yield return PlayOne(level, run, csv);
                }
            }

            File.AppendAllText(Path.Combine(_outDir, "done.txt"), DateTime.Now.ToString("s") + "\n");
            Time.timeScale = 1f;
            EditorPrefs.SetBool(KeyEnabled, false);
            EditorApplication.isPlaying = false;
        }

        private IEnumerator PlayOne(int level, int run, string csv)
        {
            PlayerPrefs.SetInt("OpenLevel", level);
            InvokeIfExists(_health, "RestoreAllLives");

            var start = Time.realtimeSinceStartup;
            var load = _sceneLoader.GetType().GetMethod("LoadGameAsync");
            var isLoading = _sceneLoader.GetType().GetProperty("IsLoading");
            var previous = LevelManager.THIS;
            var requested = -100f;

            // (Re)request the game scene until a fresh LevelManager reaches Playing: SceneLoader ignores
            // requests while another load (e.g. Bootstrap -> menu) is still running.
            while (Time.realtimeSinceStartup - start < 60f)
            {
                var lm0 = LevelManager.THIS;
                if (lm0 != null && lm0 != previous && lm0.gameStatus == GameState.Playing && lm0.currentLevel == level)
                {
                    break;
                }

                var busy = isLoading != null && (bool)isLoading.GetValue(_sceneLoader);
                if (!busy && (lm0 == null || lm0 == previous) && Time.realtimeSinceStartup - requested > 8f)
                {
                    requested = Time.realtimeSinceStartup;
                    ((UniTask)load.Invoke(_sceneLoader, null)).Forget();
                }

                yield return null;
            }

            var lm = LevelManager.THIS;
            var result = "load_timeout";
            var moves = 0;
            var lastProgress = Time.realtimeSinceStartup;
            var trail = new StringBuilder();
            var lastTrailMove = -1;
            var lastCheckedMove = -1;
            var settledFrames = 0;

            if (lm != null && _measure && lm.gameStatus == GameState.Playing)
            {
                var timed = lm.limitType == LIMIT.TIME;
                if (lm.target == Target.SCORE)
                {
                    lm.Limit = timed ? 90 : MeasureScoreMoves;
                    lm.star1 = lm.star2 = lm.star3 = 100000000;
                }
                else
                {
                    lm.Limit = timed ? 240 : MeasureMoves;
                    lm.star1 = 1;
                }
            }

            var startLimit = lm != null ? lm.Limit : 0;
            var endLimit = startLimit;
            var endScore = 0;

            if (lm != null && lm.gameStatus == GameState.Playing && lm.currentLevel == level)
            {
                result = "timeout";
                var state = lm.Playing;

                while (Time.realtimeSinceStartup - start < RunTimeout)
                {
                    Time.timeScale = _timeScale;
                    var status = lm.gameStatus;
                    endLimit = lm.Limit;
                    endScore = LevelManager.Score;
                    if (status == GameState.PreWinAnimations || status == GameState.Win)
                    {
                        result = "win";
                        break;
                    }

                    if (status == GameState.GameOver || status == GameState.PreFailed || status == GameState.PreFailedBomb)
                    {
                        result = "lose";
                        break;
                    }

                    // Score after every settled move (moves are counted by the game's own limit).
                    var used = startLimit - lm.Limit;
                    if (status == GameState.Playing && !lm.DragBlocked && !state.IsMoveInProgress && used != lastTrailMove && !BoardBusy(lm))
                    {
                        lastTrailMove = used;
                        if (used > 0)
                        {
                            trail.Append(trail.Length > 0 ? "|" : string.Empty).Append(LevelManager.Score);
                        }
                    }

                    // Board integrity once per settled move: the bot waits a few frames so landings finish.
                    var settled = status == GameState.Playing && !lm.DragBlocked && !state.IsMoveInProgress && !BoardBusy(lm);
                    settledFrames = settled ? settledFrames + 1 : 0;
                    if (settled && used != lastCheckedMove)
                    {
                        if (settledFrames < BoardCheckFrames)
                        {
                            yield return null;
                            continue;
                        }

                        lastCheckedMove = used;
                        CheckBoard(lm, level, run, used);
                    }

                    // Out of moves while the last goal piece is still flying: wait for the result like a player.
                    if (status != GameState.Playing || lm.DragBlocked || state.IsMoveInProgress || BoardBusy(lm) ||
                        (lm.limitType == LIMIT.MOVES && lm.Limit <= 0))
                    {
                        if (Time.realtimeSinceStartup - lastProgress > StuckTimeout)
                        {
                            result = "stuck_" + status + (lm.DragBlocked ? "_drag" : string.Empty);
                            break;
                        }

                        yield return null;
                        continue;
                    }

                    var chain = FindBestChain(lm);
                    if (chain == null)
                    {
                        if (Time.realtimeSinceStartup - lastProgress > StuckTimeout)
                        {
                            result = "no_moves";
                            break;
                        }

                        yield return null;
                        continue;
                    }

                    var limitBefore = lm.Limit;
                    var played = TryPlay(lm, state, chain);
                    if (played)
                    {
                        moves++;
                        lastProgress = Time.realtimeSinceStartup;
                    }

                    if (_debug)
                    {
                        File.AppendAllText(Path.Combine(_outDir, "debug.log"),
                            $"L{level} R{run} t={Time.realtimeSinceStartup - start:0.00} played={played} chain={chain.Count} limitBefore={limitBefore} " +
                            $"limitNow={lm.Limit} drag={lm.DragBlocked} status={lm.gameStatus} score={LevelManager.Score} bombs={BombTimers(lm)} " +
                            $"need={lm.bombsCollect} timers={lm.bombTimers.Count}\n");
                    }

                    // Let the move start (DragBlocked goes up) before polling again.
                    yield return null;
                    yield return null;
                }

                if (result == "win" && !_measure)
                {
                    // Stars are final once WinState ran.
                    if (_debug)
                    {
                        File.AppendAllText(Path.Combine(_outDir, "debug.log"),
                            $"L{level} R{run} WIN-START bombs={BombTimers(lm)} need={lm.bombsCollect} fly={lm.ingredientFly}\n");
                    }

                    var t = Time.realtimeSinceStartup;
                    while (lm != null && lm.gameStatus != GameState.Win && Time.realtimeSinceStartup - t < 30f)
                    {
                        Time.timeScale = _timeScale;
                        yield return null;
                    }

                    if (_debug && lm != null)
                    {
                        File.AppendAllText(Path.Combine(_outDir, "debug.log"),
                            $"L{level} R{run} WIN-END bombs={BombTimers(lm)} need={lm.bombsCollect}\n");
                    }

                    if (lm != null && lm.gameStatus != GameState.Win)
                    {
                        result = "win_stuck_prewin";
                    }
                }
            }

            var stars = lm != null ? lm.stars : 0;
            var mode = lm != null ? lm.target.ToString() : "?";
            var errors = _runErrors.Count;
            var secs = (Time.realtimeSinceStartup - start).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var line = $"{level},{run},{result},{stars},{endLimit},{startLimit},{startLimit - endLimit},{moves},{endScore},{secs},{errors},{mode},{trail},{GoalLeft(lm)}\n";
            File.AppendAllText(csv, line);

            if (errors > 0)
            {
                var sb = new StringBuilder();
                foreach (var e in _runErrors)
                {
                    sb.Append($"L{level} R{run}: ").Append(e).Append('\n');
                }

                File.AppendAllText(Path.Combine(_outDir, "errors.log"), sb.ToString());
            }

            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private static int GoalLeft(LevelManager lm)
        {
            if (lm == null)
            {
                return -1;
            }

            switch (lm.target)
            {
                case Target.BLOCKS:
                    return lm.TargetBlocks;
                case Target.CAGES:
                    return lm.TargetCages;
                case Target.BOMBS:
                    return lm.bombsCollect - lm.TargetBombs;
                case Target.COLLECT:
                case Target.ITEMS:
                    var left = 0;
                    foreach (var t in lm.ingrTarget)
                    {
                        left += Mathf.Max(0, t.count);
                    }

                    return left;
                default:
                    return 0;
            }
        }

        private static string BombTimers(LevelManager lm)
        {
            var sb = new StringBuilder();
            foreach (var it in lm.GetItems())
            {
                if (it != null && it.currentType == ItemsTypes.BOMB)
                {
                    sb.Append(it.bombTimer).Append(',');
                }
            }

            return sb.Append(" got=").Append(lm.TargetBombs).ToString();
        }

        private const int BoardCheckFrames = 12;

        // Writes board.log lines for holes, broken square<->item links, pieces off their cell and ghost pieces.
        private void CheckBoard(LevelManager lm, int level, int run, int move)
        {
            if (lm.ingredientFly)
            {
                return;
            }

            var issues = new List<string>();
            for (var row = 0; row < lm.maxRows; row++)
            {
                for (var col = 0; col < lm.maxCols; col++)
                {
                    var sq = lm.GetSquare(col, row);
                    if (sq == null || sq.IsNone())
                    {
                        continue;
                    }

                    var it = sq.item;
                    if (it == null)
                    {
                        if (sq.CanGoInto())
                        {
                            issues.Add($"hole({col},{row}) {sq.type}{(sq.IsHaveSolidAbove() ? " solidAbove" : string.Empty)}");
                        }

                        continue;
                    }

                    if (it.square != sq)
                    {
                        issues.Add($"link({col},{row})");
                    }

                    var d = Vector2.Distance(it.transform.position, sq.transform.position);
                    if (d > 0.25f)
                    {
                        issues.Add($"offset({col},{row}) {d:0.00}");
                    }
                }
            }

            foreach (var go in GameObject.FindGameObjectsWithTag("Item"))
            {
                var it = go.GetComponent<Item>();
                if (it != null && !it.destroying && (it.square == null || it.square.item != it))
                {
                    issues.Add($"ghost {it.currentType} at {(Vector2)go.transform.position}");
                }
            }

            if (issues.Count > 0)
            {
                File.AppendAllText(Path.Combine(_outDir, "board.log"), $"L{level} R{run} move {move}: {string.Join("; ", issues)}\n");
            }
        }

        private static bool BoardBusy(LevelManager lm)
        {
            var items = lm.GetItems();
            for (var i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it != null && (it.falling || it.destroying))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryPlay(LevelManager lm, PlayingState state, List<Item> chain)
        {
            lm.destroyAnyway.Clear();
            foreach (var item in chain)
            {
                state.SelectItem(item);
            }

            if (lm.destroyAnyway.Count < 3 || lm.destroyAnyway.Count != chain.Count)
            {
                state.ClearSelection();
                return false;
            }

            var center = Vector3.zero;
            foreach (var it in lm.destroyAnyway)
            {
                center += it.transform.position;
            }

            GameFeelManager.Instance?.OnMatchReleased(lm.destroyAnyway.Count, center / lm.destroyAnyway.Count);
            state.ProcessMatchedItems().Forget();
            return true;
        }

        // ---------- chain search ----------

        private Item[,] _grid;
        private int _cols, _rows;

        private List<Item> FindBestChain(LevelManager lm)
        {
            _cols = lm.maxCols;
            _rows = lm.maxRows;
            _grid = new Item[_cols, _rows];
            var all = new List<Item>();
            foreach (var it in lm.GetItems())
            {
                if (it == null || it.square == null || it.destroying || it.falling || it.currentType == ItemsTypes.INGREDIENT || it.color < 0)
                {
                    continue;
                }

                if (it.square.col < 0 || it.square.col >= _cols || it.square.row < 0 || it.square.row >= _rows)
                {
                    continue;
                }

                _grid[it.square.col, it.square.row] = it;
                all.Add(it);
            }

            List<Item> best = null;
            var bestScore = float.MinValue;
            var path = new List<Item>();
            var used = new HashSet<Item>();

            // A budget per start: a shared one was spent on the first big colour cluster,
            // so cells later in the list (often the last goal cells) were never considered.
            for (var i = all.Count - 1; i > 0; i--)
            {
                var j = _rnd.Next(i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }

            foreach (var startItem in all)
            {
                var budget = 200;
                path.Clear();
                used.Clear();
                path.Add(startItem);
                used.Add(startItem);
                Dfs(lm, path, used, ref budget, ref best, ref bestScore);
            }

            return best;
        }

        private void Dfs(LevelManager lm, List<Item> path, HashSet<Item> used, ref int budget, ref List<Item> best, ref float bestScore)
        {
            if (--budget <= 0)
            {
                return;
            }

            if (path.Count >= 3)
            {
                var s = Score(lm, path) + (float)_rnd.NextDouble() * 0.5f;
                if (s > bestScore)
                {
                    bestScore = s;
                    best = new List<Item>(path);
                }
            }

            if (path.Count >= 14)
            {
                return;
            }

            var last = path[path.Count - 1];
            var lc = last.square.col;
            var lr = last.square.row;
            // Random neighbour order so long chains are found from different directions.
            var start = _rnd.Next(8);
            for (var k = 0; k < 8; k++)
            {
                var d = (start + k) % 8;
                var dc = d switch { 0 => -1, 1 => 0, 2 => 1, 3 => -1, 4 => 1, 5 => -1, 6 => 0, _ => 1 };
                var dr = d switch { 0 => -1, 1 => -1, 2 => -1, 3 => 0, 4 => 0, 5 => 1, 6 => 1, _ => 1 };
                var c = lc + dc;
                var r = lr + dr;
                if (c < 0 || r < 0 || c >= _cols || r >= _rows)
                {
                    continue;
                }

                var next = _grid[c, r];
                if (next == null || used.Contains(next) || next.color != path[0].color)
                {
                    continue;
                }

                path.Add(next);
                used.Add(next);
                Dfs(lm, path, used, ref budget, ref best, ref bestScore);
                path.RemoveAt(path.Count - 1);
                used.Remove(next);
                if (budget <= 0)
                {
                    return;
                }
            }
        }

        private float Score(LevelManager lm, List<Item> path)
        {
            float s = path.Count;
            var touchedStones = new HashSet<Square>();
            foreach (var it in path)
            {
                var sq = it.square;
                if (it.currentType == ItemsTypes.HORIZONTAL_STRIPPED || it.currentType == ItemsTypes.VERTICAL_STRIPPED ||
                    it.currentType == ItemsTypes.PACKAGE || it.currentType == ItemsTypes.CHOCOBOMB)
                {
                    s += 3f;
                }

                switch (lm.target)
                {
                    case Target.BLOCKS:
                        if ((sq.type == SquareTypes.BLOCK || sq.type == SquareTypes.DOUBLEBLOCK) && sq.block.Count > 0)
                        {
                            // Goal cells outweigh any chain length (a 14-chain scores about 18).
                            s += 20f;
                        }

                        break;
                    case Target.CAGES:
                        if (sq.type == SquareTypes.WIREBLOCK && sq.cageHP > 0)
                        {
                            s += 20f + 2f * path.Count;
                        }

                        break;
                    case Target.BOMBS:
                        if (it.currentType == ItemsTypes.BOMB)
                        {
                            // defusing beats any long chain, the closer to blowing up the more
                            s += 15f + 2f * Mathf.Max(0, 12 - it.bombTimer);
                        }

                        break;
                    case Target.COLLECT:
                        // Clearing cells under an ingredient lets it fall towards the exit.
                        if (HasIngredientAbove(lm, sq.col, sq.row))
                        {
                            s += 8f;
                        }

                        break;
                    case Target.ITEMS:
                        for (var i = 0; i < lm.collectItems.Length; i++)
                        {
                            if (lm.collectItems[i] != CollectItems.None && (int)lm.collectItems[i] - 1 == it.color &&
                                i < lm.ingrTarget.Count && lm.ingrTarget[i].count > 0)
                            {
                                s += 6f;
                                break;
                            }
                        }

                        break;
                }

                foreach (var n in sq.GetAllNeghbors())
                {
                    if (n != null && (n.type == SquareTypes.SOLIDBLOCK || n.type == SquareTypes.THRIVING))
                    {
                        touchedStones.Add(n);
                    }
                }
            }

            s += touchedStones.Count * 1.5f;
            if (lm.target == Target.COLLECT)
            {
                // A stone under an ingredient is what holds it back.
                foreach (var stone in touchedStones)
                {
                    if (HasIngredientAbove(lm, stone.col, stone.row))
                    {
                        s += 12f;
                    }
                }
            }

            if (path.Count >= 6)
            {
                s += 2f * (path.Count / 6);
            }

            return s;
        }

        private bool HasIngredientAbove(LevelManager lm, int col, int row)
        {
            for (var r = row - 1; r >= 0; r--)
            {
                var aboveSq = lm.GetSquare(col, r, true);
                if (aboveSq != null && aboveSq.item != null && aboveSq.item.currentType == ItemsTypes.INGREDIENT)
                {
                    return true;
                }

                if (_grid[col, r] == null && (aboveSq == null || !aboveSq.CanGoInto()))
                {
                    return false;
                }
            }

            return false;
        }

        // ---------- helpers ----------

        private static object Resolve(string typeName)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                return null;
            }

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (scope == null || scope.Container == null)
                {
                    continue;
                }

                try
                {
                    var obj = scope.Container.Resolve(type);
                    if (obj != null)
                    {
                        return obj;
                    }
                }
                catch (Exception)
                {
                    // not registered in this scope
                }
            }

            return null;
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        private static void InvokeIfExists(object target, string method)
        {
            target?.GetType().GetMethod(method, Type.EmptyTypes)?.Invoke(target, null);
        }

        public static IEnumerable<int> ParseLevels(string spec)
        {
            foreach (var part in spec.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var dash = part.IndexOf('-');
                if (dash > 0)
                {
                    var a = int.Parse(part.Substring(0, dash));
                    var b = int.Parse(part.Substring(dash + 1));
                    for (var i = a; i <= b; i++)
                    {
                        yield return i;
                    }
                }
                else
                {
                    yield return int.Parse(part);
                }
            }
        }
    }
}
#endif
