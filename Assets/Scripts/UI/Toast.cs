using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Short message at the bottom of the screen ("Ad is not available", purchase errors...).
    /// Created lazily on its own overlay canvas and kept across scenes.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private const float ShowSeconds = 2.2f;

        private static Toast _instance;

        private CanvasGroup _group;
        private TextMeshProUGUI _text;
        private Sequence _sequence;

        public static void Show(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return;
            }

            if(_instance == null)
            {
                _instance = Create();
            }

            _instance.Play(message);
        }

        private static Toast Create()
        {
            var root = new GameObject("Toast", typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(root);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(root.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.1f, 0.18f);
            panelRect.anchorMax = new Vector2(0.9f, 0.18f);
            panelRect.sizeDelta = new Vector2(0, 150);

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.12f, 0.08f, 0.2f, 0.88f);
            image.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30, 10);
            textRect.offsetMax = new Vector2(-30, -10);

            var toast = root.AddComponent<Toast>();
            toast._group = panel.GetComponent<CanvasGroup>();
            toast._group.alpha = 0f;
            toast._group.blocksRaycasts = false;
            toast._text = textGo.GetComponent<TextMeshProUGUI>();
            toast._text.alignment = TextAlignmentOptions.Center;
            toast._text.fontSize = 52;
            toast._text.enableAutoSizing = true;
            toast._text.fontSizeMin = 28;
            toast._text.fontSizeMax = 52;
            toast._text.color = Color.white;
            toast._text.raycastTarget = false;
            return toast;
        }

        private void Play(string message)
        {
            _text.text = message;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(_group.DOFade(1f, 0.15f))
                .AppendInterval(ShowSeconds)
                .Append(_group.DOFade(0f, 0.3f))
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            if(_instance == this)
            {
                _instance = null;
            }
        }
    }
}
