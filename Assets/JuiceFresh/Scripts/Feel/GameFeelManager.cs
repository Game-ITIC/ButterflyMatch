using System.Collections;
using System.Collections.Generic;
using JuiceFresh;
using Lofelt.NiceVibrations;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

/// <summary>
/// Central hub for soft Feel feedback: gentle pulses, flutter motion, restrained camera shake and haptics.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-500)]
public sealed class GameFeelManager : MonoBehaviour
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

    private MMF_Player _matchPlayer;
    private MMF_Player _comboPlayer;
    private MMF_Player _winPlayer;
    private Camera _gameCamera;
    private readonly Dictionary<int, ItemPulseState> _itemPulses = new Dictionary<int, ItemPulseState>();
    private readonly Dictionary<int, SelectionGlowState> _selectionGlows = new Dictionary<int, SelectionGlowState>();
    private readonly List<GameObject> _releasedGlowObjects = new List<GameObject>();
    private Coroutine _boardMotionCoroutine;
    private Coroutine _boardPulseCoroutine;
    private Vector3 _boardCanonicalLocalPosition;
    private Vector3 _boardCanonicalLocalScale;
    private bool _boardCanonicalCaptured;

    public static void EnsureInitialized(Camera gameCamera, Transform board = null)
    {
        if(Instance != null)
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
        if(Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if(persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        if(targetCamera != null)
        {
            BindCamera(targetCamera);
        }

        if(boardRoot != null)
        {
            BindBoard(boardRoot);
        }

        ApplyFeelPresetIfNeeded(force: true);
        EnsureFeelInfrastructure();
        BuildFeedbackPlayers();
    }

    private void OnValidate()
    {
        ApplyFeelPresetIfNeeded(force: false);
    }

    public GameFeelPresetType ActivePreset => feelPreset;

    public void SetFeelPreset(GameFeelPresetType preset)
    {
        feelPreset = preset;
        ApplyFeelPresetIfNeeded(force: true);

        if(Application.isPlaying)
        {
            BuildFeedbackPlayers();
        }
    }

    private void ApplyFeelPresetIfNeeded(bool force)
    {
        if(feelPreset == GameFeelPresetType.Custom)
        {
            lastFeelPreset = feelPreset;
            return;
        }

        if(!force && feelPreset == lastFeelPreset)
        {
            return;
        }

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

    private void OnDestroy()
    {
        DestroyFeedbackPlayers();
        RestoreAnimatedTransforms();

        if(Instance == this)
        {
            Instance = null;
        }
    }

    public void BindCamera(Camera gameCamera)
    {
        if(gameCamera == null)
        {
            return;
        }

        _gameCamera = gameCamera;
        targetCamera = gameCamera;
        EnsureCameraShaker(gameCamera);
    }

    public void BindBoard(Transform board)
    {
        if(board == null || boardRoot == board)
        {
            return;
        }

        RestoreBoardTransform();
        boardRoot = board;

        CaptureBoardCanonicalTransform();
    }

    /// <summary>
    /// Re-capture the board's resting transform after intro tweens or layout changes.
    /// </summary>
    public void RefreshBoardCanonicalTransform()
    {
        StopBoardAnimations();
        CaptureBoardCanonicalTransform();
    }

    private void CaptureBoardCanonicalTransform()
    {
        if(boardRoot == null)
        {
            _boardCanonicalCaptured = false;
            return;
        }

        _boardCanonicalLocalPosition = boardRoot.localPosition;
        _boardCanonicalLocalScale = boardRoot.localScale;
        _boardCanonicalCaptured = true;
    }

    private void StopBoardAnimations()
    {
        StopBoardMotion();
        StopBoardPulse();
    }

    private void StopBoardMotion()
    {
        if(_boardMotionCoroutine != null)
        {
            StopCoroutine(_boardMotionCoroutine);
            _boardMotionCoroutine = null;
        }

        if(boardRoot != null && _boardCanonicalCaptured)
        {
            boardRoot.localPosition = _boardCanonicalLocalPosition;
        }
    }

    private void StopBoardPulse()
    {
        if(_boardPulseCoroutine != null)
        {
            StopCoroutine(_boardPulseCoroutine);
            _boardPulseCoroutine = null;
        }

        if(boardRoot != null && _boardCanonicalCaptured)
        {
            boardRoot.localScale = _boardCanonicalLocalScale;
        }
    }

    private static bool UsesScaleMotion(BoardMotionStyle style)
    {
        return style == BoardMotionStyle.ElasticBounce;
    }

    public void OnItemSelected(Transform item, int chainCount)
    {
        if(item == null)
        {
            return;
        }

        if(enableItemPunch && itemMotionStyle != ItemMotionStyle.None)
        {
            float pulse = CalculateSelectionPulse(chainCount);
            PlayItemPulse(item, pulse);
        }

        if(enableSelectionGlow)
        {
            AddSelectionGlow(item, chainCount);
        }

        bool milestone = ShouldCelebrateChainStep(chainCount);
        if(milestone)
        {
            float milestoneStrength = boardPulseStrength * globalIntensity * (0.45f + chainCount * 0.035f);
            TriggerBoardPulse(0.18f, milestoneStrength);
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
        RemoveSelectionGlow(removedItem, false);

        if(enableItemPunch && itemMotionStyle != ItemMotionStyle.None && remainingItem != null)
        {
            float pulse = itemPulseStrength * globalIntensity * (0.48f + Mathf.Min(chainCount, 8) * 0.015f);
            PlayItemPulse(remainingItem, pulse);
        }

        PlayHaptic(HapticPatterns.PresetType.Selection);
    }

    private float CalculateSelectionPulse(int chainCount)
    {
        float chainEnergy = Mathf.InverseLerp(1f, 10f, chainCount);

        switch(chainRhythmStyle)
        {
            case ChainRhythmStyle.Quiet:
                return itemPulseStrength * globalIntensity * 0.65f;
            case ChainRhythmStyle.EveryThird:
                return itemPulseStrength * globalIntensity * (chainCount % 3 == 0 ? 1.45f : 0.82f);
            case ChainRhythmStyle.Rising:
                return itemPulseStrength * globalIntensity * Mathf.Lerp(0.7f, 1.65f, chainEnergy);
            case ChainRhythmStyle.Milestones:
            default:
                return itemPulseStrength * globalIntensity * (1f + chainEnergy * 0.35f);
        }
    }

    private bool ShouldCelebrateChainStep(int chainCount)
    {
        switch(chainRhythmStyle)
        {
            case ChainRhythmStyle.Quiet:
                return false;
            case ChainRhythmStyle.EveryThird:
                return chainCount >= 3 && chainCount % 3 == 0;
            case ChainRhythmStyle.Rising:
                return chainCount >= 4 && chainCount % 2 == 0;
            case ChainRhythmStyle.Milestones:
            default:
                return chainCount == 3 || chainCount == 5 || chainCount == 8;
        }
    }

    public void OnItemDestroyed(Vector3 position)
    {
        // Per-item camera shake was too weak and got lost in chain pops.
    }

    public void OnMatchReleased(int matchedCount, Vector3 centerPosition)
    {
        CompleteAllItemPulses();
        ReleaseAllSelectionGlows(true);
        float normalized = Mathf.Clamp01((matchedCount - 3f) / 7f);
        float response = matchResponseMultiplier;
        bool scaleMotion = UsesScaleMotion(boardMotionStyle);

        TriggerBoardMotion(
            0.2f + normalized * 0.08f,
            boardShakeStrength * globalIntensity * (0.45f + normalized * 0.35f) * response);

        if(enableBoardPulseOnMatch && !scaleMotion)
        {
            TryTriggerBoardPulse(
                0.24f,
                boardPulseStrength * globalIntensity * (0.75f + normalized * 0.5f) * response);
        }

        if(enableCameraShake)
        {
            _matchPlayer?.PlayFeedbacks(centerPosition, 0.35f + normalized * 0.25f);
        }

        PlayHaptic(matchedCount >= 6
            ? HapticPatterns.PresetType.SoftImpact
            : HapticPatterns.PresetType.LightImpact);
    }

    public void OnCombo(int combo)
    {
        if(combo < comboTriggerAt)
        {
            return;
        }

        float normalized = Mathf.Clamp01((combo - comboTriggerAt) / 8f);

        TriggerBoardMotion(0.26f + normalized * 0.08f, boardShakeStrength * globalIntensity * 0.7f);
        if(enableBoardPulseOnMatch)
        {
            TryTriggerBoardPulse(0.28f, boardPulseStrength * globalIntensity * (1f + normalized * 0.45f));
        }
        if(enableCameraShake)
        {
            _comboPlayer?.PlayFeedbacks(Vector3.zero, 0.5f + normalized * 0.25f);
        }

        if(enableFreezeFrames && combo >= 12)
        {
            MMFreezeFrameEvent.Trigger(0.012f);
        }

        PlayHaptic(HapticPatterns.PresetType.SoftImpact);
    }

    public void OnBoostExplosion(Vector3 position)
    {
        TriggerBoardMotion(0.32f, boardShakeStrength * globalIntensity * 1.15f);
        TryTriggerBoardPulse(0.3f, boardPulseStrength * globalIntensity * 1.35f);
        if(enableCameraShake)
        {
            _comboPlayer?.PlayFeedbacks(position, 0.9f);
        }

        if(enableFreezeFrames)
        {
            MMFreezeFrameEvent.Trigger(0.018f);
        }

        PlayHaptic(HapticPatterns.PresetType.MediumImpact);
    }

    public void OnInvalidMove()
    {
        CompleteAllItemPulses();
        ReleaseAllSelectionGlows(false);
        TriggerBoardMotion(0.16f, boardShakeStrength * globalIntensity * 0.32f);
        PlayHaptic(HapticPatterns.PresetType.Selection);
    }

    public void OnPreWin()
    {
        TriggerBoardMotion(0.3f, boardShakeStrength * globalIntensity * 0.55f);
        TryTriggerBoardPulse(0.34f, boardPulseStrength * globalIntensity * 1.1f);
        PlayHaptic(HapticPatterns.PresetType.SoftImpact);
    }

    public void OnWin()
    {
        TriggerBoardMotion(0.42f, boardShakeStrength * globalIntensity * 0.8f);
        TryTriggerBoardPulse(0.45f, boardPulseStrength * globalIntensity * 1.4f);
        if(enableCameraShake)
        {
            _winPlayer?.PlayFeedbacks(Vector3.zero, 0.8f);
        }
        PlayHaptic(HapticPatterns.PresetType.Success);
    }

    public void OnLose()
    {
        TriggerBoardMotion(0.34f, boardShakeStrength * globalIntensity * 0.45f);
        PlayHaptic(HapticPatterns.PresetType.Warning);
    }

    public void OnTargetCollected()
    {
        PlayHaptic(HapticPatterns.PresetType.Selection);
    }

    private void EnsureFeelInfrastructure()
    {
        if(MMTimeManager.Instance == null)
        {
            GameObject timeManager = new GameObject("MMTimeManager");
            timeManager.AddComponent<MMTimeManager>();
            DontDestroyOnLoad(timeManager);
        }

        Camera camera = _gameCamera != null ? _gameCamera : Camera.main;
        if(camera != null)
        {
            EnsureCameraShaker(camera);
        }
    }

    private static void EnsureCameraShaker(Camera camera)
    {
        MMCameraShaker shaker = camera.GetComponent<MMCameraShaker>();
        if(shaker == null)
        {
            if(camera.GetComponent<MMWiggle>() == null)
            {
                camera.gameObject.AddComponent<MMWiggle>();
            }

            shaker = camera.gameObject.AddComponent<MMCameraShaker>();
        }

        shaker.CooldownBetweenShakes = 0f;
    }

    private void BuildFeedbackPlayers()
    {
        if(!Application.isPlaying)
        {
            return;
        }

        DestroyFeedbackPlayers();

        _matchPlayer = CreateShakePlayer("Feel_Match", 0.2f, matchShakeStrength * globalIntensity, cameraShakeFrequency);
        _comboPlayer = CreateShakePlayer("Feel_Combo", 0.26f, comboShakeStrength * globalIntensity, cameraShakeFrequency * 0.85f);
        _winPlayer = CreateShakePlayer("Feel_Win", 0.38f, winShakeStrength * globalIntensity, cameraShakeFrequency * 0.7f);
    }

    private void DestroyFeedbackPlayers()
    {
        DestroyPlayer(ref _matchPlayer);
        DestroyPlayer(ref _comboPlayer);
        DestroyPlayer(ref _winPlayer);
    }

    private void DestroyPlayer(ref MMF_Player player)
    {
        if(player == null)
        {
            return;
        }

        if(Application.isPlaying)
        {
            Destroy(player.gameObject);
        }
        else
        {
            DestroyImmediate(player.gameObject);
        }

        player = null;
    }

    private MMF_Player CreateShakePlayer(string playerName, float duration, float amplitude, float frequency)
    {
        GameObject playerObject = new GameObject(playerName);
        playerObject.transform.SetParent(transform);
        MMF_Player player = playerObject.AddComponent<MMF_Player>();
        ConfigureCameraShake(player, duration, amplitude, frequency);
        player.Initialization();
        return player;
    }

    private static void ConfigureCameraShake(MMF_Player player, float duration, float amplitude, float frequency)
    {
        MMF_CameraShake shake = (MMF_CameraShake)player.AddFeedback(typeof(MMF_CameraShake));
        shake.CameraShakeProperties = new MMCameraShakeProperties(duration, amplitude, frequency);
    }

    private void TriggerBoardMotion(float duration, float magnitude)
    {
        if(!enableBoardShake || boardRoot == null || magnitude <= 0f || boardMotionStyle == BoardMotionStyle.None)
        {
            return;
        }

        duration *= motionDurationScale;
        StopBoardMotion();

        if(UsesScaleMotion(boardMotionStyle))
        {
            StopBoardPulse();
        }

        switch(boardMotionStyle)
        {
            case BoardMotionStyle.VerticalFloat:
                _boardMotionCoroutine = StartCoroutine(BoardVerticalFloatCoroutine(duration, magnitude));
                break;
            case BoardMotionStyle.GentleSway:
                _boardMotionCoroutine = StartCoroutine(BoardGentleSwayCoroutine(duration, magnitude));
                break;
            case BoardMotionStyle.ElasticBounce:
                _boardMotionCoroutine = StartCoroutine(BoardElasticBounceCoroutine(duration, magnitude));
                break;
            case BoardMotionStyle.SoftSink:
                _boardMotionCoroutine = StartCoroutine(BoardSoftSinkCoroutine(duration, magnitude));
                break;
            default:
                _boardMotionCoroutine = StartCoroutine(BoardFlutterCoroutine(duration, magnitude));
                break;
        }
    }

    private void TriggerBoardFlutter(float duration, float magnitude)
    {
        TriggerBoardMotion(duration, magnitude);
    }

    private IEnumerator BoardFlutterCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(boardRoot == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float x = Mathf.Sin(t * Mathf.PI * 3f) * magnitude * envelope;
            float y = Mathf.Sin(t * Mathf.PI * 2f) * magnitude * 0.22f * envelope;
            boardRoot.localPosition = _boardCanonicalLocalPosition + new Vector3(x, y, 0f);
            yield return null;
        }

        if(boardRoot != null)
        {
            boardRoot.localPosition = _boardCanonicalLocalPosition;
        }

        _boardMotionCoroutine = null;
    }

    private IEnumerator BoardVerticalFloatCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(boardRoot == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float y = Mathf.Sin(t * Mathf.PI * 1.5f) * magnitude * 1.8f * envelope;
            boardRoot.localPosition = _boardCanonicalLocalPosition + new Vector3(0f, y, 0f);
            yield return null;
        }

        if(boardRoot != null)
        {
            boardRoot.localPosition = _boardCanonicalLocalPosition;
        }

        _boardMotionCoroutine = null;
    }

    private IEnumerator BoardGentleSwayCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(boardRoot == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float x = Mathf.Sin(t * Mathf.PI * 1.2f) * magnitude * 2.4f * envelope;
            float y = Mathf.Sin(t * Mathf.PI * 0.8f) * magnitude * 0.15f * envelope;
            boardRoot.localPosition = _boardCanonicalLocalPosition + new Vector3(x, y, 0f);
            yield return null;
        }

        if(boardRoot != null)
        {
            boardRoot.localPosition = _boardCanonicalLocalPosition;
        }

        _boardMotionCoroutine = null;
    }

    private IEnumerator BoardElasticBounceCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(boardRoot == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float bounce = Mathf.Sin(t * Mathf.PI * 2.2f) * Mathf.Exp(-t * 3.5f);
            boardRoot.localScale = _boardCanonicalLocalScale * (1f + bounce * magnitude * 2.8f);
            yield return null;
        }

        if(boardRoot != null)
        {
            boardRoot.localScale = _boardCanonicalLocalScale;
        }

        _boardMotionCoroutine = null;
    }

    private IEnumerator BoardSoftSinkCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(boardRoot == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float sink = Mathf.Sin(t * Mathf.PI);
            float y = -sink * magnitude * 1.6f;
            boardRoot.localPosition = _boardCanonicalLocalPosition + new Vector3(0f, y, 0f);
            yield return null;
        }

        if(boardRoot != null)
        {
            boardRoot.localPosition = _boardCanonicalLocalPosition;
        }

        _boardMotionCoroutine = null;
    }

    private void TryTriggerBoardPulse(float duration, float strength)
    {
        if(UsesScaleMotion(boardMotionStyle))
        {
            return;
        }

        TriggerBoardPulse(duration, strength);
    }

    private void TriggerBoardPulse(float duration, float strength)
    {
        if(boardRoot == null || strength <= 0f)
        {
            return;
        }

        duration *= motionDurationScale;
        StopBoardPulse();
        _boardPulseCoroutine = StartCoroutine(BoardPulseCoroutine(duration, strength));
    }

    private IEnumerator BoardPulseCoroutine(float duration, float strength)
    {
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(boardRoot == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI) * strength;
            boardRoot.localScale = _boardCanonicalLocalScale * (1f + pulse);
            yield return null;
        }

        if(boardRoot != null)
        {
            boardRoot.localScale = _boardCanonicalLocalScale;
        }

        _boardPulseCoroutine = null;
    }

    private void AddSelectionGlow(Transform target, int chainCount)
    {
        if(target == null)
        {
            return;
        }

        int id = target.GetInstanceID();
        if(_selectionGlows.ContainsKey(id))
        {
            return;
        }

        Item item = target.GetComponent<Item>();
        SpriteRenderer source = item != null && item.sprRenderer != null
            ? item.sprRenderer
            : target.GetComponentInChildren<SpriteRenderer>();
        if(source == null || source.sprite == null)
        {
            return;
        }

        GameObject auraObject = new GameObject("Feel_SelectionGlow");
        auraObject.layer = source.gameObject.layer;
        auraObject.transform.SetParent(source.transform.parent, false);
        auraObject.transform.localPosition = source.transform.localPosition;
        auraObject.transform.localRotation = source.transform.localRotation;
        auraObject.transform.localScale = source.transform.localScale * selectionGlowScale;

        SpriteRenderer aura = auraObject.AddComponent<SpriteRenderer>();
        aura.sprite = source.sprite;
        aura.sharedMaterial = source.sharedMaterial;
        aura.sortingLayerID = source.sortingLayerID;
        aura.sortingOrder = source.sortingOrder + 2;
        aura.flipX = source.flipX;
        aura.flipY = source.flipY;

        float chainEnergy = Mathf.InverseLerp(1f, 10f, chainCount);
        Color glowStart = selectionGlowColorStart;
        glowStart.a = selectionGlowAlpha;
        Color glowEnd = selectionGlowColorEnd;
        glowEnd.a = selectionGlowAlpha + 0.08f;
        Color glowColor = Color.Lerp(glowStart, glowEnd, chainEnergy);
        aura.color = glowColor;

        SelectionGlowState state = new SelectionGlowState(
            target,
            source,
            auraObject,
            aura,
            source.transform.localScale,
            auraObject.transform.localRotation,
            glowColor);
        state.Coroutine = StartCoroutine(SelectionGlowCoroutine(state, chainCount));
        _selectionGlows[id] = state;
    }

    private IEnumerator SelectionGlowCoroutine(SelectionGlowState state, int chainCount)
    {
        float elapsed = chainCount * 0.17f;

        while(state.Target != null && state.Source != null && state.AuraObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float phase = elapsed * selectionGlowWaveSpeed;
            float wave;

            switch(selectionAuraStyle)
            {
                case SelectionAuraStyle.Still:
                    wave = 0.35f;
                    break;
                case SelectionAuraStyle.Heartbeat:
                    float heartbeat = Mathf.Max(0f, Mathf.Sin(phase));
                    wave = heartbeat * heartbeat * heartbeat;
                    break;
                case SelectionAuraStyle.SlowSpin:
                    wave = Mathf.Sin(phase * 0.45f) * 0.5f + 0.5f;
                    state.AuraObject.transform.localRotation = state.BaseRotation
                        * Quaternion.Euler(0f, 0f, elapsed * 18f);
                    break;
                case SelectionAuraStyle.Breathe:
                default:
                    wave = Mathf.Sin(phase) * 0.5f + 0.5f;
                    break;
            }

            float scale = selectionGlowScale + wave * selectionGlowBreathing;
            state.AuraObject.transform.localScale = state.RestScale * scale;
            state.Aura.sprite = state.Source.sprite;

            Color color = state.GlowColor;
            color.a *= 0.78f + wave * 0.22f;
            state.Aura.color = color;
            yield return null;
        }
    }

    private void RemoveSelectionGlow(Transform target, bool successfulRelease)
    {
        if(target == null)
        {
            return;
        }

        int id = target.GetInstanceID();
        if(!_selectionGlows.TryGetValue(id, out SelectionGlowState state))
        {
            return;
        }

        if(state.Coroutine != null)
        {
            StopCoroutine(state.Coroutine);
        }

        _selectionGlows.Remove(id);
        StartCoroutine(ReleaseSelectionGlowCoroutine(state, successfulRelease));
    }

    private void ReleaseAllSelectionGlows(bool successfulRelease)
    {
        if(_selectionGlows.Count == 0)
        {
            return;
        }

        List<SelectionGlowState> activeGlows = new List<SelectionGlowState>(_selectionGlows.Values);
        _selectionGlows.Clear();

        foreach(SelectionGlowState state in activeGlows)
        {
            if(state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }

            StartCoroutine(ReleaseSelectionGlowCoroutine(state, successfulRelease));
        }
    }

    private IEnumerator ReleaseSelectionGlowCoroutine(SelectionGlowState state, bool successfulRelease)
    {
        if(state.AuraObject == null || state.Aura == null)
        {
            yield break;
        }

        state.AuraObject.transform.SetParent(null, true);
        _releasedGlowObjects.Add(state.AuraObject);

        Vector3 startScale = state.AuraObject.transform.localScale;
        Vector3 endScale = startScale;
        Vector3 startPosition = state.AuraObject.transform.position;
        float startAlpha = state.Aura.color.a;
        float verticalTravel = 0f;
        float duration = 0.18f;

        if(successfulRelease)
        {
            switch(glowReleaseStyle)
            {
                case GlowReleaseStyle.Fade:
                    endScale = startScale * 0.96f;
                    duration = 0.24f;
                    break;
                case GlowReleaseStyle.FloatAway:
                    endScale = startScale * 1.08f;
                    verticalTravel = 0.38f;
                    duration = 0.42f;
                    break;
                case GlowReleaseStyle.Snap:
                    endScale = startScale * 0.12f;
                    verticalTravel = -0.06f;
                    duration = 0.14f;
                    break;
                case GlowReleaseStyle.Bloom:
                default:
                    endScale = startScale * glowReleasePopScale;
                    verticalTravel = 0.12f;
                    duration = 0.3f;
                    break;
            }
        }
        else
        {
            endScale = startScale * 0.82f;
        }

        duration *= motionDurationScale;
        float elapsed = 0f;

        while(elapsed < duration && state.AuraObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            state.AuraObject.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            state.AuraObject.transform.position = startPosition + Vector3.up * (verticalTravel * eased);

            Color color = state.Aura.color;
            color.a = Mathf.Lerp(startAlpha, 0f, t * t);
            state.Aura.color = color;
            yield return null;
        }

        _releasedGlowObjects.Remove(state.AuraObject);
        if(state.AuraObject != null)
        {
            Destroy(state.AuraObject);
        }
    }

    private void PlayHaptic(HapticPatterns.PresetType preset)
    {
        if(!enableHaptics
           || !Services.Settings.SettingsPreferences.VibrationEnabled
           || preset == HapticPatterns.PresetType.None)
        {
            return;
        }

        HapticPatterns.PlayPreset(preset);
    }

    private void PlayItemPulse(Transform target, float pulseAmount)
    {
        int id = target.GetInstanceID();
        if(_itemPulses.TryGetValue(id, out ItemPulseState activePulse))
        {
            if(activePulse.Coroutine != null)
            {
                StopCoroutine(activePulse.Coroutine);
            }

            if(activePulse.Target != null)
            {
                activePulse.Restore();
            }
        }

        ItemPulseState pulse = new ItemPulseState(
            target,
            GetItemPulseRestScale(target),
            target.localPosition,
            target.localRotation);
        pulse.Coroutine = StartCoroutine(ItemPulseCoroutine(id, pulse, pulseAmount));
        _itemPulses[id] = pulse;
    }

    private static Vector3 GetItemPulseRestScale(Transform target)
    {
        if(target == null)
            return Vector3.one;

        if(target.GetComponent<Item>() == null)
            return target.localScale;

        const float expectedScale = 0.9f;
        Vector3 scale = target.localScale;
        if(scale.x < expectedScale * 0.75f || scale.y < expectedScale * 0.75f)
            return Vector3.one * expectedScale;

        return scale;
    }

    private IEnumerator ItemPulseCoroutine(int id, ItemPulseState pulse, float pulseAmount)
    {
        if(pulse.Target == null)
        {
            _itemPulses.Remove(id);
            yield break;
        }

        float duration = 0.2f * motionDurationScale * itemPulseDurationScale;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            if(pulse.Target == null)
            {
                _itemPulses.Remove(id);
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float envelope = Mathf.Sin(t * Mathf.PI);

            switch(itemMotionStyle)
            {
                case ItemMotionStyle.CrispTap:
                    float tap = t < 0.28f
                        ? Mathf.SmoothStep(0f, 1f, t / 0.28f)
                        : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.28f) / 0.72f);
                    pulse.Target.localScale = pulse.RestScale * (1f + pulseAmount * tap);
                    break;
                case ItemMotionStyle.SquashStretch:
                    float bounce = Mathf.Sin(t * Mathf.PI * 2.5f) * Mathf.Exp(-t * 2.4f);
                    pulse.Target.localScale = new Vector3(
                        pulse.RestScale.x * (1f + pulseAmount * bounce),
                        pulse.RestScale.y * (1f - pulseAmount * bounce * 0.62f),
                        pulse.RestScale.z);
                    break;
                case ItemMotionStyle.FloatUp:
                    pulse.Target.localScale = pulse.RestScale * (1f + pulseAmount * envelope * 0.45f);
                    pulse.Target.localPosition = pulse.RestPosition + Vector3.up * (pulseAmount * envelope * 0.75f);
                    break;
                case ItemMotionStyle.Wobble:
                    float wobble = Mathf.Sin(t * Mathf.PI * 4f) * envelope;
                    pulse.Target.localScale = pulse.RestScale * (1f + pulseAmount * envelope * 0.35f);
                    pulse.Target.localRotation = pulse.RestRotation * Quaternion.Euler(0f, 0f, wobble * pulseAmount * 55f);
                    break;
                case ItemMotionStyle.SoftPop:
                default:
                    pulse.Target.localScale = pulse.RestScale * (1f + pulseAmount * envelope);
                    break;
            }
            yield return null;
        }

        if(pulse.Target != null)
        {
            pulse.Restore();
        }

        _itemPulses.Remove(id);
    }

    private void CompleteAllItemPulses()
    {
        if(_itemPulses.Count == 0)
        {
            return;
        }

        foreach(ItemPulseState pulse in _itemPulses.Values)
        {
            if(pulse.Coroutine != null)
            {
                StopCoroutine(pulse.Coroutine);
            }

            if(pulse.Target != null)
            {
                pulse.Restore();
            }
        }

        _itemPulses.Clear();
    }

    private void RestoreAnimatedTransforms()
    {
        CompleteAllItemPulses();

        foreach(SelectionGlowState glow in _selectionGlows.Values)
        {
            if(glow.AuraObject != null)
            {
                Destroy(glow.AuraObject);
            }
        }

        _selectionGlows.Clear();

        for(int i = _releasedGlowObjects.Count - 1; i >= 0; i--)
        {
            if(_releasedGlowObjects[i] != null)
            {
                Destroy(_releasedGlowObjects[i]);
            }
        }

        _releasedGlowObjects.Clear();
        RestoreBoardTransform();
    }

    private void RestoreBoardTransform()
    {
        StopBoardAnimations();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreAnimatedTransforms();
    }

    private sealed class ItemPulseState
    {
        public ItemPulseState(
            Transform target,
            Vector3 restScale,
            Vector3 restPosition,
            Quaternion restRotation)
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
            if(Target == null)
                return;

            Target.localScale = RestScale;
            Target.localPosition = RestPosition;
            Target.localRotation = RestRotation;
        }
    }

    private sealed class SelectionGlowState
    {
        public SelectionGlowState(
            Transform target,
            SpriteRenderer source,
            GameObject auraObject,
            SpriteRenderer aura,
            Vector3 restScale,
            Quaternion baseRotation,
            Color glowColor)
        {
            Target = target;
            Source = source;
            AuraObject = auraObject;
            Aura = aura;
            RestScale = restScale;
            BaseRotation = baseRotation;
            GlowColor = glowColor;
        }

        public Transform Target { get; }
        public SpriteRenderer Source { get; }
        public GameObject AuraObject { get; }
        public SpriteRenderer Aura { get; }
        public Vector3 RestScale { get; }
        public Quaternion BaseRotation { get; }
        public Color GlowColor { get; }
        public Coroutine Coroutine { get; set; }
    }
}
