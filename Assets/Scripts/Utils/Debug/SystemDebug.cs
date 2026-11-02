using Meta.Quests.Events;
using Models;
using Providers;
using Sirenix.OdinInspector;
using Systems;
using UnityEngine;
using VContainer;

namespace Utils.Debug
{
    public class SystemDebug : MonoBehaviour
    {
        [Inject] private HealthSystem _healthSystem;
        [Inject] private Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        [Inject] private BoostersProvider _boostersProvider;

        [Title("Keyboard Cheats")]
        [SerializeField] private bool enableKeyboardCheats = true;
        [SerializeField] private KeyCode addGemsKey = KeyCode.R;
        [SerializeField] private KeyCode addStarsKey = KeyCode.T;
        [SerializeField] private KeyCode addBoostersKey = KeyCode.Y;
        [SerializeField] private KeyCode restoreLivesKey = KeyCode.U;

        [SerializeField] private int keyboardGemsAmount = 1000;
        [SerializeField] private int keyboardStarsAmount = 100;
        [SerializeField] private int keyboardBoostersAmount = 10;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if(!enableKeyboardCheats)
            {
                return;
            }

            if(Input.GetKeyDown(addGemsKey))
            {
                AddGems(keyboardGemsAmount);
            }

            if(Input.GetKeyDown(addStarsKey))
            {
                AddStars(keyboardStarsAmount);
            }

            if(Input.GetKeyDown(addBoostersKey))
            {
                AddAllBoosters(keyboardBoostersAmount);
            }

            if(Input.GetKeyDown(restoreLivesKey))
            {
                RestoreAllLives();
            }
        }
#endif
        
        [Button("Collect Quest Item")]
        public void CollectQuestItem(string itemId, int amount)
        {
            QuestEvents.ItemCollected(itemId, amount);
        }

        [Button("Add Gems")]
        public void AddGems(int amount = 1000)
        {
            _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, Mathf.Max(0, amount));
        }

        [Button("Add Stars")]
        public void AddStars(int amount = 100)
        {
            _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Star, Mathf.Max(0, amount));
        }

        [Button("Add All Boosters")]
        public void AddAllBoosters(int amount = 10)
        {
            amount = Mathf.Max(0, amount);

            foreach(var boosterModel in _boostersProvider.BoostersModels)
            {
                boosterModel.Add(amount);
            }

            _boostersProvider.Save();
        }

        [Button("Restore All Lives")]
        public void RestoreAllLives()
        {
            _healthSystem.RestoreAllLives();
        }

        [Button("Health")]
        public void Health(int i)
        {
            _healthSystem.AddLives(i);
        }
    }
}
