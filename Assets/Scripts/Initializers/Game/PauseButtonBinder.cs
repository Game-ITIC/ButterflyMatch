using System;
using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Initializers
{
    /// <summary>Binds the top bar pause button (it can appear after the scene starts, so binding is retried).</summary>
    internal sealed class PauseButtonBinder : IDisposable
    {
        private const string PausePath = "Canvas/Panel/TopBarPanel/Pause";

        private readonly LevelManager _levelManager;
        private readonly GamePauseView _pauseView;
        private Button _pauseButton;

        public PauseButtonBinder(LevelManager levelManager, GamePauseView pauseView)
        {
            _levelManager = levelManager;
            _pauseView = pauseView;
        }

        public void EnsureBound()
        {
            if (_pauseButton != null)
                return;

            Transform pauseTransform = _levelManager.Level != null ? _levelManager.Level.transform.Find(PausePath) : null;
            GameObject pauseObject = pauseTransform != null ? pauseTransform.gameObject : GameObject.Find("Pause");
            if (pauseObject == null || !pauseObject.TryGetComponent(out Button pauseButton))
                return;

            _pauseButton = pauseButton;
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(PauseGame);
        }

        private void PauseGame()
        {
            if (_levelManager.gameStatus != GameState.Playing)
                return;

            if (SoundBase.Instance != null && SoundBase.Instance.click != null)
                SoundBase.Instance.PlaySound(SoundBase.Instance.click);

            _pauseView.Show();
            _levelManager.gameStatus = GameState.Pause;
        }

        public void Dispose()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(PauseGame);
        }
    }
}
