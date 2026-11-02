using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace GameLocalization
{
    /// <summary>"Language" row in Settings: shows the current language and opens the picker.</summary>
    public class LanguageRowView : MonoBehaviour
    {
        [SerializeField] private Button _openButton;
        [SerializeField] private TMP_Text _currentLabel;
        [SerializeField] private LanguagePicker _picker;

        private void Awake()
        {
            if (_openButton != null)
                _openButton.onClick.AddListener(OpenPicker);
        }

        private void OnEnable()
        {
            UpdateLabel(LocalizationSettings.SelectedLocale);
            LocalizationSettings.SelectedLocaleChanged += UpdateLabel;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= UpdateLabel;
        }

        private void OnDestroy()
        {
            if (_openButton != null)
                _openButton.onClick.RemoveListener(OpenPicker);
        }

        private void OpenPicker()
        {
            if (_picker != null)
                _picker.Show();
        }

        private void UpdateLabel(Locale locale)
        {
            if (_currentLabel != null)
                _currentLabel.text = LanguagePicker.NativeName(locale);
        }
    }
}
