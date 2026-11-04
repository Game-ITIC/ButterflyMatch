using System.Collections.Generic;
using JuiceFresh;
using UnityEngine;

/// <summary>
/// The line around the board: one animated LineRenderer loop per contour of the level's cells.
/// The contours come from <see cref="GridContourTracer"/>, the line look from <see cref="GridOutlineLineStyle"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class GridPerimeterRenderer : MonoBehaviour
{
    [Header("Grid Perimeter Line")]
    [Tooltip("Prefab with LineRenderer (use Assets/Line.prefab).")]
    [SerializeField] private GameObject gridPerimeterLinePrefab;

    [Tooltip("Z offset for the perimeter line (relative to GameField).")]
    [SerializeField] private float gridPerimeterLineZOffset = -0.2f;

    [Tooltip("Time used to draw each grid contour after the cells finish appearing.")]
    [SerializeField, Min(0.05f)] private float gridOutlineDrawDuration = 0.72f;

    [Tooltip("Small delay between separate contour loops.")]
    [SerializeField, Min(0f)] private float gridOutlineLoopDelay = 0.08f;

    private readonly List<GameObject> _lineInstances = new List<GameObject>();
    private readonly List<Material> _lineMaterials = new List<Material>();

    private LevelManager _levelManager;

    public void Initialize(LevelManager levelManager)
    {
        _levelManager = levelManager;
    }

    public static bool ShouldHideForState(GameState state)
    {
        return state == GameState.Win ||
               state == GameState.GameOver ||
               state == GameState.ToMap ||
               state == GameState.Map;
    }

    public void Draw()
    {
        if(_levelManager == null || _levelManager.GameField == null)
            return;

        if(gridPerimeterLinePrefab == null)
        {
            Debug.LogWarning(
                "Grid perimeter line prefab is not assigned. Assign Assets/Line.prefab to 'Grid Perimeter Line Prefab'.",
                this);
            return;
        }

        Clear();

        List<List<Vector2Int>> contours =
            GridContourTracer.Trace(_levelManager.maxCols, _levelManager.maxRows, IsGridCellActive);

        for(int i = 0; i < contours.Count; i++)
            CreateGridContourLine(contours[i], i);
    }

    public void Clear()
    {
        for(int i = 0; i < _lineInstances.Count; i++)
        {
            if(_lineInstances[i] != null)
            {
                _lineInstances[i].SetActive(false);
                Destroy(_lineInstances[i]);
            }
        }

        _lineInstances.Clear();

        for(int i = 0; i < _lineMaterials.Count; i++)
        {
            if(_lineMaterials[i] != null)
                Destroy(_lineMaterials[i]);
        }

        _lineMaterials.Clear();
    }

    public void SetVisible(bool visible)
    {
        for(int i = 0; i < _lineInstances.Count; i++)
        {
            if(_lineInstances[i] != null)
                _lineInstances[i].SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        Clear();
    }

    private bool IsGridCellActive(int col, int row)
    {
        Square square = _levelManager.GetSquare(col, row, true);
        return square != null && square.type != SquareTypes.NONE;
    }

    private void CreateGridContourLine(List<Vector2Int> contour, int contourIndex)
    {
        if(contour.Count < 4)
            return;

        GameObject instance = Instantiate(gridPerimeterLinePrefab, _levelManager.GameField, false);
        instance.name = $"Grid Perimeter Line {contourIndex + 1}";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        LineRenderer lineRenderer = instance.GetComponentInChildren<LineRenderer>();
        if(lineRenderer == null)
        {
            Debug.LogWarning("Grid perimeter line prefab has no LineRenderer component.", this);
            Destroy(instance);
            return;
        }

        Material runtimeMaterial = GridOutlineLineStyle.Apply(lineRenderer, contour.Count);
        if(runtimeMaterial != null)
            _lineMaterials.Add(runtimeMaterial);

        List<Vector3> positions = new List<Vector3>(contour.Count);
        for(int i = 0; i < contour.Count; i++)
            positions.Add(GridCornerToLocalPosition(contour[i]));

        lineRenderer.SetPositions(positions.ToArray());

        GridOutlineRevealAnimation reveal = instance.AddComponent<GridOutlineRevealAnimation>();
        reveal.Play(
            lineRenderer,
            positions,
            gridOutlineDrawDuration,
            contourIndex * gridOutlineLoopDelay);

        _lineInstances.Add(instance);
    }

    private Vector3 GridCornerToLocalPosition(Vector2Int corner)
    {
        float x = _levelManager.firstSquarePosition.x + (corner.x - 0.5f) * _levelManager.squareWidth;
        float y = _levelManager.firstSquarePosition.y - (corner.y - 0.5f) * _levelManager.squareHeight;
        return new Vector3(x, y, gridPerimeterLineZOffset);
    }
}
