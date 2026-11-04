using GameLocalization;
using Services;
using Systems.CurrencySystem;
using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Buying boosters for diamonds in the in-level booster shop.</summary>
    public static class BoosterPurchase
    {
        /// <summary>
        /// Pays first: the boosters are only added when the payment went through. False when it did not.
        /// </summary>
        public static bool TryBuy(BoostType boostType, int price, int count)
        {
            LevelManager level = LevelManager.THIS;
            var currencyService = level.CurrencyService;
            if (currencyService == null || !currencyService.SpendCurrency(CurrencyType.Diamond, price))
                return false;

            SoundBase.Instance.PlaySound(SoundBase.Instance.cash);
            var boostersProvider = level.BoostersProvider;
            boostersProvider.GetBoosterModel(boostType).Add(count);
            boostersProvider.Save();
            return true;
        }

        /// <summary>The "you got ..." popup for the bought booster.</summary>
        public static void ShowReward(BoostType boostType, Sprite icon)
        {
            string boosterName = L10n.Get("boost_" + boostType.ToString().ToLowerInvariant(), boostType.ToString());
            var rewardPopup = LevelManager.THIS.GameProvider.RewardPopup;
            rewardPopup.Init(new RewardInfo { Icon = icon, Description = L10n.Format("you_got_n", boosterName) });
            rewardPopup.Show();
        }
    }
}
