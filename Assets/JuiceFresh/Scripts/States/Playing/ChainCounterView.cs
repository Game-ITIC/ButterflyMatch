using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.States.Playing
{
    /// <summary>
    /// The length of the chain, in a badge on the corner of its last cell. Grey until the chain can be played.
    /// Uses the bomb timer prefab, created once on the first chain.
    /// </summary>
    public sealed class ChainCounterView
    {
        private const int MinPlayableChain = 3;
        private const int SortingOrder = 7;
        private const float BadgeScale = 1.2f;
        private static readonly Vector3 CornerOffset = new Vector3(0.5f, 0.5f, 0f);
        private static readonly Color TooShortColor = new Color(0.36f, 0.34f, 0.44f);
        private static readonly Color PlayableColor = new Color(0.1f, 0.62f, 0.3f);

        private readonly LevelManager _levelManager;

        private GameObject _root;
        private TMP_Text _text;
        private Image _badge;
        private Tween _punch;

        public ChainCounterView(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void Show(Item last, int count)
        {
            if (_root == null && !Create())
                return;

            _root.SetActive(true);
            _root.transform.position = last.square.transform.position + CornerOffset;
            _text.text = count.ToString();
            if (_badge != null)
                _badge.color = count >= MinPlayableChain ? PlayableColor : TooShortColor;

            _punch?.Kill();
            _root.transform.localScale = Vector3.one * BadgeScale;
            _punch = _root.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 4, 0.5f);
        }

        public void Hide()
        {
            if (_root == null)
                return;

            _punch?.Kill();
            _root.SetActive(false);
        }

        private bool Create()
        {
            GameObject prefab = _levelManager.itemPrefab.GetComponent<Item>().timerTextPrefab;
            if (prefab == null)
                return false;

            _root = Object.Instantiate(prefab);
            _root.name = "ChainCounter";
            _root.GetComponent<Canvas>().sortingOrder = SortingOrder;
            _text = _root.GetComponentInChildren<TMP_Text>();
            _badge = _root.GetComponentInChildren<Image>();
            return true;
        }
    }
}
