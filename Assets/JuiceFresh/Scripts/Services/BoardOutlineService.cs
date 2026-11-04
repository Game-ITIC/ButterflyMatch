using UnityEngine;

namespace JuiceFresh.Scripts
{
    /// <summary>
    /// The border around the board. The line perimeter (GridPerimeterRenderer) replaced the old per-cell
    /// sprite outline (GridOutlineGenerator), so drawing it also removes the old one.
    /// </summary>
    public sealed class BoardOutlineService
    {
        private const string SpriteOutlineName = "outline";

        private readonly Transform _gameField;
        private readonly GridOutlineGenerator _spriteOutline;
        private readonly GridPerimeterRenderer _perimeter;

        public BoardOutlineService(Transform gameField, GridOutlineGenerator spriteOutline, GridPerimeterRenderer perimeter)
        {
            _gameField = gameField;
            _spriteOutline = spriteOutline;
            _perimeter = perimeter;
        }

        public void Draw()
        {
            RemoveSpriteOutline();
            _perimeter?.Draw();
        }

        public void Clear()
        {
            _perimeter?.Clear();
        }

        /// <summary>Pause hides the border, Playing shows it again, leaving the level removes it.</summary>
        public void ApplyGameState(GameState state)
        {
            if (_perimeter == null)
                return;

            if (GridPerimeterRenderer.ShouldHideForState(state))
                _perimeter.Clear();
            else if (state == GameState.Pause)
                _perimeter.SetVisible(false);
            else if (state == GameState.Playing)
                _perimeter.SetVisible(true);
        }

        private void RemoveSpriteOutline()
        {
            if (_spriteOutline != null)
                _spriteOutline.enabled = false;

            if (_gameField == null)
                return;

            // GridOutlineGenerator creates "outline" children under the squares.
            for (int i = 0; i < _gameField.childCount; i++)
            {
                Transform square = _gameField.GetChild(i);
                for (int j = square.childCount - 1; j >= 0; j--)
                {
                    Transform child = square.GetChild(j);
                    if (child.name == SpriteOutlineName)
                        Object.Destroy(child.gameObject);
                }
            }
        }
    }
}
