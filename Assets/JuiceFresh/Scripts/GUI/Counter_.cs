using JuiceFresh.Counters;
using TMPro;
using UI;
using UnityEngine;

/// <summary>
/// A HUD / popup text counter. What it shows comes from its GameObject name ("Score", "Limit", "TargetBlocks", ...)
/// and is resolved once into a <see cref="CounterDisplay"/>; <see cref="ShowAs"/> switches the kind at runtime.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class Counter_ : MonoBehaviour
{
    public int ingrTrackNumber;
    public int totalCount;

    private TMP_Text _text;
    private TargetGUI _parentGUI;
    private CounterDisplay _display;
    private string _lastPunchText;

    public TMP_Text Text => _text;
    public TargetGUI ParentGUI => _parentGUI;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _display = CounterDisplays.Create(name);
    }

    private void Start()
    {
        _parentGUI = transform.parent.GetComponent<TargetGUI>();
    }

    private void OnEnable()
    {
        _lastPunchText = null;
        _display.OnEnabled(this);
    }

    private void Update()
    {
        _display.Tick(this);
        if (_display.PunchOnChange)
            PunchIfChanged();
    }

    /// <summary>Turns this counter into another kind (a goal card reused for blocks / cages / bombs).</summary>
    public void ShowAs(string counterName)
    {
        // The name stays in sync: the top bar still looks some goal counters up by name.
        name = counterName;
        _display = CounterDisplays.Create(counterName);
        if (isActiveAndEnabled)
            OnEnable();
    }

    /// <summary>Assigns only when changed: avoids re-layout of the TMP mesh every frame.</summary>
    public void SetText(string value)
    {
        if (!string.Equals(_text.text, value))
            _text.text = value;
    }

    // Small bump whenever a gameplay counter changes so moves/score/targets read as alive.
    private void PunchIfChanged()
    {
        string current = _text.text;
        if (_lastPunchText != null && !string.Equals(current, _lastPunchText))
        {
            float strength = _display.PunchStrength(LevelManager.THIS);
            if (strength > 0f)
                UIFeel.Punch(_text, strength);
        }

        _lastPunchText = current;
    }
}
