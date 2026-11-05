using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>Text sizing shared by screens that show one kind of label several times.</summary>
    public static class UITextFit
    {
        /// <summary>
        /// Auto-sizes every label, then gives all of them the smallest result, so one long name
        /// (or one long locale) doesn't leave a row of cards with different font sizes.
        /// Labels need auto size range set in the prefab; it is re-measured on every call.
        /// </summary>
        public static void MatchSmallest(IReadOnlyList<TMP_Text> labels)
        {
            var smallest = float.MaxValue;
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    continue;
                }

                label.enableAutoSizing = true;
                label.ForceMeshUpdate(true);
                smallest = Mathf.Min(smallest, label.fontSize);
            }

            if (smallest == float.MaxValue)
            {
                return;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    continue;
                }

                label.enableAutoSizing = false;
                label.fontSize = smallest;
            }
        }
    }
}
