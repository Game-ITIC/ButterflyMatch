#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Providers;
using Systems;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;

namespace Utils.DebugTools
{
    /// <summary>Currency, lives and booster cheats.</summary>
    internal sealed class CheatEconomy
    {
        private readonly CheatPanel _panel;

        public CheatEconomy(CheatPanel panel)
        {
            _panel = panel;
        }

        public void AddCurrency(CurrencyType type, int amount, string message)
        {
            var currency = CheatServices.Need<ICurrencyService>();
            if(currency != null)
            {
                currency.AddCurrency(type, amount);
                _panel.Done(message);
            }
        }

        public void ZeroCurrency(CurrencyType type, string message)
        {
            var currency = CheatServices.Need<ICurrencyService>();
            var value = currency?.GetCurrency(type)?.Value ?? 0f;
            if(currency != null)
            {
                currency.SpendCurrency(type, value);
                _panel.Done(message);
            }
        }

        public void ChangeLives(Action<HealthSystem> change, string message)
        {
            var health = CheatServices.Need<HealthSystem>();
            if(health != null)
            {
                change(health);
                _panel.Done(message);
            }
        }

        public void ChangeBoosters(Action<Models.BoosterModel> change, string message)
        {
            var boosters = CheatServices.Need<BoostersProvider>();
            if(boosters == null)
            {
                return;
            }

            foreach(var model in boosters.BoostersModels)
            {
                change(model);
            }

            boosters.Save();
            _panel.Done(message);
        }
    }
}
#endif
