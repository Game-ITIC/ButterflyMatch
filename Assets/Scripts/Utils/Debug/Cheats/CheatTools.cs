#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UI;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Utils.DebugTools
{
    /// <summary>Game speed, the FPS label and language switching.</summary>
    internal sealed class CheatTools
    {
        private readonly CheatPanel _panel;
        private readonly CheatFpsCounter _fps;

        public CheatTools(CheatPanel panel, CheatFpsCounter fps)
        {
            _panel = panel;
            _fps = fps;
        }

        public void SetSpeed(float speed)
        {
            _panel.Speed = speed;
            if(Time.timeScale > 0f)
            {
                Time.timeScale = speed;
            }

            _panel.Done("Скорость x" + speed);
        }

        public void ToggleFps()
        {
            _fps.Toggle();
        }

        public void NextLanguage()
        {
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if(locales == null || locales.Count == 0)
            {
                Toast.Show("Локали ещё не загружены");
                return;
            }

            var next = locales[(locales.IndexOf(LocalizationSettings.SelectedLocale) + 1) % locales.Count];
            LocalizationSettings.SelectedLocale = next;
            Toast.Show(next.Identifier.Code);
        }
    }
}
#endif
