namespace JuiceFresh.Popups
{
    /// <summary>In-level pause menu: pauses once its open animation is done; Play resumes the level.</summary>
    public sealed class MenuPausePopup : AudioSettingsPopup
    {
        public MenuPausePopup(AnimationManager popup) : base(popup, closesOnEscape: false) { }

        public override void OnShowFinished()
        {
            if (Level.gameStatus == GameState.Playing)
                Level.gameStatus = GameState.Pause;
        }

        public override void OnPlay()
        {
            Popup.CloseMenu();
            Level.gameStatus = GameState.Playing;
        }
    }
}
