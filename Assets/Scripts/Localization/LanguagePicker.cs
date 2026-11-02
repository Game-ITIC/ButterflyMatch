using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace GameLocalization
{
    /// <summary>
    /// Language list in Settings. One button per available locale, built from a template child.
    /// Selecting a button sets LocalizationSettings.SelectedLocale; the PlayerPref startup selector
    /// ("selected-locale") restores it on the next launch.
    /// </summary>
    public class LanguagePicker : MonoBehaviour
    {
        [SerializeField] private Button _template;
        [SerializeField] private Transform _content;
        [SerializeField] private Button[] _closeButtons;
        [SerializeField] private Color _selectedColor = new(0.525f, 0.329f, 0.890f);
        [SerializeField] private Color _selectedTextColor = Color.white;
        [SerializeField] private Color _normalColor = new(1f, 0.941f, 0.957f);
        [SerializeField] private Color _normalTextColor = new(0.639f, 0.337f, 0.953f);

        private readonly List<(Locale locale, Button button)> _buttons = new();

        private static readonly Dictionary<string, string> NativeNames = new()
        {
            { "en", "English" },
            { "ru", "Русский" },
            { "es", "Español" },
            { "pt-BR", "Português" },
            { "de", "Deutsch" },
            { "fr", "Français" },
            { "it", "Italiano" },
            { "tr", "Türkçe" },
            { "id", "Bahasa Indonesia" },
            { "ja", "日本語" },
            { "ko", "한국어" },
            { "zh-Hans", "简体中文" },
        };

        public static string NativeName(Locale locale)
        {
            if (locale == null)
                return string.Empty;
            return NativeNames.TryGetValue(locale.Identifier.Code, out var name) ? name : locale.LocaleName;
        }

        private void Awake()
        {
            foreach (var button in _closeButtons ?? new Button[0])
                if (button != null) button.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            Build();
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnDestroy()
        {
            foreach (var button in _closeButtons ?? new Button[0])
                if (button != null) button.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            var opening = !gameObject.activeSelf;
            CurvedUIPanelAnimator.Show(gameObject, 0.28f,
                CurvedUIPanelAnimator.CreateDefaultOpenCurve(), CurvedUIPanelAnimator.CreateDefaultFadeCurve());
            if (opening)
                UIFeel.StaggerChildren(_content != null ? _content : _template != null ? _template.transform.parent : null);
        }

        public void Hide()
        {
            CurvedUIPanelAnimator.Hide(gameObject, 0.18f,
                CurvedUIPanelAnimator.CreateDefaultCloseCurve(), CurvedUIPanelAnimator.CreateDefaultFadeCurve());
        }

        private void Build()
        {
            if (_buttons.Count > 0 || _template == null)
            {
                Refresh();
                return;
            }

            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone)
                init.WaitForCompletion();

            _template.gameObject.SetActive(false);
            var parent = _content != null ? _content : _template.transform.parent;
            foreach (var locale in OrderedLocales())
            {
                var button = Instantiate(_template, parent);
                button.name = $"Lang_{locale.Identifier.Code}";
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = NativeName(locale);

                var captured = locale;
                button.onClick.AddListener(() => Select(captured));
                _buttons.Add((locale, button));
            }

            Refresh();
        }

        // Fixed display order (dictionary order of NativeNames), unknown locales last.
        private static List<Locale> OrderedLocales()
        {
            var order = new List<string>(NativeNames.Keys);
            var locales = new List<Locale>(LocalizationSettings.AvailableLocales.Locales);
            locales.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
            return locales;

            int Rank(Locale l)
            {
                var i = order.IndexOf(l.Identifier.Code);
                return i < 0 ? int.MaxValue : i;
            }
        }

        private void Select(Locale locale)
        {
            if (locale != null && LocalizationSettings.SelectedLocale != locale)
                LocalizationSettings.SelectedLocale = locale;
            else
                Refresh();
        }

        private void OnLocaleChanged(Locale _) => Refresh();

        private void Refresh()
        {
            var selected = LocalizationSettings.SelectedLocale;
            foreach (var (locale, button) in _buttons)
            {
                var isSelected = locale == selected;
                if (button.targetGraphic != null)
                    button.targetGraphic.color = isSelected ? _selectedColor : _normalColor;
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.color = isSelected ? _selectedTextColor : _normalTextColor;
            }
        }
    }
}
