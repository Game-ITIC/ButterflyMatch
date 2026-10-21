using System.Collections.Generic;
using UnityEngine;
using ZLinq;

namespace JuiceFresh.States
{
    public class PrepareGameState : GameStateBase
    {
        public PrepareGameState(LevelManager levelManager) : base(levelManager)
        {
        }

        public override void EnterState()
        {
            PlayGameMusic();
            PrepareGame();
        }

        public override void UpdateState()
        {
            // No specific update logic for prepare game state
        }

        public override void ExitState()
        {
            // Nothing to clean up
        }

        private void PlayGameMusic()
        {
            var audioSource = MusicBase.Instance.GetComponent<AudioSource>();
            audioSource.Stop();
            audioSource.loop = true;
            audioSource.clip = MusicBase.Instance.music[1];
            Services.Settings.SettingsPreferences.ApplyAudio();
            if (Services.Settings.SettingsPreferences.MusicEnabled)
            {
                audioSource.Play();
            }
        }

        private void PrepareGame()
        {
            // InitScript.Instance.SpendLife(1);

            levelManager.activatedBoost = null;

            LevelManager.Score = 0;
            levelManager.stars = 0;
            levelManager.moveID = 0;
            levelManager.selectedColor = -1;

            ResetUIElements();

            ResetTargetData();

            levelManager.EnableMap(false);

            levelManager.GameField.transform.position = Vector3.zero;
            levelManager.firstSquarePosition = levelManager.GameField.transform.position;

            levelManager.squaresArray = new Square[levelManager.maxCols * levelManager.maxRows];

            levelManager.LoadLevel();

            levelManager.Level.transform.Find("Canvas/PrePlay").gameObject.SetActive(true);

            ConfigureBoostButtons();

            LevelManager.TriggerOnEnterGame();
        }

        private void ResetUIElements()
        {
            // Reset all UI panels
            SetActiveIfAssigned(levelManager.blocksObject, false);
            SetActiveIfAssigned(levelManager.ingrObject, false);
            SetActiveIfAssigned(levelManager.scoreTargetObject, false);
            SetActiveIfAssigned(levelManager.cageTargetObject, false);
            SetActiveIfAssigned(levelManager.bombTargetObject, false);

        }

        private static void SetActiveIfAssigned(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void ResetTargetData()
        {
            // Reset ingredient targets
            levelManager.ingrTarget = new List<CollectedIngredients>();

            if (levelManager.target != Target.COLLECT)
                levelManager.ingrTarget.Add(new CollectedIngredients());

            // Reset collection items array
            for (int i = 0; i < levelManager.collectItems.Length; i++)
            {
                levelManager.collectItems[i] = CollectItems.None;
            }

            // Reset block targets
            levelManager.TargetBlocks = 0;
            levelManager.TargetCages = 0;
            levelManager.TargetBombs = 0;
        }

        private void ConfigureBoostButtons()
        {
            // Configure boost buttons based on limit type
            // if (levelManager.limitType == LIMIT.MOVES)
            // {
            //     levelManager.InGameBoosts[0].gameObject.SetActive(true);
            //     levelManager.InGameBoosts[1].gameObject.SetActive(false);
            // }
            // else
            // {
            //     levelManager.InGameBoosts[0].gameObject.SetActive(false);
            //     levelManager.InGameBoosts[1].gameObject.SetActive(true);
            // }
            
            var ingameBoosts = levelManager.InGameBoosts;
            var boosterProviders = levelManager.BoostersProvider;

            foreach (var booster in boosterProviders.BoostersModels)
            {
                var value = ingameBoosts.AsValueEnumerable().First(v => v.type == booster.Type);

                value.BoosterModel = booster;
            }
        }
    }
}
