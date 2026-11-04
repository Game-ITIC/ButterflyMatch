using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.Feel
{
    /// <summary>
    /// Short scale / wobble pulse on a single piece when it joins or leaves the chain.
    /// A piece always returns to its resting transform.
    /// </summary>
    public sealed class ItemPulseFeedback
    {
        // Board pieces rest at 0.9; a pulse must never start from an interrupted, shrunk scale.
        private const float BoardItemScale = 0.9f;
        private const float BaseDuration = 0.2f;

        private readonly MonoBehaviour _host;
        private readonly IGameFeelTuning _tuning;
        private readonly Dictionary<int, PulseState> _pulses = new Dictionary<int, PulseState>();

        public ItemPulseFeedback(MonoBehaviour host, IGameFeelTuning tuning)
        {
            _host = host;
            _tuning = tuning;
        }

        public void Play(Transform target, float amount)
        {
            int id = target.GetInstanceID();
            if (_pulses.TryGetValue(id, out PulseState active))
            {
                if (active.Coroutine != null)
                    _host.StopCoroutine(active.Coroutine);

                active.Restore();
            }

            var pulse = new PulseState(target, RestScaleOf(target), target.localPosition, target.localRotation);
            pulse.Coroutine = _host.StartCoroutine(PulseRoutine(id, pulse, amount));
            _pulses[id] = pulse;
        }

        public void CompleteAll()
        {
            if (_pulses.Count == 0)
                return;

            foreach (PulseState pulse in _pulses.Values)
            {
                if (pulse.Coroutine != null)
                    _host.StopCoroutine(pulse.Coroutine);

                pulse.Restore();
            }

            _pulses.Clear();
        }

        private static Vector3 RestScaleOf(Transform target)
        {
            if (target.GetComponent<Item>() == null)
                return target.localScale;

            Vector3 scale = target.localScale;
            if (scale.x < BoardItemScale * 0.75f || scale.y < BoardItemScale * 0.75f)
                return Vector3.one * BoardItemScale;

            return scale;
        }

        private IEnumerator PulseRoutine(int id, PulseState pulse, float amount)
        {
            float duration = BaseDuration * _tuning.MotionDurationScale * _tuning.ItemPulseDurationScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (pulse.Target == null)
                {
                    _pulses.Remove(id);
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                Apply(pulse, Mathf.Clamp01(elapsed / duration), amount);
                yield return null;
            }

            pulse.Restore();
            _pulses.Remove(id);
        }

        private void Apply(PulseState pulse, float t, float amount)
        {
            Transform target = pulse.Target;
            float envelope = Mathf.Sin(t * Mathf.PI);

            switch (_tuning.ItemMotionStyle)
            {
                case ItemMotionStyle.CrispTap:
                    float tap = t < 0.28f
                        ? Mathf.SmoothStep(0f, 1f, t / 0.28f)
                        : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.28f) / 0.72f);
                    target.localScale = pulse.RestScale * (1f + amount * tap);
                    break;

                case ItemMotionStyle.SquashStretch:
                    float bounce = Mathf.Sin(t * Mathf.PI * 2.5f) * Mathf.Exp(-t * 2.4f);
                    target.localScale = new Vector3(
                        pulse.RestScale.x * (1f + amount * bounce),
                        pulse.RestScale.y * (1f - amount * bounce * 0.62f),
                        pulse.RestScale.z);
                    break;

                case ItemMotionStyle.FloatUp:
                    target.localScale = pulse.RestScale * (1f + amount * envelope * 0.45f);
                    target.localPosition = pulse.RestPosition + Vector3.up * (amount * envelope * 0.75f);
                    break;

                case ItemMotionStyle.Wobble:
                    float wobble = Mathf.Sin(t * Mathf.PI * 4f) * envelope;
                    target.localScale = pulse.RestScale * (1f + amount * envelope * 0.35f);
                    target.localRotation = pulse.RestRotation * Quaternion.Euler(0f, 0f, wobble * amount * 55f);
                    break;

                default: // SoftPop
                    target.localScale = pulse.RestScale * (1f + amount * envelope);
                    break;
            }
        }

        private sealed class PulseState
        {
            public PulseState(Transform target, Vector3 restScale, Vector3 restPosition, Quaternion restRotation)
            {
                Target = target;
                RestScale = restScale;
                RestPosition = restPosition;
                RestRotation = restRotation;
            }

            public Transform Target { get; }
            public Vector3 RestScale { get; }
            public Vector3 RestPosition { get; }
            public Quaternion RestRotation { get; }
            public Coroutine Coroutine { get; set; }

            public void Restore()
            {
                if (Target == null)
                    return;

                Target.localScale = RestScale;
                Target.localPosition = RestPosition;
                Target.localRotation = RestRotation;
            }
        }
    }
}
