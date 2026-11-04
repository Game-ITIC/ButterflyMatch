namespace JuiceFresh.Counters
{
    /// <summary>
    /// What one HUD / popup counter shows. <see cref="Counter_"/> picks it once from its GameObject name
    /// (<see cref="CounterDisplays"/>) instead of comparing the name against every kind each frame.
    /// </summary>
    public abstract class CounterDisplay
    {
        /// <summary>The counter bumps when its text changes (moves, score, targets).</summary>
        public virtual bool PunchOnChange => false;

        /// <summary>Strength of that bump; 0 skips it.</summary>
        public virtual float PunchStrength(LevelManager level) => 0f;

        /// <summary>The counter was enabled.</summary>
        public virtual void OnEnabled(Counter_ counter) { }

        /// <summary>Every frame while enabled.</summary>
        public virtual void Tick(Counter_ counter) { }
    }

    /// <summary>A counter without behaviour (unknown name or a removed feature such as lives).</summary>
    public sealed class EmptyCounterDisplay : CounterDisplay
    {
    }
}
