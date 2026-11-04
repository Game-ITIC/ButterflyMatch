using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.Popups
{
    /// <summary>"Out of moves" offer shown before the level is lost.</summary>
    public sealed class PreFailedPopup : PopupBehaviour
    {
        public PreFailedPopup(AnimationManager popup) : base(popup) { }

        public override void OnShown()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[0]);
            Root.Find("Buy").GetComponent<Button>().interactable = true;
            Popup.GetComponent<Animation>().Play();
        }
    }
}
