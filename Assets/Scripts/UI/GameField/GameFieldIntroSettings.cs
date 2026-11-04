using DG.Tweening;

namespace UI
{
    /// <summary>The tuning values a board intro preset writes into GameFieldTweenAnimation's inspector fields.</summary>
    internal readonly struct GameFieldIntroSettings
    {
        public readonly float CellStartScale;
        public readonly float CellPeakScale;
        public readonly float CellRiseDuration;
        public readonly float CellSettleDuration;
        public readonly float ItemStartScale;
        public readonly float ItemPeakScale;
        public readonly float ItemDelay;
        public readonly float ItemRiseDuration;
        public readonly float ItemSettleDuration;
        public readonly float WaveStagger;
        public readonly float WaveJitter;
        public readonly float MaxRevealSpread;
        public readonly BoardRevealPattern RevealPattern;
        public readonly Ease RiseEase;
        public readonly Ease SettleEase;

        private GameFieldIntroSettings(
            float cellStartScale, float cellPeakScale, float cellRiseDuration, float cellSettleDuration,
            float itemStartScale, float itemPeakScale, float itemDelay, float itemRiseDuration, float itemSettleDuration,
            float waveStagger, float waveJitter, float maxRevealSpread,
            BoardRevealPattern revealPattern, Ease riseEase, Ease settleEase)
        {
            CellStartScale = cellStartScale;
            CellPeakScale = cellPeakScale;
            CellRiseDuration = cellRiseDuration;
            CellSettleDuration = cellSettleDuration;
            ItemStartScale = itemStartScale;
            ItemPeakScale = itemPeakScale;
            ItemDelay = itemDelay;
            ItemRiseDuration = itemRiseDuration;
            ItemSettleDuration = itemSettleDuration;
            WaveStagger = waveStagger;
            WaveJitter = waveJitter;
            MaxRevealSpread = maxRevealSpread;
            RevealPattern = revealPattern;
            RiseEase = riseEase;
            SettleEase = settleEase;
        }

        /// <summary>Values of a preset; Custom and unknown presets get SoftBloom (callers skip Custom).</summary>
        public static GameFieldIntroSettings For(GameFieldIntroPreset preset)
        {
            switch(preset)
            {
                case GameFieldIntroPreset.CenterRipple:
                    return new GameFieldIntroSettings(
                        0.08f, 1.08f, 0.34f, 0.18f,
                        0.04f, 1.12f, 0.1f, 0.3f, 0.16f,
                        0.075f, 0.004f, 1.15f,
                        BoardRevealPattern.CenterOut, Ease.OutBack, Ease.InOutSine);
                case GameFieldIntroPreset.FallingRows:
                    return new GameFieldIntroSettings(
                        0.55f, 1.02f, 0.24f, 0.08f,
                        0.42f, 1.04f, 0.025f, 0.2f, 0.07f,
                        0.045f, 0.002f, 0.75f,
                        BoardRevealPattern.TopToBottom, Ease.OutQuad, Ease.OutSine);
                case GameFieldIntroPreset.PlayfulScatter:
                    return new GameFieldIntroSettings(
                        0.04f, 1.1f, 0.4f, 0.24f,
                        0.02f, 1.16f, 0.08f, 0.36f, 0.22f,
                        0.065f, 0.025f, 0.8f,
                        BoardRevealPattern.RandomPop, Ease.OutElastic, Ease.InOutSine);
                case GameFieldIntroPreset.SoftBloom:
                default:
                    return new GameFieldIntroSettings(
                        0.32f, 1.045f, 0.48f, 0.14f,
                        0.2f, 1.08f, 0.13f, 0.42f, 0.16f,
                        0.08f, 0.006f, 1f,
                        BoardRevealPattern.CenterOut, Ease.OutCubic, Ease.InOutSine);
            }
        }
    }
}
