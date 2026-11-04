using Systems.CurrencySystem;
using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Last chance before Game Over: continue for diamonds; closing shows the Game Over menu.</summary>
    public sealed class PreGameOverPopup : PopupBehaviour
    {
        private const int ContinuePrice = 12;

        public PreGameOverPopup(AnimationManager popup) : base(popup) { }

        public override void OnPlay()
        {
            var currencyService = Level.CurrencyService;
            if (currencyService != null && currencyService.GetCurrency(CurrencyType.Diamond)?.Value >= ContinuePrice)
            {
                currencyService.SpendCurrency(CurrencyType.Diamond, ContinuePrice);
                LevelManager.Instance.gameStatus = GameState.WaitAfterClose;
                Popup.gameObject.SetActive(false);
            }
            else
            {
                Popup.BuyGems();
            }
        }

        public override void OnClosing()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[1]);
            GameObject.Find("Canvas").transform.Find("MenuGameOver").gameObject.SetActive(true);
            Popup.gameObject.SetActive(false);
        }
    }
}
