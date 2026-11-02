using R3;
using Services;

namespace Models
{
    public class AdEventModel
    {
        public Subject<Unit> OnRewardedReward = new();
        public Subject<Unit> OnRewardGranted = new();
        public Subject<Unit> OnRewardedFailed = new();
        public Subject<Unit> OnRewardedClosed = new();

        public void InvokeOnRewardedReward()
        {
            OnRewardedReward.OnNext(Unit.Default);
        }
        
        public void InvokeOnRewardedGranted()
        {
            OnRewardGranted.OnNext(Unit.Default);
        }
        
        public void InvokeOnRewardedFailed()
        {
            OnRewardedFailed.OnNext(Unit.Default);
        }

        public void InvokeOnRewardedClosed()
        {
            OnRewardedClosed.OnNext(Unit.Default);
        }
    }
}
