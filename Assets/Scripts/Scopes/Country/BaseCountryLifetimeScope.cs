using System;
using Configs;
using Configs.Tasks;
using Initializers;
using Meta.Quests.Interfaces;
using Meta.Quests.Services;
using Models;
using Monobehaviours.Buildings;
using Presenters;
using Providers;
using Services;
using Services.Tasks;
using Sirenix.OdinInspector;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Views;

namespace Scopes.Country
{
    public class BaseCountryLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenuPanel _mainMenuPanel;

        [SerializeField] private CountryConfig countryConfig;
        [SerializeField] private RegionConfig regionConfig;
        [SerializeField] private Transform regionParent;

        [Title("Tasks")]
        [FormerlySerializedAs("dailyQuestSettings")]
        [SerializeField] private TasksListSO tasksListSO;

        protected override void Configure(IContainerBuilder builder)
        {
            ResolveOptionalSceneReferences();

            RegisterComponentIfPresent(builder, _mainMenuPanel);

            var lifePopup = _mainMenuPanel != null ? _mainMenuPanel.LifePopup : null;
            RegisterComponentIfPresent(builder, lifePopup);

            var rewardPopup = _mainMenuPanel != null ? _mainMenuPanel.RewardPopup : null;
            RegisterComponentIfPresent(builder, rewardPopup);

            RegisterInstanceIfPresent(builder, countryConfig);
            RegisterInstanceIfPresent(builder, regionConfig);
            RegisterInstanceIfPresent(builder, tasksListSO);

            builder.Register<ITaskService, TaskService>(Lifetime.Singleton);

            var topBarPanel = (_mainMenuPanel != null && _mainMenuPanel.TopBarPanel != null)
                ? _mainMenuPanel.TopBarPanel
                : FindObjectOfType<TopBarPanel>(true);

            var settingsPanel = (_mainMenuPanel != null && _mainMenuPanel.SettingsPanel != null)
                ? _mainMenuPanel.SettingsPanel
                : FindObjectOfType<SettingsPanel>(true);

            if(settingsPanel != null)
            {
                var settingsButton = topBarPanel != null ? topBarPanel.SettingsButton : null;

                builder.Register<SettingsPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter(settingsPanel)
                    .WithParameter(settingsButton);
            }

            var taskPanel = (_mainMenuPanel != null && _mainMenuPanel.TaskPanel != null)
                ? _mainMenuPanel.TaskPanel
                : FindObjectOfType<TaskPanel>(true);

            if(taskPanel != null)
            {
                var taskButton = topBarPanel != null ? topBarPanel.TaskButton : null;

                builder.Register<TaskPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter(taskPanel)
                    .WithParameter(taskButton);
            }

            var menuNavPanel = (_mainMenuPanel != null && _mainMenuPanel.MenuNavPanel != null)
                ? _mainMenuPanel.MenuNavPanel
                : FindObjectOfType<MenuNavPanel>(true);
            RegisterComponentIfPresent(builder, menuNavPanel);

            var shopPanel = _mainMenuPanel != null ? _mainMenuPanel.ShopPanel : null;
            RegisterComponentIfPresent(builder, shopPanel);

            if(shopPanel != null)
            {
                Button navShopButton = null;
                var otherNavButtons = new System.Collections.Generic.List<Button>();

                if(menuNavPanel != null)
                {
                    navShopButton = menuNavPanel.ShopButton;
                    if(menuNavPanel.MenuButton != null) otherNavButtons.Add(menuNavPanel.MenuButton);
                    if(menuNavPanel.IslandButton != null) otherNavButtons.Add(menuNavPanel.IslandButton);
                }

                builder.Register<ShopPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter(shopPanel)
                    .WithParameter(navShopButton)
                    .WithParameter<System.Collections.Generic.IEnumerable<Button>>(otherNavButtons);
            }

            var islandPanel = _mainMenuPanel != null ? _mainMenuPanel.IslandPanel : null;
            if(islandPanel == null) islandPanel = FindObjectOfType<IslandPanel>(true);
            RegisterComponentIfPresent(builder, islandPanel);

            if(islandPanel != null && regionConfig != null)
            {
                Button navIslandButton = null;
                var otherNavButtons = new System.Collections.Generic.List<Button>();

                if(menuNavPanel != null)
                {
                    navIslandButton = menuNavPanel.IslandButton;
                    if(menuNavPanel.MenuButton != null) otherNavButtons.Add(menuNavPanel.MenuButton);
                    if(menuNavPanel.ShopButton != null) otherNavButtons.Add(menuNavPanel.ShopButton);
                }

                builder.Register<IslandPresenter>(Lifetime.Scoped)
                    .As<IInitializable>()
                    .WithParameter(islandPanel)
                    .WithParameter(navIslandButton)
                    .WithParameter<System.Collections.Generic.IEnumerable<Button>>(otherNavButtons);
            }

            if(topBarPanel != null)
            {
                if(topBarPanel.StarView != null)
                {
                    builder.Register<CurrencyPresenter>(Lifetime.Scoped)
                        .As<IInitializable>()
                        .WithParameter<ICurrencyView>(topBarPanel.StarView)
                        .WithParameter(CurrencyType.Star);
                }

                if(topBarPanel.GemView != null)
                {
                    builder.Register<CurrencyPresenter>(Lifetime.Scoped)
                        .As<IInitializable>()
                        .WithParameter<ICurrencyView>(topBarPanel.GemView)
                        .WithParameter(CurrencyType.Diamond);
                }

                if(topBarPanel.HealthBarView != null)
                {
                    builder.Register<LifePresenter>(Lifetime.Scoped)
                        .As<IInitializable>()
                        .WithParameter(topBarPanel.HealthBarView);
                }
            }

            builder.Register<DailyCardsPresenter>(Lifetime.Scoped)
                .As<IInitializable>();

            if(regionConfig != null)
            {
                builder.Register<RegionService>(Lifetime.Singleton)
                    .WithParameter(regionConfig)
                    .WithParameter(regionParent);

                builder.Register<RegionModel>(Lifetime.Singleton);
                builder.Register<RegionUpgradeService>(Lifetime.Singleton);
            }

            if(tasksListSO != null)
            {
                builder.Register<RewardService>(Lifetime.Singleton);

                builder.Register<IDailyQuestService, DailyQuestService>(Lifetime.Singleton);
                builder.Register<IQuestProgressTracker, DailyQuestService>(Lifetime.Singleton);
                builder.Register<IQuestDataStorage, PlayerPrefsQuestStorage>(Lifetime.Singleton);
                builder.Register<IQuestGenerator, QuestGenerator>(Lifetime.Singleton);
            }

            ConfigureCountry(builder);
        }

        private void ResolveOptionalSceneReferences()
        {
            if(_mainMenuPanel == null)
            {
                _mainMenuPanel = FindObjectOfType<MainMenuPanel>(true);
            }

            if(regionParent == null)
            {
                var worldObj = transform.Find("World");
                if(worldObj != null)
                {
                    regionParent = worldObj;
                }
            }
        }

        private static void RegisterComponentIfPresent<T>(IContainerBuilder builder, T component)
            where T : Component
        {
            if(component != null)
            {
                builder.RegisterComponent(component);
            }
        }

        private static void RegisterInstanceIfPresent<T>(IContainerBuilder builder, T instance)
            where T : class
        {
            if(instance != null)
            {
                builder.RegisterInstance(instance);
            }
        }

        protected virtual void ConfigureCountry(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<CountryInitializer>();
        }
    }
}
