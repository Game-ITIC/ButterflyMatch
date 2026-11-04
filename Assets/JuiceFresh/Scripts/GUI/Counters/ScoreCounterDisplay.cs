namespace JuiceFresh.Counters
{
    /// <summary>The level score in the top bar.</summary>
    public sealed class ScoreCounterDisplay : CounterDisplay
    {
        private int _shownScore = int.MinValue;

        public override bool PunchOnChange => true;
        public override float PunchStrength(LevelManager level) => 0.08f;

        public override void OnEnabled(Counter_ counter)
        {
            _shownScore = int.MinValue;
        }

        public override void Tick(Counter_ counter)
        {
            // Rebuild the string only when the score changes (it used to allocate every frame).
            int score = LevelManager.Score;
            if (score == _shownScore)
                return;

            _shownScore = score;
            counter.SetText("" + score);
        }
    }
}
