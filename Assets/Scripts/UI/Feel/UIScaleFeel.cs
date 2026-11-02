using DG.Tweening;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Owns small scale feedback on one UI transform. Press, idle breathing and pop/punch run as
    /// separate multipliers over the captured rest scale, so they can overlap without fighting
    /// over localScale or drifting it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIScaleFeel : MonoBehaviour
    {
        private const float MinRestScaleSqr = 0.0001f;

        [Header("Press")]
        [SerializeField, Min(0f)] private float pressedScale = 0.92f;
        [SerializeField, Min(0f)] private float pressDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseOvershoot = 1.04f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.26f;

        [Header("Wiggle")]
        [SerializeField] private float wiggleAngle = 7f;
        [SerializeField, Min(0f)] private float wiggleDuration = 0.32f;

        private Vector3 restScale = Vector3.one;
        private bool hasRestScale;

        private float pressFactor = 1f;
        private float idleFactor = 1f;
        private float popFactor = 1f;

        private Tween pressTween;
        private Tween idleTween;
        private Tween popTween;
        private Tween wiggleTween;

        private bool breathing;
        private float breathingAmount;
        private float breathingPeriod;

        private void OnEnable()
        {
            if(breathing)
            {
                StartBreathing();
            }
        }

        private void OnDisable()
        {
            ResetState();
        }

        private void OnDestroy()
        {
            KillAll();
        }

        public void Press()
        {
            if(!Prepare(ref pressTween))
            {
                return;
            }

            pressTween = DOTween.To(GetPressFactor, SetPressFactor, pressedScale, pressDuration)
                .SetEase(Ease.OutQuad);
            Configure(pressTween);
        }

        public void Release()
        {
            if(!Prepare(ref pressTween))
            {
                return;
            }

            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(GetPressFactor, SetPressFactor, releaseOvershoot, releaseDuration * 0.4f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(GetPressFactor, SetPressFactor, 1f, releaseDuration * 0.6f).SetEase(Ease.InOutSine));
            pressTween = sequence;
            Configure(pressTween);
        }

        /// <summary>Grows from <paramref name="from"/> of the rest scale back to full size with a soft overshoot.</summary>
        public void PopIn(float delay = 0f, float from = 0.85f, float duration = 0.32f, float overshoot = 1.6f)
        {
            if(!Prepare(ref popTween))
            {
                return;
            }

            popFactor = from;
            Apply();

            popTween = DOTween.To(GetPopFactor, SetPopFactor, 1f, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack, overshoot);
            Configure(popTween);
        }

        /// <summary>Short bump above the rest scale, used to acknowledge a value change.</summary>
        public void Punch(float strength = 0.12f, float duration = 0.3f)
        {
            if(!Prepare(ref popTween))
            {
                return;
            }

            popFactor = 1f;

            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(GetPopFactor, SetPopFactor, 1f + strength, duration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(GetPopFactor, SetPopFactor, 1f, duration * 0.65f).SetEase(Ease.OutBack, 2f));
            popTween = sequence;
            Configure(popTween);
        }

        /// <summary>Small "no" rotation shake, used when a disabled control is tapped.</summary>
        public void Wiggle()
        {
            if(wiggleTween != null && wiggleTween.IsActive())
            {
                wiggleTween.Complete();
            }

            wiggleTween = transform.DOPunchRotation(new Vector3(0f, 0f, wiggleAngle), wiggleDuration, 12, 0.6f);
            Configure(wiggleTween);
        }

        /// <summary>Slow looping scale breath that draws attention to a call to action.</summary>
        public void SetBreathing(bool enabled, float amount = 0.04f, float period = 1.2f)
        {
            var unchanged = breathing == enabled
                            && Mathf.Approximately(breathingAmount, amount)
                            && Mathf.Approximately(breathingPeriod, period);

            breathing = enabled;
            breathingAmount = amount;
            breathingPeriod = period;

            if(!isActiveAndEnabled)
            {
                return;
            }

            if(!enabled)
            {
                StopBreathing();
                return;
            }

            if(unchanged && idleTween != null && idleTween.IsActive())
            {
                return;
            }

            StartBreathing();
        }

        private void StartBreathing()
        {
            if(!Prepare(ref idleTween))
            {
                return;
            }

            idleFactor = 1f;
            idleTween = DOTween.To(GetIdleFactor, SetIdleFactor, 1f + breathingAmount, Mathf.Max(0.05f, breathingPeriod * 0.5f))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            Configure(idleTween);
        }

        private void StopBreathing()
        {
            Kill(ref idleTween);

            if(Mathf.Approximately(idleFactor, 1f))
            {
                return;
            }

            idleTween = DOTween.To(GetIdleFactor, SetIdleFactor, 1f, 0.15f).SetEase(Ease.OutQuad);
            Configure(idleTween);
        }

        private bool Prepare(ref Tween tween)
        {
            if(!IsAnimating())
            {
                var currentScale = transform.localScale;
                if(currentScale.sqrMagnitude < MinRestScaleSqr)
                {
                    return false;
                }

                restScale = currentScale;
                hasRestScale = true;
            }

            Kill(ref tween);
            return hasRestScale;
        }

        private void Configure(Tween tween)
        {
            tween.SetUpdate(true)
                .SetTarget(this)
                .SetLink(gameObject);
        }

        private bool IsAnimating()
        {
            return IsActive(pressTween) || IsActive(idleTween) || IsActive(popTween)
                   || !Mathf.Approximately(pressFactor, 1f)
                   || !Mathf.Approximately(idleFactor, 1f)
                   || !Mathf.Approximately(popFactor, 1f);
        }

        private void ResetState()
        {
            var wasScaled = hasRestScale && IsAnimating();

            KillAll();
            pressFactor = 1f;
            idleFactor = 1f;
            popFactor = 1f;

            if(wasScaled)
            {
                transform.localScale = restScale;
            }
        }

        private void KillAll()
        {
            Kill(ref pressTween);
            Kill(ref idleTween);
            Kill(ref popTween);

            if(wiggleTween != null && wiggleTween.IsActive())
            {
                wiggleTween.Complete();
                wiggleTween.Kill();
            }

            wiggleTween = null;
        }

        private void Apply()
        {
            if(hasRestScale)
            {
                transform.localScale = restScale * (pressFactor * idleFactor * popFactor);
            }
        }

        private float GetPressFactor() => pressFactor;
        private float GetIdleFactor() => idleFactor;
        private float GetPopFactor() => popFactor;

        private void SetPressFactor(float value)
        {
            pressFactor = value;
            Apply();
        }

        private void SetIdleFactor(float value)
        {
            idleFactor = value;
            Apply();
        }

        private void SetPopFactor(float value)
        {
            popFactor = value;
            Apply();
        }

        private static bool IsActive(Tween tween)
        {
            return tween != null && tween.IsActive();
        }

        private static void Kill(ref Tween tween)
        {
            if(tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = null;
        }
    }
}
