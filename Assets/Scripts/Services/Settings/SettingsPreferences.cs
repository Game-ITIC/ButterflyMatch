using UnityEngine;

namespace Services.Settings
{
    public static class SettingsPreferences
    {
        private const string SoundKey = "Sound";
        private const string MusicKey = "Music";
        private const string VibrationKey = "Vibration";
        private const string NotificationsKey = "Notifications";

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                PlayerPrefs.Save();
                InitScript.sound = value;
            }
        }

        public static bool MusicEnabled
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
                PlayerPrefs.Save();
                InitScript.music = value;
            }
        }

        public static bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool NotificationsEnabled
        {
            get => PlayerPrefs.GetInt(NotificationsKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(NotificationsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyNotifications();
            }
        }

        public static void ApplyAudio()
        {
            InitScript.sound = SoundEnabled;
            InitScript.music = MusicEnabled;

            ApplyToSource(ResolveSoundSource(), SoundEnabled, pauseWhenOff: false);
            ApplyToSource(ResolveMusicSource(), MusicEnabled, pauseWhenOff: true);
        }

        public static void ApplyNotifications()
        {
#if UNITY_ANDROID
            Notification.ApplyFromPreferences();
#endif
        }

        public static void TryVibrate()
        {
            if (!VibrationEnabled)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private static AudioSource ResolveSoundSource()
        {
            if (SoundBase.Instance == null)
            {
                return null;
            }

            var source = SoundBase.Instance.GetComponent<AudioSource>();
            return source != null
                ? source
                : SoundBase.Instance.GetComponentInChildren<AudioSource>(true);
        }

        private static AudioSource ResolveMusicSource()
        {
            if (MusicBase.Instance == null)
            {
                return null;
            }

            var source = MusicBase.Instance.GetComponent<AudioSource>();
            return source != null
                ? source
                : MusicBase.Instance.GetComponentInChildren<AudioSource>(true);
        }

        private static void ApplyToSource(AudioSource source, bool enabled, bool pauseWhenOff)
        {
            if (source == null)
            {
                return;
            }

            source.mute = !enabled;
            source.volume = enabled ? 1f : 0f;

            if (!pauseWhenOff)
            {
                return;
            }

            // Pause when off; UnPause when on. Never Play() — states own clip start.
            if (!enabled)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }
            }
            else
            {
                source.UnPause();
            }
        }
    }
}
