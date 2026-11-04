using System.Collections;
using UnityEngine;

namespace JuiceFresh.Popups
{
    /// <summary>Level complete: shows the earned stars one by one; closing goes on to the next level.</summary>
    public sealed class MenuCompletePopup : PopupBehaviour
    {
        private const int MaxStars = 3;
        private const float StarRevealDelay = 0.5f;

        public MenuCompletePopup(AnimationManager popup) : base(popup) { }

        public override void OnShown()
        {
            for (int i = 1; i <= MaxStars; i++)
                Star(i).SetActive(false);
        }

        public override void OnShowFinished()
        {
            Popup.StartCoroutine(RevealStars());
        }

        public override void OnClosing()
        {
            Level.gameStatus = GameState.Map;
            if (PlayerPrefs.GetInt("OpenLevel") + 1 <= LevelsMap._instance.GetMapLevels().Count)
            {
                PlayerPrefs.SetInt("OpenLevel", Level.currentLevel + 1);
                Level.gameStatus = GameState.PrepareGame;
            }
            else
            {
                ShowCongratulations();
            }
        }

        private IEnumerator RevealStars()
        {
            for (int i = 1; i <= LevelManager.Instance.stars; i++)
            {
                Star(i).SetActive(true);
                SoundBase.Instance.PlaySound(SoundBase.Instance.star[i - 1]);
                yield return new WaitForSeconds(StarRevealDelay);
            }
        }

        private GameObject Star(int index) => Root.Find("Image/Star" + index + "/Star").gameObject;

        // The last level of the map was completed.
        private static void ShowCongratulations()
        {
            Transform canvas = GameSceneUi.GlobalCanvas;
            if (canvas == null)
            {
                UnityEngine.Debug.LogWarning("[MenuComplete] " + GameSceneUi.GlobalCanvasPath + " not found; congratulations not shown.");
                return;
            }

            var congratulations =
                Object.Instantiate(Resources.Load("Prefabs/Congratulations"), Vector2.zero, Quaternion.identity) as GameObject;
            congratulations.transform.SetParent(canvas);
            congratulations.transform.localScale = Vector3.one;
            congratulations.transform.localPosition = Vector3.zero;
        }
    }
}
