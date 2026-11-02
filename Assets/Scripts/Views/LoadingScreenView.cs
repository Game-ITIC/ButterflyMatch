using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;

namespace Views
{
    public class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject screen;
        [SerializeField] private UIFillBar fillBar;
        [SerializeField] private UIFillBarMilestones milestones;
        [SerializeField] private TMP_Text percentText;
        [SerializeField, Min(0f)] private float hideFadeDuration = 0.3f;

        private CanvasGroup _screenGroup;
        private Tween _fadeTween;

        public void Show()
        {
            KillFade();
            screen.SetActive(true);
            var group = GetScreenGroup();
            group.alpha = 1f;
            group.blocksRaycasts = true;
            ResetProgress();
        }

        // Fades the screen out so the scene underneath is revealed instead of popping in.
        public void Hide()
        {
            KillFade();
            if (hideFadeDuration <= 0f || !screen.activeInHierarchy)
            {
                screen.SetActive(false);
                return;
            }

            var group = GetScreenGroup();
            group.blocksRaycasts = false;
            _fadeTween = group.DOFade(0f, hideFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(screen)
                .OnComplete(OnHideFadeComplete);
        }

        public void SetProgress(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            fillBar?.SetNormalizedFill(clamped);
            UpdatePercentText(clamped);
        }

        void OnHideFadeComplete()
        {
            _fadeTween = null;
            screen.SetActive(false);
            var group = GetScreenGroup();
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        void KillFade()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }

            _fadeTween = null;
        }

        CanvasGroup GetScreenGroup()
        {
            if (_screenGroup == null && !screen.TryGetComponent(out _screenGroup))
            {
                _screenGroup = screen.AddComponent<CanvasGroup>();
            }

            return _screenGroup;
        }

        void ResetProgress()
        {
            milestones?.ResetMilestones();
            fillBar?.SetNormalizedFill(0f, animated: false);
            UpdatePercentText(0f);
        }

        void UpdatePercentText(float normalized)
        {
            if (percentText == null)
            {
                return;
            }

            percentText.text = $"Loading {Mathf.RoundToInt(normalized * 100f)}%";
        }
    }
}
