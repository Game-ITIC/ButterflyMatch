using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Level failed: ends the level unless the player bought a continue or restarts it.</summary>
    public sealed class MenuFailedPopup : PopupBehaviour
    {
        public MenuFailedPopup(AnimationManager popup) : base(popup) { }

        public override void OnShowFinished()
        {
            if (Level.Limit <= 0 && Level.gameStatus != GameState.GameOver)
                Level.gameStatus = GameState.GameOver;
        }

        public override void OnClosing()
        {
            // Again / continue keep the level going; any other close returns to the map.
            if (!Popup.ConsumeKeepGaming())
                Level.gameStatus = GameState.Map;
        }

        public override void OnPlay()
        {
            Level.gameStatus = GameState.Map;
        }

        public override void OnWaitForGiveUp()
        {
            Popup.SetFailedBannerSpeed(0);
        }
    }
}
