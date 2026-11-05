
using UnityEngine; using System.Reflection; using System.Collections.Generic; using System.Text; using JuiceFresh;
public static class Bot {
  static LevelManager LM => LevelManager.THIS;
  static object State() => typeof(LevelManager).GetField("_currentState", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(LM);
  public static string Board() {
    var lm = LM; if (lm == null) return "no LevelManager";
    var sb = new StringBuilder();
    sb.AppendLine($"status={lm.gameStatus} state={State()?.GetType().Name} limit={lm.Limit} drag={lm.DragBlocked} size={lm.maxCols}x{lm.maxRows}");
    for (int r = 0; r < lm.maxRows; r++) {
      for (int c = 0; c < lm.maxCols; c++) {
        var sq = lm.GetSquare(c, r, true);
        if (sq == null || sq.item == null) { sb.Append(" .. "); continue; }
        var t = sq.item.currentType; string s = t == ItemsTypes.NONE ? "" : t.ToString().Substring(0,2);
        sb.Append((" " + sq.item.color + s).PadRight(5));
      }
      sb.AppendLine();
    }
    return sb.ToString();
  }
  // path: list of (col,row) pairs flattened
  public static string Move(params int[] p) {
    var st = State(); if (!(st is PlayingState)) return "not playing: " + st?.GetType().Name;
    if (LM.DragBlocked) return "drag blocked";
    var sel = typeof(PlayingState).GetMethod("SelectItem", BindingFlags.NonPublic|BindingFlags.Instance);
    var up = typeof(PlayingState).GetMethod("HandleTouchUp", BindingFlags.NonPublic|BindingFlags.Instance);
    for (int i = 0; i + 1 < p.Length; i += 2) {
      var sq = LM.GetSquare(p[i], p[i+1], true);
      if (sq?.item == null) return "no item at " + p[i] + "," + p[i+1];
      sel.Invoke(st, new object[] { sq.item });
    }
    int n = LM.destroyAnyway.Count;
    up.Invoke(st, null);
    return "selected " + n;
  }


  static int W, H; static int[] col, bonus; static bool[] vis; static List<int> best, cur; static int bestScore, nodes;
  public static List<int> Best() {
    var lm = LM; W = lm.maxCols; H = lm.maxRows; col = new int[W*H]; bonus = new int[W*H]; vis = new bool[W*H];
    for (int r=0;r<H;r++) for (int c=0;c<W;c++) { var it = lm.GetSquare(c,r,true)?.item; int i=r*W+c;
      col[i] = (it == null || it.currentType == ItemsTypes.INGREDIENT) ? -1 : it.color;
      bonus[i] = (it != null && it.currentType != ItemsTypes.NONE) ? 15 : 0; }
    best = new List<int>(); cur = new List<int>(); bestScore = 0; nodes = 0;
    for (int i=0;i<W*H;i++) if (col[i] >= 0) Dfs(i, col[i], 0);
    return best;
  }
  static void Dfs(int i, int color, int score) {
    if (++nodes > 400000) return;
    vis[i] = true; cur.Add(i % W); cur.Add(i / W); score += 10 + bonus[i];
    if (cur.Count >= 6 && score > bestScore) { bestScore = score; best = new List<int>(cur); }
    if (cur.Count < 20) { int c = i % W, r = i / W;
      for (int dc=-1;dc<=1;dc++) for (int dr=-1;dr<=1;dr++) { int nc=c+dc, nr=r+dr;
        if ((dc==0&&dr==0)||nc<0||nr<0||nc>=W||nr>=H) continue; int j=nr*W+nc;
        if (!vis[j] && col[j]==color) Dfs(j, color, score); } }
    vis[i] = false; cur.RemoveAt(cur.Count-1); cur.RemoveAt(cur.Count-1);
  }
  public static string Auto() {
    var b = Best(); if (b.Count == 0) return "no chain\n" + Board();
    var sb = new StringBuilder("path:"); for (int i=0;i<b.Count;i+=2) sb.Append($" ({b[i]},{b[i+1]})");
    return sb + " -> " + Move(b.ToArray());
  }
}