using JuiceFresh;
using JuiceFresh.Feel;
using Lofelt.NiceVibrations;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

/// <summary>
/// Turns game events (selection, match, combo, booster, win/lose) into soft feedback.
/// Holds the tuning (inspector or preset); the feedback itself lives in BoardFeedback, ItemPulseFeedback,
/// SelectionGlowFeedback and CameraShakeFeedback.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-500)]
public sealed class GameFeelManager : MonoBehaviour, IGameFeelTuning
{
    public static GameFeelManager Instance { get; private set; }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private bool persistAcrossScenes;

    [Header("Feel Preset")]
    [SerializeField] private GameFeelPresetType feelPreset = GameFeelPresetType.CozyJar;
    [SerializeField, HideInInspector] private GameFeelPresetType lastFeelPreset = (GameFeelPresetType)(-1);

    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private bool enableBoardShake = true;
    [SerializeField] private bool enableFreezeFrames;
    [SerializeField] private bool enableItemPunch = true;

    [Header("Soft Feel Intensity")]
    [SerializeField, Range(0.25f, 1.5f)] private float globalIntensity = 0.9f;
    [SerializeField, Range(0f, 0.3f)] private float matchShakeStrength = 0.045f;
    [SerializeField, Range(0f, 0.3f)] private float boardShakeStrength = 0.06f;
    [SerializeField, Range(0f, 0.3f)] private float comboShakeStrength = 0.085f;
    [SerializeField, Range(0f, 0.4f)] private float winShakeStrength = 0.14f;
    [SerializeField, Range(0f, 0.2f)] private float itemPulseStrength = 0.095f;
    [SerializeField, Range(0f, 0.08f)] private float boardPulseStrength = 0.028f;

    [Header("Selection Glow")]
    [SerializeField, Range(0f, 0.6f)] private float selectionGlowAlpha = 0.26f;
    [SerializeField, Range(1f, 1.5f)] private float selectionGlowScale = 1.12f;
    [SerializeField, Range(0f, 0.1f)] private float selectionGlowBreathing = 0.035f;
    [SerializeField] private Color selectionGlowColorStart = new Color(0.86f, 0.96f, 1f, 0.26f);
    [SerializeField] private Color selectionGlowColorEnd = new Color(1f, 0.88f, 0.55f, 0.34f);
    [SerializeField, Range(0.5f, 2.5f)] private float motionDurationScale = 1f;
    [SerializeField, Range(0.3f, 2f)] private float itemPulseDurationScale = 1f;
    [SerializeField, Range(1f, 20f)] private float cameraShakeFrequency = 10f;
    [SerializeField, Range(1f, 2.5f)] private float glowReleasePopScale = 1.48f;
    [SerializeField, Range(1f, 10f)] private float selectionGlowWaveSpeed = 5.5f;
    [SerializeField, Range(0f, 2f)] private float matchResponseMultiplier = 1f;
    [SerializeField, Range(4, 20)] private int comboTriggerAt = 6;
    [SerializeField] private bool enableSelectionGlow = true;
    [SerializeField] private bool enableBoardPulseOnMatch = true;
    [SerializeField] private BoardMotionStyle boardMotionStyle = BoardMotionStyle.Flutter;

    [Header("Behavior Signature")]
    [SerializeField] private ItemMotionStyle itemMotionStyle = ItemMotionStyle.SoftPop;
    [SerializeField] private SelectionAuraStyle selectionAuraStyle = SelectionAuraStyle.Breathe;
    [SerializeField] private GlowReleaseStyle glowReleaseStyle = GlowReleaseStyle.Bloom;
    [SerializeField] private ChainRhythmStyle chainRhythmStyle = ChainRhythmStyle.Milestones;

    private BoardFeedback _board;
    private ItemPulseFeedback _itemPulse;
    private SelectionGlowFeedback _selectionGlow;
    private CameraShakeFeedback _cameraShake;

    #region IGameFeelTuning

    float IGameFeelTuning.MotionDurationScale => motionDurationScale;
    bool IGameFeelTuning.EnableBoardShake => enableBoardShake;
    BoardMotionStyle IGameFeelTuning.BoardMotionStyle => boardMotionStyle;
    ItemMotionStyle IGameFeelTuning.ItemMotionStyle => itemMotionStyle;
    float IGameFeelTuning.ItemPulseDurationScale => itemPulseDurationScale;
    float IGameFeelTuning.SelectionGlowAlpha => selectionGlowAlpha;
    float IGameFeelTuning.SelectionGlowScale => selectionGlowScale;
    float IGameFeelTuning.SelectionGlowBreathing => selectionGlowBreathing;
    float IGameFeelTuning.SelectionGlowWaveSpeed => selectionGlowWaveSpeed;
    Color IGameFeelTuning.SelectionGlowColorStart => selectionGlowColorStart;
    Color IGameFeelTuning.SelectionGlowColorEnd => selectionGlowColorEnd;
    SelectionAuraStyle IGameFeelTuning.SelectionAuraStyle => selectionAuraStyle;
    GlowReleaseStyle IGameFeelTuning.GlowReleaseStyle => glowReleaseStyle;
    float IGameFeelTuning.GlowReleasePopScale => glowReleasePopScale;

    #endregion

    // Created on first use: BindCamera/BindBoard can run before Awake on an object added at runtime.
    private BoardFeedback Board => _board ??= new BoardFeedback(this, this);
    private ItemPulseFeedback ItemPulse => _itemPulse ??= new ItemPulseFeedback(this, this);
    private SelectionGlowFeedback SelectionGlow => _selectionGlow ??= new SelectionGlowFeedback(this, this);
    private CameraShakeFeedback CameraShake => _cameraShake ??= new CameraShakeFeedback(transform);

    public GameFeelPresetType ActivePreset => feelPreset;

    #region Lifecycle

    /// <summary>Creates a persistent manager if the scene has none and binds it to the level's camera and board.</summary>
    public static void EnsureInitialized(Camera gameCamera, Transform board = null)
    {
        if (Instance != null)
        {
            Instance.BindCamera(gameCamera);
            Instance.BindBoard(board);
            return;
        }

        GameObject host = new GameObject("GameFeelManager");
        GameFeelManager manager = host.AddComponent<GameFeelManager>();
        manager.persistAcrossScenes = true;
        manager.BindCamera(gameCamera);
        manager.BindBoard(board);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        if (targetCamera != null)
            BindCamera(targetCamera);

        if (boardRoot != null)
            BindBoard(boardRoot);

        ApplyFeelPresetIfNeeded(force: true);
        EnsureFeelInfrastructure();
        BuildCameraShakes();
    }

    private void OnValidate()
    {
        ApplyFeelPresetIfNeeded(force: false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreAnimatedTransforms();
    }

    private void OnDestroy()
    {
        _cameraShake?.DestroyPlayers();
        RestoreAnimatedTransforms();

        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Binding

    public void BindCamera(Camera gameCamera)
    {
        if (gameCamera == null)
            return;

        targetCamera = gameCamera;
        CameraShakeFeedback.PrepareCamera(gameCamera);
    }

    public void BindBoard(Transform board)
    {
        if (board == null)
            return;

        Board.Bind(board);
        boardRoot = Board.Board;
    }

    /// <summary>Re-captures the board's resting transform after intro tweens or layout changes.</summary>
    public void RefreshBoardCanonicalTransform()
    {
        Board.RefreshRest();
    }

    #endregion

    #region Presets

    public void SetFeelPreset(GameFeelPresetType preset)
    {
        feelPreset = preset;
        ApplyFeelPresetIfNeeded(force: true);

        if (Application.isPlaying)
            BuildCameraShakes();
    }

    private void ApplyFeelPresetIfNeeded(bool force)
    {
        if (feelPreset == GameFeelPresetType.Custom)
        {
            lastFeelPreset = feelPreset;
            return;
        }

        if (!force && feelPreset == lastFeelPreset)
            return;

        lastFeelPreset = feelPreset;
        ApplyFeelPreset(GameFeelPresetLibrary.Get(feelPreset));
    }

    private void ApplyFeelPreset(GameFeelPresetData preset)
    {
        enableHaptics = preset.enableHaptics;
        enableCameraShake = preset.enableCameraShake;
        enableBoardShake = preset.enableBoardShake;
        enableFreezeFrames = preset.enableFreezeFrames;
        enableItemPunch = preset.enableItemPunch;
        globalIntensity = preset.globalIntensity;
        matchShakeStrength = preset.matchShakeStrength;
        boardShakeStrength = preset.boardShakeStrength;
        comboShakeStrength = preset.comboShakeStrength;
        winShakeStrength = preset.winShakeStrength;
        itemPulseStrength = preset.itemPulseStrength;
        boardPulseStrength = preset.boardPulseStrength;
        selectionGlowAlpha = preset.selectionGlowAlpha;
        selectionGlowScale = preset.selectionGlowScale;
        selectionGlowBreathing = preset.selectionGlowBreathing;
        selectionGlowColorStart = preset.glowColorStart;
        selectionGlowColorEnd = preset.glowColorEnd;
        motionDurationScale = preset.motionDurationScale;
        itemPulseDurationScale = preset.itemPulseDurationScale;
        cameraShakeFrequency = preset.cameraShakeFrequency;
        glowReleasePopScale = preset.glowReleasePopScale;
        selectionGlowWaveSpeed = preset.selectionGlowWaveSpeed;
        matchResponseMultiplier = preset.matchResponseMultiplier;
        comboTriggerAt = preset.comboTriggerAt;
        enableSelectionGlow = preset.enableSelectionGlow;
        enableBoardPulseOnMatch = preset.enableBoardPulseOnMatch;
        boardMotionStyle = preset.boardMotionStyle;
        itemMotionStyle = preset.itemMotionStyle;
        selectionAuraStyle = preset.selectionAuraStyle;
        glowReleaseStyle = preset.glowReleaseStyle;
        chainRhythmStyle = preset.chainRhythmStyle;
    }

    private void EnsureFeelInfrastructure()
    {
        CameraShakeFeedback.EnsureTimeManager();

        Camera camera = targetCamera != null ? targetCamera : Camera.main;
        if (camera != null)
            CameraShakeFeedback.PrepareCamera(camera);
    }

    private void BuildCameraShakes()
    {
        CameraShake.Build(
            matchShakeStrength * globalIntensity,
            comboShakeStrength * globalIntensity,
            winShakeStrength * globalIntensity,
            cameraShakeFrequency);
    }

    #endregion

    #region Game Events

    public void OnItemSelected(Transform item, int chainCount)
    {
        if (item == null)
            return;

        if (ItemPunchEnabled)
            ItemPulse.Play(item, SelectionPulse(chainCount));

        if (enableSelectionGlow)
            SelectionGlow.Add(item, chainCount);

        if (IsChainMilestone(chainCount))
        {
            float milestoneStrength = boardPulseStrength * globalIntensity * (0.45f + chainCount * 0.035f);
            Board.TriggerPulse(0.18f, milestoneStrength);
            PlayHaptic(chainCount >= 8
                ? HapticPatterns.PresetType.SoftImpact
                : HapticPatterns.PresetType.LightImpact);
        }
        else
        {
            PlayHaptic(HapticPatterns.PresetType.Selection);
        }
    }

    public void OnItemDeselected(Transform removedItem, Transform remainingItem, int chainCount)
    {
        SelectionGlow.Remove(removedItem, false);

        if (ItemPunchEnabled && remainingItem != null)
        {
            float pulse = itemPulseStrength * globalIntensity * (0.48f + Mathf.Min(chainCount, 8) * 0.015f);
            ItemPulse.Play(remainingItem, pulse);
        }

        PlayHaptic(HapticPatterns.PresetType.Selection);
    }

    public void OnItemDestroyed(Vector3 position)
    {
        // Per-item camera shake was too weak and got lost in chain pops.
    }

    public void OnMatchReleased(int matchedCount, Vector3 centerPosition)
    {
        ItemPulse.CompleteAll();
        SelectionGlow.ReleaseAll(true);

        float normalized = Mathf.Clamp01((matchedCount - 3f) / 7f);
        float response = matchResponseMultiplier;

        Board.TriggerMotion(
            0.2f + normalized * 0.08f,
            boardShakeStrength * globalIntensity * (0.45f + normalized * 0.35f) * response);

        if (enableBoardPulseOnMatch && !Board.UsesScaleMotion)
        {
            Board.TryTriggerPulse(
                0.24f,
                boardPulseStrength * globalIntensity * (0.75f + normalized * 0.5f) * response);
        }

        if (enableCameraShake)
            CameraShake.PlayMatch(centerPosition, 0.35f + normalized * 0.25f);

        PlayHaptic(matchedCount >= 6
            ? HapticPatterns.PresetType.SoftImpact
            : HapticPatterns.PresetType.LightImpact);
    }

    public void OnCombo(int combo)
    {
        if (combo < comboTriggerAt)
            return;

        float normalized = Mathf.Clamp01((combo - comboTriggerAt) / 8f);

        Board.TriggerMotion(0.26f + normalized * 0.08f, boardShakeStrength * globalIntensity * 0.7f);

        if (enableBoardPulseOnMatch)
            Board.TryTriggerPulse(0.28f, boardPulseStrength * globalIntensity * (1f + normalized * 0.45f));

        if (enableCameraShake)
            CameraShake.PlayCombo(Vector3.zero, 0.5f + normalized * 0.25f);

        if (enableFreezeFrames && combo >= 12)
            MMFreezeFrameEvent.Trigger(0.012f);

        PlayHaptic(HapticPatterns.PresetType.SoftImpact);
    }

    public void OnBoostExplosion(Vector3 position)
    {
        Board.TriggerMotion(0.32f, boardShakeStrength * globalIntensity * 1.15f);
        Board.TryTriggerPulse(0.3f, boardPulseStrength * globalIntensity * 1.35f);

        if (enableCameraShake)
            CameraShake.PlayCombo(position, 0.9f);

        if (enableFreezeFrames)
            MMFreezeFrameEvent.Trigger(0.018f);

        PlayHaptic(HapticPatterns.PresetType.MediumImpact);
    }

    public void OnInvalidMove()
    {
        ItemPulse.CompleteAll();
        SelectionGlow.ReleaseAll(false);
        Board.TriggerMotion(0.16f, boardShakeStrength * globalIntensity * 0.32f);
        PlayHaptic(HapticPatterns.PresetType.Selection);
    }

    public void OnPreWin()
    {
        Board.TriggerMotion(0.3f, boardShakeStrength * globalIntensity * 0.55f);
        Board.TryTriggerPulse(0.34f, boardPulseStrength * globalIntensity * 1.1f);
        PlayHaptic(HapticPatterns.PresetType.SoftImpact);
    }

    public void OnWin()
    {
        Board.TriggerMotion(0.42f, boardShakeStrength * globalIntensity * 0.8f);
        Board.TryTriggerPulse(0.45f, boardPulseStrength * globalIntensity * 1.4f);

        if (enableCameraShake)
            CameraShake.PlayWin(0.8f);

        PlayHaptic(HapticPatterns.PresetType.Success);
    }

    public void OnLose()
    {
        Board.TriggerMotion(0.34f, boardShakeStrength * globalIntensity * 0.45f);
        PlayHaptic(HapticPatterns.PresetType.Warning);
    }

    public void OnTargetCollected()
    {
        PlayHaptic(HapticPatterns.PresetType.Selection);
    }

    #endregion

    #region Chain Rhythm

    private bool ItemPunchEnabled => enableItemPunch && itemMotionStyle != ItemMotionStyle.None;

    private float SelectionPulse(int chainCount)
    {
        float chainEnergy = Mathf.InverseLerp(1f, 10f, chainCount);
        float basePulse = itemPulseStrength * globalIntensity;

        switch (chainRhythmStyle)
        {
            case ChainRhythmStyle.Quiet:
                return basePulse * 0.65f;
            case ChainRhythmStyle.EveryThird:
                return basePulse * (chainCount % 3 == 0 ? 1.45f : 0.82f);
            case ChainRhythmStyle.Rising:
                return basePulse * Mathf.Lerp(0.7f, 1.65f, chainEnergy);
            default: // Milestones
                return basePulse * (1f + chainEnergy * 0.35f);
        }
    }

    private bool IsChainMilestone(int chainCount)
    {
        switch (chainRhythmStyle)
        {
            case ChainRhythmStyle.Quiet:
                return false;
            case ChainRhythmStyle.EveryThird:
                return chainCount >= 3 && chainCount % 3 == 0;
            case ChainRhythmStyle.Rising:
                return chainCount >= 4 && chainCount % 2 == 0;
            default: // Milestones
                return chainCount == 3 || chainCount == 5 || chainCount == 8;
        }
    }

    #endregion

    private void PlayHaptic(HapticPatterns.PresetType preset)
    {
        if (!enableHaptics || preset == HapticPatterns.PresetType.None)
            return;

        HapticPatterns.PlayPreset(preset);
    }

    private void RestoreAnimatedTransforms()
    {
        _itemPulse?.CompleteAll();
        _selectionGlow?.DestroyAll();
        _board?.Stop();
    }
}
