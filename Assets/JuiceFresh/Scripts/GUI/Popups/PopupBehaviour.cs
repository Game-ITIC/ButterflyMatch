using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>
    /// What one legacy popup does on top of the shared open/close flow of <see cref="AnimationManager"/>.
    /// All these popups carry the same AnimationManager component (their buttons and animation events call it),
    /// so each popup gets its own behaviour, chosen by the GameObject name in <see cref="PopupBehaviours"/>.
    /// </summary>
    public class PopupBehaviour
    {
        protected readonly AnimationManager Popup;

        public PopupBehaviour(AnimationManager popup, bool closesOnEscape = false)
        {
            Popup = popup;
            ClosesOnEscape = closesOnEscape;
        }

        /// <summary>The Escape key (Android back) closes this popup.</summary>
        public bool ClosesOnEscape { get; }

        protected static LevelManager Level => LevelManager.THIS;
        protected Transform Root => Popup.transform;

        /// <summary>The popup was activated (OnEnable).</summary>
        public virtual void OnShown() { }

        /// <summary>The open animation finished (OnFinished animation / tween event).</summary>
        public virtual void OnShowFinished() { }

        /// <summary>CloseMenu was called; runs before the shared close steps hide the popup.</summary>
        public virtual void OnClosing() { }

        /// <summary>The Play button of this popup.</summary>
        public virtual void OnPlay() { }

        /// <summary>Animation event of the failed banner: hold it until the player decides.</summary>
        public virtual void OnWaitForGiveUp() { }
    }
}
