namespace JuiceFresh.Popups
{
    /// <summary>"Complete" label: hides itself when its animation ends.</summary>
    public sealed class CompleteLabelPopup : PopupBehaviour
    {
        public CompleteLabelPopup(AnimationManager popup) : base(popup) { }

        public override void OnShowFinished()
        {
            Popup.gameObject.SetActive(false);
        }
    }
}
