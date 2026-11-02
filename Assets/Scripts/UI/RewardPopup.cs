using System;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RewardPopup : MonoBehaviour
    {
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private Button okButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text description;
        [SerializeField] private bool animatePanelTransitions = true;
        [SerializeField, Min(0f)] private float panelOpenDuration = 0.3f;
        [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
        [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
        [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
        [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

        private void Start()
        {
            if(okButton != null)
            {
                okButton.onClick.RemoveListener(Hide);
                okButton.onClick.AddListener(Hide);
            }
        }

        public void Init(RewardInfo rewardInfo)
        {
            if(iconImage != null)
            {
                iconImage.sprite = rewardInfo.Icon;
            }

            if(description != null)
            {
                description.text = rewardInfo.Description;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if(popupPanel == null)
            {
                return;
            }

            CurvedUIPanelAnimator.Show(
                popupPanel,
                animatePanelTransitions ? panelOpenDuration : 0f,
                panelOpenCurve,
                panelFadeCurve);

            if(animatePanelTransitions)
            {
                UIFeel.PopIn(iconImage, panelOpenDuration * 0.4f, 0.3f, 0.45f);
            }
        }

        private void Hide()
        {
            if(popupPanel == null)
            {
                gameObject.SetActive(false);
                return;
            }

            CurvedUIPanelAnimator.Hide(
                popupPanel,
                animatePanelTransitions ? panelCloseDuration : 0f,
                panelCloseCurve,
                panelFadeCurve,
                () => gameObject.SetActive(false));
        }
    }
}
