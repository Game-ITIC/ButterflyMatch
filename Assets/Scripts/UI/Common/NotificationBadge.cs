using UnityEngine;

namespace UI
{
    /// <summary>
    /// "Something is waiting for you" marker. Drop Prefabs/UI/Common/NotificationBadge under a button
    /// and call <see cref="Set"/> from the presenter that knows the state.
    /// </summary>
    public sealed class NotificationBadge : MonoBehaviour
    {
        /// <summary>Shows or hides the badge under <paramref name="host"/>; a host without a badge is ignored.</summary>
        public static void Set(Component host, bool visible)
        {
            if (host == null)
            {
                return;
            }

            var badge = host.GetComponentInChildren<NotificationBadge>(true);
            if (badge != null)
            {
                badge.SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible)
            {
                return;
            }

            gameObject.SetActive(visible);
            if (visible)
            {
                UIFeel.Punch(this, 0.25f, 0.35f);
            }
        }
    }
}
