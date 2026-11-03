using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = nameof(GameConfig), menuName = nameof(Configs) + "/" + nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        public int MaxLifes = 5;
        public float LifeRegenTimeMinutes = 20f;

        // Coins (Cash) were removed (Docs/GDD.md 3): a win gives only stars, the win-screen ad gives gems.
        [Tooltip("Gems for the optional rewarded ad on the win screen (GDD 8.1 win_bonus).")]
        public int WinAdDiamondReward = 20;

        public int AmountOfTimesToPlayToShowVideo = 3;

        [Header("Continue on the lose screen (GDD 4.3)")]
        [Tooltip("Gem price of the 1st, 2nd, 3rd paid continue in one attempt. No more paid continues after the last one.")]
        public int[] ContinueGemCosts = { 150, 300, 450 };
        [Tooltip("Rewarded 'continue' ads allowed per level attempt (GDD 8.1).")]
        public int ContinueAdsPerAttempt = 1;

        [Header("Rewarded ad limits (GDD 8.1)")]
        public int FreeLifeAdsPerDay = 5;
        public int FreeBoosterAdsPerDay = 3;

        [Header("Interstitial (GDD 8.2)")]
        public bool InterstitialEnabled = true;
        [Tooltip("Interstitials only after the player reached this level.")]
        public int InterstitialMinLevel = 8;
        public float InterstitialMinIntervalSeconds = 90f;
        public int InterstitialMinLevelsBetween = 2;
        public float InterstitialAfterRewardedSeconds = 60f;
        [Tooltip("No interstitials during the first minutes of the very first session.")]
        public float InterstitialFirstSessionGraceSeconds = 300f;
        public int InterstitialMaxPerDay = 20;
    }
}
