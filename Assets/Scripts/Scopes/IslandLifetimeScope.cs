using Configs.Tasks;
using Initializers;
using Meta.Quests.Interfaces;
using Meta.Quests.Services;
using Presenters;
using Providers;
using Services.Tasks;
using Sirenix.OdinInspector;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Views;

namespace Scopes
{
    public class IslandLifetimeScope : LifetimeScope
    {
        [SerializeField] private IslandProvider islandProvider;

        // Coins (Cash) were removed from the game (Docs/GDD.md 3): only stars and gems are shown.
        [Space(10)]
        [Title("UI Components")]
        [LabelWidth(130)]
        [LabelText("Stars Display")] [Tooltip("Text view for displaying player's stars")] [SerializeField]
        private TextView starTextView;

        [LabelText("Gems Display")] [Tooltip("Text view for displaying player's gems")] [SerializeField]
        private TextView gemTextView;

        [SerializeField] private Button dailyButton;
        [SerializeField] private bool enableLegacyDailyRewards;
        [SerializeField] private CameraProvider cameraProvider;

        [FormerlySerializedAs("dailyQuestSettings")]
        [SerializeField] private TasksListSO tasksListSO;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(islandProvider);
            builder.RegisterComponent(cameraProvider);
            if (tasksListSO != null) builder.RegisterInstance(tasksListSO);

            builder.Register<ITaskService, TaskService>(Lifetime.Singleton);
            builder.Register<RewardService>(Lifetime.Singleton);

            if (starTextView != null)
            {
                builder.Register<CurrencyPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter<ICurrencyView>(starTextView)
                    .WithParameter(CurrencyType.Star);
            }

            if (gemTextView != null)
            {
                builder.Register<CurrencyPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter<ICurrencyView>(gemTextView)
                    .WithParameter(CurrencyType.Diamond);
            }

            if (enableLegacyDailyRewards && dailyButton != null)
            {
                builder.Register<DailyRewardsPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter(dailyButton);
            }

            if (tasksListSO != null)
            {
                builder.Register<IDailyQuestService, DailyQuestService>(Lifetime.Singleton);
                builder.Register<IQuestProgressTracker, DailyQuestService>(Lifetime.Singleton);
                builder.Register<IQuestDataStorage, PlayerPrefsQuestStorage>(Lifetime.Singleton);
                builder.Register<IQuestGenerator, QuestGenerator>(Lifetime.Singleton);
            }

            builder.RegisterEntryPoint<IslandInitializer>();
        }
    }
}
