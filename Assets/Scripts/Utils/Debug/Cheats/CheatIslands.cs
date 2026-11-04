#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Services;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UI;
using UnityEngine;
using Utils.Save;

namespace Utils.DebugTools
{
    /// <summary>Island cheats: build the next slot or the whole island for free, or wipe the island progress.</summary>
    internal sealed class CheatIslands
    {
        private const string MenuOnly = "Открой главное меню";

        private readonly CheatPanel _panel;
        private readonly CheatLevels _levels;

        public CheatIslands(CheatPanel panel, CheatLevels levels)
        {
            _panel = panel;
            _levels = levels;
        }

        public void BuildIsland(bool wholeIsland)
        {
            var service = CheatServices.Find<IslandBuildService>();
            var currency = CheatServices.Need<ICurrencyService>();
            if(service == null)
            {
                Toast.Show(MenuOnly);
                return;
            }

            if(currency == null)
            {
                return;
            }

            var island = service.CurrentProgressIsland();
            if(island == null || service.IsCompleted(island))
            {
                Toast.Show("Строить больше нечего");
                return;
            }

            if(service.ActiveIsland != island)
            {
                service.SelectIsland(island);
            }

            var built = 0;
            while(TryBuildNextSlot(service, currency, island))
            {
                built++;
                if(!wholeIsland)
                {
                    break;
                }
            }

            _panel.Close();
            Toast.Show(built > 0 ? island.DisplayName + ": построено " + built : "Не удалось построить");
        }

        public void ResetIslands()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKeys.IslandBuildProgress);
            PlayerPrefs.Save();
            _levels.GoToMenu("Острова сброшены");
        }

        private static bool TryBuildNextSlot(IslandBuildService service, ICurrencyService currency, Configs.IslandSO island)
        {
            var slots = island.Slots;
            for(var i = 0; i < slots.Count; i++)
            {
                if(!service.CanSelect(island, slots[i]))
                {
                    continue;
                }

                // The slot is paid by the cheat, so the player's stars stay as they were.
                currency.AddCurrency(CurrencyType.Star, slots[i].StarCost);
                if(service.TryBuild(slots[i]))
                {
                    return true;
                }

                currency.SpendCurrency(CurrencyType.Star, slots[i].StarCost);
            }

            return false;
        }
    }
}
#endif
