namespace JuiceFresh.Popups
{
    /// <summary>Maps a popup GameObject name to its behaviour.</summary>
    public static class PopupBehaviours
    {
        public static PopupBehaviour Create(AnimationManager popup, string popupName)
        {
            switch (popupName)
            {
                case "MenuPlay": return new MenuPlayPopup(popup);
                case "PrePlay": return new PrePlayPopup(popup);
                case "PreFailed": return new PreFailedPopup(popup);
                case "Settings": return new AudioSettingsPopup(popup, closesOnEscape: true);
                case "MenuPause": return new MenuPausePopup(popup);
                case "GemsShop": return new GemsShopPopup(popup);
                case "MenuComplete": return new MenuCompletePopup(popup);
                case "MenuFailed": return new MenuFailedPopup(popup);
                case "MenuPreGameOver": return new PreGameOverPopup(popup);
                case "CompleteLabel": return new CompleteLabelPopup(popup);
                case "BoostInfo":
                case "LiveShop":
                case "BoostShop":
                case "Reward":
                    return new PopupBehaviour(popup, closesOnEscape: true);
                default:
                    return new PopupBehaviour(popup);
            }
        }
    }
}
