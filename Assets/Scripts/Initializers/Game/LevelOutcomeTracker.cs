using Meta.Quests.Models;
using Models;
using Services;
using Services.Ads;
using Services.InApp;
using Services.Tasks;

namespace Initializers
{
    /// <summary>
    /// Reports a finished level: analytics, daily tasks, interstitial pacing and the context offers.
    /// A lose screen can open again after a continue, so each outcome is counted once per attempt.
    /// </summary>
    internal sealed class LevelOutcomeTracker
    {
        private readonly LevelManager _levelManager;
        private readonly InterstitialService _interstitialService;
        private readonly ITaskService _taskService;

        private bool _winCounted;
        private bool _loseCounted;
        private int _trackedItems;
        private int _trackedBoosters;

        public LevelOutcomeTracker(LevelManager levelManager, InterstitialService interstitialService, ITaskService taskService)
        {
            _levelManager = levelManager;
            _interstitialService = interstitialService;
            _taskService = taskService;
        }

        public void OnLevelWon()
        {
            if (_winCounted)
                return;

            _winCounted = true;
            ContextOffers.RegisterWin();
            if (!_loseCounted)
                _interstitialService.OnLevelFinished();

            var stars = _levelManager.stars;
            GameAnalyticsEvents.LevelWin(_levelManager.currentLevel, _levelManager.Limit, stars, LevelSessionStats.BoostersUsed);

            // Daily tasks (Docs/TaskSystem_TODO.md): level, 3-star level, collected pieces, used boosters.
            _taskService.TrackProgress(QuestType.CompleteLevels);
            if (stars >= 3)
                _taskService.TrackProgress(QuestType.PerfectLevels);

            TrackCollectedAndBoosters();
        }

        public void OnLevelLost()
        {
            if (!_loseCounted)
            {
                _loseCounted = true;
                _interstitialService.OnLevelFinished();
                ContextOffers.RegisterLoss(_levelManager.currentLevel);
                GameAnalyticsEvents.LevelFail(_levelManager.currentLevel, LevelSessionStats.BoostersUsed);
            }

            TrackCollectedAndBoosters();
        }

        // Only what was collected / used since the last report (a lost level can be continued and reported again).
        private void TrackCollectedAndBoosters()
        {
            var items = LevelSessionStats.ItemsCollected - _trackedItems;
            var boosters = LevelSessionStats.BoostersUsed - _trackedBoosters;
            _trackedItems = LevelSessionStats.ItemsCollected;
            _trackedBoosters = LevelSessionStats.BoostersUsed;

            if (items > 0)
                _taskService.TrackProgress(QuestType.CollectItems, "butterfly", items);

            if (boosters > 0)
                _taskService.TrackProgress(QuestType.UseItems, null, boosters);
        }
    }
}
