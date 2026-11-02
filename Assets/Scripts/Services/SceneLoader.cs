using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Data;
using Itic.Services;
using Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Itic.Scopes
{
    public class SceneLoader
    {
        public event Action OnSceneLoaded = delegate { };

        private const float LoadProgressStart = 0.35f;
        private const float LoadProgressEnd = 0.98f;

        private readonly ScreenService _screenService;
        private readonly SceneModel _sceneModel;

        public SceneLoader(
            ScreenService screenService,
            SceneModel sceneModel
        )
        {
            _screenService = screenService;
            _sceneModel = sceneModel;
        }

        public async UniTask LoadMenuAsync()
        {
            await LoadSceneAsync("MainMenu");
        }

        public async UniTask LoadGameAsync()
        {
            await LoadSceneAsync(3);
        }

        public async UniTask LoadMapAsync()
        {
            await LoadSceneAsync(5);
        }

        public async UniTask LoadAsiaRegionAsync()
        {
            await LoadSceneAsync(6);
        }

        public async UniTask LoadEgyptRegionAsync()
        {
            await LoadSceneAsync(7);
        }

        public async UniTask LoadRegionScene(CountryEnum country)
        {
            switch (country)
            {
                case CountryEnum.Asia: await LoadAsiaRegionAsync(); break;
                case CountryEnum.Egypt: await LoadEgyptRegionAsync(); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(country), country, null);
            }
        }

        public async UniTask LoadLastSceneAsync()
        {
            if (_sceneModel.LastCountryConfig == null)
            {
                await LoadAsiaRegionAsync();
            }
            else
            {
                await LoadRegionScene(_sceneModel.LastCountryConfig.countryId);
            }
        }

        private bool _isLoading;

        /// <summary>True while a scene transition is running; further load requests are ignored.</summary>
        public bool IsLoading => _isLoading;

        private async UniTask LoadSceneAsync(int index)
        {
            if(_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignoring load of scene index {index}: another scene load is in progress.");
                return;
            }

            _isLoading = true;
            try
            {
                await LoadSceneInternalAsync(index);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async UniTask LoadSceneAsync(string sceneName)
        {
            if(_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignoring load of scene '{sceneName}': another scene load is in progress.");
                return;
            }

            _isLoading = true;
            try
            {
                await LoadSceneInternalAsync(sceneName);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async UniTask LoadSceneInternalAsync(int index)
        {
            var sw = Stopwatch.StartNew();
            Debug.Log($"[SceneLoader] === STEP 3/3: Starting load scene by build index '{index}' ===");

            await EnsureLoadingScreenVisibleAsync();
            IronSourceManager.Instance?.SetBannerAllowed(false);

            var activeScene = SceneManager.GetActiveScene();
            Debug.Log($"[SceneLoader] Unloading current scene '{activeScene.name}'...");
            var unloadSw = Stopwatch.StartNew();
            var unloadSceneAsyncTask = SceneManager.UnloadSceneAsync(activeScene);

            if (unloadSceneAsyncTask != null)
            {
                while (!unloadSceneAsyncTask.isDone)
                {
                    _screenService.SetLoadingProgress(MapUnloadProgress(unloadSceneAsyncTask.progress));
                    await UniTask.Yield();
                }
            }
            Debug.Log($"[SceneLoader] Unload completed in {unloadSw.ElapsedMilliseconds} ms");

            Debug.Log($"[SceneLoader] Loading scene index {index} (Additive)...");
            var loadSw = Stopwatch.StartNew();
            var sceneLoadingOperation = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);

            while (sceneLoadingOperation is { isDone: false })
            {
                _screenService.SetLoadingProgress(MapLoadProgress(sceneLoadingOperation.progress));
                await UniTask.Yield();
            }

            var scene = SceneManager.GetSceneByBuildIndex(index);
            SceneManager.SetActiveScene(scene);
            Debug.Log($"[SceneLoader] Scene '{scene.name}' loaded and activated in {loadSw.ElapsedMilliseconds} ms");

            await InstallSceneScopeAsync(scene);

            _screenService.SetLoadingProgress(LoadProgressEnd);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            OnSceneLoaded?.Invoke();
            await _screenService.HideLoadingScreenAsync();
            UpdateBannerForScene(scene);
            Debug.Log($"[SceneLoader] === STEP 3/3 Complete: Scene '{scene.name}' loaded in total {sw.ElapsedMilliseconds} ms ===");
        }

        private async UniTask LoadSceneInternalAsync(string sceneName)
        {
            var sw = Stopwatch.StartNew();
            Debug.Log($"[SceneLoader] === STEP 3/3: Starting load scene '{sceneName}' ===");

            await EnsureLoadingScreenVisibleAsync();
            IronSourceManager.Instance?.SetBannerAllowed(false);

            var activeScene = SceneManager.GetActiveScene();
            Debug.Log($"[SceneLoader] Unloading current scene '{activeScene.name}'...");
            var unloadSw = Stopwatch.StartNew();
            var unloadSceneAsyncTask = SceneManager.UnloadSceneAsync(activeScene);

            if (unloadSceneAsyncTask != null)
            {
                while (!unloadSceneAsyncTask.isDone)
                {
                    _screenService.SetLoadingProgress(MapUnloadProgress(unloadSceneAsyncTask.progress));
                    await UniTask.Yield();
                }
            }
            Debug.Log($"[SceneLoader] Unload completed in {unloadSw.ElapsedMilliseconds} ms");

            Debug.Log($"[SceneLoader] Loading scene '{sceneName}' (Additive)...");
            var loadSw = Stopwatch.StartNew();
            var sceneLoadingOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            while (sceneLoadingOperation is { isDone: false })
            {
                _screenService.SetLoadingProgress(MapLoadProgress(sceneLoadingOperation.progress));
                await UniTask.Yield();
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(scene);
            Debug.Log($"[SceneLoader] Scene '{sceneName}' loaded and activated in {loadSw.ElapsedMilliseconds} ms");

            await InstallSceneScopeAsync(scene);

            _screenService.SetLoadingProgress(LoadProgressEnd);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            OnSceneLoaded?.Invoke();
            await _screenService.HideLoadingScreenAsync();
            UpdateBannerForScene(scene);
            Debug.Log($"[SceneLoader] === STEP 3/3 Complete: Scene '{sceneName}' loaded in total {sw.ElapsedMilliseconds} ms ===");
        }

        /// <summary>GDD 8.3: the banner is allowed only in the main menu.</summary>
        private static void UpdateBannerForScene(Scene scene)
        {
            IronSourceManager.Instance?.SetBannerAllowed(scene.name == "MainMenu");
        }

        private async UniTask InstallSceneScopeAsync(Scene scene)
        {
            var sw = Stopwatch.StartNew();
            Debug.Log($"[SceneLoader] Searching for ScopeInstaller in scene '{scene.name}'...");
            var installer = FindComponentInScene<ScopeInstaller>(scene);

            if (installer != null)
            {
                _screenService.SetLoadingProgress(0.92f);
                Debug.Log($"[SceneLoader] ScopeInstaller found on GameObject '{installer.gameObject.name}'. Calling InstallScopeAsync()...");
                await installer.InstallScopeAsync();
                Debug.Log($"[SceneLoader] ScopeInstaller.InstallScopeAsync() completed in {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.LogError($"[SceneLoader] CRITICAL: No ScopeInstaller found in scene '{scene.name}'! VContainer scope not built.");
            }
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var root in rootGameObjects)
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private async UniTask EnsureLoadingScreenVisibleAsync()
        {
            if (!_screenService.IsLoadingScreenVisible)
            {
                await _screenService.ShowLoadingScreenAsync();
                _screenService.SetLoadingProgress(LoadProgressStart);
            }
        }

        private static float MapUnloadProgress(float progress)
        {
            return LoadProgressStart + Mathf.Clamp01(progress) * 0.08f;
        }

        private static float MapLoadProgress(float progress)
        {
            return LoadProgressStart + 0.08f + Mathf.Clamp01(progress) * (LoadProgressEnd - LoadProgressStart - 0.08f);
        }
    }
}
