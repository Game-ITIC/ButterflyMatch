using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(-1000)]
public class SoundBase : MonoBehaviour {
    public static SoundBase Instance;
    public AudioClip click;
    public AudioClip[] selecting;
    public AudioClip[] pops;
    public AudioClip appearStipedColorBomb;
    public AudioClip[] complete;
    public AudioClip shovel;


    public AudioClip[] swish;
    public AudioClip[] drop;
    public AudioClip alert;
    public AudioClip timeOut;
    public AudioClip[] star;
    public AudioClip[] gameOver;
    public AudioClip cash;

    public AudioClip[] destroy;
    public AudioClip boostBomb;
    public AudioClip boostColorReplace;
    public AudioClip explosion;
    public AudioClip getStarIngr;
    public AudioClip strippedExplosion;
    public AudioClip block_destroy;
    public AudioClip wrongMatch;
    public AudioClip noMatch;
    public AudioClip appearPackage;
    public AudioClip destroyPackage;
    public AudioClip colorBombExpl;
    public AudioClip iceCrack;

    AudioSource audioSource;

    List<AudioClip> clipsPlaying = new List<AudioClip>();

    //SoundBase.Instance.GetComponent<AudioSource>().PlayOneShot(clip);
    //SoundBase.Instance.PlaySound(SoundBase.Instance.timeOut);

    // Use this for initialization
    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad only accepts root GameObjects. Audio does not need
        // to live under the scene camera, so detach instances placed as children.
        transform.SetParent(null);
        audioSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
        Instance = this;
        Services.Settings.SettingsPreferences.ApplyAudio();
    }

    public void PlaySound(AudioClip clip) {
        if (clip == null || audioSource == null || !InitScript.sound)
            return;

        audioSource.PlayOneShot(clip);
    }

    public void PlaySoundsRandom(AudioClip[] clip) {
        SoundBase.Instance.PlaySound(clip[Random.Range(0, clip.Length)]);
    }

    public void PlayLimitSound(AudioClip clip) {
        if (clipsPlaying.IndexOf(clip) < 0) {
            clipsPlaying.Add(clip);
            PlaySound(clip);
            StartCoroutine(WaitForCompleteSound(clip));
        }
    }

    IEnumerator WaitForCompleteSound(AudioClip clip) {
        yield return new WaitForSeconds(0.2f);
        clipsPlaying.Remove(clip);
    }


    // Update is called once per frame
    void Update() {

    }
}
