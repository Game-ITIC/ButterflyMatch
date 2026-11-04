using Cysharp.Threading.Tasks;
using Interfaces;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class InAppView : MonoBehaviour, IPreload
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform buttonsParent;
        [SerializeField] private bool animatePanelTransitions = true;
        [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
        [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
        [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
        [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
        [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

        public Transform ButtonsParent => buttonsParent;

        public event System.Action<bool> VisibilityChanged;

        private MenuPager _pager;
        private bool _pagerResolved;

        // Inside the main menu pager the shop is a swipeable screen instead of a popup.
        private MenuPager Pager
        {
            get
            {
                if(!_pagerResolved)
                {
                    _pagerResolved = true;
                    _pager = GetComponentInParent<MenuPager>(true);
                    if(_pager != null && !_pager.Contains(this)) _pager = null;
                }

                return _pager;
            }
        }

        public void Show()
        {
            if(Pager != null)
            {
                Pager.Open(this);
                return;
            }

            var opening = !gameObject.activeSelf;
            VisibilityChanged?.Invoke(true);
            CurvedUIPanelAnimator.Show(
                gameObject,
                animatePanelTransitions ? panelOpenDuration : 0f,
                panelOpenCurve,
                panelFadeCurve);

            // Product cards are spawned after the menu feel install, so they get press feedback here.
            UIButtonFeel.InstallInChildren(buttonsParent);
            if(opening && animatePanelTransitions)
            {
                UIFeel.StaggerChildren(buttonsParent);
            }
        }

        public void Hide()
        {
            if(Pager != null)
            {
                Pager.CloseIfCurrent(this);
                return;
            }

            VisibilityChanged?.Invoke(false);
            CurvedUIPanelAnimator.Hide(
                gameObject,
                animatePanelTransitions ? panelCloseDuration : 0f,
                panelCloseCurve,
                panelFadeCurve);
        }

        public async UniTask Warmup()
        {
            if(closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
                // A screen is left by swiping or the nav bar, not by a close button.
                if(Pager != null) closeButton.gameObject.SetActive(false);
            }

            await UniTask.Yield();
        }
    }
}

