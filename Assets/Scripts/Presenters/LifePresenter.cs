using System;
using Interfaces;
using R3;
using Systems;
using VContainer.Unity;
using Views;

namespace Presenters
{
    public class LifePresenter : IInitializable, IDisposable
    {
        private readonly HealthSystem _healthSystem;
        private readonly HealthBarView _healthBarView;
        private readonly TextView _textView;
        private readonly bool _showLifeCount;

        private CompositeDisposable _disposable = new();
        private string _lastStatusText;

        public LifePresenter(HealthSystem healthSystem, HealthBarView healthBarView)
        {
            _healthSystem = healthSystem;
            _healthBarView = healthBarView;
        }

        public LifePresenter(HealthSystem healthSystem, TextView textView)
        {
            _healthSystem = healthSystem;
            _textView = textView;
            _showLifeCount = textView != null && IsLifeCountText(textView.gameObject.name);
        }

        public void Initialize()
        {
            if (_healthBarView != null)
            {
                _healthSystem.CurrentLives
                    .Subscribe(lives => _healthBarView.SetLives(lives))
                    .AddTo(_disposable);

                Observable.Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(_ =>
                    {
                        _healthSystem.UpdateRegeneration();
                        UpdateHealthBarStatus();
                    })
                    .AddTo(_disposable);

                // Initial values
                _healthBarView.SetLives(_healthSystem.CurrentLives.Value);
                UpdateHealthBarStatus();
            }
            else if (_textView != null)
            {
                _healthSystem.CurrentLives.Subscribe(_ => UpdateLegacyLifeText()).AddTo(_disposable);

                Observable.EveryUpdate(UnityFrameProvider.EarlyUpdate)
                    .Subscribe(_ =>
                    {
                        _healthSystem.UpdateRegeneration();
                        UpdateLegacyLifeText();
                    })
                    .AddTo(_disposable);
            }
        }

        private void UpdateHealthBarStatus()
        {
            if (_healthBarView == null) return;
            _healthBarView.SetCanRefill(!_healthSystem.HasUnlimitedLives
                                        && _healthSystem.CurrentLives.Value < _healthSystem.MaxLives);
            var statusText = _healthSystem.GetLifeStatusText();
            if (_lastStatusText == statusText) return;
            _lastStatusText = statusText;
            _healthBarView.SetReloadStatus(statusText);
        }

        private void UpdateLegacyLifeText()
        {
            if (_textView == null) return;
            var lifeText = _showLifeCount
                ? _healthSystem.CurrentLives.Value.ToString()
                : _healthSystem.GetLifeStatusText();

            if (_lastStatusText == lifeText) return;
            _lastStatusText = lifeText;
            _textView.SetText(lifeText);
        }

        private static bool IsLifeCountText(string objectName)
        {
            return string.Equals(objectName, "CountHelth", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(objectName, "CountHealth", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(objectName, "Life Count", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(objectName, "Lives Count", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
