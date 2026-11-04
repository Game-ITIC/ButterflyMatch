namespace JuiceFresh.Scripts
{
    /// <summary>
    /// The booster the player picked on the level's boost bar: locks the other boost buttons while one is armed
    /// and applies the instant ones (+5 moves) right away.
    /// </summary>
    public sealed class InGameBoostController
    {
        private const int ExtraMovesAmount = 5;

        private readonly LevelManager _levelManager;

        public InGameBoostController(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        /// <summary>The armed boost, or the level's empty boost icon when none is armed (never null in a level).</summary>
        public BoostIcon Active =>
            _levelManager.activatedBoost == null ? _levelManager.emptyBoostIcon : _levelManager.activatedBoost;

        public void SetActive(BoostIcon boost)
        {
            if (boost == null)
                UnlockAll();

            _levelManager.activatedBoost = boost;

            if (boost != null)
                LockOthers();

            if (_levelManager.activatedBoost != null && _levelManager.activatedBoost.type == BoostType.ExtraMoves)
            {
                ApplyExtraMoves();
                SetActive(null);
            }
        }

        public void LockOthers()
        {
            BoostIcon active = Active;
            foreach (BoostIcon boost in _levelManager.InGameBoosts)
            {
                if (boost != active)
                    boost.LockBoost();
            }
        }

        public void UnlockAll()
        {
            foreach (BoostIcon boost in _levelManager.InGameBoosts)
                boost.UnLockBoost();
        }

        private void ApplyExtraMoves()
        {
            // +5 seconds on timed levels was never sold, only moves.
            if (_levelManager.limitType != LIMIT.MOVES)
                return;

            var model = _levelManager.BoostersProvider.BoostersModels.Find(t => t.Type == BoostType.ExtraMoves);
            if (model == null || !model.HasBooster())
                return;

            _levelManager.Limit += ExtraMovesAmount;
            model.Use();
            _levelManager.BoostersProvider.Save();
        }
    }
}
