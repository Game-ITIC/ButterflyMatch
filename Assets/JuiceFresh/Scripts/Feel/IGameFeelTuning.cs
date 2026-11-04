using UnityEngine;

namespace JuiceFresh.Feel
{
    /// <summary>
    /// The live tuning values of GameFeelManager (set by the inspector or a preset) that the feedback parts read.
    /// </summary>
    public interface IGameFeelTuning
    {
        float MotionDurationScale { get; }

        bool EnableBoardShake { get; }
        BoardMotionStyle BoardMotionStyle { get; }

        ItemMotionStyle ItemMotionStyle { get; }
        float ItemPulseDurationScale { get; }

        float SelectionGlowAlpha { get; }
        float SelectionGlowScale { get; }
        float SelectionGlowBreathing { get; }
        float SelectionGlowWaveSpeed { get; }
        Color SelectionGlowColorStart { get; }
        Color SelectionGlowColorEnd { get; }
        SelectionAuraStyle SelectionAuraStyle { get; }
        GlowReleaseStyle GlowReleaseStyle { get; }
        float GlowReleasePopScale { get; }
    }
}
