using R3;

namespace Models
{
    public class BoosterModel
    {
        public ReactiveProperty<int> Count { get; private set; } = new(0);
        public BoostType Type { get; private set; }

        public BoosterModel(BoostType type)
        {
            Type = type;
        }
    
        public void Add(int amount)
        {
            Count.Value += amount;
        }

        public void Use()
        {
            if (HasBooster())
            {
                Count.Value--;
                LevelSessionStats.BoostersUsed++;
            }
        }

        public bool HasBooster()
        {
            return Count.Value > 0;
        }
    }
}