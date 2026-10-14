using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class DailyCardView : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject glowObject;
        [SerializeField] private CanvasGroup canvasGroup;

        private int _dayNumber;
        private int _rewardValue;
        private Sprite _rewardSprite;

        public Button ClaimButton => claimButton;
        public int DayNumber => _dayNumber;
        public int RewardValue => _rewardValue;
        public Sprite RewardSprite => _rewardSprite;

        private void Awake()
        {
            ResolveReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        } 
#endif

        public void Bind(int dayNumber, Sprite rewardSprite, int rewardValue)
        {
            ResolveReferences();
            gameObject.SetActive(true);

            _dayNumber = dayNumber;
            _rewardSprite = rewardSprite;
            _rewardValue = rewardValue;

            if(itemImage != null)
            {
                itemImage.sprite = rewardSprite;
                itemImage.enabled = rewardSprite != null;
            }

            if(rewardText != null)
            {
                rewardText.text = rewardValue.ToString();
            }
        }

        public void ApplyCalendarState(bool isToday, bool isClaimed)
        {
            ResolveReferences();

            if(claimButton != null)
            {
                claimButton.enabled = isToday;
                claimButton.interactable = isToday;
            }

            if(glowObject != null)
            {
                glowObject.SetActive(isToday);
            }

            if(canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();

                if(canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            canvasGroup.alpha = isClaimed ? 0.45f : 1f;
        }

        private void ResolveReferences()
        {
            if(claimButton == null)
            {
                claimButton = GetComponent<Button>();
            }

            if(itemImage == null)
            {
                var imageTransform = transform.Find("ItemImage");
                itemImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            }

            if(rewardText == null)
            {
                var textTransform = transform.Find("Text (TMP)");
                rewardText = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            }

            if(glowObject == null)
            {
                var glowTransform = transform.Find("GlowBGImage");
                glowObject = glowTransform != null ? glowTransform.gameObject : null;
            }
        }
    }
}
