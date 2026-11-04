using TMPro;
using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Legacy gems shop: fills its four packs from LevelManager.gemsProducts.</summary>
    public sealed class GemsShopPopup : PopupBehaviour
    {
        private const int PackCount = 4;

        public GemsShopPopup(AnimationManager popup) : base(popup, closesOnEscape: true) { }

        public override void OnShown()
        {
            for (int i = 1; i <= PackCount; i++)
            {
                GemProduct product = Level.gemsProducts[i - 1];
                Root.Find("Image/Pack" + i + "/Count").GetComponent<TMP_Text>().text = "" + product.count;
                Root.Find("Image/Pack" + i + "/Buy/Price").GetComponent<TMP_Text>().text = "" + product.price;
            }
        }

        /// <summary>
        /// A pack button was pressed: remembers its gem count for the purchase callback
        /// (<see cref="InitScript.waitedPurchaseGems"/>). Only Pack1..Pack4 are packs.
        /// </summary>
        public static void RememberPackGems(GameObject pack)
        {
            if (!IsPack(pack.name))
                return;

            string count = pack.transform.Find("Count").GetComponent<TMP_Text>().text;
            InitScript.waitedPurchaseGems = int.Parse(count.Replace("x ", ""));
        }

        private static bool IsPack(string objectName)
        {
            for (int i = 1; i <= PackCount; i++)
            {
                if (objectName == "Pack" + i)
                    return true;
            }

            return false;
        }
    }
}
