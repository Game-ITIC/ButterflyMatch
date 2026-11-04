using System;
using System.Collections.Generic;
using DG.Tweening;
using JuiceFresh;
using UnityEngine;

public enum BoardRevealPattern
{
    CenterOut,
    EdgeIn,
    TopToBottom,
    LeftToRight,
    Diagonal,
    RandomPop,
    AllAtOnce,
}

public enum GameFieldIntroPreset
{
    Custom,
    SoftBloom,
    CenterRipple,
    FallingRows,
    PlayfulScatter,
}

namespace UI
{
    /// <summary>
    /// Board intro: cells bloom in a wave and their pieces pop in after them. Holds the tuning (inspector or
    /// preset) and plays the sequence; the cells come from BoardRevealGroups, preset values from
    /// GameFieldIntroSettings (UI/GameField).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFieldTweenAnimation : MonoBehaviour
    {
        [SerializeField] private bool useUnscaledTime;

        [Header("Grid Appearance (independent from Game Feel)")]
        [SerializeField] private GameFieldIntroPreset introPreset = GameFieldIntroPreset.SoftBloom;
        [SerializeField, HideInInspector] private GameFieldIntroPreset lastIntroPreset = (GameFieldIntroPreset)(-1);

        [Header("Cell Bloom")]
        [SerializeField, Range(0.05f, 0.8f)] private float cellStartScale = 0.32f;
        [SerializeField, Range(1f, 1.15f)] private float cellPeakScale = 1.045f;
        [SerializeField, Min(0.05f)] private float cellRiseDuration = 0.48f;
        [SerializeField, Min(0f)] private float cellSettleDuration = 0.14f;

        [Header("Butterfly Reveal")]
        [SerializeField, Range(0.05f, 0.8f)] private float itemStartScale = 0.2f;
        [SerializeField, Range(1f, 1.2f)] private float itemPeakScale = 1.08f;
        [SerializeField, Min(0f)] private float itemDelay = 0.13f;
        [SerializeField, Min(0.05f)] private float itemRiseDuration = 0.42f;
        [SerializeField, Min(0f)] private float itemSettleDuration = 0.16f;

        [Header("Wave")]
        [SerializeField, Min(0f)] private float waveStagger = 0.08f;
        [SerializeField, Range(0f, 0.05f)] private float waveJitter = 0.01f;
        [SerializeField, Min(0f)] private float maxRevealSpread = 1f;
        [SerializeField] private BoardRevealPattern revealPattern = BoardRevealPattern.CenterOut;

        [Header("Ease")]
        [SerializeField] private Ease riseEase = Ease.OutCubic;
        [SerializeField] private Ease settleEase = Ease.InOutSine;

        private Sequence sequence;
        private readonly List<CellRevealGroup> revealGroups = new List<CellRevealGroup>();

        private void OnValidate()
        {
            ApplyIntroPresetIfNeeded();
        }

        private void OnDisable()
        {
            KillTween();
        }

        public void SetIntroPreset(GameFieldIntroPreset preset)
        {
            introPreset = preset;
            lastIntroPreset = (GameFieldIntroPreset)(-1);
            ApplyIntroPresetIfNeeded();
        }

        private void ApplyIntroPresetIfNeeded()
        {
            if(introPreset == GameFieldIntroPreset.Custom || introPreset == lastIntroPreset)
                return;

            lastIntroPreset = introPreset;
            ApplyIntroSettings(GameFieldIntroSettings.For(introPreset));
        }

        private void ApplyIntroSettings(GameFieldIntroSettings settings)
        {
            cellStartScale = settings.CellStartScale;
            cellPeakScale = settings.CellPeakScale;
            cellRiseDuration = settings.CellRiseDuration;
            cellSettleDuration = settings.CellSettleDuration;
            itemStartScale = settings.ItemStartScale;
            itemPeakScale = settings.ItemPeakScale;
            itemDelay = settings.ItemDelay;
            itemRiseDuration = settings.ItemRiseDuration;
            itemSettleDuration = settings.ItemSettleDuration;
            waveStagger = settings.WaveStagger;
            waveJitter = settings.WaveJitter;
            maxRevealSpread = settings.MaxRevealSpread;
            revealPattern = settings.RevealPattern;
            riseEase = settings.RiseEase;
            settleEase = settings.SettleEase;
        }

        public void Play(Vector3 targetLocalPosition, Action onComplete)
        {
            KillTween();

            transform.localPosition = targetLocalPosition;
            BoardRevealGroups.Collect(revealGroups, revealPattern, waveStagger, maxRevealSpread);

            if(revealGroups.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            sequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetTarget(this);

            for(int i = 0; i < revealGroups.Count; i++)
            {
                CellRevealGroup group = revealGroups[i];
                float delay = Mathf.Max(0f, group.WaveDelay + UnityEngine.Random.Range(-waveJitter, waveJitter));
                float itemStartTime = delay + itemDelay;

                group.SquareTransform.localScale = group.SquareTargetScale * cellStartScale;
                BoardRevealGroups.SetAlpha(group.SquareRenderer, 0f);
                sequence.Insert(
                    delay,
                    group.SquareTransform
                        .DOScale(group.SquareTargetScale * cellPeakScale, cellRiseDuration)
                        .SetEase(riseEase)
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(this));
                if(group.SquareRenderer != null)
                {
                    sequence.Insert(
                        delay,
                        group.SquareRenderer
                            .DOFade(group.SquareTargetAlpha, cellRiseDuration * 0.78f)
                            .SetEase(Ease.OutSine)
                            .SetUpdate(useUnscaledTime)
                            .SetTarget(this));
                }
                sequence.Insert(
                    delay + cellRiseDuration,
                    group.SquareTransform
                        .DOScale(group.SquareTargetScale, cellSettleDuration)
                        .SetEase(settleEase)
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(this));

                if(group.ItemTransform == null)
                    continue;

                group.ItemTransform.localScale = group.ItemTargetScale * itemStartScale;
                group.ItemTransform.gameObject.SetActive(true);
                BoardRevealGroups.SetAlpha(group.ItemRenderer, 0f);
                sequence.Insert(
                    itemStartTime,
                    group.ItemTransform
                        .DOScale(group.ItemTargetScale * itemPeakScale, itemRiseDuration)
                        .SetEase(riseEase)
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(this));
                if(group.ItemRenderer != null)
                {
                    sequence.Insert(
                        itemStartTime,
                        group.ItemRenderer
                            .DOFade(group.ItemTargetAlpha, itemRiseDuration * 0.82f)
                            .SetEase(Ease.OutSine)
                            .SetUpdate(useUnscaledTime)
                            .SetTarget(this));
                }
                sequence.Insert(
                    itemStartTime + itemRiseDuration,
                    group.ItemTransform
                        .DOScale(group.ItemTargetScale, itemSettleDuration)
                        .SetEase(settleEase)
                        .SetUpdate(useUnscaledTime)
                        .SetTarget(this));
            }

            sequence.OnComplete(() =>
            {
                sequence = null;
                BoardRevealGroups.Restore(revealGroups);
                onComplete?.Invoke();
            });
        }

        public void CompleteImmediately(Vector3 targetLocalPosition, Action onComplete)
        {
            KillTween();
            transform.localPosition = targetLocalPosition;
            BoardRevealGroups.Restore(revealGroups);
            onComplete?.Invoke();
        }

        private void KillTween()
        {
            if(sequence != null && sequence.IsActive())
                sequence.Kill();

            sequence = null;
            BoardRevealGroups.Restore(revealGroups);
            revealGroups.Clear();
        }
    }
}
