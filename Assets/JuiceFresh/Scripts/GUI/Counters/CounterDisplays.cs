using GameLocalization;
using UnityEngine;

namespace JuiceFresh.Counters
{
    /// <summary>Maps a counter GameObject name to what it shows.</summary>
    public static class CounterDisplays
    {
        public static CounterDisplay Create(string counterName)
        {
            switch (counterName)
            {
                case "Score": return new ScoreCounterDisplay();
                case "Limit": return new LimitCounterDisplay();

                case "TargetBlocks":
                    return new TargetGoalCounterDisplay(
                        c => c.totalCount - LevelManager.THIS.targetBlocks,
                        c => LevelManager.THIS.targetBlocks == 0);
                case "TargetCages":
                    return new TargetGoalCounterDisplay(
                        c => c.totalCount - LevelManager.THIS.TargetCages,
                        c => LevelManager.THIS.TargetCages == 0);
                case "TargetBombs":
                    return new TargetGoalCounterDisplay(
                        c => LevelManager.THIS.TargetBombs,
                        c => LevelManager.THIS.TargetBombs >= c.totalCount);
                case "CountIngr":
                    return new TargetGoalCounterDisplay(
                        c => c.totalCount - LevelManager.THIS.ingrTarget[c.ingrTrackNumber].count,
                        c => LevelManager.THIS.ingrTarget[c.ingrTrackNumber].count == 0);
                case "CountIngrForMenu": return new MenuTargetCountDisplay();

                case "LabelKeepPlay": return new LabelCounterDisplay(KeepPlayText);
                case "BestScore":
                    return new LabelCounterDisplay(() =>
                        L10n.Format("best_score", PlayerPrefs.GetInt("Score" + PlayerPrefs.GetInt("OpenLevel"))));
                case "Level": return new LabelCounterDisplay(() => L10n.Format("level_n", PlayerPrefs.GetInt("OpenLevel")));
                case "Gems": return new LabelCounterDisplay(GemsText);
                case "TargetDescription1": return new LabelCounterDisplay(TargetDescription.Text);

                case "StarsT": return new EnableTimeLabelDisplay(() => "" + LevelManager.THIS.stars);
                case "TargetScore": return new EnableTimeLabelDisplay(TargetScoreText);

                default: return new EmptyCounterDisplay();
            }
        }

        private static string KeepPlayText()
        {
            LevelManager level = LevelManager.THIS;
            return level.limitType == LIMIT.MOVES
                ? L10n.Format("keep_play_moves", level.ExtraFailedMoves)
                : L10n.Format("keep_play_secs", level.ExtraFailedSecs);
        }

        private static string GemsText()
        {
            var currencyService = LevelManager.THIS.CurrencyService;
            int gems = currencyService != null
                ? (int)(currencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Diamond)?.Value ?? 0)
                : 0;
            return "" + gems;
        }

        // Only score levels show their star score target; other levels keep the prefab text.
        private static string TargetScoreText()
        {
            LevelManager level = LevelManager.THIS;
            return level != null && level.target == Target.SCORE ? "" + level.GetScoresOfTargetStars() : null;
        }
    }
}
