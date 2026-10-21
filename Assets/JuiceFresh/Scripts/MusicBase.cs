using UnityEngine;
using System.Collections;

public class MusicBase : MonoBehaviour {

    public static MusicBase Instance;
    public AudioClip[] music;

    ///MusicBase.Instance.audio.PlayOneShot(MusicBase.Instance.music[0]);


    // Use this for initialization
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Persistent objects must be roots; keeping this under the scene camera
        // makes Unity reject DontDestroyOnLoad.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Instance = this;
        Services.Settings.SettingsPreferences.ApplyAudio();

        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && !Services.Settings.SettingsPreferences.MusicEnabled)
        {
            audioSource.Stop();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
