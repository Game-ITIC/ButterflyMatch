#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Systems;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Utils.DebugTools
{
    /// <summary>
    /// The opened panel: dim background, card with title, close button, status line and the scrolling content
    /// the cheat rows go into.
    /// </summary>
    internal sealed class CheatPanelView
    {
        private const float CardMargin = 20f;

        private readonly TextMeshProUGUI _status;

        public CheatPanelView(CheatUi ui, Transform root, Action close)
        {
            var dim = CheatUi.NewRect("Dim", root);
            CheatUi.Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            Dim = dim.gameObject;

            SafeRoot = CheatUi.NewRect("SafeArea", dim);
            CheatUi.Stretch(SafeRoot);

            var card = CheatUi.NewRect("Card", SafeRoot);
            CheatUi.Stretch(card);
            card.offsetMin = new Vector2(CardMargin, CardMargin);
            card.offsetMax = new Vector2(-CardMargin, -CardMargin);
            ui.AddImage(card, CheatUi.CardColor);

            var title = CheatUi.NewRect("Title", card);
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.offsetMin = new Vector2(28f, -96f);
            title.offsetMax = new Vector2(-120f, -8f);
            var titleText = CheatUi.AddText(title, "Читы для тестирования", 44f, CheatUi.TitleColor);
            titleText.alignment = TextAlignmentOptions.Left;

            var closeRect = CheatUi.NewRect("Close", card);
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-14f, -10f);
            closeRect.sizeDelta = new Vector2(92f, 88f);
            ui.AddButton(closeRect, "X", CheatUi.DangerColor, close);

            var status = CheatUi.NewRect("Status", card);
            status.anchorMin = new Vector2(0f, 1f);
            status.anchorMax = new Vector2(1f, 1f);
            status.pivot = new Vector2(0.5f, 1f);
            status.offsetMin = new Vector2(28f, -240f);
            status.offsetMax = new Vector2(-28f, -100f);
            _status = CheatUi.AddText(status, string.Empty, 32f, Color.white);
            _status.alignment = TextAlignmentOptions.Left;

            Content = BuildScroll(card);
        }

        public GameObject Dim { get; }
        public RectTransform SafeRoot { get; }
        public RectTransform Content { get; }

        public void RefreshStatus(float speed)
        {
            var currency = CheatServices.Find<ICurrencyService>();
            var health = CheatServices.Find<HealthSystem>();
            var gems = currency?.GetCurrency(CurrencyType.Diamond)?.Value ?? 0f;
            var stars = currency?.GetCurrency(CurrencyType.Star)?.Value ?? 0f;
            var lives = health == null
                ? "-"
                : health.CurrentLives.Value + "/" + health.MaxLives + (health.HasUnlimitedLives ? " +безлимит" : string.Empty);
            var level = LevelManager.THIS;
            var state = level != null ? "  |  " + level.gameStatus + ", лимит " + level.Limit : string.Empty;

            _status.text = "Открыт уровень " + CheatServices.OpenLevel + "  |  Жизни " + lives
                           + "\nКристаллы " + Mathf.FloorToInt(gems) + "  |  Звёзды " + Mathf.FloorToInt(stars)
                           + "  |  x" + speed
                           + "\nСцена " + SceneManager.GetActiveScene().name + state
                           + "  |  v" + Application.version;
        }

        private static RectTransform BuildScroll(RectTransform card)
        {
            var viewport = CheatUi.NewRect("Viewport", card);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20f, 20f);
            viewport.offsetMax = new Vector2(-20f, -248f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            var content = CheatUi.NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = card.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }
    }
}
#endif
