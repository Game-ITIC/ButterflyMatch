namespace JuiceFresh.Popups
{
    /// <summary>Level intro banner: closes itself once shown, then the level waits for its start popups.</summary>
    public sealed class PrePlayPopup : PopupBehaviour
    {
        public PrePlayPopup(AnimationManager popup) : base(popup) { }

        public override void OnShowFinished()
        {
            Popup.CloseMenu();
            Level.gameStatus = GameState.WaitForPopup;
        }
    }
}
