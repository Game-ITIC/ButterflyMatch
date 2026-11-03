namespace Models
{
    /// <summary>
    /// Counters of the current level attempt (reset when a level starts). Read at the end of the level
    /// for daily tasks and analytics, so the per-tile hot path only increments an int.
    /// </summary>
    public static class LevelSessionStats
    {
        public static int ItemsCollected;
        public static int BoostersUsed;
        public static int ContinuesUsed;

        public static void Reset()
        {
            ItemsCollected = 0;
            BoostersUsed = 0;
            ContinuesUsed = 0;
        }
    }
}
