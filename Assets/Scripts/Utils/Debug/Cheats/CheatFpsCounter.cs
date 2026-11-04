#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;

namespace Utils.DebugTools
{
    /// <summary>The optional green FPS label in the top-left corner.</summary>
    internal sealed class CheatFpsCounter
    {
        private const float RefreshSeconds = 0.5f;

        private readonly TextMeshProUGUI _label;
        private float _time;
        private int _frames;

        public CheatFpsCounter(RectTransform safeRoot)
        {
            var rect = CheatUi.NewRect("FPS", safeRoot);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -8f);
            rect.sizeDelta = new Vector2(240f, 56f);
            _label = CheatUi.AddText(rect, "FPS", 36f, Color.green);
            _label.alignment = TextAlignmentOptions.Left;
            rect.gameObject.SetActive(false);
        }

        public void Tick()
        {
            if(!_label.gameObject.activeSelf)
            {
                return;
            }

            _frames++;
            _time += Time.unscaledDeltaTime;
            if(_time >= RefreshSeconds)
            {
                _label.text = "FPS " + Mathf.RoundToInt(_frames / _time);
                _frames = 0;
                _time = 0f;
            }
        }

        public void Toggle()
        {
            _label.gameObject.SetActive(!_label.gameObject.activeSelf);
            _label.text = "FPS";
            _frames = 0;
            _time = 0f;
        }
    }
}
#endif
