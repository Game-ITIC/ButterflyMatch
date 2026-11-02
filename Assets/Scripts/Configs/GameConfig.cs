using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = nameof(GameConfig), menuName = nameof(Configs) + "/" + nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        public int MaxLifes = 5;
        public float LifeRegenTimeMinutes = 20f;

        // Coins (Cash) were removed (Docs/GDD.md 3): a win gives only stars, the win-screen ad gives gems.
        [Tooltip("Gems for the optional rewarded ad on the win screen (GDD 8.1 win_bonus).")]
        public int WinAdDiamondReward = 20;

        public int AmountOfTimesToPlayToShowVideo = 3;
    }
}
