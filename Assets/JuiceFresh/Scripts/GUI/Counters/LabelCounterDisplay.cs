using System;

namespace JuiceFresh.Counters
{
    /// <summary>A text label refreshed every frame (localized texts follow a language switch); null keeps the current text.</summary>
    public sealed class LabelCounterDisplay : CounterDisplay
    {
        private readonly Func<string> _text;

        public LabelCounterDisplay(Func<string> text)
        {
            _text = text;
        }

        public override void Tick(Counter_ counter)
        {
            string text = _text();
            if (text != null)
                counter.SetText(text);
        }
    }

    /// <summary>A text set once when the counter is enabled; null keeps the current text.</summary>
    public sealed class EnableTimeLabelDisplay : CounterDisplay
    {
        private readonly Func<string> _text;

        public EnableTimeLabelDisplay(Func<string> text)
        {
            _text = text;
        }

        public override void OnEnabled(Counter_ counter)
        {
            string text = _text();
            if (text != null)
                counter.SetText(text);
        }
    }
}
