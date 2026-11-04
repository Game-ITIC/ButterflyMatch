using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Sound / music on-off toggles of the settings and pause popups (saved as PlayerPrefs "Sound" / "Music").</summary>
    public static class AudioToggles
    {
        private const string SoundOffPath = "Image/Sound/SoundOff";
        private const string MusicOffPath = "Image/Music/MusicOff";

        /// <summary>Shows the "off" marks for the saved settings.</summary>
        public static void ShowSaved(Transform popup)
        {
            popup.Find(SoundOffPath).gameObject.SetActive(PlayerPrefs.GetInt("Sound") == 0);
            popup.Find(MusicOffPath).gameObject.SetActive(PlayerPrefs.GetInt("Music") == 0);
        }

        /// <summary>Flips the sound; <paramref name="offMark"/> is the button's "off" mark.</summary>
        public static void ToggleSound(GameObject offMark)
        {
            AudioSource source = SoundBase.Instance.GetComponent<AudioSource>();
            InitScript.sound = Toggle(source, offMark);
            Save("Sound", source);
        }

        /// <summary>Flips the music; <paramref name="offMark"/> is the button's "off" mark.</summary>
        public static void ToggleMusic(GameObject offMark)
        {
            AudioSource source = GameObject.Find("Music").GetComponent<AudioSource>();
            InitScript.music = Toggle(source, offMark);
            Save("Music", source);
        }

        // The mark is shown while the channel is off. Returns whether the channel is on now.
        private static bool Toggle(AudioSource source, GameObject offMark)
        {
            bool turnOn = offMark.activeSelf;
            source.volume = turnOn ? 1 : 0;
            offMark.SetActive(!turnOn);
            return turnOn;
        }

        private static void Save(string key, AudioSource source)
        {
            PlayerPrefs.SetInt(key, (int)source.volume);
            PlayerPrefs.Save();
        }
    }
}
