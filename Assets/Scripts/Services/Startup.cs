using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Debug = UnityEngine.Debug;

namespace Itic.Services
{
    public class Startup : IAsyncStartable
    {
        public async UniTask StartAsync(CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            Debug.Log("[Startup] === STEP 1/3: Starting Bootstrap Startup ===");

#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
            if (Application.isMobilePlatform)
            {
                QualitySettings.SetQualityLevel(0, true);
                Application.focusChanged += focused => Application.targetFrameRate = focused ? 30 : 5;
            }
#else
            Application.targetFrameRate = 120;
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            try
            {
                await InitializePlugins();
                Debug.Log($"[Startup] DOTween initialized in {sw.ElapsedMilliseconds} ms");

                var loadSceneSw = Stopwatch.StartNew();
                await SceneManager.LoadSceneAsync("Services", LoadSceneMode.Additive)
                    .ToUniTask(cancellationToken: token);
                Debug.Log($"[Startup] 'Services' scene loaded additively in {loadSceneSw.ElapsedMilliseconds} ms");

                var servicesScene = SceneManager.GetSceneByName("Services");
                var scope = FindComponentInScene<LifetimeScope>(servicesScene);
                if (scope != null)
                {
                    var buildSw = Stopwatch.StartNew();
                    scope.Build();
                    Debug.Log($"[Startup] ServicesLifetimeScope built in {buildSw.ElapsedMilliseconds} ms");
                }
                else
                {
                    Debug.LogError("[Startup] CRITICAL: LifetimeScope not found in Services scene!");
                }

                Debug.Log($"[Startup] === STEP 1/3 Complete: Bootstrap finished in {sw.ElapsedMilliseconds} ms ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Startup] CRITICAL ERROR during Bootstrap Startup ({sw.ElapsedMilliseconds} ms): {ex}");
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

        private async UniTask InitializePlugins()
        {
            DOTween.Init();
            // A full board reveal creates roughly 280 concurrent tweens.
            DOTween.SetTweensCapacity(512, 128);
            await UniTask.CompletedTask;
        }
    }
}
