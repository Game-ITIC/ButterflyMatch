using Configs;
using GameLocalization;
using Providers;
using Services;
using Views;

namespace Initializers
{
    /// <summary>Shows what a watched rewarded ad gave (win bonus gems, continue moves, other rewards).</summary>
    internal sealed class AdRewardPopupPresenter
    {
        private readonly GameProvider _gameProvider;
        private readonly GameCompleteView _completeView;
        private readonly BoostShopView _boostShopView;
        private readonly AdRewardService _adRewardService;
        private readonly GameConfig _gameConfig;
        private readonly LevelManager _levelManager;

        public AdRewardPopupPresenter(
            GameProvider gameProvider,
            GameCompleteView completeView,
            BoostShopView boostShopView,
            AdRewardService adRewardService,
            GameConfig gameConfig,
            LevelManager levelManager)
        {
            _gameProvider = gameProvider;
            _completeView = completeView;
            _boostShopView = boostShopView;
            _adRewardService = adRewardService;
            _gameConfig = gameConfig;
            _levelManager = levelManager;
        }

        public void Show(AdRewardType rewardType)
        {
            _boostShopView.Hide();
            var popup = _gameProvider.RewardPopup;

            switch (rewardType)
            {
                case AdRewardType.X2:
                    // Win-screen bonus: gems (stars stay as earned, GDD 4.2).
                    popup.Init(new RewardInfo
                    {
                        Icon = _gameProvider.DiamondSprite,
                        Description = L10n.Format("reward_gems_n", _gameConfig.WinAdDiamondReward)
                    });
                    _completeView.AdsButton.gameObject.SetActive(false);
                    break;
                case AdRewardType.Continue:
                    popup.Init(new RewardInfo
                    {
                        Icon = _gameProvider.ExtraStepSprite,
                        Description = L10n.Format("reward_extra_moves", _levelManager.ExtraFailedMoves)
                    });
                    break;
                default:
                    popup.Init(_adRewardService.GetInfo());
                    break;
            }

            popup.Show();
        }
    }
}
