// WinState.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using JuiceFresh.States;

public class WinState : GameStateBase
{
    private GameObject menuCompleteUI;
    private bool _progressSaved;

    public WinState(LevelManager levelManager) : base(levelManager) { }

    public override void EnterState()
    {
        // A win always pays at least 1 star (Docs/GDD.md 4.1).
        levelManager.stars = Mathf.Clamp(levelManager.stars, 1, 3);

        // Increment the pass level counter
        levelManager.passLevelCounter++;

        // Show the complete menu UI
        ShowCompleteUI();
        GameFeelManager.Instance?.OnWin();

        levelManager.GameEventDispatcher.DispatchWin();

        // Trigger the win events
        LevelManager.TriggerOnMenuComplete();
        LevelManager.TriggerOnWin();

        // The life taken at level start is returned on a win.
        levelManager.HealthSystem?.RefundLevelLife();

        // NOTE: the old "rewarded ad every 3rd level" was removed: rewarded ads must be opt-in.
        // Interstitials with caps are handled separately (see Docs/GDD.md 8.2).
    }

    public override void UpdateState()
    {
        // No specific update logic for win state
    }

    public override void ExitState()
    {
        // Hide the complete UI if it's still visible
        if(menuCompleteUI != null && menuCompleteUI.activeSelf)
        {
            menuCompleteUI.SetActive(false);
        }
    }

    private void ShowCompleteUI()
    {
        // Find and show the complete menu UI
        menuCompleteUI = GameSceneUi.FindInGlobalCanvas(GameSceneUi.MenuComplete).gameObject;
        menuCompleteUI.SetActive(true);
        // A win pays only stars (Docs/GDD.md 4.1); the reward line shows them with a star icon.
        LevelManager.THIS.GameCompleteView.CoinsCollectedText.text = levelManager.stars.ToString();

        // Set up the menu content
        SetupCompleteMenu();
    }

    private void SetupCompleteMenu()
    {
        // Get the score display and set it
        // Text scoreText = menuCompleteUI.transform.Find("Score").GetComponent<Text>();
        // if (scoreText != null)
        // {
        //     scoreText.text = LevelManager.Score.ToString();
        // }

        // Set up the stars display
        SetupStarsDisplay();

        // Set up next level button
        // Button nextButton = menuCompleteUI.transform.Find("Next").GetComponent<Button>();
        // if (nextButton != null)
        // {
        //     nextButton.onClick.RemoveAllListeners();
        //     nextButton.onClick.AddListener(() => OnNextLevelClicked());
        //
        //     // Disable next button if this is the last level
        //     // nextButton.interactable = levelManager.currentLevel < InitScript.Instance.GetLastUnlockedLevel();
        // }
        //
        // // Set up retry button
        // Button retryButton = menuCompleteUI.transform.Find("ButtonReplay").GetComponent<Button>();
        // if (retryButton != null)
        // {
        //     retryButton.onClick.RemoveAllListeners();
        //     retryButton.onClick.AddListener(() => OnRetryClicked());
        // }
        //
        // // Set up map button
        // Button mapButton = menuCompleteUI.transform.Find("ButtonMap").GetComponent<Button>();
        // if (mapButton != null)
        // {
        //     mapButton.onClick.RemoveAllListeners();
        //     mapButton.onClick.AddListener(() => OnMapClicked());
        // }
    }

    private void SetupStarsDisplay()
    {
        // Find the stars container and set up the earned stars
        Transform starsTransform = menuCompleteUI.transform.Find("Stars");

        if(starsTransform != null)
        {
            // Set stars visibility based on earned stars
            for (int i = 1; i <= 3; i++)
            {
                Transform star = starsTransform.Find("Star" + i);

                if(star != null)
                {
                    bool earned = levelManager.stars >= i;
                    star.gameObject.SetActive(earned);
                }
            }
        }

        SaveLevelProgressAndStars();
    }

    /// <summary>
    /// Stars and level progress are saved together, at the moment of the win, so killing the app
    /// on the win screen can no longer replay the same level for its stars again.
    /// </summary>
    private void SaveLevelProgressAndStars()
    {
        if(_progressSaved)
        {
            return;
        }

        _progressSaved = true;

        levelManager.CurrencyService.AddCurrency(Systems.CurrencySystem.CurrencyType.Star, levelManager.stars);

        var openLevel = PlayerPrefs.GetInt("OpenLevel", 1);
        PlayerPrefs.SetInt("OpenLevel", Mathf.Max(1, openLevel) + 1);
        PlayerPrefs.Save();
    }

    private void OnNextLevelClicked()
    {
        // Unlock the next level if we're not on the last level
        // if (levelManager.currentLevel < InitScript.Instance.GetLastUnlockedLevel())
        // {
        // Increment the current level
        levelManager.currentLevel++;
        PlayerPrefs.SetInt("OpenLevel", levelManager.currentLevel);

        // Start the next level
        levelManager.gameStatus = GameState.PrepareGame;
        // }
    }

    private void OnRetryClicked()
    {
        // Retry the current level
        levelManager.gameStatus = GameState.PrepareGame;
    }

    private void OnMapClicked()
    {
        // Return to map

        levelManager.currentLevel++;
        PlayerPrefs.SetInt("OpenLevel", levelManager.currentLevel);
        levelManager.gameStatus = GameState.Map;
    }
}