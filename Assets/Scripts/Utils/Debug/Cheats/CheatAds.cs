#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Services.InApp;
using UI;

namespace Utils.DebugTools
{
    /// <summary>Ad cheats: force an interstitial, or grant Remove Ads.</summary>
    internal sealed class CheatAds
    {
        private readonly CheatPanel _panel;

        public CheatAds(CheatPanel panel)
        {
            _panel = panel;
        }

        public void ShowInterstitial()
        {
            if(IronSourceManager.Instance == null)
            {
                Toast.Show("Реклама ещё не готова");
                return;
            }

            _panel.Close();
            Toast.Show(IronSourceManager.Instance.ShowInterstitial() ? "Показ рекламы" : "Реклама не готова, запрошена загрузка");
        }

        public void GrantRemoveAds()
        {
            var grants = CheatServices.Need<InAppGrantService>();
            if(grants != null)
            {
                grants.SetRemoveAds();
                _panel.Done("Реклама отключена, вернуть можно полным сбросом");
            }
        }
    }
}
#endif
