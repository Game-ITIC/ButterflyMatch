using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Level start card: loads the chosen level and lists its targets.</summary>
    public sealed class MenuPlayPopup : PopupBehaviour
    {
        private const string TargetsPath = "Image/TargetIngr/TargetIngr";

        public MenuPlayPopup(AnimationManager popup) : base(popup, closesOnEscape: true) { }

        public override void OnShown()
        {
            Level.LoadLevel();
            // Always built with the default target: the old level parser of AnimationManager was never called.
            Level.CreateCollectableTarget(Root.Find(TargetsPath).gameObject, default(Target));
        }

        public override void OnPlay()
        {
            Level.gameStatus = GameState.PrepareGame;
            Popup.CloseMenu();
        }

        public override void OnClosing()
        {
            foreach (Transform target in Root.Find(TargetsPath))
                Object.Destroy(target.gameObject);
        }
    }
}
