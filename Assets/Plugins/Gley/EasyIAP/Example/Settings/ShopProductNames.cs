#if GLEY_IAP_IOS || GLEY_IAP_GOOGLEPLAY || GLEY_IAP_AMAZON || GLEY_IAP_MACOS || GLEY_IAP_WINDOWS
#define GleyIAPEnabled
#endif
#if !GleyIAPEnabled
namespace Gley.EasyIAP
{
	public enum ShopProductNames
	{
		RemoveAds,
		CrystalsStarter,
		CrystalsSmall,
		CrystalsMedium,
		CrystalsBig,
		CrystalsHuge,
		CrystalsRoyal,
		StarterBundle,
		BeginnerBundle,
		BuilderBundle,
		MasterBundle,
		NeedMovesOffer,
		OutOfLivesOffer,
		HardLevelOffer,
		DailyGemsDiscount,
		DailyBoostersOffer,
		DailyUnlimitedLivesOffer,
		DailyMovesOffer,
	}
}
#endif
