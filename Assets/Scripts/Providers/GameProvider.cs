using UI;
using UnityEngine;

namespace Providers
{
    public class GameProvider : MonoBehaviour
    {
        [field: SerializeField] public RewardPopup RewardPopup { get; private set; }
        [field: SerializeField] public Sprite DiamondSprite { get; private set; }
        [field: SerializeField] public Sprite ExtraStepSprite { get; private set; }
    }
}