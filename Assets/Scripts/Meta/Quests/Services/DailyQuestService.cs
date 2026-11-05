using System;
using System.Collections.Generic;
using System.Linq;
using Configs.Tasks;
using Meta.Quests.Events;
using Meta.Quests.Interfaces;
using Meta.Quests.Models;
using UnityEngine;

namespace Meta.Quests.Services
{
    public class DailyQuestService : IDailyQuestService, IQuestProgressTracker, IDisposable
    {
        private readonly TasksListSO _settings;
        private readonly IQuestDataStorage _storage;
        private readonly IQuestGenerator _generator;

        private List<DailyQuest> _activeQuests = new List<DailyQuest>();
        private DateTime _lastRefreshTime;

        public IReadOnlyList<DailyQuest> ActiveQuests => _activeQuests.AsReadOnly();

        public event Action<DailyQuest> OnQuestCompleted;
        public event Action<IReadOnlyList<DailyQuest>> OnQuestsRefreshed;

        public DailyQuestService(
            TasksListSO settings,
            IQuestDataStorage storage,
            IQuestGenerator generator
        )
        {
            _settings = settings;
            _storage = storage;
            _generator = generator;
        }

        public void Initialize()
        {
            LoadQuestData();
            SubscribeToEvents();
            CheckAndRefreshIfNeeded();
        }

        private void SubscribeToEvents()
        {
            QuestEvents.OnItemCollected += TrackItemCollected;
            QuestEvents.OnLevelCompleted += OnLevelCompletedEvent;
            QuestEvents.OnItemUsed += TrackItemUsed;
        }

        private void LoadQuestData()
        {
            _activeQuests = _storage.LoadQuestData();
            _lastRefreshTime = _storage.GetLastRefreshTime();
        }

        public bool CheckAndRefreshIfNeeded()
        {
            var now = DateTime.Now;
            var timeSinceLastRefresh = now - _lastRefreshTime;

            if (timeSinceLastRefresh.TotalHours >= _settings.hoursUntilRefresh)
            {
                RefreshQuests();
                return true;
            }

            return false;
        }

        public void ForceRefreshQuests()
        {
            RefreshQuests();
        }

        private void RefreshQuests()
        {
            _activeQuests = _generator.GenerateQuests(_settings.simultaneousQuestsCount);
            _lastRefreshTime = DateTime.Now;

            _storage.SaveQuestData(_activeQuests, _lastRefreshTime);
            OnQuestsRefreshed?.Invoke(ActiveQuests);

            if (_settings.enableDebugLogs)
                Debug.Log($"Daily quests refreshed. Generated {_activeQuests.Count} new quests.");
        }

        public void TrackItemCollected(string itemId, int amount = 1)
        {
            UpdateQuestProgress(QuestType.CollectItems, itemId, amount);
        }

        private void OnLevelCompletedEvent(bool isPerfect)
        {
            TrackLevelCompleted(isPerfect);
        }

        public void TrackLevelCompleted(bool isPerfect = false)
        {
            UpdateQuestProgress(QuestType.CompleteLevels, null, 1);

            if (isPerfect)
                UpdateQuestProgress(QuestType.PerfectLevels, null, 1);
        }

        public void TrackItemUsed(string itemId, int amount = 1)
        {
            UpdateQuestProgress(QuestType.UseItems, itemId, amount);
        }

        private void UpdateQuestProgress(QuestType type, string targetId, int amount)
        {
            bool anyQuestUpdated = false;

            foreach (var quest in _activeQuests)
            {
                if (quest.isCompleted || quest.type != type)
                    continue;

                if (!string.IsNullOrEmpty(quest.targetItemId) && quest.targetItemId != targetId)
                    continue;

                int previousProgress = quest.currentProgress;
                quest.UpdateProgress(quest.currentProgress + amount);

                if (quest.currentProgress != previousProgress)
                {
                    anyQuestUpdated = true;

                    if (quest.isCompleted)
                    {
                        OnQuestCompleted?.Invoke(quest);

                        if (_settings.enableDebugLogs)
                            Debug.Log($"Quest completed: {quest.GetLocalizedDescription()}");
                    }
                }
            }

            if (anyQuestUpdated)
            {
                _storage.SaveQuestData(_activeQuests, _lastRefreshTime);
            }
        }

        public void Dispose()
        {
            QuestEvents.OnItemCollected -= TrackItemCollected;
            QuestEvents.OnLevelCompleted -= OnLevelCompletedEvent;
            QuestEvents.OnItemUsed -= TrackItemUsed;
        }
    }
}