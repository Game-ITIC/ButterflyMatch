using Meta.Quests.Models;
using Models;
using Providers;
using ZLinq;

namespace Meta.Quests.Services
{
    public class RewardService
    {
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;

        public RewardService(
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            BoostersProvider boostersProvider
        )
        {
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
        }

        public void GiveReward(QuestReward reward)
        {
            switch (reward.type)
            {
                case RewardType.Coins:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, reward.amount);
                    break;
                case RewardType.Gems:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, reward.amount);
                    break;
                case RewardType.Boosters:
                    // _boostersProvider.BoostersModels
                        // .AsValueEnumerable()
                        // .First(v => v.Type == reward.)
                    break;
            }
        }
    }
}