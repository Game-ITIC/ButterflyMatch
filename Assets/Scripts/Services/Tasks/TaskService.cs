using System;
using System.Collections.Generic;
using Configs.Tasks;
using Meta.Quests.Models;
using Meta.Quests.Services;
using Models.Tasks;
using UnityEngine;

namespace Services.Tasks
{
    public class TaskService : ITaskService, IDisposable
    {
        private readonly TasksListSO _config;
        private readonly RewardService _rewardService;
        private readonly TaskStorage _storage = new();
        private List<TaskModel> _tasks = new();
        private DateTime _lastRefreshTime;

        public IReadOnlyList<TaskModel> Tasks => _tasks;
        public event Action<IReadOnlyList<TaskModel>> OnTasksChanged;
        public event Action<TaskModel> OnTaskClaimed;

        public TaskService(TasksListSO config = null, RewardService rewardService = null)
        {
            _config = config;
            _rewardService = rewardService;
            Initialize();
        }

        public void Initialize()
        {
            _tasks = _storage.Load();
            _lastRefreshTime = _storage.GetLastRefreshTime();

            if (!CheckAndRefreshIfNeeded())
            {
                RestoreSprites();
                OnTasksChanged?.Invoke(_tasks);
            }
        }

        public bool CheckAndRefreshIfNeeded()
        {
            if (!_config) return false;

            // Daily tasks renew at 00:00 UTC (Docs/GDD.md 10); hoursUntilRefresh stays as an upper bound.
            var lastUtc = _lastRefreshTime == DateTime.MinValue ? DateTime.MinValue : _lastRefreshTime.ToUniversalTime();
            var nowUtc = DateTime.UtcNow;
            if (_tasks.Count == 0 || lastUtc.Date != nowUtc.Date || (nowUtc - lastUtc).TotalHours >= _config.hoursUntilRefresh)
            {
                RefreshTasks();
                return true;
            }

            return false;
        }

        public void ForceRefreshTasks() => RefreshTasks();

        private void RefreshTasks()
        {
            if (!_config) return;

            _tasks = TaskGenerator.GenerateTasks(_config);
            _lastRefreshTime = DateTime.UtcNow;
            _storage.Save(_tasks, _lastRefreshTime);
            OnTasksChanged?.Invoke(_tasks);
        }

        private void RestoreSprites()
        {
            if (!_config || _config.availableQuestTemplates == null) return;

            foreach (var task in _tasks)
            {
                var template = _config.availableQuestTemplates.Find(t => t && t.questType == task.type);
                if (template) task.sprite = template.icon;
            }
        }

        public void TrackProgress(QuestType type, string targetId = null, int amount = 1)
        {
            var changed = false;

            foreach (var task in _tasks)
            {
                if (task.isCompleted || task.type != type) continue;
                if (!string.IsNullOrEmpty(task.targetItemId) && task.targetItemId != targetId) continue;

                var prev = task.currentProgress;
                task.UpdateProgress(task.currentProgress + amount);
                if (task.currentProgress != prev) changed = true;
            }

            if (changed)
            {
                _storage.Save(_tasks, _lastRefreshTime);
                OnTasksChanged?.Invoke(_tasks);
            }
        }

        public void ClaimTask(string taskId)
        {
            var task = _tasks.Find(t => t.id == taskId);
            if (task == null || !task.isCompleted || task.isClaimed) return;

            task.isClaimed = true;
            if (task.reward != null) _rewardService?.GiveReward(task.reward);

            _storage.Save(_tasks, _lastRefreshTime);
            OnTaskClaimed?.Invoke(task);
            OnTasksChanged?.Invoke(_tasks);
        }

        public void Dispose() { }
    }
}
