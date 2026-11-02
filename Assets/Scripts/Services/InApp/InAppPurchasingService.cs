using System;
using Cysharp.Threading.Tasks;
using Gley.EasyIAP;
using UnityEngine;

namespace Services.InApp
{
    /// <summary>
    /// UI-facing purchase entry point. The reward itself is granted by <see cref="InAppGrantService"/>
    /// from ProcessPurchase; this only reports whether the tapped purchase succeeded.
    /// </summary>
    public static class InAppPurchasingService
    {
        private static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(10);
        private static bool _inProgress;

        public static bool IsPurchaseInProgress => _inProgress;

        public static async UniTask<bool> TryBuyConsumableAsync(ShopProductNames shopProduct)
        {
            if(_inProgress)
            {
                Debug.LogWarning($"[InAppPurchasingService] Purchase already in progress, ignoring {shopProduct}.");
                return false;
            }

            _inProgress = true;

            try
            {
                if(!API.IsInitialized() && !await TryInitializeAsync())
                {
                    Debug.LogWarning($"[InAppPurchasingService] Store is not initialized, cannot buy {shopProduct}.");
                    return false;
                }

                var task = new UniTaskCompletionSource<bool>();
                var expectedName = shopProduct.ToString();

                API.BuyProduct(shopProduct, (status, message, product) =>
                                            {
                                                // A different purchase (re-delivered / pending) can arrive while this one is open.
                                                if(status == IAPOperationStatus.Success && product != null && product.productName != expectedName)
                                                {
                                                    return;
                                                }

                                                if(status != IAPOperationStatus.Success)
                                                {
                                                    Debug.LogWarning($"[InAppPurchasingService] {shopProduct} failed: {message}");
                                                }

                                                task.TrySetResult(status == IAPOperationStatus.Success);
                                            });

                return await task.Task;
            }
            finally
            {
                _inProgress = false;
            }
        }

        /// <summary>Retry store init (e.g. there was no internet at app start).</summary>
        public static async UniTask<bool> TryInitializeAsync()
        {
            if(API.IsInitialized())
            {
                return true;
            }

            var task = new UniTaskCompletionSource<bool>();
            API.Initialize((status, message) => task.TrySetResult(status == IAPOperationStatus.Success));

            var (isTimeout, success) = await task.Task.TimeoutWithoutException(InitTimeout);
            return !isTimeout && success && API.IsInitialized();
        }
    }
}
