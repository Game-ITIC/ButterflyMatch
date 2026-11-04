using Systems.CurrencySystem;

namespace JuiceFresh.Popups
{
    /// <summary>Buying a continue after running out of moves / time.</summary>
    public static class LevelContinue
    {
        /// <summary>Pays LevelManager.FailedCost diamonds. False when the player can't afford it.</summary>
        public static bool TryPay(LevelManager level)
        {
            var currencyService = level.CurrencyService;
            if (currencyService == null || !(currencyService.GetCurrency(CurrencyType.Diamond)?.Value >= level.FailedCost))
                return false;

            currencyService.SpendCurrency(CurrencyType.Diamond, level.FailedCost);
            return true;
        }

        /// <summary>Gives the extra moves (or seconds) and puts back the bombs a bomb level needs.</summary>
        public static void GrantExtraLimit(LevelManager level)
        {
            if (level.limitType == LIMIT.MOVES)
                level.Limit += level.ExtraFailedMoves;
            else
                level.Limit += level.ExtraFailedSecs;

            if (level.target == Target.BOMBS)
                level.RechargeBombs();
        }
    }
}
