// PreWinAnimationsState.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JuiceFresh.States;
using UI;

public class PreWinAnimationsState : GameStateBase
{
    private Coroutine animationCoroutine;
    private Transform preCompleteBannerTransform;
    private Transform limitTransform;

    public PreWinAnimationsState(LevelManager levelManager) : base(levelManager)
    {
    }

    public override void EnterState()
    {
        MusicBase.Instance.GetComponent<AudioSource>().Stop();
        levelManager.ResumeBoardAmbientAnimations();
        animationCoroutine = levelManager.StartCoroutine(PreWinAnimationsCor());
    }

    public override void UpdateState()
    {
    }

    public override void ExitState()
    {
        if (animationCoroutine != null)
        {
            levelManager.StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

   private IEnumerator PreWinAnimationsCor()
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.complete[1]);
        GameFeelManager.Instance?.OnPreWin();

        if (preCompleteBannerTransform == null)
            preCompleteBannerTransform = GameObject.Find("Level/Canvas")?.transform.Find("PreCompleteBanner");

        if (preCompleteBannerTransform != null)
        {
            preCompleteBannerTransform.gameObject.SetActive(true);
            UIPreCompleteBannerTweenAnimation bannerAnimation = preCompleteBannerTransform.GetComponent<UIPreCompleteBannerTweenAnimation>();
            if (bannerAnimation != null)
                yield return bannerAnimation.WaitForCompletion();
            else
            {
                yield return new WaitForSeconds(3f);
                preCompleteBannerTransform.gameObject.SetActive(false);
            }
        }

        if (limitTransform == null)
            limitTransform = GameObject.Find("Limit")?.transform;

        Vector3 limitPos = limitTransform != null ? limitTransform.position : Vector3.zero;

        yield return new WaitForSeconds(1);

        PlayMapMusic();

        yield return levelManager.SettleBoardBeforePreWin().ToCoroutine();

        int countFlowers = levelManager.limitType == LIMIT.MOVES ? Mathf.Clamp(levelManager.Limit, 0, 8) : 3;

        for (int i = 1; i <= countFlowers; i++)
        {
            if (levelManager.limitType == LIMIT.MOVES)
                levelManager.Limit--;

            GameObject flowerParticle = levelManager.GetFlowerFromPool();

            if (flowerParticle != null)
            {
                flowerParticle.GetComponent<SpriteRenderer>().sortingLayerName = "UI";
                flowerParticle.GetComponent<Flower>().StartFly(limitPos, true);
            }

            yield return new WaitForSeconds(0.5f);
        }

        levelManager.Limit = 0;

        float flowerWaitDeadline = Time.time + 15f;
        while (levelManager.CheckFlowerStillFly())
        {
            if (Time.time >= flowerWaitDeadline)
            {
                levelManager.ForceCleanupFlowers();
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        yield return ClearRemainingExtraItems();
        levelManager.ResumeBoardAmbientAnimations();

        yield return new WaitForSeconds(1f);

        float dragWaitDeadline = Time.time + 5f;
        while (levelManager.DragBlocked && Time.time < dragWaitDeadline)
            yield return new WaitForSeconds(0.1f);

        if (levelManager.DragBlocked)
            levelManager.DragBlocked = false;

        SaveLevelProgress();

        levelManager.LevelsMap.SetActive(false);
        levelManager.LevelsMap.SetActive(true);

        levelManager.gameStatus = GameState.Win;
    }

    IEnumerator ClearRemainingExtraItems()
    {
        while (levelManager.GetAllExtraItems().Count > 0)
        {
            List<Item> extraItems = new List<Item>(levelManager.GetAllExtraItems());
            levelManager.DragBlocked = true;

            foreach (Item item in extraItems)
                item.DestroyItem(false, "", false, true);

            yield return new WaitForSeconds(0.1f);
            yield return levelManager.ProcessMatchesAndFalling(freezeAnimations: false).ToCoroutine();

            float timeout = Time.time + 10f;
            while (levelManager.DragBlocked && Time.time < timeout)
                yield return new WaitForFixedUpdate();

            if (Time.time >= timeout)
                levelManager.DragBlocked = false;
        }
    }

    private void PlayMapMusic()
    {
        var audioSource = MusicBase.Instance.GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.clip = MusicBase.Instance.music[0];
        Services.Settings.SettingsPreferences.ApplyAudio();
        if (Services.Settings.SettingsPreferences.MusicEnabled)
        {
            audioSource.Play();
        }
    }

    private void SaveLevelProgress()
    {
        int currentLevel = levelManager.currentLevel;
        int stars = levelManager.stars;

        if (PlayerPrefs.GetInt(string.Format("Level.{0:000}.StarsCount", currentLevel), 0) < stars)
            PlayerPrefs.SetInt(string.Format("Level.{0:000}.StarsCount", currentLevel), stars);

        if (LevelManager.Score > PlayerPrefs.GetInt("Score" + currentLevel))
            PlayerPrefs.SetInt("Score" + currentLevel, LevelManager.Score);
    }
}
