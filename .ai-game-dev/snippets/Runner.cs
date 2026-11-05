
using UnityEngine; using System.Text; using System.Threading.Tasks; using JuiceFresh;
public static class Runner {
  static UnityEngine.UI.Button FindBtn(string name) {
    foreach (var b in Object.FindObjectsOfType<UnityEngine.UI.Button>()) if (b.isActiveAndEnabled && b.name == name) return b; return null; }
  public static async Task<string> PlayLevel(int maxMoves = 40) {
    var log = new StringBuilder();
    int lvl = LevelManager.THIS ? LevelManager.THIS.currentLevel : -1;
    for (int k = 0; k < maxMoves; k++) {
      int w = 0;
      while ((LevelManager.THIS == null || LevelManager.THIS.gameStatus != GameState.Playing || LevelManager.THIS.DragBlocked) && w < 10000) {
        if (LevelManager.THIS != null && (LevelManager.THIS.gameStatus == GameState.Win || LevelManager.THIS.gameStatus == GameState.GameOver)) break;
        await Task.Delay(250); w += 250; }
      var lm = LevelManager.THIS;
      if (lm == null || lm.gameStatus != GameState.Playing) {
        // wait for end state to settle
        int w2 = 0; while (lm != null && lm.gameStatus != GameState.Win && lm.gameStatus != GameState.GameOver && lm.gameStatus != GameState.Playing && w2 < 15000) { await Task.Delay(250); w2 += 250; }
        if (lm != null && lm.gameStatus == GameState.Playing) continue;
        log.Append($"L{lvl} end={lm?.gameStatus} score={LevelManager.Score} stars={lm?.stars} movesLeft={lm?.Limit} after {k} moves"); return log.ToString(); }
      await Task.Delay(500);
      var r = Bot.Auto(); if (r.StartsWith("no chain")) { log.AppendLine("no chain!"); await Task.Delay(2000); continue; }
      await Task.Delay(1200);
    }
    return log + " timeout status=" + LevelManager.THIS?.gameStatus;
  }
  public static async Task<string> Next() {
    for (int i = 0; i < 40; i++) { var b = FindBtn("Next"); if (b != null && b.interactable) { b.onClick.Invoke(); break; } await Task.Delay(250); }
    for (int i = 0; i < 60; i++) { await Task.Delay(250); var lm = LevelManager.THIS; if (lm != null && lm.gameStatus == GameState.Playing && !lm.DragBlocked) return "next ok L" + lm.currentLevel; }
    return "next: status=" + LevelManager.THIS?.gameStatus;
  }
}