using System;
using Configs;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;
using Utils.Save;

namespace Services.Ads
{
    /// <summary>
    /// Interstitial caps from Docs/GDD.md 8.2. Shown only when the player leaves a result screen
    /// (Home / Next / Restart) and only if every condition holds.
    /// </summary>
    public class InterstitialService
    {
        private const string DailyKey = "ads-interstitial";
        private const string SessionsKey = "app-sessions";
        private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(90);

        private readonly GameConfig _config;
        private readonly InternetState _internetState;
        private readonly AdRewardService _adRewardService;
        private readonly bool _isFirstSession;
        private readonly float _sessionStart;

        private float _lastShownRealtime = -10000f;
        private int _levelsSinceLast;
        private bool _showing;

        public InterstitialService(GameConfig config, InternetState internetState, AdRewardService adRewardService)
        {
            _config = config;
            _internetState = internetState;
            _adRewardService = adRewardService;

            var sessions = PlayerPrefs.GetInt(SessionsKey, 0) + 1;
            PlayerPrefs.SetInt(SessionsKey, sessions);
            _isFirstSession = sessions == 1;
            _sessionStart = Time.realtimeSinceStartup;

            // The first interstitial may come after the first finished levels, not only after 2 more.
            _levelsSinceLast = config.InterstitialMinLevelsBetween;
        }

        /// <summary>Called once per finished level (win or lose).</summary>
        public void OnLevelFinished()
        {
            _levelsSinceLast++;
        }

        public bool CanShow()
        {
            if(!_config.InterstitialEnabled || _showing)
            {
                return false;
            }

            if(_internetState != null && _internetState.HasRemoveAds)
            {
                return false;
            }

            var now = Time.realtimeSinceStartup;
            var reachedLevel = PlayerPrefs.GetInt("OpenLevel", 1);

            return reachedLevel >= _config.InterstitialMinLevel
                   && _levelsSinceLast >= _config.InterstitialMinLevelsBetween
                   && now - _lastShownRealtime >= _config.InterstitialMinIntervalSeconds
                   && now - _adRewardService.LastRewardedClosedRealtime >= _config.InterstitialAfterRewardedSeconds
                   && !(_isFirstSession && now - _sessionStart < _config.InterstitialFirstSessionGraceSeconds)
                   && DailyCounter.IsBelow(DailyKey, _config.InterstitialMaxPerDay);
        }

        /// <summary>Shows an interstitial if allowed and waits until it is closed. Always safe to await.</summary>
        public async UniTask TryShowAsync()
        {
            var ads = IronSourceManager.Instance;
            if(ads == null || !CanShow())
            {
                return;
            }

            if(!ads.IsInterstitialReady())
            {
                ads.LoadInterstitial();
                return;
            }

            var finished = new UniTaskCompletionSource();
            void OnFinished() => finished.TrySetResult();

            _showing = true;
            ads.InterstitialFinished += OnFinished;

            try
            {
                if(!ads.ShowInterstitial())
                {
                    return;
                }

                _lastShownRealtime = Time.realtimeSinceStartup;
                _levelsSinceLast = 0;
                DailyCounter.Increment(DailyKey);
                GameAnalyticsEvents.AdInterstitialShown();

                await finished.Task.TimeoutWithoutException(MaxWait, DelayType.Realtime);
            }
            finally
            {
                ads.InterstitialFinished -= OnFinished;
                _showing = false;
                AudioListener.pause = false;
            }
        }
    }
}
