using Configs;
using Models;
using Unity.Services.LevelPlay;
using UnityEngine;
using VContainer;
using LevelPlayAdFormat = com.unity3d.mediation.LevelPlayAdFormat;

public class IronSourceManager : MonoBehaviour
{
    public static IronSourceManager Instance;

    private LevelPlayInterstitialAd _interstitial;
    private LevelPlayBannerAd _banner;
    private LevelPlayRewardedAd _rewarded;

    private const int MaxRetryAttempts = 2;
    private int _retryAttempts = 0;
    private const float RetryDelay = 5f;

    private bool _isInitialized = false;
    private bool _bannerLoaded = false;
    private bool _interstitialLoaded = false;
    private bool _rewardedLoaded = false;
    private bool _initializationRequested;
    private bool _initializationEventsSubscribed;

    private IronSourceConfigSO _config;
    private AdEventModel _adEventModel;
    private Data.InternetState _internetState;

    private bool HasRemoveAds => _internetState != null && _internetState.HasRemoveAds;

    [Inject]
    void Construct(
        IronSourceConfigSO config,
        AdEventModel adEventModel,
        Data.InternetState internetState
        )
    {
        _config = config;
        _adEventModel = adEventModel;
        _internetState = internetState;
    }

    /// <summary>Called when the No Ads purchase is granted: removes the banner right away.</summary>
    public void ApplyRemoveAds()
    {
        DestroyBannerAd();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AreAdsReady()
    {
        return _isInitialized && (_banner != null) && (_interstitial != null) && (_rewarded != null);
    }

    public void InitializeLevelPlay()
    {
        if(_isInitialized || _initializationRequested)
        {
            return;
        }

        if(_config == null || string.IsNullOrWhiteSpace(_config.AppKey))
        {
            Debug.LogError($"{nameof(IronSourceManager)} cannot initialize: LevelPlay config is not injected or AppKey is empty.", this);
            return;
        }

        _initializationRequested = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LevelPlay.ValidateIntegration();
#endif

        if(!_initializationEventsSubscribed)
        {
            LevelPlay.OnInitSuccess += LevelPlayOnInitSuccess;
            LevelPlay.OnInitFailed += LevelPlayOnInitFailed;
            _initializationEventsSubscribed = true;
        }

        LevelPlay.Init(_config.AppKey, null, new[]
        {
            LevelPlayAdFormat.BANNER,
            LevelPlayAdFormat.INTERSTITIAL,
            LevelPlayAdFormat.REWARDED
        });
    }

    private void LevelPlayOnInitSuccess(LevelPlayConfiguration obj)
    {
        _initializationRequested = false;
        _retryAttempts = 0;
        _isInitialized = true;

        RegisterBanner();
        RegisterInterstitial();
        RegisterRewarded();
    }

    private void LevelPlayOnInitFailed(LevelPlayInitError obj)
    {
        _initializationRequested = false;

        if (_retryAttempts < MaxRetryAttempts)
        {
            _retryAttempts++;
            CancelInvoke(nameof(InitializeLevelPlay));
            Invoke(nameof(InitializeLevelPlay), RetryDelay);
        }
    }

    private void RegisterBanner()
    {
        if (HasRemoveAds)
        {
            return;
        }

        _banner = new LevelPlayBannerAd(_config.BannerId);

        _banner.OnAdLoaded += BannerOnAdLoadedEvent;
        _banner.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
        _banner.OnAdDisplayed += BannerOnAdDisplayedEvent;
        _banner.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
        _banner.OnAdClicked += BannerOnAdClickedEvent;
        _banner.OnAdCollapsed += BannerOnAdCollapsedEvent;
        _banner.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
        _banner.OnAdExpanded += BannerOnAdExpandedEvent;

        LoadBannerAd();
    }

    #region banner

    public void LoadBannerAd()
    {
        if (_banner == null) return;
        _bannerLoaded = false;
        _banner.LoadAd();
    }

    public void ShowBannerAd()
    {
        if (HasRemoveAds)
        {
            DestroyBannerAd();
            return;
        }

        if (_banner != null)
        {
            _banner.ShowAd();
        }
    }

    private void BannerOnAdLoadedEvent(LevelPlayAdInfo obj)
    {
        _bannerLoaded = true;
        ShowBannerAd();
    }

    private void BannerOnAdLoadFailedEvent(LevelPlayAdError obj)
    {
        Invoke(nameof(LoadBannerAd), RetryDelay);
    }

    private void BannerOnAdDisplayedEvent(LevelPlayAdInfo obj)
    {
    }

    private void BannerOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError obj)
    {
    }

    private void BannerOnAdClickedEvent(LevelPlayAdInfo obj)
    {
    }

    private void BannerOnAdCollapsedEvent(LevelPlayAdInfo obj)
    {
    }

    private void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo obj)
    {
    }

    private void BannerOnAdExpandedEvent(LevelPlayAdInfo obj)
    {
    }

    public void DestroyBannerAd()
    {
        if (_banner != null)
        {
            _banner.DestroyAd();
            _banner = null;
        }
    }

    #endregion

    #region Interstital

    private void RegisterInterstitial()
    {
        _interstitial = new LevelPlayInterstitialAd(_config.InterstitialId);

        _interstitial.OnAdLoaded += InterstitialOnAdLoadedEvent;
        _interstitial.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        _interstitial.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        _interstitial.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        _interstitial.OnAdClicked += InterstitialOnAdClickedEvent;
        _interstitial.OnAdClosed += InterstitialOnAdClosedEvent;
        _interstitial.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

        LoadInterstitial();
    }

    public void LoadInterstitial()
    {
        if (_interstitial == null) return;
        _interstitialLoaded = false;
        _interstitial.LoadAd();
    }


    public void ShowInterstitial()
    {
        if (_interstitial == null || HasRemoveAds)
        {
            return;
        }

        if (_interstitial.IsAdReady())
        {
            _interstitial.ShowAd();
        }
        else
        {
            LoadInterstitial();
        }
    }

    public void DestroyInterstitial()
    {
        if (_interstitial != null)
        {
            _interstitial.DestroyAd();
            _interstitial = null;
        }
    }

    private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo obj)
    {
        _interstitialLoaded = true;
    }

    private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError obj)
    {
        Invoke(nameof(LoadInterstitial), RetryDelay);
    }

    private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo obj)
    {
        AudioListener.pause = true;
    }

    private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError obj)
    {
        AudioListener.pause = false;
        LoadInterstitial();
    }

    private void InterstitialOnAdClickedEvent(LevelPlayAdInfo obj)
    {
    }

    private void InterstitialOnAdClosedEvent(LevelPlayAdInfo obj)
    {
        AudioListener.pause = false;
        LoadInterstitial();
    }

    private void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo obj)
    {
    }

    #endregion

    #region Rewarded

    private void RegisterRewarded()
    {
        _rewarded = new LevelPlayRewardedAd(_config.RewardedId);

        _rewarded.OnAdLoaded += RewardedOnAdLoadedEvent;
        _rewarded.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
        _rewarded.OnAdDisplayed += RewardedOnAdDisplayedEvent;
        _rewarded.OnAdDisplayFailed += RewardedOnAdDisplayFailedEvent;
        _rewarded.OnAdClicked += RewardedOnAdClickedEvent;
        _rewarded.OnAdClosed += RewardedOnAdClosedEvent;
        _rewarded.OnAdRewarded += RewardedOnAdRewarded;
        _rewarded.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;

        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        if (_rewarded == null) return;
        _rewardedLoaded = false;
        _rewarded.LoadAd();
    }

    public bool IsRewardedReady()
    {
        return _rewarded != null && _rewarded.IsAdReady();
    }

    /// <summary>Prefer AdRewardService.TryShowRewarded so the reward is bound to this show.</summary>
    public bool ShowRewardedAd()
    {
        if (_rewarded == null)
        {
            return false;
        }

        if (_rewarded.IsAdReady())
        {
            _rewarded.ShowAd();
            return true;
        }

        LoadRewardedAd();
        return false;
    }

    public void DestroyRewardedAd()
    {
        if (_rewarded != null)
        {
            _rewarded.DestroyAd();
            _rewarded = null;
        }
    }

    private void RewardedOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        _rewardedLoaded = true;
    }

    private void RewardedOnAdLoadFailedEvent(LevelPlayAdError ironSourceError)
    {
        Invoke(nameof(LoadRewardedAd), RetryDelay);
    }

    private void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
    }

    private void RewardedOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
        AudioListener.pause = true;
    }

    private void RewardedOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError adInfoError)
    {
        AudioListener.pause = false;
        _adEventModel?.InvokeOnRewardedFailed();
        LoadRewardedAd();
    }

    private void RewardedOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        AudioListener.pause = false;
        _adEventModel?.InvokeOnRewardedClosed();
        LoadRewardedAd();
    }

    private void RewardedOnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
    {
        _adEventModel.InvokeOnRewardedReward();
    }

    private void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
    }

    #endregion

    void OnApplicationPause(bool isPaused)
    {
        IronSource.Agent.onApplicationPause(isPaused);
    }

    private void OnDestroy()
    {
        if(Instance != this)
        {
            return;
        }

        Instance = null;
        CancelInvoke();

        if(_initializationEventsSubscribed)
        {
            LevelPlay.OnInitSuccess -= LevelPlayOnInitSuccess;
            LevelPlay.OnInitFailed -= LevelPlayOnInitFailed;
            _initializationEventsSubscribed = false;
        }

        DestroyBannerAd();
        DestroyInterstitial();
        DestroyRewardedAd();
    }
}
