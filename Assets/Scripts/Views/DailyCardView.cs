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

        public void ApplyState(int currentDay, bool timeExpired)
        {
            var claimed = _dayNumber - 1 < currentDay;
            var isCurrent = _dayNumber - 1 == currentDay;
            // Claim is only valid for the current day after the Gley timer has expired.
            var available = isCurrent && timeExpired;

            if(claimButton != null)
            {
                // Hide + lock when not claimable: ColorTint alone still looks like an active Get/Claim CTA.
                claimButton.gameObject.SetActive(available);
                claimButton.interactable = available;
            }

            if(glowObject != null)
            {
                // Glow should only highlight a day that can actually be claimed now.
                glowObject.SetActive(available);
            }

            if(canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();

                if(canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            canvasGroup.alpha = claimed ? 0.45f : 1f;
        }

        public bool CanClaim(int currentDay, bool timeExpired)
        {
            return _dayNumber - 1 == currentDay && timeExpired;
        }

        private void ResolveReferences()
        {
            if(claimButton == null)
            {
                var claimTransform = transform.Find("GetButton");
                claimButton = claimTransform != null ? claimTransform.GetComponent<Button>() : null;
            }

            if(itemImage == null)
            {
                var imageTransform = transform.Find("ItemImage");
                itemImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            }

            if(rewardText == null && claimButton != null)
            {
                rewardText = claimButton.GetComponentInChildren<TMP_Text>(true);
            }

            if(glowObject == null)
            {
                var glowTransform = transform.Find("GlowBGImage");
                glowObject = glowTransform != null ? glowTransform.gameObject : null;
            }
        }
    }
}
