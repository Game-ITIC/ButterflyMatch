namespace JuiceFresh.Popups
{
    /// <summary>Popup with the sound and music toggles: shows their saved state when opened.</summary>
    public class AudioSettingsPopup : PopupBehaviour
    {
        public AudioSettingsPopup(AnimationManager popup, bool closesOnEscape) : base(popup, closesOnEscape) { }

        public override void OnShown()
        {
            AudioToggles.ShowSaved(Root);
        }
    }
}
