using TMPro;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Bomb piece: timer label, burning wick, selection light and the per-move countdown.
    /// </summary>
    public class ItemBomb
    {
        private readonly Item _item;
        private TMP_Text _timerText;
        private GameObject _light;

        public ItemBomb(Item item)
        {
            _item = item;
        }

        public void Setup()
        {
            _item.Animation.PlayAmbient(strong: true);

            GameObject t = Object.Instantiate(_item.timerTextPrefab);
            t.transform.SetParent(_item.transform);
            t.transform.localPosition = new Vector3(1.5f, -0.8f, 0);
            t.transform.localScale = Vector3.one;
            _timerText = t.transform.GetChild(0).GetComponent<TMP_Text>();

            if (_item.bombTimer <= 0)
                _item.bombTimer = LevelManager.Instance.bombTimer;

            GameObject wickBurning = Object.Instantiate(_item.wickBurningPrefab);
            wickBurning.transform.SetParent(_item.transform);
            wickBurning.transform.localPosition = new Vector3(0.69f, 0.85f, 0);
            wickBurning.transform.localScale = Vector3.one * 0.2f;
        }

        public void RefreshTimerText()
        {
            if (_item.currentType == ItemsTypes.BOMB && _timerText != null)
                _timerText.text = "" + _item.bombTimer;
        }

        public void Tick()
        {
            // Only live bombs count down (every piece used to, so a piece that turned into a bomb later
            // started from a stale negative timer).
            if (_item.currentType != ItemsTypes.BOMB)
                return;

            _item.bombTimer--;
            if (_item.bombTimer <= 0 && LevelManager.Instance.gameStatus == GameState.Playing)
                Explode();
        }

        private void Explode()
        {
            LevelManager.Instance.gameStatus = GameState.PreFailedBomb;
            _item.AwakeItem();
            _item.sprRenderer.sortingOrder = 4;
            if (_timerText != null)
                _timerText.enabled = false;
            _item.square = null;
            AnimatorHelper.Instance.BombFailed(_item.gameObject, _item.transform.position, Vector3.zero, 20, _item.BombFailedAnimCallBack);
        }

        public void ShowLight()
        {
            if (_light == null)
            {
                _light = Object.Instantiate(_item.bombSelection);
                _light.transform.SetParent(_item.transform);
                _light.name = "BombSelection";
                _light.transform.localPosition = Vector3.zero;
                _light.transform.localScale = Vector3.one * (_item.currentType == ItemsTypes.BOMB ? 3.6f : 2f);
            }

            _light.SetActive(true);
        }

        public void HideLight()
        {
            if (_light != null)
                _light.SetActive(false);
        }
    }
}
