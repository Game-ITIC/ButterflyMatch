using System;
using System.Collections.Generic;
using System.Linq;
using Gley.DailyRewards.Internal;
using Services.DailyRewards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer.Unity;
using Views;
using Object = UnityEngine.Object;

namespace Presenters
{
    public class DailyCardsPresenter : IInitializable, IDisposable
    {
        private readonly DailyRewardService _rewardService;

        private readonly List<DailyCardView> _cards = new();
        private Button _dailyButton;

        public DailyCardsPresenter(DailyRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        public void Initialize()
        {
            BindCards();
            WireDailyButton();
            RefreshCards();
        }

        private void BindCards()
        {
            _cards.Clear();

            var settings = Resources.Load<DailyRewardsData>(Constants.DATA_NAME_RUNTIME);
            var rewards = _rewardService.GetAllRewards();
            var cardViews = Object.FindObjectsByType<DailyCardView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(card => card.transform, new HierarchyOrderComparer())
                .ToList();

            if(cardViews.Count == 0 || rewards.Count == 0)
            {
                return;
            }

            var daysToBind = Mathf.Min(cardViews.Count, rewards.Count);

            for(var i = 0; i < daysToBind; i++)
            {
                var card = cardViews[i];
                var reward = rewards[i];
                Sprite icon = null;
                if(settings != null && settings.allDays != null && i < settings.allDays.Count)
                {
                    icon = settings.allDays[i].dayTexture;
                }

                card.Bind(i + 1, icon, reward.Amount);
                WireClaimButton(card);
                _cards.Add(card);
            }
        }

        private void WireClaimButton(DailyCardView card)
        {
            if(card.ClaimButton == null)
            {
                return;
            }

            card.ClaimButton.onClick.RemoveAllListeners();
            card.ClaimButton.onClick.AddListener(ClaimToday);
        }

        private void WireDailyButton()
        {
            _dailyButton = FindButton("Daily Button", "DailyButton");
            if(_dailyButton == null)
            {
                return;
            }

            _dailyButton.onClick.RemoveListener(OpenPanel);
            _dailyButton.onClick.AddListener(OpenPanel);
        }

        private void OpenPanel()
        {
            if(_cards.Count == 0)
            {
                return;
            }

            RefreshCards();

            var taskPanel = _cards[0].GetComponentInParent<TaskPanel>(true);
            if(taskPanel != null)
            {
                taskPanel.ShowDailyRewards();
            }
        }

        private void ClaimToday()
        {
            if(!_rewardService.CanClaimToday())
            {
                return;
            }

            _rewardService.ClaimReward();
            RefreshCards();
        }

        private void RefreshCards()
        {
            var rewards = _rewardService.GetAllRewards();
            if(_cards.Count == 0 || rewards.Count == 0)
            {
                return;
            }

            var dayIndex = _rewardService.GetCurrentDayIndex();
            var cycleIndex = dayIndex % rewards.Count;
            var canClaim = _rewardService.CanClaimToday();
            var wrappedAfterLastDay = !canClaim && cycleIndex == 0 && dayIndex > 0;

            for(var i = 0; i < _cards.Count; i++)
            {
                var isToday = canClaim && i == cycleIndex;
                var isClaimed = wrappedAfterLastDay ? i == rewards.Count - 1 : i < cycleIndex;
                _cards[i].ApplyCalendarState(isToday, isClaimed);
            }
        }

        public void Dispose()
        {
            if(_dailyButton != null)
            {
                _dailyButton.onClick.RemoveListener(OpenPanel);
            }
        }

        private static Button FindButton(params string[] names)
        {
            for(var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if(!scene.isLoaded)
                {
                    continue;
                }

                foreach(var root in scene.GetRootGameObjects())
                {
                    var buttons = root.GetComponentsInChildren<Button>(true);
                    foreach(var button in buttons)
                    {
                        foreach(var name in names)
                        {
                            if(string.Equals(button.gameObject.name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                return button;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private sealed class HierarchyOrderComparer : IComparer<Transform>
        {
            public int Compare(Transform a, Transform b)
            {
                var pathA = SiblingPath(a);
                var pathB = SiblingPath(b);
                var count = Mathf.Min(pathA.Count, pathB.Count);

                for(var i = 0; i < count; i++)
                {
                    var difference = pathA[i].CompareTo(pathB[i]);
                    if(difference != 0)
                    {
                        return difference;
                    }
                }

                return pathA.Count.CompareTo(pathB.Count);
            }

            private static List<int> SiblingPath(Transform transform)
            {
                var path = new List<int>(8);
                while(transform != null)
                {
                    path.Add(transform.GetSiblingIndex());
                    transform = transform.parent;
                }

                path.Reverse();
                return path;
            }
        }

        private sealed class HierarchyOrderComparer : IComparer<Transform>
        {
            public int Compare(Transform a, Transform b)
            {
                var pathA = SiblingPath(a);
                var pathB = SiblingPath(b);
                var count = Mathf.Min(pathA.Count, pathB.Count);

                for(var i = 0; i < count; i++)
                {
                    var difference = pathA[i].CompareTo(pathB[i]);
                    if(difference != 0)
                    {
                        return difference;
                    }
                }

                return pathA.Count.CompareTo(pathB.Count);
            }

            private static List<int> SiblingPath(Transform transform)
            {
                var path = new List<int>(8);
                while(transform != null)
                {
                    path.Add(transform.GetSiblingIndex());
                    transform = transform.parent;
                }

                path.Reverse();
                return path;
            }
        }
    }
}
