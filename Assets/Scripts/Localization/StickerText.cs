using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLocalization
{
    /// <summary>
    /// Localized text that replaces a baked English "sticker" image (PAUSE, GAME OVER, ...).
    /// The source Image stays (disabled) so existing tweens keep working; its alpha is mirrored here.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class StickerText : MonoBehaviour
    {
        [SerializeField] private Image _source;

        private TMP_Text _text;
        private float _lastAlpha = -1f;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void LateUpdate()
        {
            if (_source == null)
                return;

            var alpha = _source.color.a;
            if (Mathf.Approximately(alpha, _lastAlpha))
                return;

            _lastAlpha = alpha;
            _text.alpha = alpha;
        }
    }
}
