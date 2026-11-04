using System.Collections.Generic;
using JuiceFresh;
using UnityEngine;

namespace UI
{
    /// <summary>One board cell in the intro: the cell sprite and its piece, their resting look and when the cell starts.</summary>
    internal readonly struct CellRevealGroup
    {
        public CellRevealGroup(
            Transform squareTransform,
            Vector3 squareTargetScale,
            SpriteRenderer squareRenderer,
            float squareTargetAlpha,
            Transform itemTransform,
            Vector3 itemTargetScale,
            SpriteRenderer itemRenderer,
            float itemTargetAlpha,
            float waveDelay)
        {
            SquareTransform = squareTransform;
            SquareTargetScale = squareTargetScale;
            SquareRenderer = squareRenderer;
            SquareTargetAlpha = squareTargetAlpha;
            ItemTransform = itemTransform;
            ItemTargetScale = itemTargetScale;
            ItemRenderer = itemRenderer;
            ItemTargetAlpha = itemTargetAlpha;
            WaveDelay = waveDelay;
        }

        public Transform SquareTransform { get; }
        public Vector3 SquareTargetScale { get; }
        public SpriteRenderer SquareRenderer { get; }
        public float SquareTargetAlpha { get; }
        public Transform ItemTransform { get; }
        public Vector3 ItemTargetScale { get; }
        public SpriteRenderer ItemRenderer { get; }
        public float ItemTargetAlpha { get; }
        public float WaveDelay { get; }
    }

    /// <summary>
    /// Collects the level's cells for the board intro (resting scale / alpha and the wave delay per cell)
    /// and puts them back to rest.
    /// </summary>
    internal static class BoardRevealGroups
    {
        private const float DefaultItemScale = 0.9f;

        public static void Collect(List<CellRevealGroup> groups, BoardRevealPattern pattern, float waveStagger, float maxRevealSpread)
        {
            groups.Clear();

            LevelManager levelManager = LevelManager.THIS;
            Square[] squares = levelManager?.squaresArray;
            if(squares == null || levelManager == null)
                return;

            float centerCol = (levelManager.maxCols - 1) * 0.5f;
            float centerRow = (levelManager.maxRows - 1) * 0.5f;
            Vector2 gridCenter = new Vector2(centerCol, centerRow);
            float maxDistance = 0f;

            for(int i = 0; i < squares.Length; i++)
            {
                Square square = squares[i];
                if(square == null || square.IsNone())
                    continue;

                float distance = Vector2.Distance(new Vector2(square.col, square.row), gridCenter);
                if(distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            if(maxDistance < 0.001f)
            {
                maxDistance = 1f;
            }

            float spread = maxRevealSpread > 0f ? maxRevealSpread : 1f;

            for(int i = 0; i < squares.Length; i++)
            {
                Square square = squares[i];
                if(square == null || square.IsNone())
                    continue;

                Vector2 cell = new Vector2(square.col, square.row);
                float distance = Vector2.Distance(cell, gridCenter);
                float waveDelay = CalculateRevealDelay(
                    pattern,
                    cell,
                    gridCenter,
                    distance,
                    maxDistance,
                    square.row,
                    square.col,
                    levelManager.maxRows,
                    levelManager.maxCols,
                    waveStagger,
                    spread);
                SpriteRenderer squareRenderer = square.GetComponent<SpriteRenderer>();
                float squareTargetAlpha = squareRenderer != null ? squareRenderer.color.a : 1f;

                Transform itemTransform = null;
                Vector3 itemTargetScale = Vector3.one * DefaultItemScale;
                SpriteRenderer itemRenderer = null;
                float itemTargetAlpha = 1f;

                if(square.item != null)
                {
                    itemTransform = square.item.transform;
                    itemTargetScale = GetItemTargetScale(itemTransform);
                    itemRenderer = square.item.sprRenderer != null
                        ? square.item.sprRenderer
                        : itemTransform.GetComponentInChildren<SpriteRenderer>();
                    itemTargetAlpha = itemRenderer != null ? itemRenderer.color.a : 1f;
                }

                groups.Add(new CellRevealGroup(
                    square.transform,
                    square.transform.localScale,
                    squareRenderer,
                    squareTargetAlpha,
                    itemTransform,
                    itemTargetScale,
                    itemRenderer,
                    itemTargetAlpha,
                    waveDelay));
            }
        }

        public static void Restore(List<CellRevealGroup> groups)
        {
            for(int i = 0; i < groups.Count; i++)
            {
                CellRevealGroup group = groups[i];
                if(group.SquareTransform != null)
                    group.SquareTransform.localScale = group.SquareTargetScale;

                SetAlpha(group.SquareRenderer, group.SquareTargetAlpha);

                if(group.ItemTransform == null)
                    continue;

                group.ItemTransform.localScale = group.ItemTargetScale;
                group.ItemTransform.gameObject.SetActive(true);
                SetAlpha(group.ItemRenderer, group.ItemTargetAlpha);
            }
        }

        public static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if(renderer == null)
                return;

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private static float CalculateRevealDelay(
            BoardRevealPattern pattern,
            Vector2 cell,
            Vector2 gridCenter,
            float distanceFromCenter,
            float maxDistance,
            int row,
            int col,
            int maxRows,
            int maxCols,
            float stagger,
            float spread)
        {
            switch(pattern)
            {
                case BoardRevealPattern.AllAtOnce:
                    return 0f;
                case BoardRevealPattern.EdgeIn:
                    return (maxDistance - distanceFromCenter) * stagger * spread;
                case BoardRevealPattern.TopToBottom:
                    return row * stagger * spread;
                case BoardRevealPattern.LeftToRight:
                    return col * stagger * spread;
                case BoardRevealPattern.Diagonal:
                    return (col + row) * stagger * spread;
                case BoardRevealPattern.RandomPop:
                    return UnityEngine.Random.Range(0f, Mathf.Max(stagger, maxDistance * stagger) * spread);
                case BoardRevealPattern.CenterOut:
                default:
                    return distanceFromCenter * stagger * spread;
            }
        }

        private static Vector3 GetItemTargetScale(Transform itemTransform)
        {
            Vector3 scale = itemTransform.localScale;
            if(scale.sqrMagnitude < 0.01f)
                return Vector3.one * DefaultItemScale;

            return scale;
        }
    }
}
