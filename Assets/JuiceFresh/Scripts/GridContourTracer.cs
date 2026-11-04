using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Outlines of the active cells of a grid: the closed loops of cell corners around each group of cells (and holes),
/// with straight runs collapsed to their corners. Pure geometry, no Unity objects.
/// </summary>
public static class GridContourTracer
{
    private readonly struct BoundaryEdge
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int End;

        public BoundaryEdge(Vector2Int start, Vector2Int end)
        {
            Start = start;
            End = end;
        }
    }

    /// <summary>Corner loops in grid corner coordinates (col, row); a loop with fewer than 4 corners is dropped.</summary>
    public static List<List<Vector2Int>> Trace(int cols, int rows, Func<int, int, bool> isCellActive)
    {
        bool IsActive(int col, int row) =>
            col >= 0 && col < cols && row >= 0 && row < rows && isCellActive(col, row);

        return TraceContours(BuildBoundaryEdges(cols, rows, IsActive));
    }

    private static List<BoundaryEdge> BuildBoundaryEdges(int cols, int rows, Func<int, int, bool> isActive)
    {
        List<BoundaryEdge> edges = new List<BoundaryEdge>();

        for(int row = 0; row < rows; row++)
        {
            for(int col = 0; col < cols; col++)
            {
                if(!isActive(col, row))
                    continue;

                Vector2Int topLeft = new Vector2Int(col, row);
                Vector2Int topRight = new Vector2Int(col + 1, row);
                Vector2Int bottomRight = new Vector2Int(col + 1, row + 1);
                Vector2Int bottomLeft = new Vector2Int(col, row + 1);

                if(!isActive(col, row - 1))
                    edges.Add(new BoundaryEdge(topLeft, topRight));
                if(!isActive(col + 1, row))
                    edges.Add(new BoundaryEdge(topRight, bottomRight));
                if(!isActive(col, row + 1))
                    edges.Add(new BoundaryEdge(bottomRight, bottomLeft));
                if(!isActive(col - 1, row))
                    edges.Add(new BoundaryEdge(bottomLeft, topLeft));
            }
        }

        return edges;
    }

    private static List<List<Vector2Int>> TraceContours(List<BoundaryEdge> edges)
    {
        List<List<Vector2Int>> contours = new List<List<Vector2Int>>();

        while(edges.Count > 0)
        {
            BoundaryEdge edge = edges[0];
            edges.RemoveAt(0);

            Vector2Int start = edge.Start;
            Vector2Int current = edge.End;
            Vector2Int direction = edge.End - edge.Start;
            List<Vector2Int> contour = new List<Vector2Int> { start };
            int safety = 0;

            while(current != start && safety++ < 10000)
            {
                contour.Add(current);
                int nextIndex = FindNextEdge(edges, current, direction);
                if(nextIndex < 0)
                    break;

                BoundaryEdge next = edges[nextIndex];
                edges.RemoveAt(nextIndex);
                direction = next.End - next.Start;
                current = next.End;
            }

            if(current == start && contour.Count >= 4)
                contours.Add(Simplify(contour));
        }

        return contours;
    }

    // Prefers a right turn, then straight, then left, so touching loops split at shared corners.
    private static int FindNextEdge(List<BoundaryEdge> edges, Vector2Int start, Vector2Int previousDirection)
    {
        int previousDirectionIndex = DirectionIndex(previousDirection);
        int bestIndex = -1;
        int bestRank = int.MaxValue;

        for(int i = 0; i < edges.Count; i++)
        {
            if(edges[i].Start != start)
                continue;

            int directionIndex = DirectionIndex(edges[i].End - edges[i].Start);
            int turn = (directionIndex - previousDirectionIndex + 4) % 4;
            int rank = turn == 1 ? 0 : turn == 0 ? 1 : turn == 3 ? 2 : 3;
            if(rank >= bestRank)
                continue;

            bestRank = rank;
            bestIndex = i;
        }

        return bestIndex;
    }

    private static int DirectionIndex(Vector2Int direction)
    {
        if(direction.x > 0)
            return 0;
        if(direction.y > 0)
            return 1;
        if(direction.x < 0)
            return 2;
        return 3;
    }

    private static List<Vector2Int> Simplify(List<Vector2Int> contour)
    {
        List<Vector2Int> simplified = new List<Vector2Int>();
        int count = contour.Count;

        for(int i = 0; i < count; i++)
        {
            Vector2Int previous = contour[(i - 1 + count) % count];
            Vector2Int current = contour[i];
            Vector2Int next = contour[(i + 1) % count];

            if(current - previous == next - current)
                continue;

            simplified.Add(current);
        }

        return simplified;
    }
}
