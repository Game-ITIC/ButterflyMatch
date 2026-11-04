using System;

namespace JuiceFresh.Counters
{
    /// <summary>"collected/total" of a level goal (blocks, cages, bombs, one ingredient); ticks its target icon when done.</summary>
    public sealed class TargetGoalCounterDisplay : CounterDisplay
    {
        private readonly Func<Counter_, int> _collected;
        private readonly Func<Counter_, bool> _isDone;

        private int _shownCollected = int.MinValue;
        private int _shownTotal = int.MinValue;

        public TargetGoalCounterDisplay(Func<Counter_, int> collected, Func<Counter_, bool> isDone)
        {
            _collected = collected;
            _isDone = isDone;
        }

        public override bool PunchOnChange => true;
        public override float PunchStrength(LevelManager level) => 0.18f;

        public override void OnEnabled(Counter_ counter)
        {
            _shownCollected = int.MinValue;
        }

        public override void Tick(Counter_ counter)
        {
            int collected = _collected(counter);
            if (collected != _shownCollected || counter.totalCount != _shownTotal)
            {
                _shownCollected = collected;
                _shownTotal = counter.totalCount;
                counter.SetText("" + collected + "/" + counter.totalCount);
            }

            if (_isDone(counter))
                counter.ParentGUI.Done();
        }
    }

    /// <summary>Goal amount on the level start card (bombs show how many must be defused).</summary>
    public sealed class MenuTargetCountDisplay : CounterDisplay
    {
        public override void Tick(Counter_ counter)
        {
            LevelManager level = LevelManager.THIS;
            counter.SetText(level.target == Target.BOMBS ? "" + level.bombsCollect : "" + counter.totalCount);
        }
    }
}
