#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Itic.Scopes;
using JuiceFresh.Scripts;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Utils.DebugTools
{
    /// <summary>Finds game services and scene state for the cheats; the Need* variants toast when something is missing.</summary>
    internal static class CheatServices
    {
        private const string LevelOnly = "Только во время уровня";

        public static int OpenLevel => Mathf.Max(1, PlayerPrefs.GetInt("OpenLevel", 1));

        public static T Need<T>() where T : class
        {
            var service = Find<T>();
            if(service == null)
            {
                Toast.Show(typeof(T).Name + " пока недоступен");
            }

            return service;
        }

        // Services live in per-scene VContainer scopes; only called from clicks and the 2 Hz status line.
        public static T Find<T>() where T : class
        {
            foreach(var scope in UnityEngine.Object.FindObjectsByType<LifetimeScope>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(scope == null || scope.Container == null)
                {
                    continue;
                }

                try
                {
                    if(scope.Container.Resolve(typeof(T)) is T service)
                    {
                        return service;
                    }
                }
                catch(VContainerException)
                {
                    // Not registered in this scope, try the next one.
                }
            }

            return null;
        }

        public static SceneLoader NeedIdleLoader()
        {
            var loader = Need<SceneLoader>();
            if(loader != null && loader.IsLoading)
            {
                Toast.Show("Сцена ещё загружается");
                return null;
            }

            return loader;
        }

        public static LevelManager NeedPlayingLevel()
        {
            var level = LevelManager.THIS;
            if(level == null || level.gameStatus != GameState.Playing)
            {
                Toast.Show(LevelOnly);
                return null;
            }

            return level;
        }
    }
}
#endif
