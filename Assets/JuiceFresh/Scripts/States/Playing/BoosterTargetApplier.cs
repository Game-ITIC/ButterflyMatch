using UnityEngine;

namespace JuiceFresh.States.Playing
{
    /// <summary>
    /// The board boosters (bomb, shovel, energy): once one is armed, the next tapped piece receives it.
    /// The booster's effect prefab (BoostAnimation) does the actual destruction.
    /// </summary>
    public sealed class BoosterTargetApplier
    {
        private const int EffectSortingOrder = 5;

        private readonly LevelManager _levelManager;

        public BoosterTargetApplier(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        /// <summary>True while a board booster is armed: taps go to the booster, not to the chain.</summary>
        public bool IsBoardBoosterArmed => IsBoardBooster(_levelManager.ActivatedBoost?.type);

        /// <summary>Applies the armed booster to the piece in <paramref name="square"/>.</summary>
        public void TryApply(Square square)
        {
            if (square == null)
                return;

            Item item = square.item;
            if (item == null || item.currentType == ItemsTypes.INGREDIENT)
                return;

            BoostType? boostType = _levelManager.ActivatedBoost?.type;
            if (!IsBoardBooster(boostType))
                return;

            PlayEffect(boostType.Value, square);

            var boosterModel = _levelManager.BoostersProvider.BoostersModels.Find(v => v.Type == boostType);
            boosterModel.Use();
            _levelManager.BoostersProvider.Save();
        }

        private void PlayEffect(BoostType boostType, Square square)
        {
            if (boostType != BoostType.Shovel)
                SoundBase.Instance.PlaySound(SoundBase.Instance.boostBomb);

            _levelManager.DragBlocked = true;

            GameObject effect = Object.Instantiate(Resources.Load(EffectPrefabPath(boostType)),
                square.transform.position,
                square.transform.rotation) as GameObject;
            effect.GetComponent<SpriteRenderer>().sortingOrder = EffectSortingOrder;
            effect.GetComponent<BoostAnimation>().square = square;

            // The effect finishes the booster; until then it waits here.
            _levelManager.waitingBoost = _levelManager.ActivatedBoost;
            _levelManager.ActivatedBoost = null;
        }

        private static bool IsBoardBooster(BoostType? type) =>
            type == BoostType.Bomb || type == BoostType.Shovel || type == BoostType.Energy;

        private static string EffectPrefabPath(BoostType type)
        {
            switch (type)
            {
                case BoostType.Bomb:
                    return "Prefabs/Effects/bomb";
                case BoostType.Shovel:
                    return "Prefabs/Effects/shovel";
                default:
                    return "Prefabs/Effects/energy";
            }
        }
    }
}
