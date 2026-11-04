using GameLocalization;

namespace JuiceFresh.Counters
{
    /// <summary>The goal sentence of the level start card ("Collect ...", "Free the cages", ...).</summary>
    public static class TargetDescription
    {
        /// <summary>Null for an unknown target (the label keeps its text).</summary>
        public static string Text()
        {
            LevelManager level = LevelManager.THIS;
            var descriptions = level.targetDiscriptions;
            switch (level.target)
            {
                case Target.SCORE: return L10n.Format("target_stars", (int)level.starsTargetCount);
                case Target.BLOCKS: return L10n.Get("target_blocks", descriptions[1]);
                case Target.COLLECT: return L10n.Get("target_collect", descriptions[2]);
                case Target.ITEMS: return L10n.Get("target_items", descriptions[3]);
                case Target.CAGES: return L10n.Get("target_cages", descriptions[4]);
                case Target.BOMBS: return L10n.Get("target_bombs", descriptions[5]);
                default: return null;
            }
        }
    }
}
