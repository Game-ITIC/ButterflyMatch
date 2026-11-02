using System;
using Models;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Views
{
    public class BoostShopView : MonoBehaviour
    {
        [SerializeField] private TMP_Text currentCoinsText;

        [Inject] public Systems.CurrencySystem.Interfaces.ICurrencyService CurrencyService;

        private CompositeDisposable _compositeDisposable = new();
        
        private void Start()
        {   
            if (CurrencyService != null)
            {
                var observable = CurrencyService.GetCurrencyObservable(Systems.CurrencySystem.CurrencyType.Diamond);
                if (observable != null)
                {
                    currentCoinsText.text = ((int)observable.Value.Value).ToString();
                    observable.Subscribe(v =>
                    {
                        if (v != null && currentCoinsText != null)
                        {
                            currentCoinsText.text = ((int)v.Value).ToString();
                        }
                    }).AddTo(_compositeDisposable);
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            _compositeDisposable.Dispose();
        }
    }
}
