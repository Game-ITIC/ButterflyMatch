using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class DailyCardView : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject glowObject;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Calendar state")]
        [SerializeField] private GameObject claimedMark;
        [SerializeField] private Image claimButtonImage;
        [SerializeField] private TMP_Text claimButtonText;
        [SerializeField] private TMP_Text todayBadgeText;
        [SerializeField] private Sprite inactiveButtonSprite;
        [SerializeField] private Color inactiveButtonTextColor = new(0.42f, 0.18f, 0.72f, 1f);

        [Header("Extra rewards (big day card)")]
        [SerializeField] private GameObject extraMovesGroup;
        [SerializeField] private Image extraMovesImage;
        [SerializeField] private TMP_Text extraMovesText;
        [SerializeField] private GameObject livesGroup;
        [SerializeField] private Image livesImage;
        [SerializeField] private TMP_Text livesText;

        private int _dayNumber;
        private int _rewardValue;
        private Sprite _rewardSprite;
        private Sprite _activeButtonSprite;
        private float _activeButtonPixelsPerUnit = 1f;
        private Color _activeButtonTextColor;
        private bool _buttonStyleCached;

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

        public void Bind(int dayNumber, Sprite rewardSprite, int rewardValue) => Bind(dayNumber, rewardSprite, rewardValue, null);

        /// <summary>amountLabel overrides the plain number (e.g. "x1" for boosters).</summary>
        public void Bind(int dayNumber, Sprite rewardSprite, int rewardValue, string amountLabel)
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
                rewardText.text = string.IsNullOrEmpty(amountLabel) ? rewardValue.ToString() : amountLabel;
            }

            if(dayText != null)
            {
                dayText.text = GameLocalization.L10n.Format("daily_day", dayNumber);
            }
        }

        /// <summary>Extra rewards of the big day: extra moves and unlimited lives. Hidden when zero.</summary>
        public void BindExtras(Sprite movesIcon, int extraMoves, Sprite livesIcon, int unlimitedLivesMinutes)
        {
            if(extraMovesGroup != null)
            {
                extraMovesGroup.SetActive(extraMoves > 0);
            }

            if(extraMovesImage != null && movesIcon != null)
            {
                extraMovesImage.sprite = movesIcon;
            }

            if(extraMovesText != null)
            {
                extraMovesText.text = "x" + extraMoves;
            }

            if(livesGroup != null)
            {
                livesGroup.SetActive(unlimitedLivesMinutes > 0);
            }

            if(livesImage != null && livesIcon != null)
            {
                livesImage.sprite = livesIcon;
            }

            if(livesText != null)
            {
                livesText.text = GameLocalization.L10n.Format("shop_duration_minutes", unlimitedLivesMinutes);
            }
        }

        public void ApplyCalendarState(bool isToday, bool isClaimed)
        {
            ResolveReferences();

            if(claimedMark != null)
            {
                claimedMark.SetActive(isClaimed);
            }

            // Shop-style card: the yellow button claims today, past days read "Claimed", future days "Soon".
            if(claimButtonText != null)
            {
                claimButtonText.text = isToday
                    ? GameLocalization.L10n.Get("daily_claim", "Claim")
                    : isClaimed
                        ? GameLocalization.L10n.Get("daily_claimed", "Claimed")
                        : GameLocalization.L10n.Get("soon", "Soon");
            }

            ApplyButtonStyle(isToday);

            if(todayBadgeText != null)
            {
                todayBadgeText.text = GameLocalization.L10n.Get("daily_today", "Today");
            }

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

            canvasGroup.alpha = isClaimed ? 0.7f : 1f;
            UIFeel.SetBreathing(this, isToday, 0.04f, 1.2f);
        }

        /// <summary>Yellow shop button for today, a light pill with purple text for other days.</summary>
        private void ApplyButtonStyle(bool active)
        {
            if(!_buttonStyleCached)
            {
                _activeButtonSprite = claimButtonImage != null ? claimButtonImage.sprite : null;
                _activeButtonPixelsPerUnit = claimButtonImage != null ? claimButtonImage.pixelsPerUnitMultiplier : 1f;
                _activeButtonTextColor = claimButtonText != null ? claimButtonText.color : Color.white;
                _buttonStyleCached = true;
            }

            if(claimButtonImage != null && inactiveButtonSprite != null)
            {
                claimButtonImage.sprite = active ? _activeButtonSprite : inactiveButtonSprite;
                // The light pill has much bigger slice borders: scale it so both states share the same corner radius.
                claimButtonImage.pixelsPerUnitMultiplier = active ? _activeButtonPixelsPerUnit : InactivePixelsPerUnit();
            }

            if(claimButtonText != null)
            {
                claimButtonText.color = active || inactiveButtonSprite == null ? _activeButtonTextColor : inactiveButtonTextColor;
            }
        }

        private float InactivePixelsPerUnit()
        {
            var activeBorder = _activeButtonSprite != null ? _activeButtonSprite.border.x : 0f;
            var inactiveBorder = inactiveButtonSprite.border.x;
            if(activeBorder <= 0f || inactiveBorder <= 0f)
            {
                return _activeButtonPixelsPerUnit;
            }

            return _activeButtonPixelsPerUnit * inactiveBorder / activeBorder;
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

            if(dayText == null)
            {
                var dayTransform = transform.Find("DayLabel");
                dayText = dayTransform != null ? dayTransform.GetComponent<TMP_Text>() : null;
            }

            if(glowObject == null)
            {
                var glowTransform = transform.Find("GlowBGImage");
                glowObject = glowTransform != null ? glowTransform.gameObject : null;
            }
        }
    }
}
