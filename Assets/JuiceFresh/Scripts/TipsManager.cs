using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Board hint state shared with the tutorial and the bomb spawner.
/// The legacy hint search (pattern scan + hint animation) had been switched off: its calls were commented out,
/// so it never found a hint. That dead code was removed; the observable state is unchanged:
/// no hint is ever shown, <see cref="gotTip"/> stays false and <see cref="GetCombine"/> stays empty.
/// A real hint should be built on PossibleMovesDetector, which already knows if a move exists.
/// </summary>
public class TipsManager : MonoBehaviour
{
    public static TipsManager THIS;

    /// <summary>True while a hint is being shown (bomb spawning waits for it to end).</summary>
    public bool gotTip;

    private readonly List<Item> _hintItems = new List<Item>();

    private void Start()
    {
        THIS = this;
    }

    /// <summary>The pieces of the hinted move (empty when there is no hint).</summary>
    public List<Item> GetCombine()
    {
        return _hintItems;
    }
}
