using System;
using Configs;
using R3;
using UnityEngine;
using Utils.Save;
using VContainer.Unity;

namespace Systems
{
    public class HealthSystem
    {
        private const string FullLivesText = "Full";

        private int _maxLives = 5;
        private float _regenTimeMinutes = 20f;

        private ReactiveProperty<int> _currentLives = new();
        private DateTime _lastSaveTime;
        private DateTime _unlimitedLivesUntil = DateTime.MinValue;
        private bool _isRegenerating = false;
        private bool _levelLifeReserved;

        public ReactiveProperty<int> CurrentLives => _currentLives;
        public int MaxLives => _maxLives;
        public bool HasUnlimitedLives => DateTime.UtcNow < _unlimitedLivesUntil;
        public bool CanPlay => HasUnlimitedLives || _currentLives.Value > 0;
        public TimeSpan TimeUntilNextLife
        {
            get
            {
                if (_currentLives.Value >= _maxLives)
                {
                    return TimeSpan.Zero;
                }

                var elapsedSeconds = Math.Max(0d, (DateTime.Now - _lastSaveTime).TotalSeconds);
                var regenSeconds = Math.Max(1d, _regenTimeMinutes * 60d);
                var remainingSeconds = Math.Max(0d, regenSeconds - elapsedSeconds);

                return TimeSpan.FromSeconds(Math.Ceiling(remainingSeconds));
            }
        }

        public HealthSystem(GameConfig gameConfig)
        {
            _maxLives = gameConfig.MaxLifes;
            _regenTimeMinutes = Mathf.Max(1f, gameConfig.LifeRegenTimeMinutes);
            LoadData();
        }

        public bool TryUseLife()
        {
            if(HasUnlimitedLives)
            {
                return true;
            }

            if (_currentLives.Value <= 0)
            {
                return false;
            }

            _currentLives.Value--;
            SaveData();
            UpdateUI();

            if (!_isRegenerating && _currentLives.Value < _maxLives)
            {
                StartRegeneration();
            }

            return true;
        }

        /// <summary>
        /// Called when a level attempt starts: the life is taken up front (so quitting / killing the app
        /// mid-level costs a life) and returned by <see cref="RefundLevelLife"/> on a win.
        /// </summary>
        public bool ConsumeLifeForLevel()
        {
            _levelLifeReserved = false;

            if(HasUnlimitedLives)
            {
                return true;
            }

            if(!TryUseLife())
            {
                return false;
            }

            _levelLifeReserved = true;
            Debug.Log($"[HealthSystem] Life reserved for level. Lives left: {_currentLives.Value}");
            return true;
        }

        public void RefundLevelLife()
        {
            if(!_levelLifeReserved)
            {
                return;
            }

            _levelLifeReserved = false;
            AddLives(1);
        }

        public void AddLives(int amount)
        {
            _currentLives.Value = Mathf.Clamp(_currentLives.Value + amount, 0, _maxLives);

            if (_currentLives.Value >= _maxLives)
            {
                _isRegenerating = false;
            }
            else if (!_isRegenerating)
            {
                StartRegeneration();
            }

            SaveData();
            UpdateUI();
        }

        public void RestoreAllLives()
        {
            _currentLives.Value = _maxLives;
            _isRegenerating = false;
            SaveData();
            UpdateUI();
        }

        public void AddUnlimitedLives(TimeSpan duration)
        {
            if(duration <= TimeSpan.Zero)
            {
                return;
            }

            var startTime = HasUnlimitedLives ? _unlimitedLivesUntil : DateTime.UtcNow;
            _unlimitedLivesUntil = startTime.Add(duration);
            SaveData();
            UpdateUI();
        }

        private void StartRegeneration()
        {
            _isRegenerating = true;
            _lastSaveTime = DateTime.Now;
            SaveData();
        }


        public void UpdateRegeneration()
        {
            if (_currentLives.Value >= _maxLives)
            {
                _isRegenerating = false;
                return;
            }

            TimeSpan timePassed = DateTime.Now - _lastSaveTime;

            if (timePassed.TotalSeconds < 0)
            {
                _lastSaveTime = DateTime.Now;
                SaveData();
                return;
            }

            double minutesPassed = timePassed.TotalMinutes;

            int livesToAdd = Mathf.FloorToInt((float)(minutesPassed / _regenTimeMinutes));

            if (livesToAdd > 0)
            {
                _currentLives.Value = Mathf.Min(_currentLives.Value + livesToAdd, _maxLives);
                _lastSaveTime = _lastSaveTime.AddMinutes(livesToAdd * _regenTimeMinutes);
                SaveData();

                if (_currentLives.Value >= _maxLives)
                {
                    _isRegenerating = false;
                    return;
                }
            }
        }

        public string GetLifeStatusText()
        {
            if(HasUnlimitedLives)
            {
                return "∞ " + FormatTime(_unlimitedLivesUntil - DateTime.UtcNow);
            }

            return _currentLives.Value >= _maxLives
                ? FullLivesText
                : FormatTime(TimeUntilNextLife);
        }

        private void SaveData()
        {
            PlayerPrefs.SetInt(PlayerPrefsKeys.Life, _currentLives.Value);
            PlayerPrefs.SetString(PlayerPrefsKeys.LifeLastSavedTime, _lastSaveTime.ToBinary().ToString());
            PlayerPrefs.SetString(PlayerPrefsKeys.UnlimitedLivesUntil, _unlimitedLivesUntil.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        private void LoadData()
        {
            _currentLives.Value = PlayerPrefs.GetInt(PlayerPrefsKeys.Life, _maxLives);
            _currentLives.Value = Mathf.Clamp(_currentLives.Value, 0, _maxLives);

            string lastSaveTimeString = PlayerPrefs.GetString(PlayerPrefsKeys.LifeLastSavedTime, "");
            string unlimitedLivesString = PlayerPrefs.GetString(PlayerPrefsKeys.UnlimitedLivesUntil, "");

            if(!string.IsNullOrEmpty(unlimitedLivesString))
            {
                try
                {
                    _unlimitedLivesUntil = DateTime.FromBinary(Convert.ToInt64(unlimitedLivesString));
                }
                catch
                {
                    _unlimitedLivesUntil = DateTime.MinValue;
                }
            }

            if (string.IsNullOrEmpty(lastSaveTimeString))
            {
                _lastSaveTime = DateTime.Now;
                _currentLives.Value = _maxLives;
            }
            else
            {
                try
                {
                    long lastSaveTimeBinary = Convert.ToInt64(lastSaveTimeString);
                    _lastSaveTime = DateTime.FromBinary(lastSaveTimeBinary);

                    if (_currentLives.Value < _maxLives)
                    {
                        TimeSpan offlineTime = DateTime.Now - _lastSaveTime;
                        int livesToAdd = Mathf.FloorToInt((float)(offlineTime.TotalMinutes / _regenTimeMinutes));

                        if (livesToAdd > 0)
                        {
                            _currentLives.Value = Mathf.Min(_currentLives.Value + livesToAdd, _maxLives);
                            _lastSaveTime = _lastSaveTime.AddMinutes(livesToAdd * _regenTimeMinutes);
                        }
                    }
                }
                catch
                {
                    _lastSaveTime = DateTime.Now;
                    _currentLives.Value = _maxLives;
                }
            }

            _isRegenerating = _currentLives.Value < _maxLives;
            SaveData();
        }

        private static string FormatTime(TimeSpan remaining)
        {
            var totalMinutes = Math.Max(0, (int)remaining.TotalMinutes);
            var seconds = Math.Max(0, remaining.Seconds);

            return $"{totalMinutes:00}:{seconds:00}";
        }

        #region UI

        private void UpdateUI()
        {
            // if (livesText != null)
            // {
            //     livesText.text = $"{currentLives}/{maxLives}";
            // }
            //
            // if (playButton != null)
            // {
            //     playButton.interactable = CanPlay;
            // }
            //
            // if (currentLives < maxLives && !isRegenerating)
            // {
            //     StartRegeneration();
            // }
        }

        #endregion
    }
}
