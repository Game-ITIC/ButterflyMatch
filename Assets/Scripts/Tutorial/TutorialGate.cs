using System.Collections.Generic;
using UnityEngine;

namespace GameTutorials
{
    /// <summary>
    /// What the running tutorial lets the board do. PlayingState reads it: the board may be blocked
    /// (a button must be tapped first) or limited to the pieces of the shown chain.
    /// </summary>
    public static class TutorialGate
    {
        private static readonly HashSet<Item> Allowed = new HashSet<Item>();
        private static bool _restricted;

        /// <summary>A tutorial is on screen: the idle move hint stays off so it does not point elsewhere.</summary>
        public static bool IsShowing { get; set; }

        /// <summary>Board touches are ignored (the player has to tap a highlighted button or the dim).</summary>
        public static bool BlocksBoard { get; set; }

        /// <summary>True when the piece may join a chain right now.</summary>
        public static bool Allows(Item item) => !_restricted || Allowed.Contains(item);

        public static void RestrictTo(IEnumerable<Item> items)
        {
            Allowed.Clear();
            Allowed.UnionWith(items);
            _restricted = true;
        }

        public static void Clear()
        {
            Allowed.Clear();
            _restricted = false;
            BlocksBoard = false;
            IsShowing = false;
        }

        // Domain reload may be off in the editor: a stopped Play mode must not leave the board blocked.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();
    }
}
