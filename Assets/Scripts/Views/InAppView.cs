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

        public void Show()
        {
            var opening = !gameObject.activeSelf;
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
            }

            await UniTask.Yield();
        }
    }
}

