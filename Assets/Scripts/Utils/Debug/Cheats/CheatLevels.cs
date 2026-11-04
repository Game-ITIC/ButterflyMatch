#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using Systems;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.DebugTools
{
    /// <summary>Level jumps (with the level number field), scene changes and current-level win / lose / limit cheats.</summary>
    internal sealed class CheatLevels
    {
        private readonly CheatPanel _panel;
        private TMP_InputField _levelInput;

        public CheatLevels(CheatPanel panel)
        {
            _panel = panel;
        }

        public int InputLevel
        {
            get => int.TryParse(_levelInput.text, out var value) ? Mathf.Max(1, value) : CheatServices.OpenLevel;
            set => _levelInput.SetTextWithoutNotify(Mathf.Max(1, value).ToString());
        }

        /// <summary>The field shows the open level each time the panel opens.</summary>
        public void ResetInput()
        {
            _levelInput.SetTextWithoutNotify(CheatServices.OpenLevel.ToString());
        }

        public void BuildLevelRow(CheatUi ui, RectTransform content)
        {
            var row = CheatUi.NewRowRect(content);
            ui.AddRowButton(row, "-10", CheatUi.ButtonColor, () => InputLevel -= 10);
            ui.AddRowButton(row, "-1", CheatUi.ButtonColor, () => InputLevel -= 1);

            var field = CheatUi.NewRect("Level Input", row);
            field.gameObject.SetActive(false); // TMP_InputField must get its references before its first OnEnable.
            field.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.5f;
            var background = ui.AddImage(field, CheatUi.FieldColor);

            var area = CheatUi.NewRect("Text Area", field);
            CheatUi.Stretch(area);
            area.gameObject.AddComponent<RectMask2D>();

            var textRect = CheatUi.NewRect("Text", area);
            CheatUi.Stretch(textRect);
            var text = CheatUi.AddText(textRect, string.Empty, 44f, Color.white);

            _levelInput = field.gameObject.AddComponent<TMP_InputField>();
            _levelInput.targetGraphic = background;
            _levelInput.textViewport = area;
            _levelInput.textComponent = text;
            _levelInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _levelInput.characterLimit = 5;
            _levelInput.pointSize = 44f;
            field.gameObject.SetActive(true);

            ui.AddRowButton(row, "+1", CheatUi.ButtonColor, () => InputLevel += 1);
            ui.AddRowButton(row, "+10", CheatUi.ButtonColor, () => InputLevel += 10);
        }

        public void GoToLevel(int level)
        {
            var loader = CheatServices.NeedIdleLoader();
            if(loader == null)
            {
                return;
            }

            PlayerPrefs.SetInt("OpenLevel", Mathf.Max(1, level));
            PlayerPrefs.Save();
            var health = CheatServices.Find<HealthSystem>();
            if(health != null && !health.CanPlay)
            {
                health.AddLives(1);
            }

            Time.timeScale = _panel.Speed;
            _panel.Close();
            loader.LoadGameAsync().Forget();
        }

        public void MakeCurrentAndGoToMenu()
        {
            PlayerPrefs.SetInt("OpenLevel", InputLevel);
            PlayerPrefs.Save();
            GoToMenu("Текущий уровень " + InputLevel);
        }

        public void GoToMenu(string message = null)
        {
            var loader = CheatServices.NeedIdleLoader();
            if(loader == null)
            {
                return;
            }

            Time.timeScale = _panel.Speed;
            _panel.Close();
            loader.LoadMenuAsync().Forget();
            if(message != null)
            {
                Toast.Show(message);
            }
        }

        public void WinLevel(int stars)
        {
            var level = CheatServices.NeedPlayingLevel();
            if(level == null)
            {
                return;
            }

            if(stars >= 3)
            {
                LevelManager.Score = Mathf.Max(LevelManager.Score, level.star3);
            }
            else
            {
                // Stars only ever go up with the score, and leftover moves turn into score on the win screen.
                LevelManager.Score = level.star1;
                level.Limit = 0;
            }

            level.stars = stars;
            _panel.Close();
            level.gameStatus = GameState.PreWinAnimations;
        }

        public void LoseLevel()
        {
            var level = CheatServices.NeedPlayingLevel();
            if(level != null)
            {
                level.Limit = 0;
                _panel.Close();
                level.gameStatus = GameState.GameOver;
            }
        }

        public void AddLimit(int amount)
        {
            var level = CheatServices.NeedPlayingLevel();
            if(level != null)
            {
                level.Limit += amount;
                _panel.Done("Лимит уровня +" + amount);
            }
        }
    }
}
#endif
