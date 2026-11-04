using JuiceFresh.Popups;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared component of the legacy menus and popups (MenuPlay, MenuComplete, MenuFailed, MenuPause, shops, ...):
/// the target of their buttons and of their animation / tween events.
/// The steps every popup shares live here; what one popup does differently lives in its own
/// <see cref="PopupBehaviour"/> (folder GUI/Popups), chosen by the GameObject name in <see cref="PopupBehaviours"/>.
/// Method names are bound in prefabs and animation clips: do not rename them.
/// </summary>
public class AnimationManager : MonoBehaviour
{
    private const string GameScene = "game";

    public bool PlayOnEnable = true;

    private PopupBehaviour _behaviour;
    private string _behaviourName;

    // Set by Again / continue so that closing the failed menu keeps the level going.
    private bool _keepGaming;

    // Re-resolved if the object is renamed (instantiated popups get "(Clone)" names until renamed).
    private PopupBehaviour Behaviour
    {
        get
        {
            string popupName = name;
            if (_behaviour == null || _behaviourName != popupName)
            {
                _behaviour = PopupBehaviours.Create(this, popupName);
                _behaviourName = popupName;
            }

            return _behaviour;
        }
    }

    #region Lifecycle and animation events

    private void OnEnable()
    {
        if (PlayOnEnable)
            SoundBase.Instance.PlaySound(SoundBase.Instance.swish[0]);

        Behaviour.OnShown();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape) && Behaviour.ClosesOnEscape)
            CloseMenu();
    }

    /// <summary>Animation / tween event: the open animation finished.</summary>
    public void OnFinished()
    {
        Behaviour.OnShowFinished();
    }

    /// <summary>Animation event of the failed banner: holds it until the player decides.</summary>
    public void WaitForGiveUp()
    {
        Behaviour.OnWaitForGiveUp();
    }

    public void PlaySoundButton()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
    }

    public void SwishSound()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.swish[1]);
    }

    #endregion

    #region Open / close

    public void CloseMenu()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        Behaviour.OnClosing();

        if (SceneManager.GetActiveScene().name == GameScene && LevelManager.Instance.gameStatus == GameState.Pause)
            LevelManager.Instance.gameStatus = GameState.WaitAfterClose;

        SoundBase.Instance.PlaySound(SoundBase.Instance.swish[1]);
        gameObject.SetActive(false);
    }

    public void CloseAndDestroy()
    {
        Destroy(gameObject);
    }

    public void ShowInfo()
    {
        GameObject.Find("CanvasGlobal").transform.Find("BoostInfo").gameObject.SetActive(true);
    }

    public void BuyGems()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        GameObject.Find("CanvasGlobal").transform.Find("GemsShop").gameObject.SetActive(true);
    }

    #endregion

    #region Level flow buttons

    /// <summary>The popup's main button; what it does depends on the popup.</summary>
    public void Play()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        Behaviour.OnPlay();
    }

    public void PlayTutorial()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        LevelManager.Instance.gameStatus = GameState.Playing;
    }

    public void Next()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        CloseMenu();
    }

    public void Again()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        LevelManager.THIS.gameStatus = GameState.PrepareGame;
        _keepGaming = true;
        CloseMenu();
    }

    public void BackToMap()
    {
        Time.timeScale = 1;
        LevelManager.THIS.gameStatus = GameState.GameOver;
        CloseMenu();
    }

    public void BackToMapWithoutDie()
    {
        Time.timeScale = 1;
        LevelManager.THIS.gameStatus = GameState.ToMap;
        CloseMenu();
        SceneManager.LoadScene("RegionSandbox");
    }

    /// <summary>Continue for diamonds from the failed offer.</summary>
    public void BuyFailed(GameObject button)
    {
        if (LevelContinue.TryPay(LevelManager.THIS))
            GoOnFailed();
    }

    /// <summary>Gives the continue (extra moves or time) and resumes the level.</summary>
    public void GoOnFailed()
    {
        LevelContinue.GrantExtraLimit(LevelManager.THIS);
        _keepGaming = true;
        MusicBase.Instance.GetComponent<AudioSource>().Play();
        CloseMenu();

        LevelManager.THIS.gameStatus = GameState.Playing;
        LevelManager.THIS.RestartTimer();
    }

    /// <summary>Plays the rest of the failed banner animation.</summary>
    public void GiveUp()
    {
        SetFailedBannerSpeed(1);
    }

    #endregion

    #region Shops

    /// <summary>Gems pack button (legacy gems shop).</summary>
    public void Buy(GameObject pack)
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        GemsShopPopup.RememberPackGems(pack);
        CloseMenu();
    }

    /// <summary>Lives were removed from the game; the LiveShop buttons still call these.</summary>
    public void BuyLifeShop() { }

    public void BuyLife(GameObject button) { }

    public void BuyBoost(BoostType boostType, int price, int count, Sprite icon)
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        if (!BoosterPurchase.TryBuy(boostType, price, count))
            return;

        CloseMenu();
        BoosterPurchase.ShowReward(boostType, icon);
    }

    #endregion

    #region Settings

    public void SoundOff(GameObject off)
    {
        AudioToggles.ToggleSound(off);
    }

    public void MusicOff(GameObject off)
    {
        AudioToggles.ToggleMusic(off);
    }

    #endregion

    #region Popup behaviour access

    internal void SetFailedBannerSpeed(float speed)
    {
        GetComponent<Animation>()["bannerFailed"].speed = speed;
    }

    /// <summary>Returns whether the level should keep going and clears the flag.</summary>
    internal bool ConsumeKeepGaming()
    {
        bool keepGaming = _keepGaming;
        _keepGaming = false;
        return keepGaming;
    }

    #endregion
}
