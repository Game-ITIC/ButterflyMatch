namespace JuiceFresh.Scripts
{
    /// <summary>The border around the board: draws the line perimeter (GridPerimeterRenderer) and follows the game state.</summary>
    public sealed class BoardOutlineService
    {
        private readonly GridPerimeterRenderer _perimeter;

        public BoardOutlineService(GridPerimeterRenderer perimeter)
        {
            _perimeter = perimeter;
        }

        public void Draw()
        {
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
    }
}
