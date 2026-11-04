using System.Collections;
using UnityEngine;

namespace JuiceFresh.Feel
{
    /// <summary>
    /// Moves and pulses the whole board (flutter, sway, bounce, ...) around its resting transform
    /// and always puts it back there.
    /// </summary>
    public sealed class BoardFeedback
    {
        private readonly MonoBehaviour _host;
        private readonly IGameFeelTuning _tuning;

        private Transform _board;
        private Coroutine _motion;
        private Coroutine _pulse;
        private Vector3 _restPosition;
        private Vector3 _restScale;
        private bool _restCaptured;

        public BoardFeedback(MonoBehaviour host, IGameFeelTuning tuning)
        {
            _host = host;
            _tuning = tuning;
        }

        public Transform Board => _board;

        /// <summary>The bounce style animates scale, so it replaces the scale pulse.</summary>
        public bool UsesScaleMotion => _tuning.BoardMotionStyle == BoardMotionStyle.ElasticBounce;

        public void Bind(Transform board)
        {
            if (board == null || _board == board)
                return;

            Stop();
            _board = board;
            CaptureRest();
        }

        /// <summary>
        /// The board was just placed by its owner (level intro, layout change): makes that placement the new rest.
        /// Stopping with Stop() first snapped the board back to the previous rest before re-capturing it, so the rest
        /// never changed after Bind and every level ended up where the scene had the board (9-column layout):
        /// narrower boards sat shifted to the left.
        /// </summary>
        public void RefreshRest()
        {
            CancelMotion();
            StopPulse();
            CaptureRest();
        }

        public void Stop()
        {
            StopMotion();
            StopPulse();
        }

        public void TriggerMotion(float duration, float magnitude)
        {
            BoardMotionStyle style = _tuning.BoardMotionStyle;
            if (!_tuning.EnableBoardShake || _board == null || magnitude <= 0f || style == BoardMotionStyle.None)
                return;

            duration *= _tuning.MotionDurationScale;
            StopMotion();

            if (UsesScaleMotion)
                StopPulse();

            _motion = _host.StartCoroutine(MotionRoutine(style, duration, magnitude));
        }

        /// <summary>Scale pulse, skipped when the motion style already animates scale.</summary>
        public void TryTriggerPulse(float duration, float strength)
        {
            if (!UsesScaleMotion)
                TriggerPulse(duration, strength);
        }

        public void TriggerPulse(float duration, float strength)
        {
            if (_board == null || strength <= 0f)
                return;

            duration *= _tuning.MotionDurationScale;
            StopPulse();
            _pulse = _host.StartCoroutine(PulseRoutine(duration, strength));
        }

        private void CaptureRest()
        {
            if (_board == null)
            {
                _restCaptured = false;
                return;
            }

            _restPosition = _board.localPosition;
            _restScale = _board.localScale;
            _restCaptured = true;
        }

        private void StopMotion()
        {
            CancelMotion();

            if (_board != null && _restCaptured)
                _board.localPosition = _restPosition;
        }

        private void CancelMotion()
        {
            if (_motion != null)
            {
                _host.StopCoroutine(_motion);
                _motion = null;
            }
        }

        private void StopPulse()
        {
            if (_pulse != null)
            {
                _host.StopCoroutine(_pulse);
                _pulse = null;
            }

            if (_board != null && _restCaptured)
                _board.localScale = _restScale;
        }

        private IEnumerator MotionRoutine(BoardMotionStyle style, float duration, float magnitude)
        {
            bool scaleMotion = style == BoardMotionStyle.ElasticBounce;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_board == null)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (scaleMotion)
                    _board.localScale = _restScale * (1f + ElasticBounce(t) * magnitude * 2.8f);
                else
                    _board.localPosition = _restPosition + Offset(style, t, magnitude);

                yield return null;
            }

            if (_board != null)
            {
                if (scaleMotion)
                    _board.localScale = _restScale;
                else
                    _board.localPosition = _restPosition;
            }

            _motion = null;
        }

        private static float ElasticBounce(float t) => Mathf.Sin(t * Mathf.PI * 2.2f) * Mathf.Exp(-t * 3.5f);

        private static Vector3 Offset(BoardMotionStyle style, float t, float magnitude)
        {
            float envelope = Mathf.Sin(t * Mathf.PI);

            switch (style)
            {
                case BoardMotionStyle.VerticalFloat:
                    return new Vector3(0f, Mathf.Sin(t * Mathf.PI * 1.5f) * magnitude * 1.8f * envelope, 0f);

                case BoardMotionStyle.GentleSway:
                    return new Vector3(
                        Mathf.Sin(t * Mathf.PI * 1.2f) * magnitude * 2.4f * envelope,
                        Mathf.Sin(t * Mathf.PI * 0.8f) * magnitude * 0.15f * envelope,
                        0f);

                case BoardMotionStyle.SoftSink:
                    return new Vector3(0f, -envelope * magnitude * 1.6f, 0f);

                default: // Flutter
                    return new Vector3(
                        Mathf.Sin(t * Mathf.PI * 3f) * magnitude * envelope,
                        Mathf.Sin(t * Mathf.PI * 2f) * magnitude * 0.22f * envelope,
                        0f);
            }
        }

        private IEnumerator PulseRoutine(float duration, float strength)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_board == null)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _board.localScale = _restScale * (1f + Mathf.Sin(t * Mathf.PI) * strength);
                yield return null;
            }

            if (_board != null)
                _board.localScale = _restScale;

            _pulse = null;
        }
    }
}
