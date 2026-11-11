using System.Text;
using UnityEngine;

/// <summary>Lists every "Window" rect (active or not) that sticks out of its root canvas on the current screen.</summary>
public static class OverflowCheck
{
    public static string Run()
    {
        var sb = new StringBuilder("screen " + Screen.width + "x" + Screen.height + "\n");
        var corners = new Vector3[4];
        foreach (var canvas in Object.FindObjectsOfType<Canvas>(true))
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                continue;

            var root = (RectTransform)canvas.transform;
            foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "Window")
                    continue;

                rt.GetWorldCorners(corners);
                Vector2 lo = root.InverseTransformPoint(corners[0]);
                Vector2 hi = root.InverseTransformPoint(corners[2]);
                Rect r = root.rect;
                float over = Mathf.Max(r.xMin - lo.x, lo.y < r.yMin ? r.yMin - lo.y : 0f, hi.x - r.xMax, hi.y - r.yMax);
                string path = rt.parent != null ? rt.parent.name + "/" + rt.name : rt.name;
                sb.AppendLine((over > 1f ? "OVER " + (int)over + " " : "ok ") + canvas.name + " :: " + path
                              + " size " + (int)rt.rect.width + "x" + (int)rt.rect.height + " in " + (int)r.width + "x" + (int)r.height);
            }
        }
        return sb.ToString();
    }
}
