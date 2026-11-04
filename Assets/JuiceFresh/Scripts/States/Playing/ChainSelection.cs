using JuiceFresh.Scripts;
using UnityEngine;

namespace JuiceFresh.States.Playing
{
    /// <summary>
    /// The chain the player drags over the board: adding pieces, stepping back and clearing.
    /// The chain itself lives in <see cref="LevelManager.destroyAnyway"/> because the board cascade reads it.
    /// </summary>
    public sealed class ChainSelection
    {
        // Two cells apart (or more) is not a neighbour, diagonals are ~1.41.
        private const float MaxStepDistance = 2f;

        // Every N-th piece of a chain sends a flower (cages postpone it by one piece each).
        private const int ExtraItemEvery = 6;

        private readonly LevelManager _levelManager;

        private int _selectedColor = -1;
        private float _offset;

        public ChainSelection(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public int Count => _levelManager.destroyAnyway.Count;

        /// <summary>A new touch starts: any colour may begin the next chain.</summary>
        public void ResetGesture()
        {
            _selectedColor = -1;
            _offset = 0f;
        }

        /// <summary>The colour check of a drag: only pieces of the chain's colour can join it.</summary>
        public bool AcceptsColor(int color) => _selectedColor == -1 || _selectedColor == color;

        /// <summary>Adds the piece to the chain, or steps back when it is the previous piece of the chain.</summary>
        public void Select(Item item)
        {
            if (_levelManager.extraCageAddItem < 0)
                _levelManager.extraCageAddItem = 0;

            _selectedColor = item.color;

            if (_levelManager.DragBlocked || _levelManager.gameStatus != GameState.Playing)
                return;

            var chain = _levelManager.destroyAnyway;
            if (chain.Count > 1)
                _offset = CellDistance(chain[chain.Count - 1], item);

            int index = chain.IndexOf(item);
            if (index < 0 && _offset < MaxStepDistance)
                Add(item);
            else if (index > -1)
                StepBack(item);
        }

        /// <summary>Puts every piece of the chain back to rest and empties it.</summary>
        public void Clear()
        {
            foreach (Item item in _levelManager.destroyAnyway)
            {
                if (item != null)
                {
                    item.SleepItem();
                    item.square.SetActiveCage(false);
                }
            }

            _levelManager.destroyAnyway.Clear();
            _levelManager.gatheredTypes.Clear();
        }

        public Vector3 CenterOfChain()
        {
            var chain = _levelManager.destroyAnyway;
            Vector3 center = Vector3.zero;
            foreach (Item item in chain)
                center += item.transform.position;

            return chain.Count > 0 ? center / chain.Count : center;
        }

        private void Add(Item item)
        {
            var chain = _levelManager.destroyAnyway;
            if (chain.Count > 0)
            {
                _offset = CellDistance(chain[chain.Count - 1], item);
                if (_offset >= MaxStepDistance)
                {
                    _offset = 0;
                    return;
                }
            }

            chain.Add(item);

            int selectingSoundNum = Mathf.Clamp(chain.Count - 1, 0, 9);
            SoundBase.Instance.PlaySound(SoundBase.Instance.selecting[selectingSoundNum]);
            GameFeelManager.Instance?.OnItemSelected(item.transform, chain.Count);

            if (IsExtraItemStep(chain.Count))
            {
                if (item.square.cageHP <= 0)
                    item.SetLight();
                else
                    _levelManager.extraCageAddItem += 1;
            }

            if (item.IsExtraItem())
                _levelManager.gatheredTypes.Add(item.currentType);

            HighlightManager.SelectItem(item);
            item.square.SetActiveCage(true);
            item.AwakeItem();
        }

        private void StepBack(Item item)
        {
            var chain = _levelManager.destroyAnyway;

            if (IsExtraItemStep(chain.Count) && item.square.cageHP > 0)
                _levelManager.extraCageAddItem -= 1;

            // Only the last piece can be taken back, by moving onto the piece before it.
            if (chain.Count <= 1 || chain[chain.Count - 2] != item)
                return;

            Item lastItem = chain[chain.Count - 1];

            if (lastItem.IsExtraItem() && _levelManager.gatheredTypes.Count > 0)
                _levelManager.gatheredTypes.RemoveAt(_levelManager.gatheredTypes.Count - 1);

            lastItem.SleepItem();
            lastItem.square.SetActiveCage(false);
            HighlightManager.DeselectItem(lastItem, item);

            chain.Remove(lastItem);
            GameFeelManager.Instance?.OnItemDeselected(lastItem.transform, item.transform, chain.Count);
        }

        private bool IsExtraItemStep(int chainCount) =>
            chainCount % (ExtraItemEvery + _levelManager.extraCageAddItem) == 0;

        private static float CellDistance(Item a, Item b) =>
            Vector2.Distance(new Vector2(a.square.col, a.square.row), new Vector2(b.square.col, b.square.row));
    }
}
