using System.Collections.Generic;
using Initializers;
using Models;
using Monobehaviours;
using Providers;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Views;

namespace Scopes
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameEventDispatcher gameEventDispatcher;
        [SerializeField] private Button noAds;
        [SerializeField] private GameCompleteView gameCompleteView;
        [SerializeField] private GamePauseView gamePauseView;
        [SerializeField] private GameOverView gameOverView;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private GameProvider gameProvider;
        [SerializeField] private BoostShopView boostShopView;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(gameCompleteView);
            builder.RegisterComponent(gamePauseView);
            builder.RegisterComponent(gameOverView);
            builder.RegisterComponent(boostShopView);
            builder.RegisterComponent(gameProvider);
            builder.RegisterComponent(levelManager);
            
            builder.RegisterComponent<IGameEvents>(gameEventDispatcher).AsSelf();

            // Same PlayerPrefs storage as the menu's TaskService; without a config it never regenerates tasks,
            // it only adds level progress (the menu refreshes them daily).
            builder.Register<Services.Tasks.ITaskService>(_ => new Services.Tasks.TaskService(), Lifetime.Singleton);

            builder.RegisterEntryPoint<GameInitializer>()
                .WithParameter("noAds", noAds);
        }
    }
}