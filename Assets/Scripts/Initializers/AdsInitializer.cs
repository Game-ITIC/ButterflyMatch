using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using Gley.EasyIAP;
using Interfaces;
using Itic.Scopes;
using Itic.Services;
using JetBrains.Annotations;
using Services;
using UnityEngine;
using VContainer.Unity;
using Views;
using Debug = UnityEngine.Debug;

namespace Initializers
{
    [UsedImplicitly]
    public class AdsInitializer : IPreload
    {
        private readonly IronSourceInitializer _ironSourceInitializer;
        private readonly InternetChecker _internetChecker;
        private readonly InternetState _internetState;
        private readonly ScreenService _screenService;
        private readonly Services.InApp.InAppGrantService _inAppGrantService;

#if UNITY_EDITOR
        private const int INTERNET_CHECK_TIMEOUT = 3000;
        private const int IRONSOURCE_INIT_TIMEOUT = 3000;
        private const int IAP_INIT_TIMEOUT = 3000;
#else
        private const int INTERNET_CHECK_TIMEOUT = 5000;
        private const int IRONSOURCE_INIT_TIMEOUT = 8000;
        private const int IAP_INIT_TIMEOUT = 6000;
#endif

        public AdsInitializer(
            IronSourceInitializer ironSourceInitializer,
            InternetChecker internetChecker,
            SceneLoader sceneLoader,
            InternetState internetState,
            ScreenService screenService,
            Services.InApp.InAppGrantService inAppGrantService
        )
        {
            _ironSourceInitializer = ironSourceInitializer;
            _internetChecker = internetChecker;
            _internetState = internetState;
            _screenService = screenService;
            _inAppGrantService = inAppGrantService;
        }

        public async UniTask Warmup()
        {
            var sw = Stopwatch.StartNew();
            Debug.Log("[AdsInitializer] Starting AdsInitializer.Warmup()...");

            // Must be attached before the store initializes: purchases re-delivered on startup are granted here.
            _inAppGrantService.Attach();

            await _screenService.ShowLoadingScreenAsync();
            _screenService.SetLoadingProgress(0f);

            try
            {
                _screenService.SetLoadingProgress(0.08f);
                var netSw = Stopwatch.StartNew();
                var hasInternetAccess = await CheckInternetWithTimeout();
                await UniTask.SwitchToMainThread();
                _internetState.HasInternet = hasInternetAccess;
                Debug.Log($"[AdsInitializer] Internet check finished in {netSw.ElapsedMilliseconds} ms. HasInternet: {hasInternetAccess}");
                _screenService.SetLoadingProgress(0.18f);

                if (hasInternetAccess)
                {
                    var ironSw = Stopwatch.StartNew();
                    var ironResult = await WaitForIronSourceWithTimeout();
                    await UniTask.SwitchToMainThread();
                    Debug.Log($"[AdsInitializer] IronSource initialization finished in {ironSw.ElapsedMilliseconds} ms. Result: {ironResult}");
                    _screenService.SetLoadingProgress(0.26f);

                    var iapSw = Stopwatch.StartNew();
                    await InitializeIAPWithTimeout();
                    await UniTask.SwitchToMainThread();
                    Debug.Log($"[AdsInitializer] EasyIAP initialization finished in {iapSw.ElapsedMilliseconds} ms. HasRemoveAds: {_internetState.HasRemoveAds}");
                    _screenService.SetLoadingProgress(0.32f);
                }
                else
                {
                    Debug.LogWarning("[AdsInitializer] Skipping IronSource & EasyIAP init because no internet access was detected.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdsInitializer] Warmup failed with exception ({sw.ElapsedMilliseconds} ms): {ex}");
                _internetState.HasInternet = false;
                _internetState.HasRemoveAds = _inAppGrantService.HasRemoveAds;
            }

            await UniTask.SwitchToMainThread();
            _screenService.SetLoadingProgress(0.35f);
            Debug.Log($"[AdsInitializer] Warmup complete in {sw.ElapsedMilliseconds} ms.");
        }

        private async UniTask<bool> CheckInternetWithTimeout()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(INTERNET_CHECK_TIMEOUT);
                var result = await _internetChecker.HasInternetAccess().AttachExternalCancellation(cts.Token);
                await UniTask.SwitchToMainThread();
                return result;
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogWarning($"[AdsInitializer] CheckInternet timed out after {sw.ElapsedMilliseconds} ms (limit: {INTERNET_CHECK_TIMEOUT} ms).");
                return false;
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogWarning($"[AdsInitializer] CheckInternet failed ({sw.ElapsedMilliseconds} ms): {ex.Message}");
                return false;
            }
        }

        private async UniTask<bool> WaitForIronSourceWithTimeout()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(IRONSOURCE_INIT_TIMEOUT);
                var result = await _ironSourceInitializer.WaitForIronSourceInit().AttachExternalCancellation(cts.Token);
                await UniTask.SwitchToMainThread();
                return result;
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogWarning($"[AdsInitializer] WaitForIronSource timed out after {sw.ElapsedMilliseconds} ms (limit: {IRONSOURCE_INIT_TIMEOUT} ms).");
                return false;
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogError($"[AdsInitializer] WaitForIronSource failed ({sw.ElapsedMilliseconds} ms): {ex}");
                return false;
            }
        }

        private async UniTask InitializeIAPWithTimeout()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(IAP_INIT_TIMEOUT);
                var completionSource = new UniTaskCompletionSource();

                cts.Token.Register(() =>
                {
                    if (!completionSource.Task.Status.IsCompleted())
                    {
                        completionSource.TrySetCanceled();
                    }
                });

                API.Initialize((status, message) =>
                {
                    try
                    {
                        Debug.Log($"[AdsInitializer] EasyIAP callback: status={status}, message='{message}' (took {sw.ElapsedMilliseconds} ms)");
                        if (status == IAPOperationStatus.Success)
                        {
                            if(API.IsActive(ShopProductNames.RemoveAds))
                            {
                                _inAppGrantService.SetRemoveAds();
                            }

                            _internetState.HasRemoveAds = _inAppGrantService.HasRemoveAds;
                        }
                        else
                        {
                            Debug.LogWarning($"[AdsInitializer] EasyIAP init non-success status: {status} ({message})");
                            _internetState.HasRemoveAds = _inAppGrantService.HasRemoveAds;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AdsInitializer] Error processing EasyIAP callback: {ex}");
                        _internetState.HasRemoveAds = _inAppGrantService.HasRemoveAds;
                    }
                    finally
                    {
                        completionSource.TrySetResult();
                    }
                });

                await completionSource.Task;
                await UniTask.SwitchToMainThread();
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogWarning($"[AdsInitializer] InitializeIAP timed out after {sw.ElapsedMilliseconds} ms (limit: {IAP_INIT_TIMEOUT} ms). Continuing without IAP cache...");
                _internetState.HasRemoveAds = _inAppGrantService.HasRemoveAds;
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogError($"[AdsInitializer] InitializeIAP failed ({sw.ElapsedMilliseconds} ms): {ex}");
                _internetState.HasRemoveAds = _inAppGrantService.HasRemoveAds;
            }
        }
    }
}
