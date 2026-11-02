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

        // Respect the Settings toggle ("Music": 1 on / 0 off) saved from the main menu.
        var source = GetComponent<AudioSource>();
        if (source != null) source.volume = PlayerPrefs.GetInt("Music", 1) == 0 ? 0f : 1f;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
