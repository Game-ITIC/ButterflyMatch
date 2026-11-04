using UnityEngine;

namespace JuiceFresh.Counters
{
    /// <summary>
    /// Moves left (alert sound once at 5 or fewer) or time left as mm:ss (time-out sound every 30 s below 30 s).
    /// </summary>
    public sealed class LimitCounterDisplay : CounterDisplay
    {
        private const int LowMoves = 5;
        private const int LowSeconds = 30;
        private const float TimeOutSoundInterval = 30f;
        private const float TimeTextScale = 0.35f;

        private bool _alert;
        private float _lastTimeOutSound;
        private int _shownLimit = int.MinValue;
        private LIMIT _shownType;

        public override bool PunchOnChange => true;

        // The time text has its scale forced every frame, so only the moves counter bumps.
        public override float PunchStrength(LevelManager level) =>
            level.limitType == LIMIT.MOVES ? (level.Limit <= LowMoves ? 0.28f : 0.15f) : 0f;

        public override void OnEnabled(Counter_ counter)
        {
            _alert = false;
            _lastTimeOutSound = 0;
            _shownLimit = int.MinValue;
        }

        public override void Tick(Counter_ counter)
        {
            LevelManager level = LevelManager.THIS;
            if (LevelManager.Instance.limitType == LIMIT.MOVES)
                TickMoves(counter, level);
            else
                TickTime(counter, level);
        }

        private void TickMoves(Counter_ counter, LevelManager level)
        {
            ShowLimit(counter, level, LIMIT.MOVES, () => "" + level.Limit);

            if (level.Limit <= LowMoves)
            {
                if (!_alert)
                {
                    _alert = true;
                    SoundBase.Instance.PlaySound(SoundBase.Instance.alert);
                }
            }
            else
            {
                _alert = false;
            }
        }

        private void TickTime(Counter_ counter, LevelManager level)
        {
            ShowLimit(counter, level, LIMIT.TIME, () =>
            {
                int minutes = Mathf.FloorToInt(level.Limit / 60F);
                int seconds = Mathf.FloorToInt(level.Limit - minutes * 60);
                return string.Format("{0:00}:{1:00}", minutes, seconds);
            });
            counter.Text.transform.localScale = Vector3.one * TimeTextScale;

            if (level.Limit <= LowSeconds && level.gameStatus == GameState.Playing
                && _lastTimeOutSound + TimeOutSoundInterval < Time.time)
            {
                _lastTimeOutSound = Time.time;
                SoundBase.Instance.PlaySound(SoundBase.Instance.timeOut);
            }
        }

        // Rebuilds the text only when the limit (or its type) changed.
        private void ShowLimit(Counter_ counter, LevelManager level, LIMIT type, System.Func<string> format)
        {
            if (level.Limit == _shownLimit && type == _shownType)
                return;

            _shownLimit = level.Limit;
            _shownType = type;
            counter.SetText(format());
        }
    }
}
