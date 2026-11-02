using System.Collections.Generic;
using Meta.Quests.Models;
using UnityEngine;

namespace Configs.Tasks
{
    [CreateAssetMenu(fileName = nameof(TaskSO), menuName = "Configs/Tasks/" + nameof(TaskSO), order = 0)]
    public class TaskSO : ScriptableObject
    {
        public QuestType questType = QuestType.CollectItems;
        public List<string> availableTargetIds = new();
        public Vector2Int amountRange = new(1, 10);
        public float selectionWeight = 1f;

        [Header("Visuals")]
        public string title;
        public Sprite icon;

        [Header("Rewards")]
        public RewardType rewardType = RewardType.Gems;
        public Vector2Int rewardRange = new(10, 20);
        public List<string> possibleBoosterIds = new();

        [Header("Localization")]
        public string localizationKeyPrefix = "task_collect";
    }
}
