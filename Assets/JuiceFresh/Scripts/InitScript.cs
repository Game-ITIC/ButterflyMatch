using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Legacy JuiceFresh bootstrap. It is no longer placed in any scene, but its
/// static fields are still shared state for the legacy code: the life timer (RestLifeTimer / DateOfExit),
/// the sound / music flags and the pending gem purchase. The serialized fields are edited by LevelMakerEditor.
/// The old ads, lives, gems and boosters code that was commented out here was removed.
/// </summary>
public class InitScript : MonoBehaviour
{
    public static InitScript Instance;
    public static int openLevel;

    public static float RestLifeTimer;
    public static string DateOfExit;

    public static int Gems;
    public static int waitedPurchaseGems;
    public static int Stars;

    public static bool sound = false;
    public static bool music = false;

    public float TotalTimeForRestLifeHours = 0;
    public float TotalTimeForRestLifeMin = 15;
    public float TotalTimeForRestLifeSec = 60;
    public int FirstGems = 20;
    public int ShowRateEvery;
    public bool losingLifeEveryGame;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Application.targetFrameRate = 60;
        Instance = this;
        RestLifeTimer = PlayerPrefs.GetFloat("RestLifeTimer");
        DateOfExit = PlayerPrefs.GetString("DateOfExit", "");
        Gems = PlayerPrefs.GetInt("Gems");
        Stars = PlayerPrefs.GetInt("Stars");

        if (PlayerPrefs.GetInt("Lauched") == 0)
            SaveFirstLaunchDefaults();

        DontDestroyOnLoad(this);
    }

    private void SaveFirstLaunchDefaults()
    {
        Gems = FirstGems;
        Stars = 0;
        PlayerPrefs.SetInt("Gems", Gems);
        PlayerPrefs.SetInt("Stars", Stars);
        PlayerPrefs.SetInt("Music", 1);
        PlayerPrefs.SetInt("Sound", 1);
        PlayerPrefs.SetInt("Lauched", 1);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Debug cheat: U gives one star on every map level.
    private void Update()
    {
        if (!Input.GetKeyUp(KeyCode.U))
            return;

        GameObject levels = GameObject.Find("Levels");
        if (levels == null)
            return;

        for (int i = 1; i < levels.transform.childCount; i++)
        {
            Debug.Log(string.Format("Stars count {0} of level {1} saved.", 1, i));
            PlayerPrefs.SetInt(string.Format("Level.{0:000}.StarsCount", i), 1);
        }
    }
#endif

    private void OnApplicationFocus(bool focusStatus)
    {
        if (MusicBase.Instance)
            MusicBase.Instance.GetComponent<AudioSource>().Play();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveLifeTimer(onlyWhenRunning: true);
    }

    private void OnApplicationQuit()
    {
        SaveLifeTimer(onlyWhenRunning: true);
    }

    private void OnEnable()
    {
        LevelsMap.LevelSelected += OnLevelClicked;
    }

    private void OnDisable()
    {
        LevelsMap.LevelSelected -= OnLevelClicked;
        SaveLifeTimer(onlyWhenRunning: false);
    }

    // The life timer keeps counting while the game is closed (DateOfExit).
    private static void SaveLifeTimer(bool onlyWhenRunning)
    {
        if (!onlyWhenRunning || RestLifeTimer > 0)
            PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);

        PlayerPrefs.SetString("DateOfExit", DateTime.Now.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>Legacy map: a level was tapped (ignored while a map popup is open).</summary>
    public void OnLevelClicked(object sender, LevelReachedEventArgs args)
    {
        if (EventSystem.current.IsPointerOverGameObject(-1))
            return;

        Transform canvas = GameSceneUi.GlobalCanvas;
        if (canvas.Find("MenuPlay").gameObject.activeSelf
            || canvas.Find("GemsShop").gameObject.activeSelf
            || canvas.Find("LiveShop").gameObject.activeSelf)
            return;

        PlayerPrefs.SetInt("OpenLevel", args.Number);
        PlayerPrefs.Save();
        LevelManager.THIS.LoadLevel();
        openLevel = args.Number;
    }
}
