using System;
using System.Collections.Generic;
using Models.Tasks;
using Services.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer.Unity;

namespace Presenters
{
    public class TaskPresenter : IInitializable, IDisposable
    {
        private readonly TaskPanel _taskPanel;
        private readonly ITaskService _taskService;
        private Button _taskButton;
        private readonly List<(Button Button, UnityAction Action)> _closeActions = new();
        private readonly List<TaskView> _activeViews = new();

        public IReadOnlyList<TaskView> ActiveViews => _activeViews;
        public TaskPanel Panel => _taskPanel;

        public TaskPresenter(TaskPanel taskPanel, ITaskService taskService = null, Button taskButton = null)
        {
            _taskPanel = taskPanel;
            _taskService = taskService;
            _taskButton = taskButton;
        }

        public void Initialize()
        {
            if (!_taskPanel) return;

            _taskPanel.HideImmediate();
            SetupTaskButton();
            SetupCloseButtons();

            if (_taskService != null)
            {
                _taskService.OnTasksChanged += RefreshViews;
                RefreshViews(_taskService.Tasks);
            }
        }

        private void SetupTaskButton()
        {
            if (!_taskButton) return;

            _taskButton.onClick.RemoveListener(OnTaskButtonClicked);
            _taskButton.onClick.AddListener(OnTaskButtonClicked);
        }

        private void SetupCloseButtons()
        {
            if (_taskPanel.CloseButtons == null) return;

            foreach (var closeButton in _taskPanel.CloseButtons)
            {
                if (!closeButton) continue;

                UnityAction closeAction = OnCloseButtonClicked;
                closeButton.onClick.RemoveListener(closeAction);
                closeButton.onClick.AddListener(closeAction);
                _closeActions.Add((closeButton, closeAction));
            }
        }

        public void Show()
        {
            if (_taskPanel) _taskPanel.Show();
        }

        public void Hide()
        {
            if (_taskPanel) _taskPanel.Hide();
        }

        public void RefreshViews(IReadOnlyList<TaskModel> tasks)
        {
            ClearViews();
            if (!_taskPanel)
            {
                return;
            }

            var hasAvailableTasks = false;
            if (tasks != null)
            {
                for (var i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    if (task != null && !task.isClaimed)
                    {
                        hasAvailableTasks = true;
                        break;
                    }
                }
            }

            _taskPanel.SetEmptyStateVisible(!hasAvailableTasks);
            if (!hasAvailableTasks || !_taskPanel.TaskViewPrefab)
            {
                return;
            }

            var parent = _taskPanel.TaskViewParent ? _taskPanel.TaskViewParent : _taskPanel.transform;
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null || task.isClaimed)
                {
                    continue;
                }

                var view = UnityEngine.Object.Instantiate(_taskPanel.TaskViewPrefab, parent);
                BindTaskView(view, task);
                _activeViews.Add(view);
            }
        }

        private void BindTaskView(TaskView view, TaskModel task)
        {
            if (!view || task == null) return;

            view.SetTitle(task.title);
            view.SetSubtitle($"{task.currentProgress} / {task.targetAmount}");
            view.SetIcon(task.sprite);
            view.SetClaimButtonText(task.reward != null ? task.reward.amount.ToString() : "");
            view.SetClaimInteractable(task.isCompleted && !task.isClaimed);
            view.SetClaimAction(() => _taskService?.ClaimTask(task.id));
        }

        public void ClearViews()
        {
            foreach (var view in _activeViews)
            {
                if (view) UnityEngine.Object.Destroy(view.gameObject);
            }

            _activeViews.Clear();
        }

        private void OnTaskButtonClicked() => Show();

        private void OnCloseButtonClicked() => Hide();

        public void Dispose()
        {
            if (_taskButton) _taskButton.onClick.RemoveListener(OnTaskButtonClicked);

            if (_taskService != null)
                _taskService.OnTasksChanged -= RefreshViews;

            foreach (var (button, action) in _closeActions)
            {
                if (button && action != null) button.onClick.RemoveListener(action);
            }

            _closeActions.Clear();
            ClearViews();
        }
    }
}
